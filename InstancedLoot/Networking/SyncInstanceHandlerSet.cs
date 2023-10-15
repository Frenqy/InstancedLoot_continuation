using System;
using System.Collections.Generic;
using System.Linq;
using InstancedLoot.Components;
using R2API.Networking.Interfaces;
using RoR2;
using RoR2.Networking;
using UnityEngine;
using UnityEngine.Networking;
using Object = UnityEngine.Object;

namespace InstancedLoot.Networking;

//TODO: Network them all at once
public class SyncInstanceHandlerSet : INetMessage
{
    public struct InstanceHandlerEntry
    {
        public GameObject target;
        public bool isInstanced;
        public IEnumerable<GameObject> players;
        public GameObject origPlayer;

        public InstanceHandlerEntry(InstanceHandler instanceHandler)
        {
            target = instanceHandler.gameObject;
            isInstanced = true;
            players = instanceHandler.Players.Select(player => player.gameObject);
        }

        public void Serialize(NetworkWriter writer)
        {
            writer.Write(target);
            writer.Write(isInstanced);
            if (isInstanced)
            {
                writer.Write((int)players.Count());
                foreach (var player in players)
                {
                    writer.Write(player);
                }
            }
            writer.Write(origPlayer);
        }

        public static InstanceHandlerEntry Deserialize(NetworkReader reader)
        {
            InstanceHandlerEntry entry = new();
            entry.target = reader.ReadGameObject();
            entry.isInstanced = reader.ReadBoolean();
            if (entry.isInstanced)
            {
                int count = reader.ReadInt32();
                GameObject[] players = new GameObject[count];
                for (int i = 0; i < count; i++)
                {
                    players[i] = reader.ReadGameObject();
                }
                
                entry.players = players;
            }

            entry.origPlayer = reader.ReadGameObject();

            return entry;
        }
    }

    private InstanceHandlerEntry[] instanceHandlerEntries;

    public SyncInstanceHandlerSet()
    {
        instanceHandlerEntries = Array.Empty<InstanceHandlerEntry>();
    }

    public SyncInstanceHandlerSet(IEnumerable<InstanceHandler> instanceHandlers)
    {
        instanceHandlerEntries = instanceHandlers.Select(instanceHandler => new InstanceHandlerEntry(instanceHandler)).ToArray();
    }
    
    public SyncInstanceHandlerSet(InstanceHandler instanceHandler)
    {
        instanceHandlerEntries = new[] { new InstanceHandlerEntry(instanceHandler) };
    }

    public SyncInstanceHandlerSet(GameObject target, bool hasInstances, IEnumerable<GameObject> players)
    {
        instanceHandlerEntries = new[]
        {
            new InstanceHandlerEntry
            {
                target = target,
                isInstanced = hasInstances,
                players = players
            }
        };
    }

    public void Serialize(NetworkWriter writer)
    {
        writer.Write((int)instanceHandlerEntries.Length);
        foreach (var entry in instanceHandlerEntries)
        {
            entry.Serialize(writer);
        }
    }

    public void Deserialize(NetworkReader reader)
    {
        int entryCount = reader.ReadInt32();
        instanceHandlerEntries = new InstanceHandlerEntry[entryCount];
        for (int entryIndex = 0; entryIndex < entryCount; entryIndex++)
        {
            instanceHandlerEntries[entryIndex] = InstanceHandlerEntry.Deserialize(reader);
        }
    }

    public void OnReceived()
    {
        if (NetworkServer.active)
        {
            InstancedLoot.Instance._logger.LogWarning("SyncInstanceHandlerSet ran on Host, ignoring");
            return;
        }

        foreach (var entry in instanceHandlerEntries)
        {
            InstanceHandler instanceHandler = entry.target.GetComponent<InstanceHandler>();
            if (entry.isInstanced)
            {
                if (instanceHandler == null)
                    instanceHandler = entry.target.AddComponent<InstanceHandler>();

                instanceHandler.SetPlayers(entry.players.Select(player =>
                    player.GetComponent<PlayerCharacterMasterController>()), false);

                instanceHandler.OrigPlayer = entry.origPlayer.GetComponent<PlayerCharacterMasterController>();
            }

            if(instanceHandler != null && !entry.isInstanced)
                Object.Destroy(instanceHandler);
        }

        InstanceHandler[] instanceHandlers = instanceHandlerEntries
            .Select(entry => entry.target.GetComponent<InstanceHandler>()).Where(handler => handler != null).ToArray();

        foreach (var instanceHandler in instanceHandlers)
        {
            instanceHandler.SetLinkedHandlers(instanceHandlers, false);
        }

        foreach (var instanceHandler in instanceHandlers)
        {
            instanceHandler.SyncPlayers();
        }
    }
}