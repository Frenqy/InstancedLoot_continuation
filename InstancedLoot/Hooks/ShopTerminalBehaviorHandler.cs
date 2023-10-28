using InstancedLoot.Components;
using RoR2;
using UnityEngine.Networking;

namespace InstancedLoot.Hooks;

public class ShopTerminalBehaviorHandler : AbstractHookHandler
{
    public override void RegisterHooks()
    {
        On.RoR2.ShopTerminalBehavior.Start += On_ShopTerminalBehavior_Start;
    }

    public override void UnregisterHooks()
    {
        On.RoR2.ShopTerminalBehavior.Start -= On_ShopTerminalBehavior_Start;
    }

    private void On_ShopTerminalBehavior_Start(On.RoR2.ShopTerminalBehavior.orig_Start orig, ShopTerminalBehavior self)
    {
        if (NetworkServer.active)
        {
            InstanceHandler instanceHandler = self.GetComponent<InstanceHandler>();

            if (instanceHandler != null)
            {
                ShopTerminalBehavior source = instanceHandler.SourceObject.GetComponent<ShopTerminalBehavior>();
                
                self.hasStarted = true;
                self.rng = new Xoroshiro128Plus(source.rng);

                if (self.selfGeneratePickup)
                {
                    self.NetworkpickupIndex = source.NetworkpickupIndex;
                    self.Networkhidden = source.Networkhidden;
                }
            }
            else
            {
                orig(self);
                
                string objName = self.name;
                string source = null;
                
                //TODO: Get the actual names lol
                if(objName.StartsWith("TripleShop")) source = Enums.ItemSource.TripleShop;
                if(objName.StartsWith("TripleShopLarge")) source = Enums.ItemSource.TripleShopLarge;
                if(objName.StartsWith("TripleShopEquipment")) source = Enums.ItemSource.TripleShopEquipment;
                
                Plugin._logger.LogWarning($"ShopTerminalBehavior registering {source}");
                
                if(source != null) Plugin.HandleInstancingNextTick(self.gameObject, new InstanceInfoTracker.InstanceOverrideInfo(source));
            }
        }
        else
        {
            orig(self);
        }
    }
}