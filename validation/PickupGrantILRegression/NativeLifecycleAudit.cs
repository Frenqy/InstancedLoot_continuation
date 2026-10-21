using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class NativeLifecycleAudit
{
    internal static void Run(ModuleDefinition module)
    {
        int checks = 0;
        void Check(bool condition, string description) { if (!condition) throw new Exception("Installed lifecycle changed: " + description); checks++; }
        MethodDefinition Method(string type, string name) => module.GetType(type).Methods.Single(method => method.Name == name);
        bool Calls(MethodDefinition method, string type, string name) => method.Body.Instructions.Any(i => i.Operand is MethodReference called && called.DeclaringType.FullName == type && called.Name == name);
        bool Field(MethodDefinition method, string type, string name) => method.Body.Instructions.Any(i => i.Operand is FieldReference field && field.DeclaringType.FullName == type && field.Name == name);
        var awake = Method("RoR2.MultiShopController", "Awake");
        var start = Method("RoR2.MultiShopController", "Start");
        Check(awake.Body.Instructions.Count(i => i.Operand is MethodReference m && m.Name == "CreateTerminals") == 1 && !Calls(start, "RoR2.MultiShopController", "CreateTerminals"), "MultiShop creates terminals once in Awake, not Start");
        Check(Calls(start, "RoR2.MultiShopController", "set_Networkavailable") && Calls(start, "RoR2.MultiShopController", "set_Networkcost"), "MultiShop Start availability and price snapshot are required");
        var terminal = Method("RoR2.ShopTerminalBehavior", "Start");
        Check(Field(terminal, "RoR2.ShopTerminalBehavior", "hasStarted") && Calls(terminal, "RoR2.ShopTerminalBehavior", "GenerateNewPickupServer"), "Terminal Start readiness and optional native reroll");
        var summon = Method("RoR2.SummonMasterBehavior", "OpenSummonReturnMaster");
        Check(Field(summon, "RoR2.MasterSummon", "summonerBodyObject") && Calls(summon, "RoR2.MasterSummon", "Perform") && Field(summon, "RoR2.SummonMasterBehavior", "destroyAfterSummoning") && Calls(summon, "UnityEngine.Object", "Destroy"), "Repair event summons from its activator and destroys the damaged purchase object");
        Check(!Calls(Method("RoR2.SummonMasterBehavior", "Start"), "RoR2.MasterSummon", "Perform") && module.GetType("RoR2.SummonMasterBehavior").Methods.Any(m => m.Name == "set_NetworkdroneUpgradeCount"), "Repair display Start is safe to run once after network upgrade state is copied");
        var costs = module.GetType("RoR2.CostTypeCatalog").NestedTypes.SelectMany(type => type.Methods).Where(method => method.HasBody).ToArray();
        var money = costs.Single(method => Calls(method, "RoR2.CharacterMaster", "set_money") && Calls(method, "RoR2.Items.MultiShopCardUtils", "OnMoneyPurchase"));
        Check(Field(money, "RoR2.CostTypeDef/PayCostContext", "activatorMaster") && !Calls(money, "RoR2.TeamManager", "GiveTeamMoney"), "Money purchase debits activatorMaster and retains Executive Card callback, not team money");
        var purchase = Method("RoR2.PurchaseInteraction", "OnInteractionBegin");
        Check(Calls(purchase, "RoR2.CostTypeDef", "PayCost") && Field(purchase, "RoR2.PurchaseInteraction", "onPurchase"), "Native purchase still owns payment and purchase events");
        var blood = Method("RoR2.ShrineBloodBehavior", "AddShrineStack");
        Check(Calls(blood, "RoR2.CharacterMaster", "GiveMoney") && !Calls(blood, "RoR2.TeamManager", "GiveTeamMoney"), "Blood Shrine grants money to its activator master");
        var order = Method("RoR2.ShrineRestackBehavior", "AddShrineStack");
        Check(Calls(order, "RoR2.Inventory", "ShrineRestackInventory") && Calls(order, "RoR2.CharacterMaster", "get_inventory") && !Field(order, "RoR2.PlayerCharacterMasterController", "instances"), "Order Shrine changes only the activator inventory");
        Check(Method("RoR2.Interactor", "PerformInteraction").Body.Instructions.Any(i => i.Operand is GenericInstanceMethod method && method.Name == "GetComponents" && method.GenericArguments.Any(t => t.FullName == "RoR2.IInteractable")), "Native interaction executes all interface components; server gate applies before them");
        Console.WriteLine($"Installed native lifecycle/payment audit: {checks} assertions (method bodies inspected, no game methods executed).");
    }
}
