using System.Collections.Generic;
using System.Linq;
using InstancedLoot.Hooks;
using InstancedLoot.Networking;
using R2API.Networking;
using R2API.Networking.Interfaces;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace InstancedLoot;

public class InstanceHandler : MonoBehaviour
{
    public HashSet<PlayerCharacterMasterController> Players;

    public void SetPlayers(IEnumerable<PlayerCharacterMasterController> players)
    {
        Players = new HashSet<PlayerCharacterMasterController>(players);
        SyncPlayers();
    }

    public void RemovePlayer(PlayerCharacterMasterController player)
    {
        Players.Remove(player);
        SyncPlayers();
    }
    
    public void AddPlayer(PlayerCharacterMasterController player)
    {
        Players.Add(player);
        SyncPlayers();
    }

    public void SyncPlayers()
    {
        if (NetworkServer.active)
        {
            new SyncInstanceHandler(gameObject, true, Players.Select(player => player.gameObject)).Send(
                NetworkDestination.Clients);
        }
        
        //TODO: Update visuals etc.
        var localPlayer = PlayerCharacterMasterController.instances[0]; // Seems hacky to me, but it's recommended?
        var fadeBehavior = GetComponent<FadeBehavior>();

        if (Players.Contains(localPlayer) && fadeBehavior)
            Destroy(fadeBehavior);
        
        if (!Players.Contains(localPlayer))
            FadeBehavior.Attach(gameObject);
    }
}