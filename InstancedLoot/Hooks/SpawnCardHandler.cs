using InstancedLoot.Components;
using RoR2;
using UnityEngine;

namespace InstancedLoot.Hooks;

public class SpawnCardHandler : AbstractHookHandler
{
    public override void RegisterHooks()
    {
        On.RoR2.SpawnCard.DoSpawn += On_SpawnCard_DoSpawn;
        // On.RoR2.InteractableSpawnCard.Spawn += On_InteractableSpawnCard_Spawn;
    }

    public override void UnregisterHooks()
    {
        On.RoR2.SpawnCard.DoSpawn -= On_SpawnCard_DoSpawn;
        // On.RoR2.InteractableSpawnCard.Spawn -= On_InteractableSpawnCard_Spawn;
    }

    private void HandleSpawnResult(SpawnCard spawnCard, SpawnCard.SpawnResult result)
    {
        if (result.spawnedInstance)
        {
            SpawnCardTracker spawnCardTracker = result.spawnedInstance.AddComponent<SpawnCardTracker>();
            spawnCardTracker.SpawnCard = spawnCard;
        }
    }

    private SpawnCard.SpawnResult On_SpawnCard_DoSpawn(On.RoR2.SpawnCard.orig_DoSpawn orig, SpawnCard self, Vector3 position, Quaternion rotation, DirectorSpawnRequest spawnRequest)
    {
        SpawnCard.SpawnResult result = orig(self, position, rotation, spawnRequest);

        HandleSpawnResult(self, result);
        
        return result;
    }

    // private void On_InteractableSpawnCard_Spawn(On.RoR2.InteractableSpawnCard.orig_Spawn orig, InteractableSpawnCard self, Vector3 position, Quaternion rotation, DirectorSpawnRequest directorSpawnRequest, ref SpawnCard.SpawnResult result)
    // {
    //     orig(self, position, rotation, directorSpawnRequest, ref result);
    //
    //     HandleSpawnResult(self, result);
    // }
}