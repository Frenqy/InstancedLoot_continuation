using System.Linq;
using InstancedLoot.Components;
using InstancedLoot.Enums;
using RoR2;
using UnityEngine;

namespace InstancedLoot;

public class Utils
{
    public static bool IsObjectInteractibleForPlayer(GameObject gameObject, PlayerCharacterMasterController player)
    {
        if (gameObject == null) return true;

        if (gameObject.GetComponent<InstanceHandler>() is var instanceHandler && instanceHandler != null)
        {
            if (instanceHandler.AllPlayers.Contains(player))
            {
                return instanceHandler.LinkedHandlers.Where(handler => handler.Players.Contains(player)).Any(handler =>
                {
                    if (handler.GetComponent<IInteractable>() is var interactable && interactable != null
                        && player.body is var body && body
                        && body.GetComponent<Interactor>() is var interactor && interactor)
                    {
                        if (interactable.GetInteractability(interactor) == Interactability.Disabled)
                            return false;
                    }
                    
                    return true;
                });
            }
            return instanceHandler.AllPlayers.Contains(player);
        }

        return true;
    }
    
    public static bool IsObjectInstanceInteractibleForPlayer(GameObject gameObject, PlayerCharacterMasterController player)
    {
        if (gameObject == null) return true;
        
        if (gameObject.GetComponent<InstanceHandler>() is var instanceHandler && instanceHandler != null) return instanceHandler.Players.Contains(player);

        return true;
    }

    public static bool IsObjectInteractibleForCameraRigController(GameObject gameObject,
        CameraRigController cameraRigController)
    {
        if (cameraRigController == null) return true;
        
        CharacterBody body = cameraRigController.targetBody;
        if (body == null) return true;

        CharacterMaster master = body.master;
        if (master == null) return true;

        PlayerCharacterMasterController player = master.playerCharacterMasterController;
        if (player == null) return true;

        return IsObjectInteractibleForPlayer(gameObject, player);
    }

    public static bool IsObjectInstanceInteractibleForCameraRigController(GameObject gameObject,
        CameraRigController cameraRigController)
    {
        if (cameraRigController == null) return true;
        
        CharacterBody body = cameraRigController.targetBody;
        if (body == null) return true;

        CharacterMaster master = body.master;
        if (master == null) return true;

        PlayerCharacterMasterController player = master.playerCharacterMasterController;
        if (player == null) return true;

        return IsObjectInstanceInteractibleForPlayer(gameObject, player);
    }

    public static bool IncreasesItemCount(InstanceMode instanceMode)
    {
        switch (instanceMode)
        {
            case InstanceMode.InstanceBoth:
            case InstanceMode.InstanceItems:
            case InstanceMode.InstanceObject:
                return true;
        }

        return false;
    }
}