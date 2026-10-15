using InstancedLoot.Hooks;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.Utils;

internal static class CommandCubeRegression
{
    private static UnityEngine.GameObject spawned;
    private static UnityEngine.GameObject observed;
    private static int observations;

    internal static void Run()
    {
        string previousGenerator = Environment.GetEnvironmentVariable("MONOMOD_DMD_TYPE");
        try
        {
            Environment.SetEnvironmentVariable("MONOMOD_DMD_TYPE", "cecil");
            RunSynthetic();
        }
        finally
        {
            Environment.SetEnvironmentVariable("MONOMOD_DMD_TYPE", previousGenerator);
        }
    }

    private static void RunSynthetic()
    {
        foreach (bool useSwitch in new[] { false, true })
        {
            using var original = Synthetic(useSwitch);
            // Reproduce the old production injector through ILContext.Invoke, including label conversion.
            using (var context = new ILContext(original.Definition))
                context.Invoke(il =>
                {
                    var cursor = new ILCursor(il);
                    cursor.Goto(SpawnSite(il.Method), MoveType.Before);
                    cursor.Emit(OpCodes.Dup);
                    cursor.Index++;
                    cursor.EmitDelegate<Action<UnityEngine.GameObject>>(Observe);
                });
            try
            {
                CommandCubeStack.Verify(original.Definition);
                throw new Exception("Old command cube injector unexpectedly passed control-flow stack validation.");
            }
            catch (InvalidOperationException error) when (error.Message.Contains("stack")) { }
            try
            {
                var invalid = (Action<UnityEngine.GameObject, int>)DMDCecilGenerator.Generate(original)
                    .CreateDelegate(typeof(Action<UnityEngine.GameObject, int>));
                invalid(new UnityEngine.GameObject(), 0);
                throw new Exception("CLR unexpectedly executed the old invalid command cube method.");
            }
            catch (InvalidProgramException) { }

            using var structural = Synthetic(useSwitch);
            Verify(structural.Definition);
            using var corrected = Synthetic(useSwitch);
            var seen = new List<UnityEngine.GameObject>();
            Action<UnityEngine.GameObject> observe = cube => { Observe(cube); seen.Add(cube); };
            using var capturedContext = new ILContext(corrected.Definition) { ReferenceBag = new RuntimeILReferenceBag() };
            capturedContext.Invoke(il => CommandCubeIL.Apply(il, observe));
            // The Cecil generator works on current .NET; the old DynamicMethod backend
            // in the game's MonoMod.Utils release assumes runtime internals absent in .NET 8.
            var invoke = (Action<UnityEngine.GameObject, int>)DMDCecilGenerator.Generate(corrected)
                .CreateDelegate(typeof(Action<UnityEngine.GameObject, int>));
            foreach (int branch in new[] { 0, 1, 2 })
            {
                spawned = observed = null;
                observations = 0;
                var cube = new UnityEngine.GameObject();
                invoke(cube, branch);
                if (!ReferenceEquals(spawned, cube) || !ReferenceEquals(observed, cube) || observations != 1 ||
                    !ReferenceEquals(seen[^1], cube) || seen.Count != branch + 1)
                    throw new Exception("Command cube branch did not spawn and observe exactly the original GameObject.");
            }
        }

        foreach (string change in new[] { "missing", "extra parameter", "wrong parameter", "return value", "instance", "multiple" })
        {
            using var synthetic = Synthetic(false);
            var site = SpawnSite(synthetic.Definition);
            var called = (MethodReference)site.Operand;
            // Copy the reference so these negative cases cannot mutate an imported reflection reference.
            var changed = new MethodReference(called.Name, called.ReturnType, called.DeclaringType);
            changed.Parameters.Add(new ParameterDefinition(called.Parameters[0].ParameterType));
            site.Operand = changed;
            if (change == "missing") changed.Name = "DifferentSpawn";
            else if (change == "extra parameter") changed.Parameters.Add(new ParameterDefinition(synthetic.Module.TypeSystem.Int32));
            else if (change == "wrong parameter") changed.Parameters[0].ParameterType = synthetic.Module.TypeSystem.Int32;
            else if (change == "return value") changed.ReturnType = synthetic.Module.TypeSystem.Int32;
            else if (change == "instance") changed.HasThis = true;
            else synthetic.Definition.Body.GetILProcessor().InsertBefore(site, Instruction.Create(OpCodes.Call, changed));
            using var context = new ILContext(synthetic.Definition);
            try
            {
                context.Invoke(il => CommandCubeIL.Apply<Action<UnityEngine.GameObject>>(il, Observe));
                throw new Exception($"Command cube emitter silently accepted {change}.");
            }
            catch (InvalidOperationException error) when (error.Message.Contains("Spawn(GameObject) pattern not found")) { }
        }
        Console.WriteLine("Command cube: CLR and stack verifier rejected the old injector on both branch shapes; production emitter executed 6 captured-callback conditional/switch/unconditional/fallthrough paths and rejected 6 changed signatures.");
    }

