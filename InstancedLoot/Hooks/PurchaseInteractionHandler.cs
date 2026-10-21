using System.Collections.Generic;
using InstancedLoot.Components;
using RoR2;
using UnityEngine.Networking;

namespace InstancedLoot.Hooks;

public sealed class PurchaseInteractionHandler : AbstractHookHandler
{
    private readonly HashSet<UnityEngine.Object> inProgress = new();
    public override void RegisterHooks()
    {
        On.RoR2.PurchaseInteraction.GetInteractability += GetInteractability;
        On.RoR2.PurchaseInteraction.OnInteractionBegin += Begin;
    }
    public override void UnregisterHooks()
    {
        On.RoR2.PurchaseInteraction.GetInteractability -= GetInteractability;
        On.RoR2.PurchaseInteraction.OnInteractionBegin -= Begin;
        inProgress.Clear();
    }
    private Interactability GetInteractability(On.RoR2.PurchaseInteraction.orig_GetInteractability orig, PurchaseInteraction self, Interactor actor) =>
        InstanceAccessPolicy.CanInteract(self.gameObject, actor) ? orig(self, actor) : Interactability.Disabled;
    private void Begin(On.RoR2.PurchaseInteraction.orig_OnInteractionBegin orig, PurchaseInteraction self, Interactor actor)
    {
        if (!NetworkServer.active) { orig(self, actor); return; }
        if (!self.GetComponent<InstanceHandler>() && !PendingOwnership.IsPending(self.gameObject))
        {
            orig(self, actor);
            return;
        }
        // Do not turn a cooldown/failed shrine roll into permanent consumption, or
        // force-close shop terminals: native OnPurchase handles Executive Card etc.
        var terminal = self.GetComponent<ShopTerminalBehavior>();
        UnityEngine.Object transaction = terminal && terminal.serverMultiShopController ? terminal.serverMultiShopController : self;
        if (!InstanceAccessPolicy.CanInteract(self.gameObject, actor) || !self.available || !inProgress.Add(transaction)) return;
        try { orig(self, actor); }
        finally { inProgress.Remove(transaction); }
    }
}
