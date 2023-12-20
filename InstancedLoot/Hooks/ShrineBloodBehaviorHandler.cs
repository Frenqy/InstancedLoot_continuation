using InstancedLoot.Components;
using InstancedLoot.Enums;
using RoR2;
using UnityEngine.Networking;

namespace InstancedLoot.Hooks;

public class ShrineBloodBehaviorHandler : AbstractHookHandler
{
    public override void RegisterHooks()
    {
        On.RoR2.ShrineBloodBehavior.Start += On_ShrineBloodBehavior_Start;
    }

    public override void UnregisterHooks()
    {
        On.RoR2.ShrineBloodBehavior.Start -= On_ShrineBloodBehavior_Start;
    }

    private void On_ShrineBloodBehavior_Start(On.RoR2.ShrineBloodBehavior.orig_Start orig, ShrineBloodBehavior self)
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

                if (objName.StartsWith("ShrineBlood")) objectType = Enums.ObjectType.ShrineBlood;

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