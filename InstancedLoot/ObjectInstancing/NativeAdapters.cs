using System;
using System.Collections.Generic;
using System.Linq;
using RoR2;
using UnityEngine;

namespace InstancedLoot.ObjectInstancing;

public static class NativeAdapters
{
    public static readonly string[] ChestTypes =
    {
        "Chest1", "Chest2", "GoldChest", "Chest1StealthedVariant", "CategoryChestDamage", "CategoryChestHealing",
        "CategoryChestUtility", "CategoryChest2Damage", "CategoryChest2Healing", "CategoryChest2Utility",
        "EquipmentBarrel", "LunarChest", "VoidChest", "Lockbox", "ScavBackpack"
    };
    public static readonly string[] ObjectTypes = ChestTypes.Concat(new[]
    {
        "VoidTriple", "LockboxVoid", "CasinoChest", "TripleShop", "TripleShopEquipment", "FreeChestMultiShop",
        "ShrineChance", "ShrineBlood", "ShrineRestack", "Drone", "Turret"
    }).ToArray();
    public static string Identify(GameObject target, IEnumerable<string> types) =>
        types.FirstOrDefault(type => string.Equals(target.name.Replace("(Clone)", "").Trim(), type, StringComparison.Ordinal));

    public sealed class Chest : ObjectAdapter
    {
        public override Action<GameObject> CaptureState(GameObject source)
        {
            var purchase = base.CaptureState(source);
            var chest = source.GetComponent<ChestBehavior>();
            var rng = new Xoroshiro128Plus(chest.rng);
            var pickup = chest.currentPickup;
            int count = chest.dropCount;
            return target =>
            {
                purchase(target);
                var copy = target.GetComponent<ChestBehavior>();
                copy.rng = new Xoroshiro128Plus(rng);
                copy.currentPickup = pickup;
                copy.dropCount = count;
            };
        }
    }
    public sealed class OptionChest : ObjectAdapter
    {
        public override Action<GameObject> CaptureState(GameObject source)
        {
            var purchase = base.CaptureState(source);
            var chest = source.GetComponent<OptionChestBehavior>();
            var rng = new Xoroshiro128Plus(chest.rng);
            var pickups = chest.generatedPickups.ToArray();
            return target =>
            {
                purchase(target);
                var copy = target.GetComponent<OptionChestBehavior>();
                copy.rng = new Xoroshiro128Plus(rng);
                copy.generatedPickups = pickups.ToList();
            };
        }
    }
    public sealed class Roulette : ObjectAdapter
    {
        public override Action<GameObject> CaptureState(GameObject source)
        {
            var purchase = base.CaptureState(source);
            var rng = new Xoroshiro128Plus(source.GetComponent<RouletteChestController>().rng);
            return target => { purchase(target); target.GetComponent<RouletteChestController>().rng = new Xoroshiro128Plus(rng); };
        }
    }
    public sealed class MultiShop : ObjectAdapter
    {
        public override bool IsReady(GameObject source) => source.GetComponent<MultiShopController>().terminalGameObjects
            .All(terminal => terminal && terminal.GetComponent<ShopTerminalBehavior>().hasStarted);
        public override IEnumerable<GameObject> OwnedObjects(GameObject root)
        {
            yield return root;
            foreach (var terminal in root.GetComponent<MultiShopController>().terminalGameObjects) yield return terminal;
        }
        public override Action<GameObject> CaptureState(GameObject source)
        {
            var controller = source.GetComponent<MultiShopController>();
            int cost = controller.Networkcost;
            bool available = controller.Networkavailable;
            var rng = new Xoroshiro128Plus(controller.rng);
            var terminals = controller.terminalGameObjects.Select(terminal =>
            {
                var purchase = base.CaptureState(terminal);
                var shop = terminal.GetComponent<ShopTerminalBehavior>();
                var pickup = shop.Networkpickup;
                bool hidden = shop.Networkhidden;
                var terminalRng = shop.rng == null ? null : new Xoroshiro128Plus(shop.rng);
                return (Action<GameObject>)(target =>
                {
                    purchase(target);
                    var copy = target.GetComponent<ShopTerminalBehavior>();
                    copy.hasStarted = true;
                    if (terminalRng != null) copy.rng = new Xoroshiro128Plus(terminalRng);
                    copy.Networkpickup = pickup;
                    copy.Networkhidden = hidden;
                    copy.UpdatePickupDisplayAndAnimations();
                });
            }).ToArray();
            return target =>
            {
                var copy = target.GetComponent<MultiShopController>();
                // Awake has already called CreateTerminals. Repeating it creates a
                // second orphan group, which cannot share the player's lifecycle.
                if (copy.terminalGameObjects.Length != terminals.Length)
                    throw new InvalidOperationException("MultiShop terminal count differs from the initialized source.");
                copy.Networkcost = cost;
                copy.Networkavailable = available;
                copy.rng = new Xoroshiro128Plus(rng);
                for (int i = 0; i < terminals.Length; i++) terminals[i](copy.terminalGameObjects[i]);
            };
        }
    }
    public sealed class Shrine : ObjectAdapter
    {
        public override Action<GameObject> CaptureState(GameObject source)
        {
            var purchase = base.CaptureState(source);
            var chance = source.GetComponent<ShrineChanceBehavior>();
            var order = source.GetComponent<ShrineRestackBehavior>();
            var rng = chance ? new Xoroshiro128Plus(chance.rng) : order ? new Xoroshiro128Plus(order.rng) : null;
            return target =>
            {
                purchase(target);
                var targetPurchase = target.GetComponent<PurchaseInteraction>();
                var targetChance = target.GetComponent<ShrineChanceBehavior>();
                var targetOrder = target.GetComponent<ShrineRestackBehavior>();
                var targetBlood = target.GetComponent<ShrineBloodBehavior>();
                if (targetChance) { targetChance.purchaseInteraction = targetPurchase; targetChance.rng = new Xoroshiro128Plus(rng); }
                if (targetOrder) { targetOrder.purchaseInteraction = targetPurchase; targetOrder.rng = new Xoroshiro128Plus(rng); }
                if (targetBlood) targetBlood.purchaseInteraction = targetPurchase;
            };
        }
    }
    public sealed class Repair : ObjectAdapter
    {
        public override Action<GameObject> CaptureState(GameObject source)
        {
            var purchase = base.CaptureState(source);
            var summon = source.GetComponent<SummonMasterBehavior>();
            var master = summon.masterPrefab;
            int upgrades = summon.droneUpgradeCount;
            bool destroy = summon.destroyAfterSummoning;
            return target =>
            {
                purchase(target);
                var copy = target.GetComponent<SummonMasterBehavior>();
                copy.masterPrefab = master;
                copy.NetworkdroneUpgradeCount = upgrades;
                copy.destroyAfterSummoning = destroy;
            };
        }
    }
}
