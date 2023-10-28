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
        IL.RoR2.MultiShopController.CreateTerminals += IL_MultiShopController_CreateTerminals;
    }

    public override void UnregisterHooks()
    {
        On.RoR2.MultiShopController.Start -= On_MultiShopController_Start;
        IL.RoR2.MultiShopController.CreateTerminals -= IL_MultiShopController_CreateTerminals;
    }

    private void On_MultiShopController_Start(On.RoR2.MultiShopController.orig_Start orig, MultiShopController self)
    {
        if (NetworkServer.active)
        {
            InstanceHandler instanceHandler = self.GetComponent<InstanceHandler>();

            if (instanceHandler != null)
            {
                MultiShopController source = instanceHandler.SourceObject.GetComponent<MultiShopController>();
                
                //Temporary RNG to create terminals
                self.rng = new Xoroshiro128Plus(0);
                
                self.CreateTerminals();
                
                self.Networkcost = source.Networkcost;
                self.rng = new Xoroshiro128Plus(source.rng);
            }
            else
            {
                orig(self);
                
                string objName = self.name;
                string source = null;
                
                if(objName.StartsWith("TripleShop")) source = Enums.ItemSource.TripleShop;
                // if(objName.StartsWith("TripleShopLarge")) source = Enums.ItemSource.TripleShopLarge;
                if(objName.StartsWith("TripleShopEquipment")) source = Enums.ItemSource.TripleShopEquipment;
                if(objName.StartsWith("FreeChestMultiShop")) source = Enums.ItemSource.FreeChestMultiShop;
                
                Plugin._logger.LogWarning($"MultiShopController registering {source}");
                
                if(source != null) Plugin.HandleInstancingNextTick(self.gameObject, new InstanceInfoTracker.InstanceOverrideInfo(source));
            }
        }
        else
        {
            orig(self);
        }
        
    }

    private void IL_MultiShopController_CreateTerminals(ILContext il)
    {
        ILCursor cursor = new ILCursor(il);

        ILLabel labelSkipSettingItem = cursor.DefineLabel();
        int varTerminalIndex = -1;
        int varShopTerminalBehavior = -1;

        cursor.GotoNext(i => i.MatchLdfld<MultiShopController>("doCloseOnTerminalPurchase"),
            i => i.MatchLdloc(out varTerminalIndex));

        cursor.GotoNext(MoveType.Before, i => i.MatchLdloc(out varShopTerminalBehavior),
            i => i.MatchLdfld<ShopTerminalBehavior>("selfGeneratePickup"));

        cursor.Emit(OpCodes.Ldarg_0);
        cursor.Emit(OpCodes.Ldloc, varTerminalIndex);
        cursor.Emit(OpCodes.Ldloc, varShopTerminalBehavior);
        cursor.EmitDelegate<Func<MultiShopController, int, ShopTerminalBehavior, bool>>((self, terminalIndex, targetTerminal) =>
        {
            bool skipInit = false;
            
            InstanceHandler targetShopHandler = self.GetComponent<InstanceHandler>();
            InstanceInfoTracker instanceInfoTracker = self.GetComponent<InstanceInfoTracker>();
            
            if (targetTerminal.selfGeneratePickup && targetShopHandler != null && instanceInfoTracker != null)
            {
                skipInit = true;
                
                MultiShopController sourceShop = targetShopHandler.SourceObject.GetComponent<MultiShopController>();
                ShopTerminalBehavior sourceTerminal =
                    sourceShop._terminalGameObjects[terminalIndex].GetComponent<ShopTerminalBehavior>();
                
                Plugin.ObjectHandlerManager.InstanceSingleObject(instanceInfoTracker.ItemSource, sourceTerminal.gameObject, targetTerminal.gameObject, targetShopHandler.Players.ToArray());
                
                targetTerminal.rng = new Xoroshiro128Plus(sourceTerminal.rng);
                targetTerminal.NetworkpickupIndex = sourceTerminal.NetworkpickupIndex;
                targetTerminal.Networkhidden = sourceTerminal.Networkhidden;
            }
            
            return skipInit;
        });
        cursor.Emit(OpCodes.Brtrue, labelSkipSettingItem);

        cursor.GotoNext(MoveType.Before, i => i.MatchLdloc(out _), i => i.MatchCallOrCallvirt<NetworkServer>("Spawn"));
        // cursor.GotoNext(MoveType.AfterLabel, i => i.MatchCallOrCallvirt<ShopTerminalBehavior>("SetHidden"));
        cursor.MarkLabel(labelSkipSettingItem);

    }
}