using InstancedLoot.Components;
using RoR2;
using UnityEngine.Networking;

namespace InstancedLoot.Hooks;

public class PurchaseInteractionHandler : AbstractHookHandler
{
    private readonly System.Collections.Generic.HashSet<PurchaseInteraction> purchasesInProgress = new();

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
        // GetInteractability is also a client prompt check; enforce ownership and the
        // native one-shot availability before the server invokes PayCost or purchase
        // events, even for a direct interaction call. The availability check closes a
        // small race where two server-side callers enter the callback before the
        // terminal/chest has hidden itself.
        if (NetworkServer.active && (!self.available || !CanInteract(self, activator)))
            return;

        if (NetworkServer.active && !purchasesInProgress.Add(self))
            return;

        try
        {
            bool wasAvailable = self.available;
            PlayerCharacterMasterController player = InstanceAccessPolicy.ResolvePlayer(activator);
            if (player)
                InstanceInfoTracker.InstanceOverrideInfo.SetOwner(self.gameObject, player);

            orig(self, activator);

            InstanceAccessPolicy.ConsumeCompletedPurchase(
                self.GetComponent<InstanceHandler>(), player, wasAvailable, self.available);
        }
        finally
        {
            purchasesInProgress.Remove(self);
        }
    }

    private static bool CanInteract(PurchaseInteraction purchase, Interactor activator)
    {
        var instanceHandler = purchase.GetComponent<InstanceHandler>();
        if (!instanceHandler) return true;
        return InstanceAccessPolicy.CanInteract(instanceHandler, activator);
    }
}
