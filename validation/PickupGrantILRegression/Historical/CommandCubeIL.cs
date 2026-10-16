using System;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace InstancedLoot.Hooks;

internal static class CommandCubeIL
{
    internal static void Apply<TDelegate>(ILContext il, TDelegate onSpawned) where TDelegate : Delegate
    {
        var spawns = il.Body.Instructions.Where(i =>
            i.OpCode == OpCodes.Call && i.Operand is MethodReference method &&
            method.DeclaringType.FullName == "UnityEngine.Networking.NetworkServer" && method.Name == "Spawn").ToArray();
        if (spawns.Length != 1 || spawns[0].Operand is not MethodReference spawn || spawn.HasThis ||
            spawn.ReturnType.MetadataType != MetadataType.Void || spawn.Parameters.Count != 1 ||
            spawn.Parameters[0].ParameterType.FullName != "UnityEngine.GameObject")
            throw new InvalidOperationException($"{il.Method.FullName}: Command cube Spawn(GameObject) pattern not found.");

        var cursor = new ILCursor(il);
        cursor.Goto(spawns[0], MoveType.Before);
        // Vanilla's maxOptionCount branch targets Spawn directly. Both paths must duplicate
        // the GameObject before Spawn consumes it, so incoming labels must target the dup.
        cursor.MoveAfterLabels();
        cursor.Emit(OpCodes.Dup);
        cursor.Index++;
        cursor.EmitDelegate(onSpawned);
    }
}
