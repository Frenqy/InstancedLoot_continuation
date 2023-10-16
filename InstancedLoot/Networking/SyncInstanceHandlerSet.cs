using System;
using System.Collections.Generic;
using System.Linq;
using InstancedLoot.Components;
using InstancedLoot.Enums;
using R2API.Networking.Interfaces;
using RoR2;
using RoR2.Networking;
using UnityEngine;
using UnityEngine.Networking;

namespace InstancedLoot.Networking;

//TODO: Try using NetworkInstanceId to automatically retry if objects are missing
public class SyncInstanceHandlerSet : INetMessage
{
    public struct InstanceHandlerEntry
    {
        public GameObject target;
        public bool isInstanced;
        public IEnumerable<GameObject> players;
        public GameObject origPlayer;
        public ObjectInstanceMode objectInstanceMode;

        public InstanceHandlerEntry(InstanceHandler instanceHandler)
        {
            target = instanceHandler.gameObject;
            isInstanced = true;
            players = instanceHandler.Players.Select(player => player.gameObject);
            objectInstanceMode = instanceHandler.ObjectInstanceMode;
        }

        public void Serialize(NetworkWriter writer)
        {
            writer.Write(target);
            writer.Write(isInstanced);
            if (isInstanced)
            {
                writer.Write(origPlayer);
                writer.Write((int)objectInstanceMode);
                writer.Write((int)players.Count());
                foreach (var player in players)
                {
                    writer.Write(player);
                }
            }
        }

        public static InstanceHandlerEntry Deserialize(NetworkReader reader)
        {
            InstanceHandlerEntry entry = new();
            entry.target = reader.ReadGameObject();
            entry.isInstanced = reader.ReadBoolean();
            if (entry.isInstanced)
            {
                entry.origPlayer = reader.ReadGameObject();
                entry.objectInstanceMode = (ObjectInstanceMode)reader.ReadInt32();
                
                int count = reader.ReadInt32();
                GameObject[] players = new GameObject[count];
                for (int i = 0; i < count; i++)
                {
                    players[i] = reader.ReadGameObject();
                }
                
                entry.players = players;
            }

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

        InstancedLoot.Instance._logger.LogWarning("1");
        foreach (var entry in instanceHandlerEntries)
        {
            InstancedLoot.Instance._logger.LogWarning("2.1");
            InstanceHandler instanceHandler = entry.target.GetComponent<InstanceHandler>();
            if (entry.isInstanced)
            {
                InstancedLoot.Instance._logger.LogWarning("2.2");
                if (instanceHandler == null)
                    instanceHandler = entry.target.AddComponent<InstanceHandler>();

                InstancedLoot.Instance._logger.LogWarning("2.3");
                instanceHandler.SetPlayers(entry.players.Select(player =>
                    player.GetComponent<PlayerCharacterMasterController>()), false);

                instanceHandler.ObjectInstanceMode = entry.objectInstanceMode;

                InstancedLoot.Instance._logger.LogWarning("2.4");
                if(entry.origPlayer)
                    instanceHandler.OrigPlayer = entry.origPlayer.GetComponent<PlayerCharacterMasterController>();
            }

            InstancedLoot.Instance._logger.LogWarning("2.5");
            if(instanceHandler != null && !entry.isInstanced)
                UnityEngine.Object.Destroy(instanceHandler);
        }

        InstancedLoot.Instance._logger.LogWarning("3");
        InstanceHandler[] instanceHandlers = instanceHandlerEntries
            .Select(entry => entry.target.GetComponent<InstanceHandler>()).Where(handler => handler != null).ToArray();

        InstancedLoot.Instance._logger.LogWarning("4");
        foreach (var instanceHandler in instanceHandlers)
        {
            InstancedLoot.Instance._logger.LogWarning("4.1");
            instanceHandler.SetLinkedHandlers(instanceHandlers, false);
        }

        InstancedLoot.Instance._logger.LogWarning("5");
        foreach (var instanceHandler in instanceHandlers)
        {
            InstancedLoot.Instance._logger.LogWarning("5.1");
            instanceHandler.SyncPlayers();
        }
    }
}