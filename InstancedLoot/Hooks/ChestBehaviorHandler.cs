using System.Reflection;
using InstancedLoot.Components;
using RoR2;
using UnityEngine.Networking;

namespace InstancedLoot.Hooks;

public class ChestBehaviorHandler : AbstractHookHandler
{
    protected FieldInfo Field_ChestBehavior_rng =
        typeof(ChestBehavior).GetField("rng", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
    protected FieldInfo Field_ChestBehavior_dropCount =
        typeof(ChestBehavior).GetField("dropCount", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
    protected PropertyInfo Property_ChestBehavior_dropPickup = typeof(ChestBehavior).GetProperty("dropPickup",
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
    
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
                if(objName.StartsWith("CategoryChestDamage")) source = Enums.ItemSource.CategoryChestDamage;
                if(objName.StartsWith("CategoryChestHealing")) source = Enums.ItemSource.CategoryChestHealing;
                if(objName.StartsWith("CategoryChestUtility")) source = Enums.ItemSource.CategoryChestUtility;
                if(objName.StartsWith("CategoryChest2Damage")) source = Enums.ItemSource.CategoryChest2Damage;
                if(objName.StartsWith("CategoryChest2Healing")) source = Enums.ItemSource.CategoryChest2Healing;
                if(objName.StartsWith("CategoryChest2Utility")) source = Enums.ItemSource.CategoryChest2Utility;
                
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
                
                Field_ChestBehavior_rng.SetValue(self, Field_ChestBehavior_rng.GetValue(source));
                Field_ChestBehavior_dropCount.SetValue(self, Field_ChestBehavior_dropCount.GetValue(source));
                Property_ChestBehavior_dropPickup.SetValue(self, Property_ChestBehavior_dropPickup.GetValue(source));
            }

        }
    }
}