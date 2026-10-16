using RoR2;
using UnityEngine;

namespace InstancedLoot.Components;

public static class InstanceAccessPolicy
{
    public static PlayerCharacterMasterController ResolvePlayer(Interactor interactor) =>
        interactor ? ResolvePlayer(interactor.GetComponent<CharacterBody>()) : null;
    public static PlayerCharacterMasterController ResolvePlayer(CharacterBody body) =>
        body && body.master ? body.master.playerCharacterMasterController : null;
    public static bool CanInteract(InstanceHandler handler, PlayerCharacterMasterController player) =>
        handler && handler.IsInstancedFor(player);
    public static bool CanInteract(InstanceHandler handler, Interactor interactor) => CanInteract(handler, ResolvePlayer(interactor));
    public static bool CanInteract(GameObject target, Interactor interactor)
    {
        if (PendingOwnership.IsPending(target)) return false;
        var handler = target ? target.GetComponent<InstanceHandler>() : null;
        return !handler || CanInteract(handler, interactor);
    }
}
