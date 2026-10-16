using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class MultiShopLifecycleAudit
{
    internal static void Run(string gameAssemblyPath)
    {
        using var game = AssemblyDefinition.ReadAssembly(gameAssemblyPath);
        var controller = game.MainModule.GetType("RoR2.MultiShopController");
        MethodDefinition Method(string name) => controller.Methods.Single(method => method.Name == name);
        static bool Calls(Instruction instruction, string type, string name) =>
            (instruction.OpCode == OpCodes.Call || instruction.OpCode == OpCodes.Callvirt) &&
            instruction.Operand is MethodReference method && method.DeclaringType.FullName == type && method.Name == name;
        static void Require(bool condition, string message)
        {
            if (!condition) throw new Exception("MultiShop lifecycle audit: " + message);
        }

        var awake = Method("Awake").Body.Instructions;
        Require(awake.Count(i => Calls(i, "RoR2.MultiShopController", "CreateTerminals")) == 1,
            "Awake must create the native terminal set; review the copy lifecycle for this build.");
        Require(awake.Any(i => Calls(i, "UnityEngine.Networking.NetworkServer", "get_active")),
            "Awake server gate changed.");
        var start = Method("Start").Body.Instructions;
        Require(!start.Any(i => Calls(i, "RoR2.MultiShopController", "CreateTerminals")) &&
            start.Any(i => Calls(i, "RoR2.MultiShopController", "set_Networkavailable")) &&
            start.Any(i => Calls(i, "RoR2.MultiShopController", "set_Networkcost")),
            "Start must initialize availability/cost without creating another terminal set.");
        var create = Method("CreateTerminals").Body.Instructions;
        Require(create.Any(i => Calls(i, "UnityEngine.Object", "Instantiate")) &&
            create.Any(i => Calls(i, "UnityEngine.Networking.NetworkServer", "Spawn")) &&
            !create.Any(i => Calls(i, "UnityEngine.Object", "Destroy")),
            "CreateTerminals must allocate and spawn without destroying previous terminals.");
        var purchase = Method("OnPurchase").Body.Instructions;
        var closeFlag = purchase.FirstOrDefault(i => i.Operand is FieldReference field && field.Name == "doCloseOnTerminalPurchase");
        Require(closeFlag != null && purchase.Any(i => i.OpCode == OpCodes.Brfalse || i.OpCode == OpCodes.Brfalse_S) &&
            purchase.Any(i => Calls(i, "RoR2.ShopTerminalBehavior", "SetNoPickup")) &&
            purchase.Any(i => Calls(i, "RoR2.PurchaseInteraction", "set_Networkavailable")),
            "Native conditional terminal closure or network state changed; preserve its equipment policy.");

        string modPath = Path.Combine(Directory.GetCurrentDirectory(), "InstancedLoot", "bin", "Release", "netstandard2.1", "InstancedLoot.dll");
        using var mod = AssemblyDefinition.ReadAssembly(modPath);
        var copy = mod.MainModule.GetType("InstancedLoot.ObjectHandlers.MultiShopHandler").Methods.Single(m => m.Name == "InstanceSingleObjectFrom");
        Require(!copy.Body.Instructions.Any(i => Calls(i, "RoR2.MultiShopController", "CreateTerminals")),
            "Compiled production copy method creates a duplicate terminal set.");
        Require(copy.Body.Instructions.Any(i => Calls(i, "RoR2.MultiShopController", "set_Networkavailable")),
            "Compiled production copy method omits controller availability initialization.");
        var purchaseHook = mod.MainModule.GetType("InstancedLoot.Hooks.PurchaseInteractionHandler");
        Require(!purchaseHook.Methods.Any(m => m.HasBody && m.Body.Instructions.Any(i =>
            Calls(i, "RoR2.ShopTerminalBehavior", "set_Networkhidden") ||
            i.OpCode == OpCodes.Stfld && i.Operand is FieldReference field &&
            field.DeclaringType.FullName == "RoR2.PurchaseInteraction" && field.Name == "available")),
            "Compiled purchase hook overrides native terminal closure or availability.");
        Console.WriteLine("MultiShop lifecycle: 8 installed-game/compiled-production checks passed (static IL; no Unity execution).");
    }
}
