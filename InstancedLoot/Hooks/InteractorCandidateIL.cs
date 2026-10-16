using System;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace InstancedLoot.Hooks;

/// <summary>Replaces both candidate acquisition sites without changing their branches.</summary>
public static class InteractorCandidateIL
{
    public static void Apply<T>(ILContext il, T filter) where T : Delegate
    {
        var sites = il.Body.Instructions.Where(instruction =>
            instruction.Operand is GenericInstanceMethod method && method.Name == "TryGetComponent" &&
            method.DeclaringType.FullName == "UnityEngine.GameObject" && method.GenericArguments.Count == 1 &&
            method.GenericArguments[0].FullName == "RoR2.IInteractable").ToArray();
        if (sites.Length != 2)
            throw new InvalidOperationException($"Expected raycast and overlap candidate sites, found {sites.Length}.");
        for (int index = 0; index < sites.Length; index++)
        {
            var cursor = new ILCursor(il);
            cursor.Goto(sites[index], MoveType.Before);
            var labels = cursor.IncomingLabels.ToArray();
            cursor.Emit(OpCodes.Ldarg_0);
            foreach (var label in labels) label.Target = cursor.Prev;
            cursor.Emit(index == 0 ? OpCodes.Ldc_I4_0 : OpCodes.Ldc_I4_1);
            cursor.Remove();
            cursor.EmitDelegate(filter);
        }
    }
}
