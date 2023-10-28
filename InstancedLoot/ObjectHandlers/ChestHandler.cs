using System.Reflection;
using InstancedLoot.Enums;
using InstancedLoot.Hooks;
using MonoMod.Utils;
using RoR2;
using UnityEngine;

namespace InstancedLoot.ObjectHandlers;

public class ChestHandler : AbstractObjectHandler
{
    public override string[] HandledSources => new string[]
    {
        ItemSource.Chest1, ItemSource.Chest2, ItemSource.GoldChest, ItemSource.Chest1StealthedVariant,
        ItemSource.CategoryChestDamage, ItemSource.CategoryChestHealing, ItemSource.CategoryChestUtility,
        ItemSource.CategoryChest2Damage, ItemSource.CategoryChest2Healing, ItemSource.CategoryChest2Utility,
        ItemSource.EquipmentBarrel,
        ItemSource.LunarChest
    };

    public override ObjectInstanceMode ObjectInstanceMode => ObjectInstanceMode.CopyObject;

    public override bool IsValidForObject(string source, GameObject gameObject)
    {
        return gameObject.GetComponent<ChestBehavior>() != null;
    }

    public override void Init(ObjectHandlerManager manager)
    {
        base.Init(manager);
        
        Plugin.HookManager.RegisterHandler<ChestBehaviorHandler>();
        Plugin.HookManager.RegisterHandler<PurchaseInteractionHandler>();
    }
}