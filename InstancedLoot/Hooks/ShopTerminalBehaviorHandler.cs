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
                string objectType = null;
                
                if(objName.StartsWith("TripleShop")) objectType = Enums.ObjectType.TripleShop;
                if(objName.StartsWith("TripleShopLarge")) objectType = Enums.ObjectType.TripleShopLarge;
                if(objName.StartsWith("TripleShopEquipment")) objectType = Enums.ObjectType.TripleShopEquipment;
                
                if(objName.StartsWith("Duplicator")) objectType = Enums.ObjectType.Duplicator;
                if(objName.StartsWith("DuplicatorLarge")) objectType = Enums.ObjectType.DuplicatorLarge;
                if(objName.StartsWith("DuplicatorWild")) objectType = Enums.ObjectType.DuplicatorWild;
                if(objName.StartsWith("DuplicatorMilitary")) objectType = Enums.ObjectType.DuplicatorMilitary;
                
                Plugin._logger.LogWarning($"ShopTerminalBehavior registering {objectType}");
                
                if(objectType != null) Plugin.HandleInstancingNextTick(self.gameObject, new InstanceInfoTracker.InstanceOverrideInfo(objectType));
            }
        }
        else
        {
            orig(self);
        }
    }
}