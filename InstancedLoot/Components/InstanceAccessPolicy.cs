using InstancedLoot.Enums;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace InstancedLoot.Components;

/// <summary>
/// Shared permission and purchase rules for every instanced interactable.
/// Keeping this decision in one place prevents CopyObject sources from using
/// the aggregate player set while clones use their owner, and keeps
/// InstancedObject consumers on their per-handler player set.
/// </summary>
public static class InstanceAccessPolicy
{
    public static PlayerCharacterMasterController ResolvePlayer(Interactor interactor)
    {
        if (!interactor)
            return null;

        CharacterBody body = interactor.GetComponent<CharacterBody>();
        return body && body.master ? body.master.playerCharacterMasterController : null;
    }

    public static PlayerCharacterMasterController ResolvePlayer(CharacterBody body)
    {
        return body && body.master ? body.master.playerCharacterMasterController : null;
    }

    public static bool CanInteract(InstanceHandler handler, PlayerCharacterMasterController player)
    {
        return handler && player && handler.IsInstancedFor(player);
    }

    public static bool CanInteract(InstanceHandler handler, Interactor interactor)
    {
        return CanInteract(handler, ResolvePlayer(interactor));
    }

    public static bool CanInteract(GameObject target, Interactor interactor)
    {
        InstanceHandler handler = target ? target.GetComponent<InstanceHandler>() : null;
        return handler == null || CanInteract(handler, interactor);
    }

    /// <summary>
    /// Consume a shared eligibility only after the native purchase reports its
    /// one-shot transition. CopyObject purchases are consumed by the native
    /// PurchaseInteraction on their individual copy; InstancedObject purchases
    /// remove only the player that completed a successful native purchase.
    /// </summary>
    public static bool ConsumeCompletedPurchase(InstanceHandler handler,
        PlayerCharacterMasterController player, bool wasAvailable, bool isAvailable)
    {
        if (!NetworkServer.active || !wasAvailable || isAvailable || !CanInteract(handler, player))
            return false;

        if (handler.ObjectInstanceMode != ObjectInstanceMode.InstancedObject)
            return false;

        handler.RemovePlayer(player);
        return true;
    }
}
