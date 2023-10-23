using System.Reflection;
using InstancedLoot.Components;
using MonoMod.Cil;
using RoR2;
using UnityEngine.Networking;

namespace InstancedLoot.Hooks;

public class ShrineChanceBehaviorHandler : AbstractHookHandler
{
    protected FieldInfo Field_ShrineChanceBehavior_rng =
        typeof(ShrineChanceBehavior).GetField("rng", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
    
    public override void RegisterHooks()
    {
        On.RoR2.ShrineChanceBehavior.AddShrineStack += On_ShrineChanceBehavior_AddShrineStack;
        // IL.RoR2.ShrineChanceBehavior.AddShrineStack += IL_ShrineChanceBehavior_AddShrineStack;
        On.RoR2.ShrineChanceBehavior.Start += On_ShrineChanceBehavior_Start;
    }

    public override void UnregisterHooks()
    {
        On.RoR2.ShrineChanceBehavior.AddShrineStack -= On_ShrineChanceBehavior_AddShrineStack;
        // IL.RoR2.ShrineChanceBehavior.AddShrineStack -= IL_ShrineChanceBehavior_AddShrineStack;
        On.RoR2.ShrineChanceBehavior.Start -= On_ShrineChanceBehavior_Start;
    }

    // private void IL_ShrineChanceBehavior_AddShrineStack(ILContext il)
    // {
    //     ILCursor cursor = new ILCursor(il);
    // }

    private void On_ShrineChanceBehavior_AddShrineStack(On.RoR2.ShrineChanceBehavior.orig_AddShrineStack orig, ShrineChanceBehavior self, Interactor activator)
    {
        if (self.GetComponent<InstanceInfoTracker>() is var instanceInfoTracker && instanceInfoTracker != null)
        {
            Plugin._logger.LogWarning($"ShrineChanceBehavior dropping {instanceInfoTracker.ItemSource}");
            hookManager.GetHandler<PickupDropletHandler>().InstanceOverrideInfo = instanceInfoTracker.Info;
            orig(self, activator);
            hookManager.GetHandler<PickupDropletHandler>().InstanceOverrideInfo = null;
        }
        else
        {
            orig(self, activator);
        }
    }

    private void On_ShrineChanceBehavior_Start(On.RoR2.ShrineChanceBehavior.orig_Start orig, ShrineChanceBehavior self)
    {
        InstanceHandler instanceHandler = self.GetComponent<InstanceHandler>();
        if (instanceHandler == null)
        {
            orig(self);

            if (NetworkServer.active)
            {
                string objName = self.name;
                string source = null;
                
                if(objName.StartsWith("ShrineChance")) source = Enums.ItemSource.ShrineChance;
                
                Plugin._logger.LogWarning($"ShrineChanceBehavior registering {source}");
                
                if(source != null) Plugin.HandleInstancingNextTick(self.gameObject, new InstanceInfoTracker.InstanceOverrideInfo(source));
            }
        }
        else
        {
            if (instanceHandler.SourceObject != null && NetworkServer.active)
            {
                Plugin._logger.LogInfo("Testing - Start called on ShrineChance with InstanceHandler");

                ShrineChanceBehavior source = instanceHandler.SourceObject.GetComponent<ShrineChanceBehavior>();
                
                Field_ShrineChanceBehavior_rng.SetValue(self, Field_ShrineChanceBehavior_rng.GetValue(source));
            }

        }
    }
    
}