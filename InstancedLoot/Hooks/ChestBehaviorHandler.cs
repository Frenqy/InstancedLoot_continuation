using InstancedLoot.Components;
using RoR2;
using UnityEngine.Networking;

namespace InstancedLoot.Hooks;

public class ChestBehaviorHandler : AbstractHookHandler
{
    public override void RegisterHooks()
    {
        On.RoR2.ChestBehavior.ItemDrop += On_ChestBehavior_ItemDrop;
        On.RoR2.ChestBehavior.Start += On_ChestBehavior_Start;
    }

    public override void UnregisterHooks()
    {
        On.RoR2.ChestBehavior.ItemDrop -= On_ChestBehavior_ItemDrop;
        On.RoR2.ChestBehavior.Start -= On_ChestBehavior_Start;
    }

    private void On_ChestBehavior_ItemDrop(On.RoR2.ChestBehavior.orig_ItemDrop orig, ChestBehavior self)
    {
        if (self.GetComponent<InstanceInfoTracker>() is var instanceInfoTracker && instanceInfoTracker != null)
        {
            PickupDropletControllerHandler pickupDropletControllerHandler =
                hookManager.GetHandler<PickupDropletControllerHandler>();
            pickupDropletControllerHandler.InstanceOverrideInfo = instanceInfoTracker.Info;
            orig(self);
            pickupDropletControllerHandler.InstanceOverrideInfo = null;
        }
        else
        {
            orig(self);
        }
    }

    private void On_ChestBehavior_Start(On.RoR2.ChestBehavior.orig_Start orig, ChestBehavior self)
    {
        if (NetworkServer.active)
        {
            if (Plugin.ObjectHandlerManager.HandleAwaitedObject(self.gameObject))
                return;
            
            InstanceHandler instanceHandler = self.GetComponent<InstanceHandler>();
            if (instanceHandler == null)
            {
                orig(self);

                string objName = self.name;
                string objectType = null;

                if (objName.StartsWith("Chest1")) objectType = Enums.ObjectType.Chest1;
                if (objName.StartsWith("Chest2")) objectType = Enums.ObjectType.Chest2;
                if (objName.StartsWith("GoldChest")) objectType = Enums.ObjectType.GoldChest;
                if (objName.StartsWith("Chest1StealthedVariant"))
                    objectType = Enums.ObjectType.Chest1StealthedVariant;
                if (objName.StartsWith("CategoryChestDamage")) objectType = Enums.ObjectType.CategoryChestDamage;
                if (objName.StartsWith("CategoryChestHealing")) objectType = Enums.ObjectType.CategoryChestHealing;
                if (objName.StartsWith("CategoryChestUtility")) objectType = Enums.ObjectType.CategoryChestUtility;
                if (objName.StartsWith("CategoryChest2Damage")) objectType = Enums.ObjectType.CategoryChest2Damage;
                if (objName.StartsWith("CategoryChest2Healing"))
                    objectType = Enums.ObjectType.CategoryChest2Healing;
                if (objName.StartsWith("CategoryChest2Utility"))
                    objectType = Enums.ObjectType.CategoryChest2Utility;
                if (objName.StartsWith("EquipmentBarrel")) objectType = Enums.ObjectType.EquipmentBarrel;
                if (objName.StartsWith("LunarChest")) objectType = Enums.ObjectType.LunarChest;
                if (objName.StartsWith("VoidChest")) objectType = Enums.ObjectType.VoidChest;
                if (objName.StartsWith("ScavBackpack")) objectType = Enums.ObjectType.ScavBackpack;

                if (objectType != null)
                    Plugin.HandleInstancing(self.gameObject,
                        new InstanceInfoTracker.InstanceOverrideInfo(objectType));
            }
        }
        else
        {
            orig(self);
        }
    }
}