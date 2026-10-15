using System.Text.RegularExpressions;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;

internal static class InstalledHookAudit
{
    internal static void Run(string assemblyPath)
    {
        using var assembly = AssemblyDefinition.ReadAssembly(assemblyPath);
        var module = assembly.MainModule;
        string root = Directory.GetCurrentDirectory();
        if (!File.Exists(Path.Combine(root, "InstancedLoot", "HookManager.cs")))
            throw new InvalidOperationException("Run the installed hook audit from the repository root.");

        string Source(string path)
        {
            string text = File.ReadAllText(Path.Combine(root, path));
            text = Regex.Replace(text, @"/\*.*?\*/", "", RegexOptions.Singleline);
            text = Regex.Replace(text, @"//[^\r\n]*", "");
            return Regex.Replace(text, @"#if DEBUG.*?#endif", "", RegexOptions.Singleline);
        }

        IEnumerable<string> Registered(string source) => Regex.Matches(source, @"RegisterHandler<(\w+)>\(\)")
            .Select(match => match.Groups[1].Value).Where(name => name != "T");

        var handlers = new HashSet<string>(Registered(Source("InstancedLoot/HookManager.cs")));
        foreach (string objectHandler in Registered(Source("InstancedLoot/ObjectHandlerManager.cs")))
            handlers.UnionWith(Registered(Source($"InstancedLoot/ObjectHandlers/{objectHandler}.cs")));

        var registered = new HashSet<string>();
        foreach (string handler in handlers)
        {
            string source = Source($"InstancedLoot/Hooks/{handler}.cs");
            int begin = source.IndexOf("public override void RegisterHooks()", StringComparison.Ordinal);
            int end = source.IndexOf("public override void UnregisterHooks()", StringComparison.Ordinal);
            foreach (Match match in Regex.Matches(source[begin..end], @"IL\.([\w.]+)\s*\+="))
                registered.Add(match.Groups[1].Value);
        }

        int passed = 0;
        var failures = new List<string>();
        void Audit(string hook, string type, string name, Action<ILCursor> patterns, int? parameterCount = null, string firstParameterType = null)
        {
            if (!registered.Remove(hook)) throw new Exception($"Audit entry is not registered: {hook}");
            try
            {
                var method = module.GetType(type).Methods.Single(candidate => candidate.Name == name &&
                    (!parameterCount.HasValue || candidate.Parameters.Count == parameterCount) &&
                    (firstParameterType == null || candidate.Parameters[0].ParameterType.FullName == firstParameterType));
                if (!method.HasBody || method.Body.Instructions.Count < 3)
                    throw new Exception("Method body is absent or stripped.");
                using var context = new ILContext(method);
                patterns(new ILCursor(context));
                passed++;
                Console.WriteLine($"MATCH {hook}");
            }
            catch (Exception error)
            {
                failures.Add($"{hook}: {error.Message}");
                Console.WriteLine($"FAIL {hook}: {error.Message}");
            }
        }

        Audit("RoR2.SceneDirector.Start", "RoR2.SceneDirector", "Start",
            cursor => cursor.GotoNext(MoveType.After, Call("RoR2.Run", "get_participatingPlayerCount")));
        Audit("RoR2.SceneDirector.PopulateScene", "RoR2.SceneDirector", "PopulateScene", cursor =>
        {
            foreach (string resource in new[] { "iscLockbox", "iscLockboxVoid", "iscFreeChest" })
            {
                cursor.GotoNext(i => i.MatchLdstr("SpawnCards/InteractableSpawnCard/" + resource));
                cursor.GotoNext(i => i.MatchLdloc(out _), i => i.MatchLdcI4(1), i => i.MatchAdd());
                cursor.GotoPrev(MoveType.After, Call("RoR2.DirectorCore", "TrySpawnObject"));
            }
        });
        Audit("RoR2.DeathRewards.OnKilledServer", "RoR2.DeathRewards", "OnKilledServer",
            cursor => cursor.GotoNext(Call("RoR2.TeamManager", "GiveTeamMoney")));
        Audit("RoR2.UI.PingIndicator.RebuildPing", "RoR2.UI.PingIndicator", "RebuildPing", cursor =>
        {
            int chatCalls = 0;
            while (cursor.TryGotoNext(MoveType.After, Call("RoR2.Chat", "AddMessage"))) chatCalls++;
            if (chatCalls == 0) throw new Exception("Optional chat-message loop matches zero calls.");
            cursor.Index = 0;
            cursor.GotoNext(GetComponent("RoR2.PurchaseInteraction"));
            cursor.GotoNext(GetComponent("RoR2.ShopTerminalBehavior"));
        });
        Audit("RoR2.UI.PingIndicator.Update", "RoR2.UI.PingIndicator", "Update", cursor =>
            cursor.GotoNext(Field("RoR2.UI.PingIndicator", "pingTargetPurchaseInteraction"), Field("RoR2.PurchaseInteraction", "available")));
        Audit("RoR2.DitherModel.RefreshObstructorsForCamera", "RoR2.DitherModel", "RefreshObstructorsForCamera", cursor =>
            cursor.GotoNext(i => i.MatchLdloc(out _), Call("RoR2.DitherModel", "UpdateDither")));
        Audit("RoR2.Hologram.HologramProjector.FindViewer", "RoR2.Hologram.HologramProjector", "FindViewer", cursor =>
        {
            cursor.GotoNext(i => i.MatchLdloc(out _), i => i.MatchLdloc(out _),
                i => i.Operand is MethodReference method && method.Name == "get_Item" &&
                     method.DeclaringType.FullName == "System.Collections.ObjectModel.ReadOnlyCollection`1<RoR2.PlayerCharacterMasterController>");
            cursor.GotoNext(Call("UnityEngine.Object", "op_Implicit"), i => i.OpCode == OpCodes.Brfalse || i.OpCode == OpCodes.Brfalse_S);
        });
        Audit("RoR2.AnimationEvents.CreatePrefab", "RoR2.AnimationEvents", "CreatePrefab",
            cursor => AnimationPrefabRegression.Verify(cursor.Context.Method));
        Audit("RoR2.EffectManager.SpawnEffect_EffectIndex_EffectData_bool", "RoR2.EffectManager", "SpawnEffect",
            cursor => cursor.GotoNext(Call("UnityEngine.Object", "Instantiate")), 3, "RoR2.EffectIndex");
        Audit("RoR2.Interactor.FindBestInteractableObject", "RoR2.Interactor", "FindBestInteractableObject",
            cursor => InteractorRegression.Verify(cursor.Context.Method));
        Audit("RoR2.GenericPickupController.AttemptGrant", "RoR2.GenericPickupController", "AttemptGrant", cursor =>
        {
            int local = -1;
            cursor.GotoNext(i => i.MatchLdloc(out local) || i.MatchLdloca(out local), Field("RoR2.PickupDef/GrantContext", "shouldDestroy"));
            if (cursor.Context.Body.Variables[local].VariableType.FullName != "RoR2.PickupDef/GrantContext")
                throw new Exception("GrantContext local type differs.");
        });
        Audit("RoR2.GenericPickupController.CreatePickup", "RoR2.GenericPickupController", "CreatePickup",
            cursor => cursor.GotoNext(Call("UnityEngine.Object", "Instantiate")));
        Audit("RoR2.PickupDropletController.CreatePickupDroplet_CreatePickupInfo_Vector3_Vector3", "RoR2.PickupDropletController", "CreatePickupDroplet",
            cursor => cursor.GotoNext(i => i.OpCode == OpCodes.Call && Call("UnityEngine.Object", "Instantiate")(i)), 3, "RoR2.GenericPickupController/CreatePickupInfo");
        Audit("RoR2.PickupDropletController.CreateCommandCube", "RoR2.PickupDropletController", "CreateCommandCube",
            cursor => CommandCubeRegression.VerifyInstalled(cursor.Context.Method));
        Audit("EntityStates.ScavBackpack.Opening.OnEnter", "EntityStates.ScavBackpack.Opening", "OnEnter",
            cursor => cursor.GotoNext(i => i.OpCode == OpCodes.Ldsfld && FieldOperand(i, "EntityStates.ScavBackpack.Opening", "maxItemDropCount")));
        Audit("EntityStates.Scrapper.ScrappingToIdle.OnEnter", "EntityStates.Scrapper.ScrappingToIdle", "OnEnter",
            cursor => cursor.GotoNext(i => i.OpCode == OpCodes.Call && Call("RoR2.PickupDropletController", "CreatePickupDroplet")(i)));
        Audit("RoR2.BossGroup.DropRewards", "RoR2.BossGroup", "DropRewards", cursor =>
            cursor.GotoNext(Call("RoR2.PickupDropletController", "CreatePickupDroplet"), i => i.MatchLdloc(out _)));
        Audit("RoR2.Artifacts.SacrificeArtifactManager.OnServerCharacterDeath", "RoR2.Artifacts.SacrificeArtifactManager", "OnServerCharacterDeath",
            cursor => cursor.GotoNext(Call("RoR2.Util", "GetExpAdjustedDropChancePercent")));

        if (registered.Count != 0) failures.Add("Registered IL hooks lack an audit: " + string.Join(", ", registered));
        Console.WriteLine($"Registered Release IL hook patterns: {passed} passed, {failures.Count} failed.");
        if (failures.Count != 0) throw new Exception(string.Join(Environment.NewLine, failures));
    }

    private static Func<Instruction, bool> Call(string type, string name) => i =>
        (i.OpCode == OpCodes.Call || i.OpCode == OpCodes.Callvirt) && i.Operand is MethodReference method &&
        method.Name == name && method.DeclaringType.FullName == type;

    private static Func<Instruction, bool> Field(string type, string name) => i =>
        i.OpCode == OpCodes.Ldfld && FieldOperand(i, type, name);

    private static bool FieldOperand(Instruction instruction, string type, string name) =>
        instruction.Operand is FieldReference field && field.Name == name && field.DeclaringType.FullName == type;

    private static Func<Instruction, bool> GetComponent(string type) => i =>
        (i.OpCode == OpCodes.Call || i.OpCode == OpCodes.Callvirt) && i.Operand is GenericInstanceMethod method &&
        method.Name == "GetComponent" && method.DeclaringType.FullName == "UnityEngine.GameObject" &&
        method.GenericArguments.Count == 1 && method.GenericArguments[0].FullName == type;
}
