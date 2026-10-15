using InstancedLoot.Components;
using RoR2;
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

        if (!CanInteract(self, activator))
            interactability = Interactability.Disabled;

        return interactability;
    }

    private void On_PurchaseInteraction_OnInteractionBegin(On.RoR2.PurchaseInteraction.orig_OnInteractionBegin orig, PurchaseInteraction self, Interactor activator)
    {
        // GetInteractability is also a client prompt check; enforce ownership before the
        // server invokes PayCost or any purchase events, even for a direct interaction call.
        if (NetworkServer.active && !CanInteract(self, activator))
            return;

        if(activator.GetComponent<CharacterBody>() is var characterBody && characterBody
           && characterBody.master is var master && master
           && master.playerCharacterMasterController is var player && player)
            InstanceInfoTracker.InstanceOverrideInfo.SetOwner(self.gameObject, player);

        orig(self, activator);
    }

    private static bool CanInteract(PurchaseInteraction purchase, Interactor activator)
    {
        var instanceHandler = purchase.GetComponent<InstanceHandler>();
        if (!instanceHandler) return true;

        var body = activator ? activator.GetComponent<CharacterBody>() : null;
        var player = body && body.master ? body.master.playerCharacterMasterController : null;
        return player && instanceHandler.Players.Contains(player);
    }
}
