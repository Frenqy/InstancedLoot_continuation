using System;
using InstancedLoot.Components;
using InstancedLoot.ObjectInstancing;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace InstancedLoot.Hooks;

public sealed class ObjectLifecycleHooks : AbstractHookHandler
{
    private readonly NativeAdapters.Chest chest = new();
    private readonly NativeAdapters.OptionChest option = new();
    private readonly NativeAdapters.MultiShop shop = new();
    private readonly NativeAdapters.Roulette roulette = new();
    private readonly NativeAdapters.Shrine shrine = new();
    private readonly NativeAdapters.Repair repair = new();
    public override void RegisterHooks()
    {
        On.RoR2.ChestBehavior.Start += ChestStart;
        On.RoR2.OptionChestBehavior.Start += OptionStart;
        On.RoR2.MultiShopController.Start += ShopStart;
        On.RoR2.ShopTerminalBehavior.Start += TerminalStart;
        On.RoR2.RouletteChestController.Start += RouletteStart;
        On.RoR2.ShrineChanceBehavior.Start += ChanceStart;
        On.RoR2.ShrineBloodBehavior.Start += BloodStart;
        On.RoR2.ShrineRestackBehavior.Start += OrderStart;
        On.RoR2.SummonMasterBehavior.Start += RepairStart;
    }
    public override void UnregisterHooks()
    {
        On.RoR2.ChestBehavior.Start -= ChestStart;
        On.RoR2.OptionChestBehavior.Start -= OptionStart;
        On.RoR2.MultiShopController.Start -= ShopStart;
        On.RoR2.ShopTerminalBehavior.Start -= TerminalStart;
        On.RoR2.RouletteChestController.Start -= RouletteStart;
        On.RoR2.ShrineChanceBehavior.Start -= ChanceStart;
        On.RoR2.ShrineBloodBehavior.Start -= BloodStart;
        On.RoR2.ShrineRestackBehavior.Start -= OrderStart;
        On.RoR2.SummonMasterBehavior.Start -= RepairStart;
    }
    private void Start(Component component, Action original, ObjectAdapter adapter, string type, bool initializeCopyDisplay = false)
    {
        if (NetworkServer.active && Plugin.Objects.InitializeCopy(component.gameObject))
        {
            if (initializeCopyDisplay && component) original();
            return;
        }
        original();
        if (NetworkServer.active && type != null) Plugin.Objects.InitializeSource(adapter, component.gameObject, type);
    }
    private void ChestStart(On.RoR2.ChestBehavior.orig_Start orig, ChestBehavior self) =>
        Start(self, () => orig(self), chest, NativeAdapters.Identify(self.gameObject, NativeAdapters.ChestTypes));
    private void OptionStart(On.RoR2.OptionChestBehavior.orig_Start orig, OptionChestBehavior self) =>
        Start(self, () => orig(self), option, NativeAdapters.Identify(self.gameObject, new[] { "VoidTriple", "LockboxVoid" }));
    private void ShopStart(On.RoR2.MultiShopController.orig_Start orig, MultiShopController self) =>
        Start(self, () => orig(self), shop, NativeAdapters.Identify(self.gameObject, new[] { "TripleShopEquipment", "TripleShop", "FreeChestMultiShop" }));
    private void TerminalStart(On.RoR2.ShopTerminalBehavior.orig_Start orig, ShopTerminalBehavior self)
    {
        // Only terminals owned by our controller copy skip the native Start; printers,
        // cauldrons, pools and unrelated terminals always retain their original path.
        if (NetworkServer.active && (Plugin.Objects.IsPendingChild(self.gameObject) || self.GetComponent<InstanceHandler>() && self.hasStarted)) return;
        orig(self);
    }
    private void RouletteStart(On.RoR2.RouletteChestController.orig_Start orig, RouletteChestController self) =>
        Start(self, () => orig(self), roulette, self.name.StartsWith("CasinoChest") ? "CasinoChest" : null);
    private void ChanceStart(On.RoR2.ShrineChanceBehavior.orig_Start orig, ShrineChanceBehavior self) =>
        Start(self, () => orig(self), shrine, "ShrineChance");
    private void BloodStart(On.RoR2.ShrineBloodBehavior.orig_Start orig, ShrineBloodBehavior self) =>
        Start(self, () => orig(self), shrine, "ShrineBlood");
    private void OrderStart(On.RoR2.ShrineRestackBehavior.orig_Start orig, ShrineRestackBehavior self) =>
        Start(self, () => orig(self), shrine, "ShrineRestack");
    private void RepairStart(On.RoR2.SummonMasterBehavior.orig_Start orig, SummonMasterBehavior self)
    {
        string type = null;
        // Identify the damaged purchase object, never a living CharacterBody.
        if (self.GetComponent<PurchaseInteraction>() && self.masterPrefab && self.destroyAfterSummoning)
        {
            string name = self.gameObject.name;
            if (name.IndexOf("Broken", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                if (name.IndexOf("Turret", StringComparison.OrdinalIgnoreCase) >= 0) type = "Turret";
                else if (name.IndexOf("Drone", StringComparison.OrdinalIgnoreCase) >= 0) type = "Drone";
            }
        }
        Start(self, () => orig(self), repair, type, true);
    }
}
