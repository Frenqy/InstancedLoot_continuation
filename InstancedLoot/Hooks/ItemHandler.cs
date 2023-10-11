using System;
using System.Linq;
using InstancedLoot.Enums;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace InstancedLoot.Hooks;

public class ItemHandler : AbstractHookHandler
{
    public override void RegisterHooks()
    {
        On.RoR2.GenericPickupController.Start += On_GenericPickupController_Start;
        On.RoR2.GenericPickupController.GetInteractability += On_GenericPickupController_GetInteractability;
        On.RoR2.GenericPickupController.OnTriggerStay += On_GenericPickupController_OnTriggerStay;
        IL.RoR2.GenericPickupController.AttemptGrant += IL_GenericPickupController_AttemptGrant;
    }

    public override void UnregisterHooks()
    {
        On.RoR2.GenericPickupController.Start -= On_GenericPickupController_Start;
        On.RoR2.GenericPickupController.GetInteractability -= On_GenericPickupController_GetInteractability;
        On.RoR2.GenericPickupController.OnTriggerStay -= On_GenericPickupController_OnTriggerStay;
        IL.RoR2.GenericPickupController.AttemptGrant -= IL_GenericPickupController_AttemptGrant;
    }

    private void On_GenericPickupController_Start(On.RoR2.GenericPickupController.orig_Start orig,
        RoR2.GenericPickupController self)
    {
        if (NetworkServer.active) 
            Plugin.MakeInstanced(self.gameObject);

        orig(self);
    }

    private Interactability On_GenericPickupController_GetInteractability(On.RoR2.GenericPickupController.orig_GetInteractability orig, GenericPickupController self, Interactor activator)
    {
        var interactability = orig(self, activator);

        var body = activator.GetComponent<CharacterBody>();
        if (body)
        {
            var player = body.master.GetComponent<PlayerCharacterMasterController>();
            var instanceTracker = self.GetComponent<InstanceTracker>();
            if (player && instanceTracker)
            {
                if (!instanceTracker.Players.Contains(player))
                    interactability = Interactability.ConditionsNotMet;
            }
        }

        return interactability;
    }

    private void On_GenericPickupController_OnTriggerStay(On.RoR2.GenericPickupController.orig_OnTriggerStay orig, GenericPickupController self, Collider other)
    {
        var interactor = other.GetComponent<Interactor>();

        if (interactor)
        {
            var interactability = self.GetInteractability(interactor);

            if (interactability != Interactability.Available) return;
        }

        orig(self, other);
    }

    private void IL_GenericPickupController_AttemptGrant(ILContext il)
    {
        var cursor = new ILCursor(il);

        cursor.GotoNext(MoveType.After, i => i.MatchLdfld<RoR2.PickupDef.GrantContext>("shouldDestroy"));
        cursor.Emit(OpCodes.Ldarg_0);
        cursor.Emit(OpCodes.Ldarg_1);
        cursor.EmitDelegate<Func<bool, GenericPickupController, CharacterBody, bool>>((shouldDestroy, self, body) =>
        {
            var player = body.master.GetComponent<PlayerCharacterMasterController>();
            var instanceTracker = self.GetComponent<InstanceTracker>();
            if (player && instanceTracker)
            {
                Plugin._logger.LogWarning($"Granting instanced item to {player}");
                instanceTracker.RemovePlayer(player);
                if(instanceTracker.Players.Count > 0)
                    shouldDestroy = false;
            }
            
            return shouldDestroy;
        });
    }
}