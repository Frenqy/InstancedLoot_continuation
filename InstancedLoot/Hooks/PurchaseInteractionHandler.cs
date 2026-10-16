using System.Collections.Generic;
using System.Linq;
using InstancedLoot.Components;
using InstancedLoot.Networking;
using RoR2;
using UnityEngine.Networking;

namespace InstancedLoot.Hooks;

public class PurchaseInteractionHandler : AbstractHookHandler
{
    private static readonly HashSet<object> PurchaseLocks = new();
    private int diagnosticCount;
    public override void RegisterHooks()
    {
        On.RoR2.PurchaseInteraction.GetInteractability += On_PurchaseInteraction_GetInteractability;
        On.RoR2.PurchaseInteraction.OnInteractionBegin += On_PurchaseInteraction_OnInteractionBegin;
    }

    public override void UnregisterHooks()
    {
        On.RoR2.PurchaseInteraction.GetInteractability -= On_PurchaseInteraction_GetInteractability;
        On.RoR2.PurchaseInteraction.OnInteractionBegin -= On_PurchaseInteraction_OnInteractionBegin;
        diagnosticCount = 0;
    }

    private Interactability On_PurchaseInteraction_GetInteractability(On.RoR2.PurchaseInteraction.orig_GetInteractability orig, PurchaseInteraction self, Interactor activator)
    {
        var interactability = orig(self, activator);

        bool allowed = CanInteract(self, activator);
        InteractionDiagnostics.Prompt(self, activator, interactability, allowed);
        if (!allowed)
        {
            interactability = Interactability.Disabled;
        }

        return interactability;
    }

    private void On_PurchaseInteraction_OnInteractionBegin(On.RoR2.PurchaseInteraction.orig_OnInteractionBegin orig, PurchaseInteraction self, Interactor activator)
    {
        // GetInteractability is also a client prompt check; enforce ownership and the
        // native availability before the server invokes PayCost or purchase events,
        // even for a direct interaction call. Unavailable can mean a shrine cooldown;
        // only the native purchase events determine whether an object stays closed.
        if (!CanInteract(self, activator) || NetworkServer.active && !self.available)
        {
            LogState(self, activator, "purchase-denied");
            return;
        }

        InstanceHandler instanceHandler = self.GetComponent<InstanceHandler>();
        PlayerCharacterMasterController buyer = GetPlayer(activator);
        MultiShopController controller = self.GetComponent<ShopTerminalBehavior>()?.serverMultiShopController;
        object lockKey = controller != null ? controller : (object)instanceHandler?.SharedInfo;
        bool lockTaken = false;

        if (NetworkServer.active && lockKey != null && !(lockTaken = PurchaseLocks.Add(lockKey)))
        {
            LogState(self, activator, "purchase-group-locked");
            return;
        }

        try
        {
            if (buyer)
                InstanceInfoTracker.InstanceOverrideInfo.SetOwner(self.gameObject, buyer);

            LogState(self, activator, "native-begin");
            orig(self, activator);
            // Returning also includes affordability/conditional rejection. Do not
            // infer a successful one-shot purchase from a temporary availability flip.
            LogState(self, activator, "native-return");
        }
        finally
        {
            if (lockTaken)
                PurchaseLocks.Remove(lockKey);
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

    private void LogState(PurchaseInteraction purchase, Interactor activator, string action)
    {
        InstancedLoot plugin = InstancedLoot.Instance;
        if (!purchase || plugin == null || !plugin.InteractionLoggingEnabled) return;
        if (diagnosticCount >= 512) return;
        if (++diagnosticCount == 512)
            plugin._logger.LogInfo("Purchase diagnostic limit reached (512 lines); restart the game to collect a new trace.");

        InstanceHandler handler = purchase.GetComponent<InstanceHandler>();
        ShopTerminalBehavior terminal = purchase.GetComponent<ShopTerminalBehavior>();
        string netId = purchase.GetComponent<NetworkIdentity>() is var identity && identity
            ? identity.netId.ToString()
            : "none";
        string sourceOrClone = handler == null ? "untracked" : handler.SourceObject == purchase.gameObject ? "source" : "clone";
        string sourceId = handler?.SourceObject?.GetComponent<NetworkIdentity>()?.netId.ToString() ?? "none";
        MultiShopController controller = terminal?.serverMultiShopController;
        string controllerId = controller?.GetComponent<NetworkIdentity>()?.netId.ToString() ?? "none";
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
            $"sourceNetId={sourceId} controllerNetId={controllerId} server={NetworkServer.active} pending={SyncInstances.IsPending(purchase.gameObject)} " +
            $"Players=[{players}] cost={purchase.cost} available={purchase.available} hidden={hidden} pickup={pickup} buyer={buyer}");
    }
}
