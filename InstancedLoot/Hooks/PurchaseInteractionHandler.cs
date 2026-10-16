using System.Collections.Generic;
using System.Linq;
using InstancedLoot.Components;
using InstancedLoot.Networking;
using RoR2;
using UnityEngine.Networking;

namespace InstancedLoot.Hooks;

public class PurchaseInteractionHandler : AbstractHookHandler
{
    private static readonly HashSet<InstanceHandler.SharedInstanceInfo> PurchaseLocks = new();
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
        {
            LogState(self, activator, "interactability-denied");
            interactability = Interactability.Disabled;
        }

        return interactability;
    }

    private void On_PurchaseInteraction_OnInteractionBegin(On.RoR2.PurchaseInteraction.orig_OnInteractionBegin orig, PurchaseInteraction self, Interactor activator)
    {
        // GetInteractability is also a client prompt check; enforce ownership and the
        // native one-shot availability before the server invokes PayCost or purchase
        // events, even for a direct interaction call. The availability check closes a
        // small race where two server-side callers enter the callback before the
        // terminal/chest has hidden itself.
        if (!CanInteract(self, activator) || NetworkServer.active && !self.available)
        {
            LogState(self, activator, "purchase-denied");
            return;
        }

        InstanceHandler instanceHandler = self.GetComponent<InstanceHandler>();
        PlayerCharacterMasterController buyer = GetPlayer(activator);
        InstanceHandler.SharedInstanceInfo sharedInfo = instanceHandler?.SharedInfo;

        if (NetworkServer.active && sharedInfo != null && !PurchaseLocks.Add(sharedInfo))
        {
            LogState(self, activator, "purchase-group-locked");
            return;
        }

        try
        {
            if (buyer)
                InstanceInfoTracker.InstanceOverrideInfo.SetOwner(self.gameObject, buyer);

            bool wasAvailable = self.available;
            orig(self, activator);

            // Native purchase code closes the interacted object after a successful
            // payment. Mirror that one-shot state to every object in this group that
            // belongs to the same buyer, while retaining legal copies for others.
            if (NetworkServer.active && wasAvailable && !self.available && instanceHandler != null && buyer)
                ClosePurchasedGroup(instanceHandler, buyer);

            LogState(self, activator, "purchase-accepted");
        }
        finally
        {
            if (sharedInfo != null)
                PurchaseLocks.Remove(sharedInfo);
        }
    }

    private static bool CanInteract(PurchaseInteraction purchase, Interactor activator)
    {
        var instanceHandler = purchase.GetComponent<InstanceHandler>();
        if (!instanceHandler)
        {
            // A client can receive the object before its SyncInstances snapshot has
            // resolved all source/clone references. Keep the native prompt disabled
            // for that short window instead of authorizing an unknown owner.
            return NetworkServer.active || !SyncInstances.IsPending(purchase.gameObject);
        }

        var player = GetPlayer(activator);
        return player && instanceHandler.IsInstancedFor(player);
    }

    private static PlayerCharacterMasterController GetPlayer(Interactor activator)
    {
        var body = activator ? activator.GetComponent<CharacterBody>() : null;
        return body && body.master ? body.master.playerCharacterMasterController : null;
    }

    private static void ClosePurchasedGroup(InstanceHandler instanceHandler, PlayerCharacterMasterController buyer)
    {
        HashSet<InstanceHandler> handlers = new(instanceHandler.LinkedHandlers ?? new List<InstanceHandler> { instanceHandler });
        ShopTerminalBehavior purchasedTerminal = instanceHandler.GetComponent<ShopTerminalBehavior>();
        MultiShopController controller = purchasedTerminal?.serverMultiShopController;
        if (controller != null)
        {
            foreach (var terminalObject in controller.terminalGameObjects ?? System.Array.Empty<UnityEngine.GameObject>())
            {
                InstanceHandler terminalHandler = terminalObject?.GetComponent<InstanceHandler>();
                if (terminalHandler != null)
                    handlers.Add(terminalHandler);
            }
        }

        foreach (InstanceHandler linkedHandler in handlers)
        {
            if (!linkedHandler || !linkedHandler.IsInstancedFor(buyer)) continue;

            PurchaseInteraction linkedPurchase = linkedHandler.GetComponent<PurchaseInteraction>();
            if (linkedPurchase != null)
                linkedPurchase.available = false;

            ShopTerminalBehavior terminal = linkedHandler.GetComponent<ShopTerminalBehavior>();
            if (terminal != null)
            {
                terminal.Networkhidden = true;
                terminal.UpdatePickupDisplayAndAnimations();
            }
        }
    }

    private void LogState(PurchaseInteraction purchase, Interactor activator, string action)
    {
        InstancedLoot plugin = InstancedLoot.Instance;
        if (plugin == null || !plugin.InteractionLoggingEnabled) return;

        InstanceHandler handler = purchase.GetComponent<InstanceHandler>();
        ShopTerminalBehavior terminal = purchase.GetComponent<ShopTerminalBehavior>();
        string netId = purchase.GetComponent<NetworkIdentity>() is var identity && identity
            ? identity.netId.ToString()
            : "none";
        string sourceOrClone = handler?.SourceObject == purchase.gameObject ? "source" : "clone";
        string origPlayer = handler?.OrigPlayer?.GetComponent<NetworkIdentity>()?.netId.ToString() ?? "none";
        string players = handler == null
            ? "none"
            : string.Join(",", handler.Players.Select(player =>
                player?.GetComponent<NetworkIdentity>()?.netId.ToString() ?? "none"));
        string buyer = GetPlayer(activator)?.GetComponent<NetworkIdentity>()?.netId.ToString() ?? "none";
        string hidden = terminal == null ? "n/a" : terminal.Networkhidden.ToString();
        string pickup = terminal == null
            ? "n/a"
            : terminal.Networkpickup is object pickupValue ? pickupValue.ToString() : "none";
        plugin._logger.LogInfo(
            $"Purchase {action}: netId={netId} name={purchase.gameObject.name} kind={sourceOrClone} OrigPlayer={origPlayer} " +
            $"Players=[{players}] available={purchase.available} hidden={hidden} pickup={pickup} buyer={buyer}");
    }
}
