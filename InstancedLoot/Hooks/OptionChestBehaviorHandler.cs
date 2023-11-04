using System.Linq;
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
            Plugin._logger.LogWarning($"OptionChestBehavior dropping {instanceInfoTracker.ObjectType}");
            hookManager.GetHandler<PickupDropletControllerHandler>().InstanceOverrideInfo = instanceInfoTracker.Info;
            orig(self);
            hookManager.GetHandler<PickupDropletControllerHandler>().InstanceOverrideInfo = null;
        }
        else
        {
            orig(self);
        }
    }

    private void On_OptionChestBehavior_Start(On.RoR2.OptionChestBehavior.orig_Start orig, OptionChestBehavior self)
    {
        InstanceHandler instanceHandler = self.GetComponent<InstanceHandler>();
        if (instanceHandler == null)
        {
            orig(self);

            if (NetworkServer.active)
            {
                string objName = self.name;
                string objectType = null;
                
                if(objName.StartsWith("LockboxVoid")) objectType = Enums.ObjectType.LockboxVoid;
                if(objName.StartsWith("VoidTriple")) objectType = Enums.ObjectType.VoidTriple;
                
                Plugin._logger.LogWarning($"OptionChestBehavior registering {objectType}");
                
                if(objectType != null) Plugin.HandleInstancingNextTick(self.gameObject, new InstanceInfoTracker.InstanceOverrideInfo(objectType));
            }
        }
        else
        {
            if (instanceHandler.SourceObject != null && NetworkServer.active)
            {
                Plugin._logger.LogInfo("Testing - Start called on OptionChest with InstanceHandler");

                OptionChestBehavior source = instanceHandler.SourceObject.GetComponent<OptionChestBehavior>();

                self.rng = new Xoroshiro128Plus(source.rng);
                self.generatedDrops = source.generatedDrops.ToArray();
            }
        }
    }
}