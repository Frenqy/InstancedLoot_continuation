using RoR2;
using UnityEngine.Networking;

namespace InstancedLoot.Hooks;

public class ChestBehaviorHandler : AbstractHookHandler
{
    public override void RegisterHooks()
    {
        On.RoR2.ChestBehavior.Awake += On_ChestBehavior_Awake;
        On.RoR2.ChestBehavior.ItemDrop += On_ChestBehavior_ItemDrop;
    }

    public override void UnregisterHooks()
    {
        On.RoR2.ChestBehavior.Awake -= On_ChestBehavior_Awake;
        On.RoR2.ChestBehavior.ItemDrop -= On_ChestBehavior_ItemDrop;
    }

    private void On_ChestBehavior_Awake(On.RoR2.ChestBehavior.orig_Awake orig, ChestBehavior self)
    {
        orig(self);

        if (NetworkServer.active)
        {
            string objName = self.name;
            string source = null;
            
            if(objName.StartsWith("Chest1")) source = Enums.ItemSource.Chest1;
            if(objName.StartsWith("Chest2")) source = Enums.ItemSource.Chest2;
            // if(objName.StartsWith("GoldChest")) source = Enums.ItemSource.GoldChest;
            
            Plugin._logger.LogWarning($"ChestBehavior registering {source}");
            
            if(source != null) Plugin.HandleInstancing(self.gameObject, new InstanceInfoTracker.InstanceOverrideInfo(source));
        }
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
}