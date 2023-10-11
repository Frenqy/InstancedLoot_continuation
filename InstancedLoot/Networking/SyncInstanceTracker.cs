using System.Collections.Generic;
using System.Linq;
using R2API.Networking.Interfaces;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace InstancedLoot.Networking;

public class SyncInstanceTracker : INetMessage
{
    private GameObject target;
    private bool hasInstances;
    private IEnumerable<GameObject> players;

    public SyncInstanceTracker()
    {
    }

    public SyncInstanceTracker(GameObject target, bool hasInstances, IEnumerable<GameObject> players)
    {
        this.target = target;
        this.hasInstances = hasInstances;
        this.players = players;
    }

    public void Serialize(NetworkWriter writer)
    {
        writer.Write(target);
        writer.Write(hasInstances);
        if (hasInstances)
        {
            writer.Write((int)players.Count());
            foreach (var player in players)
            {
                writer.Write(player);
            }
        }
    }

    public void Deserialize(NetworkReader reader)
    {
        target = reader.ReadGameObject();
        hasInstances = reader.ReadBoolean();
        if (hasInstances)
        {
            int count = reader.ReadInt32();
            List<GameObject> _players = new List<GameObject>();
            for (int i = 0; i < count; i++)
            {
                _players.Add(reader.ReadGameObject());
            }

            players = _players;
        }
    }

    public void OnReceived()
    {
        if (NetworkServer.active)
        {
            InstancedLoot.Instance._logger.LogWarning("SyncInstanceTracker ran on Host, ignoring");
            return;
        }

        InstanceTracker instanceTracker = target.GetComponent<InstanceTracker>();
        if (hasInstances)
        {
            if (instanceTracker == null && hasInstances)
                instanceTracker = target.AddComponent<InstanceTracker>();

            instanceTracker.SetPlayers(players.Select(player =>
                player.GetComponent<PlayerCharacterMasterController>()));
        }

        if(instanceTracker != null && !hasInstances)
            Object.Destroy(instanceTracker);
    }
}