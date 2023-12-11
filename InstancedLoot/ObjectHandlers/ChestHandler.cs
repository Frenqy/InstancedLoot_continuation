using System.Reflection;
using InstancedLoot.Components;
using InstancedLoot.Enums;
using InstancedLoot.Hooks;
using MonoMod.Utils;
using RoR2;
using UnityEngine;

namespace InstancedLoot.ObjectHandlers;

public class ChestHandler : AbstractObjectHandler
{
    public override string[] HandledObjectTypes { get; } =
    {
        ObjectType.Chest1, ObjectType.Chest2, ObjectType.GoldChest, ObjectType.Chest1StealthedVariant,
        ObjectType.CategoryChestDamage, ObjectType.CategoryChestHealing, ObjectType.CategoryChestUtility,
        ObjectType.CategoryChest2Damage, ObjectType.CategoryChest2Healing, ObjectType.CategoryChest2Utility,
        ObjectType.EquipmentBarrel,
        ObjectType.LunarChest, ObjectType.VoidChest,
        ObjectType.Lockbox,
    };

    public override ObjectInstanceMode ObjectInstanceMode => ObjectInstanceMode.CopyObject;

    public override bool IsValidForObject(string objectType, GameObject gameObject)
    {
        return gameObject.GetComponent<ChestBehavior>() != null;
    }

    public override void Init(ObjectHandlerManager manager)
    {
        base.Init(manager);
        
        Plugin.HookManager.RegisterHandler<ChestBehaviorHandler>();
        Plugin.HookManager.RegisterHandler<PurchaseInteractionHandler>();
    }

    public override InstanceHandler InstanceSingleObjectFrom(GameObject source, GameObject target,
        PlayerCharacterMasterController[] players)
    {
        ChestBehavior sourceChest = source.GetComponent<ChestBehavior>();
        ChestBehavior targetChest = target.GetComponent<ChestBehavior>();
        
        targetChest.rng = new Xoroshiro128Plus(sourceChest.rng);
        targetChest.dropCount = sourceChest.dropCount;
        targetChest.dropPickup = sourceChest.dropPickup;
        
        return base.InstanceSingleObjectFrom(source, target, players);
    }
}