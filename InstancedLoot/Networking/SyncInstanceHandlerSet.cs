using System;
using System.Collections;
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
        public GameObject[] players;
        public GameObject origPlayer;
        public GameObject sourceObject;
        public ObjectInstanceMode objectInstanceMode;

        private NetworkInstanceId _target;
        private NetworkInstanceId[] _players;
        private NetworkInstanceId _origPlayer;
        private NetworkInstanceId _sourceObject;
        private bool validated;

        public InstanceHandlerEntry(InstanceHandler instanceHandler)
        {
            target = instanceHandler.gameObject;
            isInstanced = true;
            players = instanceHandler.Players.Select(player => player.gameObject).ToArray();
            sourceObject = instanceHandler.SourceObject;
            origPlayer = instanceHandler.OrigPlayer.gameObject;
            objectInstanceMode = instanceHandler.ObjectInstanceMode;
        }

        public bool TryProcess()
        {
            if (validated) return true;

            target = Util.FindNetworkObject(_target);
            if (target == null) return false;
            Debug.Log(target);

            if (_origPlayer != NetworkInstanceId.Invalid)
            {
                origPlayer = Util.FindNetworkObject(_origPlayer);
                if (origPlayer == null) return false;
            }

            if (_sourceObject != NetworkInstanceId.Invalid)
            {
                sourceObject = Util.FindNetworkObject(_sourceObject);
                if (sourceObject == null) return false;
            }

            players = new GameObject[_players.Length];
            for (int i = 0; i < _players.Length; i++)
            {
                var player = Util.FindNetworkObject(_players[i]);
                if (player == null) return false;
                players[i] = player;
            }

            validated = true;
            return true;
        }

        public void Serialize(NetworkWriter writer)
        {
            // writer.Write(target);
            writer.Write(target.GetComponent<NetworkIdentity>().netId);
            writer.Write(isInstanced);
            if (isInstanced)
            {
                // writer.Write(origPlayer);
                writer.Write(origPlayer == null ? NetworkInstanceId.Invalid : origPlayer.GetComponent<NetworkIdentity>().netId);
                writer.Write(sourceObject == null ? NetworkInstanceId.Invalid : sourceObject.GetComponent<NetworkIdentity>().netId);
                writer.Write((int)objectInstanceMode);
                
                writer.Write((int)players.Count());
                foreach (var player in players)
                {
                    // writer.Write(player);
                    writer.Write(player.GetComponent<NetworkIdentity>().netId);
                }
            }
        }

        public static InstanceHandlerEntry Deserialize(NetworkReader reader)
        {
            InstanceHandlerEntry entry = new();
            entry.validated = false;
            // entry.target = reader.ReadGameObject();
            entry._target = reader.ReadNetworkId();
            entry.isInstanced = reader.ReadBoolean();
            if (entry.isInstanced)
            {
                // entry.origPlayer = reader.ReadGameObject();
                entry._origPlayer = reader.ReadNetworkId();
                entry._sourceObject = reader.ReadNetworkId();
                entry.objectInstanceMode = (ObjectInstanceMode)reader.ReadInt32();
                
                int count = reader.ReadInt32();
                // GameObject[] players = new GameObject[count];
                NetworkInstanceId[] _players = new NetworkInstanceId[count];
                for (int i = 0; i < count; i++)
                {
                    // players[i] = reader.ReadGameObject();
                    _players[i] = reader.ReadNetworkId();
                }
                
                // entry.players = players;
                entry._players = _players;
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
                players = players.ToArray()
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

        InstancedLoot.Instance.StartCoroutine(HandleMessageInternal());
    }

    private IEnumerator HandleMessageInternal()
    {
        bool validated = false;
        int retryCount = 0;
        
        while (!validated)
        {
            if (retryCount > 10)
            {
                InstancedLoot.Instance._logger.LogError($"SyncInstanceHandlerSet failed to process too many times; aborting.");
                yield break;
            }

            retryCount++;
            validated = true;

            for(int i = 0; i < instanceHandlerEntries.Length; i++)
            {
                validated = validated && instanceHandlerEntries[i].TryProcess();
            }

            if (!validated) yield return 0;
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

                instanceHandler.ObjectInstanceMode = entry.objectInstanceMode;

                if(entry.origPlayer)
                    instanceHandler.OrigPlayer = entry.origPlayer.GetComponent<PlayerCharacterMasterController>();

                if (entry.sourceObject)
                {
                    entry.target.transform.position = entry.sourceObject.transform.position;
                    entry.target.transform.rotation = entry.sourceObject.transform.rotation;
                    entry.target.transform.localScale = entry.sourceObject.transform.localScale;
                }
            }

            if(instanceHandler != null && !entry.isInstanced)
                UnityEngine.Object.Destroy(instanceHandler);
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