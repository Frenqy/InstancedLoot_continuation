using System.Linq;
using InstancedLoot.Components;
using RoR2;
using UnityEngine;

namespace InstancedLoot;

public static class Utils
{
    public static bool IsObjectInteractibleForPlayer(GameObject target, PlayerCharacterMasterController player)
    {
        if (!target || !player) return true;
        if (PendingOwnership.IsPending(target)) return false;
        var instance = target.GetComponent<InstanceHandler>();
        if (!instance) return true;
        return instance.LinkedHandlers.Where(handler => handler.IsInstancedFor(player)).Any(handler =>
        {
            var purchase = handler.GetComponent<PurchaseInteraction>();
            var actor = player.body ? player.body.GetComponent<Interactor>() : null;
            return !purchase || !actor || purchase.GetInteractability(actor) != Interactability.Disabled;
        });
    }
    public static bool IsObjectInstanceInteractibleForPlayer(GameObject target, PlayerCharacterMasterController player)
    {
        if (PendingOwnership.IsPending(target)) return false;
        var instance = target ? target.GetComponent<InstanceHandler>() : null;
        return !instance || instance.IsInstancedFor(player);
    }
    private static PlayerCharacterMasterController CameraPlayer(CameraRigController camera)
    {
        var body = camera ? camera.targetBody : null;
        return body && body.master ? body.master.playerCharacterMasterController : null;
    }
    public static bool IsObjectInteractibleForCameraRigController(GameObject target, CameraRigController camera) =>
        IsObjectInteractibleForPlayer(target, CameraPlayer(camera));
    public static bool IsObjectInstanceInteractibleForCameraRigController(GameObject target, CameraRigController camera) =>
        !CameraPlayer(camera) || IsObjectInstanceInteractibleForPlayer(target, CameraPlayer(camera));
}
