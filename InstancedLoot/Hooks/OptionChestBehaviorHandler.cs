using InstancedLoot.Components;
using RoR2;
using UnityEngine.Networking;

namespace InstancedLoot.Hooks;

public class OptionChestBehaviorHandler : AbstractHookHandler
{
    public override void RegisterHooks()
    {
        On.RoR2.OptionChestBehavior.ItemDrop += On_OptionChestBehavior_ItemDrop;
        On.RoR2.OptionChestBehavior.Start += On_OptionChestBehavior_Start;
    }

    public override void UnregisterHooks()
    {
        On.RoR2.OptionChestBehavior.ItemDrop -= On_OptionChestBehavior_ItemDrop;
        On.RoR2.OptionChestBehavior.Start -= On_OptionChestBehavior_Start;
    }

    private void On_OptionChestBehavior_ItemDrop(On.RoR2.OptionChestBehavior.orig_ItemDrop orig, OptionChestBehavior self)
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

    private void On_OptionChestBehavior_Start(On.RoR2.OptionChestBehavior.orig_Start orig, OptionChestBehavior self)
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
                
                if(objName.StartsWith("LockboxVoid")) objectType = Enums.ObjectType.LockboxVoid;
                if(objName.StartsWith("VoidTriple")) objectType = Enums.ObjectType.VoidTriple;
                
                if(objectType != null) Plugin.HandleInstancing(self.gameObject, new InstanceInfoTracker.InstanceOverrideInfo(objectType));
            }
        }
        else
        {
            orig(self);
        }
    }
}