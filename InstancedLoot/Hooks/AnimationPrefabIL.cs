using System;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace InstancedLoot.Hooks;

internal static class AnimationPrefabIL
{
    internal static void Apply<TDelegate>(ILContext il, TDelegate handleCreation) where TDelegate : Delegate
    {
        var cursor = new ILCursor(il);
        int pooledLocal = -1;
        if (!cursor.TryGotoNext(MoveType.After,
                i => i.OpCode == OpCodes.Call && i.Operand is MethodReference method &&
                     method.DeclaringType.FullName == "RoR2.EffectManager" && method.Name == "ShouldUsePooledEffect" &&
                     method.ReturnType.MetadataType == MetadataType.Boolean && method.Parameters.Count == 1 &&
                     method.Parameters[0].ParameterType.FullName == "UnityEngine.GameObject",
                i => i.MatchStloc(out pooledLocal)) ||
            il.Body.Variables[pooledLocal].VariableType.MetadataType != MetadataType.Boolean)
            throw new InvalidOperationException($"{il.Method.FullName}: CreatePrefab pooled-effect flag pattern not found.");

        cursor.Index = 0;
        int matches = 0;
        while (cursor.TryGotoNext(MoveType.After, i => i.OpCode == OpCodes.Call && i.Operand is MethodReference method &&
                   method.DeclaringType.FullName == "RoR2.AnimationEvents" && !method.HasThis &&
                   method.Name.StartsWith("<CreatePrefab>g__DoSpawnEffect|", StringComparison.Ordinal) &&
                   method.ReturnType.FullName == "UnityEngine.Transform" && method.Parameters.Count == 5 &&
                   method.Parameters[0].ParameterType.MetadataType == MetadataType.Boolean &&
                   method.Parameters[1].ParameterType.FullName == "UnityEngine.GameObject" &&
                   method.Parameters[2].ParameterType.FullName == "UnityEngine.Vector3" &&
                   method.Parameters[3].ParameterType.FullName == "UnityEngine.Quaternion" &&
                   method.Parameters[4].ParameterType.FullName == "UnityEngine.Transform"))
        {
            // Leave vanilla's returned Transform on the stack for its original consumer.
            cursor.Emit(OpCodes.Dup);
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldloc, il.Body.Variables[pooledLocal]);
            cursor.EmitDelegate(handleCreation);
            matches++;
        }

        if (matches == 0)
            throw new InvalidOperationException($"{il.Method.FullName}: CreatePrefab DoSpawnEffect pattern not found.");
    }
}
