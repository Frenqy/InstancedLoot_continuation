using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using InstancedLoot.Components;
using InstancedLoot.Enums;
using R2API.Networking.Interfaces;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace InstancedLoot.Networking;

public class SyncInstances : INetMessage
{
    public struct InstanceHandlerEntry
    {
        public GameObject target;
        public GameObject[] players;
        public GameObject origPlayer;

        private NetworkInstanceId _target;
        private NetworkInstanceId[] _players;
        private NetworkInstanceId _origPlayer;
        public NetworkInstanceId TargetId => _target;

        public InstanceHandlerEntry(InstanceHandler instanceHandler)
        {
            target = instanceHandler.gameObject;
            players = instanceHandler.Players.Select(player => player.gameObject).ToArray();
            origPlayer = instanceHandler.OrigPlayer != null ? instanceHandler.OrigPlayer.gameObject : null;
        }

        public bool TryProcess()
        {
            target = Util.FindNetworkObject(_target);
            if (target == null) return false;

            origPlayer = null;
            if (_origPlayer != NetworkInstanceId.Invalid)
            {
                origPlayer = Util.FindNetworkObject(_origPlayer);
                if (origPlayer == null || !origPlayer.GetComponent<PlayerCharacterMasterController>()) return false;
            }

            players = new GameObject[_players.Length];
            for (int i = 0; i < _players.Length; i++)
            {
                var player = Util.FindNetworkObject(_players[i]);
                if (player == null || !player.GetComponent<PlayerCharacterMasterController>()) return false;
                players[i] = player;
            }

            return true;
        }

        public void Serialize(NetworkWriter writer)
        {
            writer.Write(target.GetComponent<NetworkIdentity>().netId);
            writer.Write(origPlayer == null ? NetworkInstanceId.Invalid : origPlayer.GetComponent<NetworkIdentity>().netId);
            
            writer.Write(players.Count());
            foreach (var player in players) writer.Write(player.GetComponent<NetworkIdentity>().netId);
        }

        public static InstanceHandlerEntry Deserialize(NetworkReader reader)
        {
            InstanceHandlerEntry entry = new();
            entry._target = reader.ReadNetworkId();
            entry._origPlayer = reader.ReadNetworkId();
            
            int count = reader.ReadInt32();
            NetworkInstanceId[] _players = new NetworkInstanceId[count];
            for (int i = 0; i < count; i++) _players[i] = reader.ReadNetworkId();

            entry._players = _players;

            return entry;
        }
    }

    private NetworkInstanceId _sourceObject;
    private ObjectInstanceMode objectInstanceMode;
    private InstanceHandlerEntry[] instanceHandlerEntries;
    // Only unresolved snapshots are retained. A later received message replaces an
    // overlapping pending snapshot before its coroutine can apply stale permissions.
    private static readonly List<SyncInstances> PendingMessages = new();
    private bool superseded;
    private InstancedLoot receiver;

    public static void ClearPending()
    {
        foreach (var pending in PendingMessages) pending.superseded = true;
        PendingMessages.Clear();
    }

    public SyncInstances()
    {
        instanceHandlerEntries = Array.Empty<InstanceHandlerEntry>();
    }

    public SyncInstances(InstanceHandler.SharedInstanceInfo instanceInfo)
    {
        instanceHandlerEntries = instanceInfo.LinkedHandlers.Select(instanceHandler => new InstanceHandlerEntry(instanceHandler)).ToArray();
        objectInstanceMode = instanceInfo.ObjectInstanceMode;
        _sourceObject = instanceInfo.SourceObject == null
            ? NetworkInstanceId.Invalid
            : instanceInfo.SourceObject.GetComponent<NetworkIdentity>().netId;
    }

    public SyncInstances(IEnumerable<InstanceHandler> instanceHandlers)
    {
        instanceHandlerEntries = instanceHandlers.Select(instanceHandler => new InstanceHandlerEntry(instanceHandler)).ToArray();
    }
    
    public void Serialize(NetworkWriter writer)
    {
        writer.Write((int)objectInstanceMode);
        writer.Write(_sourceObject);
        writer.Write(instanceHandlerEntries.Length);
        foreach (var entry in instanceHandlerEntries) entry.Serialize(writer);
    }

