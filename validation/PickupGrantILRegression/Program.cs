using InstancedLoot.Hooks;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;

int assertions = 0;
foreach (bool addressLoad in new[] { false, true })
{
    using var module = ModuleDefinition.CreateModule("SyntheticGrant", ModuleKind.Dll);
    var owner = new TypeDefinition("RoR2", "PickupDef", TypeAttributes.Public, module.TypeSystem.Object);
    module.Types.Add(owner);
    var contextType = new TypeDefinition("", "GrantContext", TypeAttributes.NestedPublic | TypeAttributes.Sealed,
        module.ImportReference(typeof(ValueType)));
    owner.NestedTypes.Add(contextType);
    var shouldDestroy = new FieldDefinition("shouldDestroy", FieldAttributes.Public, module.TypeSystem.Boolean);
    contextType.Fields.Add(shouldDestroy);
    var controller = new TypeDefinition("RoR2", "GenericPickupController", TypeAttributes.Public, module.TypeSystem.Object);
    module.Types.Add(controller);
    var method = new MethodDefinition("AttemptGrant", MethodAttributes.Public, module.TypeSystem.Void);
    controller.Methods.Add(method);
    method.Parameters.Add(new ParameterDefinition("body", ParameterAttributes.None, module.TypeSystem.Object));
    // Deliberately vary the local index from the old installed game's index 5.
    method.Body.Variables.Add(new VariableDefinition(module.TypeSystem.Int32));
    var context = new VariableDefinition(contextType);
    method.Body.Variables.Add(context);
    var il = method.Body.GetILProcessor();
    var firstLoad = il.Create(addressLoad ? OpCodes.Ldloca : OpCodes.Ldloc, context);
    il.Append(firstLoad);
    il.Emit(OpCodes.Ldfld, shouldDestroy);
    il.Emit(OpCodes.Pop);
    il.Emit(OpCodes.Ldloc, context);
    il.Emit(OpCodes.Ldfld, shouldDestroy);
    il.Emit(OpCodes.Pop);
    il.Emit(OpCodes.Ret);
    Verify(method, context, firstLoad);
}

using (var module = ModuleDefinition.CreateModule("MissingGrantPattern", ModuleKind.Dll))
{
    var method = new MethodDefinition("AttemptGrant", MethodAttributes.Public, module.TypeSystem.Void);
    var owner = new TypeDefinition("RoR2", "GenericPickupController", TypeAttributes.Public, module.TypeSystem.Object);
    module.Types.Add(owner);
    owner.Methods.Add(method);
    method.Body.GetILProcessor().Emit(OpCodes.Ret);
    using var context = new ILContext(method);
    try
    {
        PickupGrantIL.Apply<UpdateContext>(context, Observe);
        throw new Exception("Missing pattern was silently accepted.");
    }
    catch (InvalidOperationException error) when (error.Message.Contains("GrantContext.shouldDestroy local pattern not found"))
    {
        assertions++;
    }
}

AnimationPrefabRegression.Run();

if (args.Length > 0)
{
    using var assembly = AssemblyDefinition.ReadAssembly(args[0]);
    var controller = assembly.MainModule.GetType("RoR2.GenericPickupController");
    var method = controller.Methods.Single(candidate => candidate.Name == "AttemptGrant");
    var firstRead = method.Body.Instructions.First(instruction => instruction.OpCode == OpCodes.Ldfld &&
        instruction.Operand is FieldReference field && field.Name == "shouldDestroy");
    var firstLoad = firstRead.Previous;
    if (!firstLoad.MatchLdloc(out int localIndex) && !firstLoad.MatchLdloca(out localIndex))
        throw new Exception("Installed assembly has an unexpected local load.");
    Verify(method, method.Body.Variables[localIndex], firstLoad);
    Console.WriteLine("Installed assembly structure checked; this does not prove a different game version's IL.");
    InstalledHookAudit.Run(args[0]);
}

Console.WriteLine($"Passed {assertions} IL structure assertions.");

void Verify(MethodDefinition method, VariableDefinition local, Instruction originalLoad)
{
    int originalReads = method.Body.Instructions.Count(i => i.OpCode == OpCodes.Ldfld);
    using var context = new ILContext(method);
    // The no-op callback is never executed: this harness verifies the production emitter's IL only.
    PickupGrantIL.Apply<UpdateContext>(context, Observe);
    var instructions = method.Body.Instructions;
    int loadIndex = instructions.IndexOf(originalLoad);
    Check(loadIndex >= 4, "Callback must precede the original load.");
    var injectedAddress = instructions.Take(loadIndex).Last(i => i.OpCode == OpCodes.Ldloca);
    Check(ReferenceEquals(injectedAddress.Operand, local), "Callback must receive the captured local's address.");
    Check(injectedAddress.Next.OpCode == OpCodes.Ldarg_0 && injectedAddress.Next.Next.OpCode == OpCodes.Ldarg_1,
        "Existing stack must be preserved while callback arguments are pushed.");
    Check(originalLoad.Next.OpCode == OpCodes.Ldfld, "Original read must remain after the callback.");
    Check(instructions.Count(i => i.OpCode == OpCodes.Ldfld) == originalReads, "All original field reads must remain.");
    var call = instructions.Take(loadIndex).Last(i => i.OpCode == OpCodes.Call || i.OpCode == OpCodes.Callvirt);
    Check(instructions.IndexOf(call) > instructions.IndexOf(injectedAddress), "Callback must run before the context is read.");
    Check(call.Operand is MethodReference callback && callback.Parameters[0].ParameterType.IsByReference,
        "Callback must take the context by reference.");
}

void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
    assertions++;
}

static void Observe(ref RoR2.PickupDef.GrantContext context, object self, object body) { }

delegate void UpdateContext(ref RoR2.PickupDef.GrantContext context, object self, object body);

namespace RoR2
{
    public class PickupDef
    {
        public struct GrantContext
        {
            public bool shouldDestroy;
        }
    }
}