    internal static void VerifyInstalled(MethodDefinition method)
    {
        // Validate the real vanilla method first, then demonstrate its old injector fails on a real branch.
        CommandCubeStack.Verify(method);
        using var originalAssembly = AssemblyDefinition.ReadAssembly(method.Module.FileName);
        var original = originalAssembly.MainModule.GetType(method.DeclaringType.FullName).Methods.Single(candidate => candidate.Name == method.Name);
        using (var context = new ILContext(original))
            context.Invoke(il =>
            {
                var cursor = new ILCursor(il);
                cursor.Goto(SpawnSite(il.Method), MoveType.Before);
                cursor.Emit(OpCodes.Dup);
                cursor.Index++;
                cursor.EmitDelegate<Action<UnityEngine.GameObject>>(Observe);
            });
        try
        {
            CommandCubeStack.Verify(original);
            throw new Exception("Old injector unexpectedly passed on the installed method.");
        }
        catch (InvalidOperationException error) when (error.Message.Contains("stack"))
        {
            Console.WriteLine($"Installed command cube old-injector negative control: {error.Message}");
        }
        Verify(method);
    }

    private static void Verify(MethodDefinition method)
    {
        var spawn = SpawnSite(method);
        var edges = method.Body.Instructions.SelectMany(i => Targets(i).Select(target => (Source: i, Target: target))).ToArray();
        var originalInstructions = method.Body.Instructions.ToArray();
        // This installed method has no exception regions; fail explicitly if that assumption changes.
        if (method.Body.HasExceptionHandlers) throw new Exception("Command cube exception regions require a new boundary review.");
        using (var context = new ILContext(method))
            context.Invoke(il => CommandCubeIL.Apply<Action<UnityEngine.GameObject>>(il, Observe));
        var duplicate = spawn.Previous;
        var callbackSite = method.Body.Instructions.SkipWhile(i => !ReferenceEquals(i, spawn)).Last(i =>
            i.OpCode == OpCodes.Call || i.OpCode == OpCodes.Callvirt);
        if (duplicate.OpCode != OpCodes.Dup || !ReferenceEquals(spawn.Next, callbackSite) ||
            callbackSite.Operand is not MethodReference callback || callback.Parameters.Count != 1 ||
            callback.Parameters[0].ParameterType.FullName != "UnityEngine.GameObject" ||
            callback.ReturnType.MetadataType != MetadataType.Void)
            throw new Exception("Command cube injection must preserve GameObject through Spawn for a void GameObject callback: " + string.Join("; ", method.Body.Instructions));
        foreach (var edge in edges)
            if (!Targets(edge.Source).Contains(ReferenceEquals(edge.Target, spawn) ? duplicate : edge.Target))
                throw new Exception("Command cube incoming branch/switch label was not preserved or retargeted to dup.");
        if (method.Body.Instructions.Count != originalInstructions.Length + 2 ||
            !originalInstructions.SequenceEqual(method.Body.Instructions.Where(originalInstructions.Contains)))
            throw new Exception("Command cube emitter changed original instruction order.");
        CommandCubeStack.Verify(method);
        Console.WriteLine($"Command cube complete stack/type flow checked: {method.Body.Instructions.Count} instructions, {edges.Length} branch edges, no exception regions.");
    }

    private static DynamicMethodDefinition Synthetic(bool useSwitch)
    {
        var method = new DynamicMethodDefinition("SyntheticCommandCube", typeof(void), new[] { typeof(UnityEngine.GameObject), typeof(int) });
        var il = method.Definition.Body.GetILProcessor();
        var spawn = il.Create(OpCodes.Call, method.Module.ImportReference(typeof(UnityEngine.Networking.NetworkServer).GetMethod("Spawn")));
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldarg_1);
        if (useSwitch)
        {
            var jump = il.Create(OpCodes.Br, spawn);
            var fallthrough = il.Create(OpCodes.Nop);
            il.Emit(OpCodes.Switch, new[] { spawn, jump });
            il.Emit(OpCodes.Br, fallthrough);
            il.Append(jump);
            il.Append(fallthrough);
        }
        else
        {
            il.Emit(OpCodes.Brfalse, spawn);
            il.Emit(OpCodes.Nop);
        }
        il.Append(spawn);
        il.Emit(OpCodes.Ret);
        return method;
    }

    private static Instruction SpawnSite(MethodDefinition method) => method.Body.Instructions.Single(i =>
        i.Operand is MethodReference called && called.DeclaringType.FullName == "UnityEngine.Networking.NetworkServer" && called.Name == "Spawn");

    internal static IEnumerable<Instruction> Targets(Instruction instruction) => instruction.Operand switch
    {
        Instruction target => new[] { target },
        Instruction[] targets => targets,
        _ => Array.Empty<Instruction>()
    };

    private static void Observe(UnityEngine.GameObject gameObject)
    {
        if (!ReferenceEquals(spawned, gameObject)) throw new Exception("Command cube callback ran before Spawn or received the wrong object.");
        observed = gameObject;
        observations++;
    }

    internal static void Spawn(UnityEngine.GameObject gameObject) => spawned = gameObject;
}

namespace UnityEngine
{
    public class GameObject { }
}

namespace UnityEngine.Networking
{
    public static class NetworkServer
    {
        public static void Spawn(GameObject gameObject) => CommandCubeRegression.Spawn(gameObject);
    }
}
