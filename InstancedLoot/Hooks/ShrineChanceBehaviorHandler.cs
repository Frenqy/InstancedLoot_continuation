using System.Reflection;
using InstancedLoot.Components;
using MonoMod.Cil;
using RoR2;
using UnityEngine.Networking;

namespace InstancedLoot.Hooks;

public class ShrineChanceBehaviorHandler : AbstractHookHandler
{
    public override void RegisterHooks()
    {
        On.RoR2.ShrineChanceBehavior.AddShrineStack += On_ShrineChanceBehavior_AddShrineStack;
        On.RoR2.ShrineChanceBehavior.Start += On_ShrineChanceBehavior_Start;
    }

    public override void UnregisterHooks()
    {
        On.RoR2.ShrineChanceBehavior.AddShrineStack -= On_ShrineChanceBehavior_AddShrineStack;
        On.RoR2.ShrineChanceBehavior.Start -= On_ShrineChanceBehavior_Start;
    }

    private void On_ShrineChanceBehavior_AddShrineStack(On.RoR2.ShrineChanceBehavior.orig_AddShrineStack orig, ShrineChanceBehavior self, Interactor activator)
    {
        if (self.GetComponent<InstanceInfoTracker>() is var instanceInfoTracker && instanceInfoTracker != null)
        {
            Plugin._logger.LogWarning($"ShrineChanceBehavior dropping {instanceInfoTracker.ObjectType}");
            hookManager.GetHandler<PickupDropletControllerHandler>().InstanceOverrideInfo = instanceInfoTracker.Info;
            orig(self, activator);
            hookManager.GetHandler<PickupDropletControllerHandler>().InstanceOverrideInfo = null;
        }
        else
        {
            orig(self, activator);
        }
    }

    private void On_ShrineChanceBehavior_Start(On.RoR2.ShrineChanceBehavior.orig_Start orig, ShrineChanceBehavior self)
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

                if (objName.StartsWith("ShrineChance")) objectType = Enums.ObjectType.ShrineChance;

                Plugin._logger.LogWarning($"ShrineChanceBehavior registering {objectType}");

                if (objectType != null)
                    Plugin.HandleInstancingNextTick(self.gameObject,
                        new InstanceInfoTracker.InstanceOverrideInfo(objectType));
            }
        }
        else
        {
            orig(self);
        }
    }
    
}