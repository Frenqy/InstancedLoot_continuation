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
        
        if (gameObject.GetComponent<InstanceHandler>() is var instanceHandler && instanceHandler != null) return instanceHandler.AllPlayers.Contains(player);

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