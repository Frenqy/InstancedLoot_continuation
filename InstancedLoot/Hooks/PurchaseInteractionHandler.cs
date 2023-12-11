
using System.Collections;
using System.Reflection;
using InstancedLoot.Components;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace InstancedLoot.Hooks;

public class PurchaseInteractionHandler : AbstractHookHandler
{
    public override void RegisterHooks()
    {
        On.RoR2.PurchaseInteraction.GetInteractability += On_PurchaseInteraction_GetInteractability;
        On.RoR2.PurchaseInteraction.OnInteractionBegin += On_PurchaseInteraction_OnInteractionBegin;
    }

    public override void UnregisterHooks()
    {
        On.RoR2.PurchaseInteraction.GetInteractability -= On_PurchaseInteraction_GetInteractability;
        On.RoR2.PurchaseInteraction.OnInteractionBegin -= On_PurchaseInteraction_OnInteractionBegin;
    }

    private Interactability On_PurchaseInteraction_GetInteractability(On.RoR2.PurchaseInteraction.orig_GetInteractability orig, PurchaseInteraction self, Interactor activator)
    {
        var interactability = orig(self, activator);

        var body = activator.GetComponent<CharacterBody>();
        if (body)
        {
            var player = body.master.GetComponent<PlayerCharacterMasterController>();
            var instanceHandler = self.GetComponent<InstanceHandler>();
            if (player && instanceHandler)
            {
                if (!instanceHandler.Players.Contains(player))
                    interactability = Interactability.Disabled;
            }
        }

        return interactability;
    }

    private void On_PurchaseInteraction_OnInteractionBegin(On.RoR2.PurchaseInteraction.orig_OnInteractionBegin orig, PurchaseInteraction self, Interactor activator)
    {
        CharacterBody body = activator.GetComponent<CharacterBody>();
        if (body != null)
        {
            CharacterMaster master = body.master;
            if (master != null)
            {
                PlayerCharacterMasterController player = master.playerCharacterMasterController;
                if (player)
                {
                    Plugin._logger.LogInfo($"Marking owner {player.GetDisplayName()} for {self}");
                    InstanceInfoTracker.InstanceOverrideInfo.SetOwner(self.gameObject, player);
                }
            }
        }

        orig(self, activator);
    }
}