using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;

internal static class InteractorRegression
{
    internal static void Verify(MethodDefinition method)
    {
        var instructions = method.Body.Instructions;
        int shouldIgnoreIndex = -1;
        for (int index = 0; index < instructions.Count; index++)
        {
            var instruction = instructions[index];
            if ((instruction.OpCode == OpCodes.Call || instruction.OpCode == OpCodes.Callvirt) &&
                instruction.Operand is MethodReference called &&
                called.Name == "ShouldIgnoreSpherecastForInteractibility")
            {
                shouldIgnoreIndex = index;
                break;
            }
        }
        if (shouldIgnoreIndex < 2)
            throw new Exception("Interactor overlap ShouldIgnoreSpherecastForInteractibility call not found.");

        var candidateLoad = instructions[shouldIgnoreIndex - 2];
        if (!TryGetLocal(candidateLoad, out int candidateLocal))
            throw new Exception("Interactor candidate receiver is not loaded from a local.");

        string candidateType = method.Body.Variables[candidateLocal].VariableType.FullName;
        if (candidateType != "RoR2.IInteractable")
            throw new Exception($"Interactor candidate local type changed to {candidateType}; review receiver cast.");

        string sourcePath = Path.Combine(Directory.GetCurrentDirectory(), "InstancedLoot", "Hooks", "InteractorHandler.cs");
        string source = File.Exists(sourcePath) ? File.ReadAllText(sourcePath) : "";
        if (!source.Contains("cursor.Emit(OpCodes.Castclass, typeof(Component));", StringComparison.Ordinal))
            throw new Exception("Interactor emitter lacks the Component cast required by GetComponent.");

        if (!source.Contains("methodGetComponent.MakeGenericMethod(typeof(InstanceHandler))", StringComparison.Ordinal))
            throw new Exception("Interactor filter does not call Component.GetComponent<InstanceHandler>.");

        Console.WriteLine("Interactor candidate receiver/type structure checked; Component cast is required.");
    }

    private static bool TryGetLocal(Instruction instruction, out int index)
    {
        if (instruction.MatchLdloc(out index)) return true;
        if (instruction.MatchLdloca(out index)) return true;
        index = -1;
        return false;
    }
}
