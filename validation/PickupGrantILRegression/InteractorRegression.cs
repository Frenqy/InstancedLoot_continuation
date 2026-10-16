using InstancedLoot.Hooks;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.Utils;

public static class InteractorRegression
{
    public delegate bool Filter(UnityEngine.GameObject target, out RoR2.IInteractable result, RoR2.Interactor actor, bool sphere);
    private sealed class Candidate : RoR2.IInteractable { }
    internal static void Run()
    {
        string generator = Environment.GetEnvironmentVariable("MONOMOD_DMD_TYPE");
        Environment.SetEnvironmentVariable("MONOMOD_DMD_TYPE", "cecil");
        try
        {
            using var method = Synthetic();
            var expectedTarget = new UnityEngine.GameObject();
            var expectedActor = new RoR2.Interactor();
            var candidate = new Candidate();
            bool expectedSphere = false;
            int calls = 0;
            Filter filter = (UnityEngine.GameObject target, out RoR2.IInteractable result, RoR2.Interactor actor, bool sphere) =>
            {
                if (!ReferenceEquals(target, expectedTarget) || !ReferenceEquals(actor, expectedActor) || sphere != expectedSphere)
                    throw new Exception("Candidate emitter passed wrong target, interactor, or search mode.");
                calls++;
                result = candidate;
                return true;
            };
            using var context = new ILContext(method.Definition) { ReferenceBag = new RuntimeILReferenceBag() };
            context.Invoke(il => InteractorCandidateIL.Apply(il, filter));
            var invoke = (Func<RoR2.Interactor, UnityEngine.GameObject, bool, RoR2.IInteractable>)DMDCecilGenerator.Generate(method)
                .CreateDelegate(typeof(Func<RoR2.Interactor, UnityEngine.GameObject, bool, RoR2.IInteractable>));
            foreach (bool sphere in new[] { false, true })
            {
                expectedSphere = sphere;
                if (!ReferenceEquals(invoke(expectedActor, expectedTarget, sphere), candidate))
                    throw new Exception("Candidate emitter lost the out interface result.");
            }
            if (calls != 2) throw new Exception("Candidate callback was not called exactly once per search path.");
            using var negative = Synthetic();
            var site = negative.Definition.Body.Instructions.First(i => i.Operand is GenericInstanceMethod);
            ((GenericInstanceMethod)site.Operand).ElementMethod.Name = "ChangedTryGetComponent";
            using var invalid = new ILContext(negative.Definition);
            try { invalid.Invoke(il => InteractorCandidateIL.Apply(il, filter)); throw new Exception("Missing candidate site silently accepted."); }
            catch (InvalidOperationException error) when (error.Message.Contains("Expected raycast and overlap")) { }
            Console.WriteLine("Candidate emitter: executed raycast and overlap CLR paths with captured callback/out-result and incoming branches; rejected missing site.");
        }
        finally { Environment.SetEnvironmentVariable("MONOMOD_DMD_TYPE", generator); }
    }
    internal static void Verify(MethodDefinition method)
    {
        var originalSites = method.Body.Instructions.Where(i => i.Operand is GenericInstanceMethod called && called.Name == "TryGetComponent" &&
            called.GenericArguments[0].FullName == "RoR2.IInteractable").ToArray();
        if (originalSites.Length != 2) throw new Exception("Installed interactor must have both candidate acquisition paths.");
        var edges = method.Body.Instructions.SelectMany(i => CommandCubeRegression.Targets(i).Select(target => (i, target))).ToArray();
        using (var context = new ILContext(method)) context.Invoke(il => InteractorCandidateIL.Apply<Filter>(il, Observe));
        if (method.Body.Instructions.Any(originalSites.Contains)) throw new Exception("Native single-interface site remains unfiltered.");
        foreach (var edge in edges)
            if (!originalSites.Contains(edge.target) && !CommandCubeRegression.Targets(edge.i).Contains(edge.target))
                throw new Exception("Candidate rewrite changed an unrelated incoming branch.");
        var callbacks = method.Body.Instructions.Where(i => i.Operand is MethodReference called && called.Name == nameof(Observe)).ToArray();
        if (callbacks.Length != 2) throw new Exception("Expected two candidate callbacks in the installed method.");
        for (int i = 0; i < callbacks.Length; i++)
        {
            if (callbacks[i].Operand is not MethodReference called || called.Parameters.Count != 4 ||
                called.Parameters[1].ParameterType.FullName != "RoR2.IInteractable&" ||
                callbacks[i].Previous.OpCode != (i == 0 ? OpCodes.Ldc_I4_0 : OpCodes.Ldc_I4_1) ||
                callbacks[i].Previous.Previous.OpCode != OpCodes.Ldarg_0)
                throw new Exception("Installed candidate callback argument stack changed.");
        }
        Console.WriteLine("Installed candidate production emitter transformed both raycast/overlap sites and preserved unrelated branch targets (static inspection, no Unity execution).");
    }
    private static bool Observe(UnityEngine.GameObject target, out RoR2.IInteractable result, RoR2.Interactor actor, bool sphere) { result = null; return false; }
    private static DynamicMethodDefinition Synthetic()
    {
        var method = new DynamicMethodDefinition("CandidatePaths", typeof(RoR2.IInteractable), new[] { typeof(RoR2.Interactor), typeof(UnityEngine.GameObject), typeof(bool) });
        var local = new VariableDefinition(method.Module.ImportReference(typeof(RoR2.IInteractable)));
        method.Definition.Body.Variables.Add(local);
        var il = method.Definition.Body.GetILProcessor();
        var overlap = Instruction.Create(OpCodes.Nop);
        il.Emit(OpCodes.Ldarg_2); il.Emit(OpCodes.Brtrue, overlap);
        void Site()
        {
            var get = typeof(UnityEngine.GameObject).GetMethod("TryGetComponent").MakeGenericMethod(typeof(RoR2.IInteractable));
            var call = Instruction.Create(OpCodes.Callvirt, method.Module.ImportReference(get));
            il.Emit(OpCodes.Ldarg_1); il.Emit(OpCodes.Ldloca, local); il.Emit(OpCodes.Br, call); il.Append(call);
            il.Emit(OpCodes.Pop); il.Emit(OpCodes.Ldloc, local); il.Emit(OpCodes.Ret);
        }
        Site(); il.Append(overlap); Site(); return method;
    }
}
namespace RoR2 { public interface IInteractable { } public sealed class Interactor { } }
