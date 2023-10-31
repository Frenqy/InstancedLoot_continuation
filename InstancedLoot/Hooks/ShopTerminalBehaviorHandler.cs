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
                
                if(objName.StartsWith("TripleShop")) source = Enums.ObjectType.TripleShop;
                if(objName.StartsWith("TripleShopLarge")) source = Enums.ObjectType.TripleShopLarge;
                if(objName.StartsWith("TripleShopEquipment")) source = Enums.ObjectType.TripleShopEquipment;
                
                if(objName.StartsWith("Duplicator")) source = Enums.ObjectType.Duplicator;
                if(objName.StartsWith("DuplicatorLarge")) source = Enums.ObjectType.DuplicatorLarge;
                if(objName.StartsWith("DuplicatorWild")) source = Enums.ObjectType.DuplicatorWild;
                if(objName.StartsWith("DuplicatorMilitary")) source = Enums.ObjectType.DuplicatorMilitary;
                
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