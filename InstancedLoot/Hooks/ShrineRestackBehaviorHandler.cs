using InstancedLoot.Components;
using InstancedLoot.Enums;
using RoR2;
using UnityEngine.Networking;

namespace InstancedLoot.Hooks;

public class ShrineRestackBehaviorHandler : AbstractHookHandler
{
    public override void RegisterHooks()
    {
        On.RoR2.ShrineRestackBehavior.Start += On_ShrineRestackBehavior_Start;
    }

    public override void UnregisterHooks()
    {
        On.RoR2.ShrineRestackBehavior.Start -= On_ShrineRestackBehavior_Start;
    }

    private void On_ShrineRestackBehavior_Start(On.RoR2.ShrineRestackBehavior.orig_Start orig, ShrineRestackBehavior self)
    {
        if (NetworkServer.active)
        {
            InstanceHandler instanceHandler = self.GetComponent<InstanceHandler>();

            if (instanceHandler != null && instanceHandler.SourceObject != null && instanceHandler.SourceObject != self.gameObject)
            {
                Plugin._logger.LogInfo("Testing - Start called on Chest with InstanceHandler");

                ShrineRestackBehavior source = instanceHandler.SourceObject.GetComponent<ShrineRestackBehavior>();

                self.rng = new Xoroshiro128Plus(source.rng);
            }
            else if(instanceHandler == null)
            {
                orig(self);
                
                Plugin.HandleInstancing(self.gameObject,
                    new InstanceInfoTracker.InstanceOverrideInfo(ObjectType.ShrineRestack));
            }
        }
        else
        {
            orig(self);
        }
    }
}