    public void Deserialize(NetworkReader reader)
    {
        objectInstanceMode = (ObjectInstanceMode)reader.ReadInt32();
        _sourceObject = reader.ReadNetworkId();
        int entryCount = reader.ReadInt32();
        instanceHandlerEntries = new InstanceHandlerEntry[entryCount];
        for (int entryIndex = 0; entryIndex < entryCount; entryIndex++) instanceHandlerEntries[entryIndex] = InstanceHandlerEntry.Deserialize(reader);
    }

    public void OnReceived()
    {
        if (NetworkServer.active)
            //This ran a lot, let's just ignore it silently
            // InstancedLoot.Instance._logger.LogWarning("SyncInstances ran on Host, ignoring");
            return;

        receiver = InstancedLoot.Instance;
        if (!receiver || !receiver.enabled) return;

        var targetIds = new HashSet<NetworkInstanceId>(instanceHandlerEntries.Select(entry => entry.TargetId));
        foreach (var pending in PendingMessages.ToArray())
        {
            if (pending.receiver != receiver || pending.instanceHandlerEntries.Any(entry => targetIds.Contains(entry.TargetId)))
            {
                pending.superseded = true;
                PendingMessages.Remove(pending);
            }
        }

        PendingMessages.Add(this);
        receiver.StartCoroutine(HandleMessageInternal());
    }

    private IEnumerator HandleMessageInternal()
    {
        var entries = instanceHandlerEntries;
        var run = Run.instance;
        float deadline = Time.realtimeSinceStartup + 30f;
        GameObject sourceObject = null;

        try
        {
            while (true)
            {
                if (superseded || !receiver || !receiver.enabled || receiver != InstancedLoot.Instance || Run.instance != run)
                    yield break;

                bool validated = true;
                // Resolve again on every pass: previously resolved objects can be destroyed
                // while a different target/player is still spawning.
                for (int i = 0; i < entries.Length; i++)
                    if (!entries[i].TryProcess()) validated = false;

                sourceObject = _sourceObject == NetworkInstanceId.Invalid ? null : Util.FindNetworkObject(_sourceObject);
                if (_sourceObject != NetworkInstanceId.Invalid && sourceObject == null) validated = false;

                if (validated) break;

                if (Time.realtimeSinceStartup >= deadline)
                {
                    receiver._logger.LogError($"SyncInstances timed out waiting for network objects (source {_sourceObject}, targets {string.Join(", ", entries.Select(entry => entry.TargetId))}).");
                    // Diagnostic history, not a replay queue. Keep it bounded across long runs.
                    if (InstancedLoot.FailedSyncs.Count >= 32) InstancedLoot.FailedSyncs.RemoveAt(0);
                    InstancedLoot.FailedSyncs.Add(entries);
                    yield break;
                }

                yield return null;
            }

            InstanceHandler.SharedInstanceInfo sharedInstanceInfo = new();
            sharedInstanceInfo.ObjectInstanceMode = objectInstanceMode;
            sharedInstanceInfo.SourceObject = sourceObject;

            foreach (var entry in entries)
            {
                InstanceHandler instanceHandler = entry.target.GetComponent<InstanceHandler>();
            
                if (instanceHandler == null)
                    instanceHandler = entry.target.AddComponent<InstanceHandler>();

                instanceHandler.SetPlayers(entry.players.Select(player =>
                    player.GetComponent<PlayerCharacterMasterController>()), false);

                instanceHandler.OrigPlayer = entry.origPlayer ? entry.origPlayer.GetComponent<PlayerCharacterMasterController>() : null;

                if (sourceObject != null)
                {
                    entry.target.transform.position = sourceObject.transform.position;
                    entry.target.transform.rotation = sourceObject.transform.rotation;
                    entry.target.transform.localScale = sourceObject.transform.localScale;
                }

                instanceHandler.SharedInfo = sharedInstanceInfo;
            }
        
            sharedInstanceInfo.RecalculateAllPlayers();

            InstanceHandler[] instanceHandlers = entries
                .Select(entry => entry.target.GetComponent<InstanceHandler>()).Where(handler => handler != null).ToArray();

            foreach (var instanceHandler in instanceHandlers) instanceHandler.UpdateVisuals();
        }
        finally
        {
            PendingMessages.Remove(this);
        }
    }
}
