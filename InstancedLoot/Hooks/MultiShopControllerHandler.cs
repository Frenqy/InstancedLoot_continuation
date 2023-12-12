using System;
using System.Linq;
using InstancedLoot.Components;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using UnityEngine.Networking;

namespace InstancedLoot.Hooks;

public class MultiShopControllerHandler : AbstractHookHandler
{
    public override void RegisterHooks()
    {
        On.RoR2.MultiShopController.Start += On_MultiShopController_Start;
    }

    public override void UnregisterHooks()
    {
        On.RoR2.MultiShopController.Start -= On_MultiShopController_Start;
    }

    private void On_MultiShopController_Start(On.RoR2.MultiShopController.orig_Start orig, MultiShopController self)
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

                if (objName.StartsWith("TripleShop")) objectType = Enums.ObjectType.TripleShop;
                if (objName.StartsWith("TripleShopLarge")) objectType = Enums.ObjectType.TripleShopLarge;
                if (objName.StartsWith("TripleShopEquipment")) objectType = Enums.ObjectType.TripleShopEquipment;
                if (objName.StartsWith("FreeChestMultiShop")) objectType = Enums.ObjectType.FreeChestMultiShop;

                Plugin._logger.LogWarning($"MultiShopController registering {objectType}");

                if (objectType != null)
                    Plugin.HandleInstancing(self.gameObject, new InstanceInfoTracker.InstanceOverrideInfo(objectType));
            }
        }
        else
        {
            orig(self);
        }
    }
}