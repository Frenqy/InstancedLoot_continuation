using System.Reflection;
using InstancedLoot.Components;
using MonoMod.Cil;
using RoR2;
using UnityEngine.Networking;

namespace InstancedLoot.Hooks;

public class RouletteChestControllerHandler : AbstractHookHandler
{
    protected FieldInfo Field_RouletteChestController_rng =
        typeof(RouletteChestController).GetField("rng", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
    
    public override void RegisterHooks()
    {
        On.RoR2.RouletteChestController.EjectPickupServer += On_RouletteChestController_EjectPickupServer;
        // IL.RoR2.ShrineChanceBehavior.AddShrineStack += IL_ShrineChanceBehavior_AddShrineStack;
        On.RoR2.RouletteChestController.Start += On_RouletteChestController_Start;
    }

    public override void UnregisterHooks()
    {
        On.RoR2.RouletteChestController.EjectPickupServer -= On_RouletteChestController_EjectPickupServer;
        // IL.RoR2.ShrineChanceBehavior.AddShrineStack -= IL_ShrineChanceBehavior_AddShrineStack;
        On.RoR2.RouletteChestController.Start -= On_RouletteChestController_Start;
    }

    // private void IL_ShrineChanceBehavior_AddShrineStack(ILContext il)
    // {
    //     ILCursor cursor = new ILCursor(il);
    // }

    private void On_RouletteChestController_EjectPickupServer(On.RoR2.RouletteChestController.orig_EjectPickupServer orig, RouletteChestController self, PickupIndex pickupIndex)
    {
        if (self.GetComponent<InstanceInfoTracker>() is var instanceInfoTracker && instanceInfoTracker != null)
        {
            Plugin._logger.LogWarning($"RouletteChestController dropping {instanceInfoTracker.ItemSource}");
            hookManager.GetHandler<PickupDropletHandler>().InstanceOverrideInfo = instanceInfoTracker.Info;
            orig(self, pickupIndex);
            hookManager.GetHandler<PickupDropletHandler>().InstanceOverrideInfo = null;
        }
        else
        {
            orig(self, pickupIndex);
        }
    }

    private void On_RouletteChestController_Start(On.RoR2.RouletteChestController.orig_Start orig, RouletteChestController self)
    {
        InstanceHandler instanceHandler = self.GetComponent<InstanceHandler>();
        if (instanceHandler == null)
        {
            orig(self);

            if (NetworkServer.active)
            {
                string objName = self.name;
                string source = null;
                
                if(objName.StartsWith("CasinoChest")) source = Enums.ItemSource.CasinoChest;
                
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
                
                Field_RouletteChestController_rng.SetValue(self, Field_RouletteChestController_rng.GetValue(source));
            }

        }
    }
    
}