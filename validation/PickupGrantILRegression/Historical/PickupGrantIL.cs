using System;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace InstancedLoot.Hooks;

internal static class PickupGrantIL
{
    private const string GrantContextType = "RoR2.PickupDef/GrantContext";

    internal static void Apply<TDelegate>(ILContext il, TDelegate updateContext) where TDelegate : Delegate
    {
        var cursor = new ILCursor(il);
        int contextLocalIndex = -1;
        if (!cursor.TryGotoNext(MoveType.Before,
                i => i.MatchLdloc(out contextLocalIndex) || i.MatchLdloca(out contextLocalIndex),
                i => i.OpCode == OpCodes.Ldfld && i.Operand is FieldReference field &&
                     field.Name == "shouldDestroy" && field.DeclaringType.FullName == GrantContextType) ||
            il.Body.Variables[contextLocalIndex].VariableType.FullName != GrantContextType)
            throw new InvalidOperationException($"{il.Method.FullName}: AttemptGrant GrantContext.shouldDestroy local pattern not found.");

        // Update the actual local before vanilla loads its value for consumed and later destruction.
        cursor.Emit(OpCodes.Ldloca, il.Body.Variables[contextLocalIndex]);
        cursor.Emit(OpCodes.Ldarg_0);
        cursor.Emit(OpCodes.Ldarg_1);
        cursor.EmitDelegate(updateContext);
    }
}
