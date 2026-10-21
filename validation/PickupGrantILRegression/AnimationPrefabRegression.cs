using InstancedLoot.Hooks;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;

internal static class AnimationPrefabRegression
{
    internal static void Run()
    {
        using (var module = ModuleDefinition.CreateModule("SyntheticAnimation", ModuleKind.Dll))
        {
            var method = CreateMethod(module);
            Verify(method);
        }

        foreach (string failure in new[] { "missing flag", "wrong flag type", "missing spawn", "wrong spawn signature" })
        {
            using var module = ModuleDefinition.CreateModule("MissingAnimationPattern", ModuleKind.Dll);
            var method = CreateMethod(module);
            var instructions = method.Body.Instructions;
            if (failure == "missing flag")
                instructions.First(i => i.Operand is MethodReference called && called.Name == "ShouldUsePooledEffect").Operand =
                    new MethodReference("DifferentFlag", module.TypeSystem.Boolean, module.TypeSystem.Object);
            else if (failure == "wrong flag type")
                method.Body.Variables[1].VariableType = module.TypeSystem.Int32;
            else
            {
                foreach (var site in SpawnSites(method))
                {
                    var called = (MethodReference)site.Operand;
                    if (failure == "missing spawn") called.Name = "DifferentSpawn";
                    else called.Parameters[0].ParameterType = module.TypeSystem.Int32;
                }
            }

            using var context = new ILContext(method);
            try
            {
                AnimationPrefabIL.Apply<Action<object, object, bool>>(context, Observe);
                throw new Exception($"Animation emitter silently accepted {failure}.");
            }
            catch (InvalidOperationException error) when (error.Message.Contains("pattern not found")) { }
        }
        Console.WriteLine("Animation emitter rejected 4 missing/changed-pattern cases.");
    }

    internal static void Verify(MethodDefinition method)
    {
        var sites = SpawnSites(method);
        if (sites.Length == 0) throw new Exception("Animation test requires at least one spawn site.");
        var consumers = sites.Select(site => site.Next).ToArray();
        int pooledLocal = -1;
        using var context = new ILContext(method);
        new ILCursor(context).GotoNext(i => i.Operand is MethodReference called && called.Name == "ShouldUsePooledEffect",
            i => i.MatchStloc(out pooledLocal));
        if (method.Body.Instructions.Count(i => i.MatchStloc(out int local) && local == pooledLocal) != 1)
            throw new Exception("The captured pool flag can be overwritten before a spawn site.");

        int originalCount = method.Body.Instructions.Count;
        AnimationPrefabIL.Apply<Action<object, object, bool>>(context, Observe);
        for (int index = 0; index < sites.Length; index++)
        {
            var duplicate = sites[index].Next;
            var parent = duplicate.Next;
            var pooled = parent.Next;
            var call = pooled.Next;
            if (duplicate.OpCode != OpCodes.Dup || parent.OpCode != OpCodes.Ldarg_0 ||
                !pooled.MatchLdloc(out int local) || local != pooledLocal ||
                method.Body.Variables[local].VariableType.MetadataType != MetadataType.Boolean ||
                call.OpCode != OpCodes.Call || call.Operand is not MethodReference callback ||
                callback.Name != nameof(Observe) || callback.ReturnType.MetadataType != MetadataType.Void ||
                callback.Parameters.Count != 3 || callback.Parameters[2].ParameterType.MetadataType != MetadataType.Boolean ||
                !ReferenceEquals(call.Next, consumers[index]) || consumers[index].OpCode != OpCodes.Pop)
                throw new Exception("Animation emitter did not preserve the Transform for vanilla's pop and supply self/pool arguments.");
        }
        if (method.Body.Instructions.Count != originalCount + sites.Length * 4)
            throw new Exception("Animation emitter inserted unexpected instructions.");
        Console.WriteLine($"Production animation emitter: {sites.Length} return-site stack structures checked in memory.");
    }

    private static Instruction[] SpawnSites(MethodDefinition method) => method.Body.Instructions.Where(i =>
        i.OpCode == OpCodes.Call && i.Operand is MethodReference called &&
        called.Name.StartsWith("<CreatePrefab>g__DoSpawnEffect|", StringComparison.Ordinal)).ToArray();

    private static MethodDefinition CreateMethod(ModuleDefinition module)
    {
        var owner = new TypeDefinition("RoR2", "AnimationEvents", TypeAttributes.Public, module.TypeSystem.Object);
        module.Types.Add(owner);
        var method = new MethodDefinition("CreatePrefab", MethodAttributes.Public, module.TypeSystem.Void);
        owner.Methods.Add(method);
        var gameObject = new TypeReference("UnityEngine", "GameObject", module, module);
        var transform = new TypeReference("UnityEngine", "Transform", module, module);
        var vector = new TypeReference("UnityEngine", "Vector3", module, module, true);
        var rotation = new TypeReference("UnityEngine", "Quaternion", module, module, true);
        var effectManager = new TypeReference("RoR2", "EffectManager", module, module);
        var shouldPool = new MethodReference("ShouldUsePooledEffect", module.TypeSystem.Boolean, effectManager);
        shouldPool.Parameters.Add(new ParameterDefinition(gameObject));
        var spawn = new MethodReference("<CreatePrefab>g__DoSpawnEffect|9_0", transform, owner);
        foreach (var type in new[] { module.TypeSystem.Boolean, gameObject, vector, rotation, transform })
            spawn.Parameters.Add(new ParameterDefinition(type));
        method.Body.InitLocals = true;
        method.Body.Variables.Add(new VariableDefinition(module.TypeSystem.Int32));
        var pooled = new VariableDefinition(module.TypeSystem.Boolean);
        var position = new VariableDefinition(vector);
        var quaternion = new VariableDefinition(rotation);
        method.Body.Variables.Add(pooled);
        method.Body.Variables.Add(position);
        method.Body.Variables.Add(quaternion);
        var il = method.Body.GetILProcessor();
        il.Emit(OpCodes.Ldnull);
        il.Emit(OpCodes.Call, shouldPool);
        il.Emit(OpCodes.Stloc, pooled);
        for (int index = 0; index < 4; index++)
        {
            il.Emit(OpCodes.Ldloc, pooled);
            il.Emit(OpCodes.Ldnull);
            il.Emit(OpCodes.Ldloc, position);
            il.Emit(OpCodes.Ldloc, quaternion);
            il.Emit(OpCodes.Ldnull);
            il.Emit(OpCodes.Call, spawn);
            il.Emit(OpCodes.Pop);
        }
        il.Emit(OpCodes.Ret);
        return method;
    }

    private static void Observe(object created, object parent, bool pooled) { }
}
