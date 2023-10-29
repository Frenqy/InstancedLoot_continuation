using System.Reflection;
using InstancedLoot.Components;
using RoR2;
using UnityEngine.Networking;

namespace InstancedLoot.Hooks;

public class ChestBehaviorHandler : AbstractHookHandler
{
    public override void RegisterHooks()
    {
        On.RoR2.ChestBehavior.Awake += On_ChestBehavior_Awake;
        On.RoR2.ChestBehavior.ItemDrop += On_ChestBehavior_ItemDrop;
        On.RoR2.ChestBehavior.Start += On_ChestBehavior_Start;
    }

    public override void UnregisterHooks()
    {
        On.RoR2.ChestBehavior.Awake -= On_ChestBehavior_Awake;
        On.RoR2.ChestBehavior.ItemDrop -= On_ChestBehavior_ItemDrop;
        On.RoR2.ChestBehavior.Start -= On_ChestBehavior_Start;
    }

    private void On_ChestBehavior_Awake(On.RoR2.ChestBehavior.orig_Awake orig, ChestBehavior self)
    {
        orig(self);
    }

    private void On_ChestBehavior_ItemDrop(On.RoR2.ChestBehavior.orig_ItemDrop orig, ChestBehavior self)
    {
        if (self.GetComponent<InstanceInfoTracker>() is var instanceInfoTracker && instanceInfoTracker != null)
        {
            Plugin._logger.LogWarning($"ChestBehavior dropping {instanceInfoTracker.ItemSource}");
            hookManager.GetHandler<PickupDropletHandler>().InstanceOverrideInfo = instanceInfoTracker.Info;
            orig(self);
            hookManager.GetHandler<PickupDropletHandler>().InstanceOverrideInfo = null;
        }
        else
        {
            orig(self);
        }
    }

    private void On_ChestBehavior_Start(On.RoR2.ChestBehavior.orig_Start orig, ChestBehavior self)
    {
        InstanceHandler instanceHandler = self.GetComponent<InstanceHandler>();
        if (instanceHandler == null)
        {
            orig(self);

            if (NetworkServer.active)
            {
                string objName = self.name;
                string source = null;
                
                if(objName.StartsWith("Chest1")) source = Enums.ItemSource.Chest1;
                if(objName.StartsWith("Chest2")) source = Enums.ItemSource.Chest2;
                if(objName.StartsWith("GoldChest")) source = Enums.ItemSource.GoldChest;
                if(objName.StartsWith("Chest1StealthedVariant")) source = Enums.ItemSource.Chest1StealthedVariant;
                if(objName.StartsWith("CategoryChestDamage")) source = Enums.ItemSource.CategoryChestDamage;
                if(objName.StartsWith("CategoryChestHealing")) source = Enums.ItemSource.CategoryChestHealing;
                if(objName.StartsWith("CategoryChestUtility")) source = Enums.ItemSource.CategoryChestUtility;
                if(objName.StartsWith("CategoryChest2Damage")) source = Enums.ItemSource.CategoryChest2Damage;
                if(objName.StartsWith("CategoryChest2Healing")) source = Enums.ItemSource.CategoryChest2Healing;
                if(objName.StartsWith("CategoryChest2Utility")) source = Enums.ItemSource.CategoryChest2Utility;
                if(objName.StartsWith("EquipmentBarrel")) source = Enums.ItemSource.EquipmentBarrel;
                if(objName.StartsWith("LunarChest")) source = Enums.ItemSource.LunarChest;
                if(objName.StartsWith("VoidChest")) source = Enums.ItemSource.VoidChest;
                
                Plugin._logger.LogWarning($"ChestBehavior registering {source}");
                
                if(source != null) Plugin.HandleInstancingNextTick(self.gameObject, new InstanceInfoTracker.InstanceOverrideInfo(source));
            }
        }
        else
        {
            if (instanceHandler.SourceObject != null && NetworkServer.active)
            {
                Plugin._logger.LogInfo("Testing - Start called on Chest with InstanceHandler");

                ChestBehavior source = instanceHandler.SourceObject.GetComponent<ChestBehavior>();

                self.rng = new Xoroshiro128Plus(source.rng);
                self.dropCount = source.dropCount;
                self.dropPickup = source.dropPickup;
            }

        }
    }
}