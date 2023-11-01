using System.Collections;
using InstancedLoot.Components;
using RoR2;
using UnityEngine.Networking;

namespace InstancedLoot.Hooks;

public class ShopTerminalBehaviorHandler : AbstractHookHandler
{
    public override void RegisterHooks()
    {
        On.RoR2.ShopTerminalBehavior.Start += On_ShopTerminalBehavior_Start;
        On.RoR2.ShopTerminalBehavior.DropPickup += On_ShopTerminalBehavior_DropPickup;
    }

    public override void UnregisterHooks()
    {
        On.RoR2.ShopTerminalBehavior.Start -= On_ShopTerminalBehavior_Start;
        On.RoR2.ShopTerminalBehavior.DropPickup -= On_ShopTerminalBehavior_DropPickup;
    }

    private void On_ShopTerminalBehavior_Start(On.RoR2.ShopTerminalBehavior.orig_Start orig, ShopTerminalBehavior self)
    {
        if (NetworkServer.active)
        {
            InstanceHandler instanceHandler = self.GetComponent<InstanceHandler>();

            if (instanceHandler == null)
            {
                orig(self);
                
                string objName = self.name;
                string objectType = null;
                
                if(objName.StartsWith("Duplicator")) objectType = Enums.ObjectType.Duplicator;
                if(objName.StartsWith("DuplicatorLarge")) objectType = Enums.ObjectType.DuplicatorLarge;
                if(objName.StartsWith("DuplicatorWild")) objectType = Enums.ObjectType.DuplicatorWild;
                if(objName.StartsWith("DuplicatorMilitary")) objectType = Enums.ObjectType.DuplicatorMilitary;
                
                Plugin._logger.LogWarning($"ShopTerminalBehavior registering {objectType}");
                
                if(objectType != null) Plugin.HandleInstancingNextTick(self.gameObject, new InstanceInfoTracker.InstanceOverrideInfo(objectType));
            }
        }
        else
        {
            orig(self);
        }
    }

    private void On_ShopTerminalBehavior_DropPickup(On.RoR2.ShopTerminalBehavior.orig_DropPickup orig, ShopTerminalBehavior self)
    {
        InstanceInfoTracker instanceInfoTracker = self.GetComponent<InstanceInfoTracker>();

        if (instanceInfoTracker != null)
        {
            PickupDropletHandler pickupDropletHandler = hookManager.GetHandler<PickupDropletHandler>();
            
            pickupDropletHandler.InstanceOverrideInfo = instanceInfoTracker.Info;
            orig(self);
            pickupDropletHandler.InstanceOverrideInfo = null;
        }
        else
        {
            orig(self);
        }
    }
}