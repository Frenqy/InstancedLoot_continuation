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
            if (Plugin.ObjectHandlerManager.HandleAwaitedObject(self.gameObject))
                return;
            
            InstanceHandler instanceHandler = self.GetComponent<InstanceHandler>();

            if (instanceHandler == null)
            {
                orig(self);

                string objName = self.name;
                string objectType = null;

                if (objName.StartsWith("ShrineRestack")) objectType = ObjectType.ShrineRestack;

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