using System.Collections.Generic;
using System.Linq;
using InstancedLoot.Components;
using R2API.Networking;
using R2API.Networking.Interfaces;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace InstancedLoot.Networking;

/// <summary>Independent entity ownership, versioned and isolated by run/scene epoch.</summary>
public sealed class OwnerSnapshot : INetMessage
{
    public const int ProtocolVersion = 2;
    private int version = ProtocolVersion;
    private NetworkInstanceId runId, targetId, ownerId;
    private int stage, epoch, revision, group;
    private string scene;
    private float expires;
    private static int serverEpoch = 1;
    private static readonly Dictionary<(NetworkInstanceId run, int epoch, NetworkInstanceId target), OwnerSnapshot> pending = new();
    private static readonly Dictionary<(NetworkInstanceId run, int epoch, NetworkInstanceId target), int> latest = new();
    private static readonly Dictionary<NetworkInstanceId, int> newestEpoch = new();
    private static readonly Dictionary<NetworkInstanceId, int> appliedEpoch = new();
    private static readonly Dictionary<NetworkInstanceId, int> minimumEpoch = new();
    private (NetworkInstanceId, int, NetworkInstanceId) Key => (runId, epoch, targetId);
    public static int PendingCount => pending.Count;

    public OwnerSnapshot() { }
    public OwnerSnapshot(InstanceHandler handler)
    {
        runId = Run.instance.netId;
        stage = Run.instance.stageClearCount;
        scene = SceneManager.GetActiveScene().name;
        epoch = serverEpoch;
        targetId = handler.GetComponent<NetworkIdentity>().netId;
        ownerId = handler.Owner.GetComponent<NetworkIdentity>().netId;
        revision = handler.Revision;
        group = handler.GroupId;
    }
    public void Serialize(NetworkWriter writer)
    {
        writer.Write(version); writer.Write(runId); writer.Write(stage); writer.Write(scene); writer.Write(epoch);
        writer.Write(targetId); writer.Write(ownerId); writer.Write(revision); writer.Write(group);
    }
    public void Deserialize(NetworkReader reader)
    {
        version = reader.ReadInt32(); runId = reader.ReadNetworkId(); stage = reader.ReadInt32(); scene = reader.ReadString(); epoch = reader.ReadInt32();
        targetId = reader.ReadNetworkId(); ownerId = reader.ReadNetworkId(); revision = reader.ReadInt32(); group = reader.ReadInt32();
    }
    private bool CurrentSession => Run.instance && Run.instance.netId == runId &&
        Run.instance.stageClearCount == stage && SceneManager.GetActiveScene().name == scene;
    private bool Obsolete => minimumEpoch.TryGetValue(runId, out int minimum) && epoch < minimum ||
        newestEpoch.TryGetValue(runId, out int newest) && epoch < newest;
    public void OnReceived()
    {
        if (NetworkServer.active || version != ProtocolVersion || runId == NetworkInstanceId.Invalid ||
            targetId == NetworkInstanceId.Invalid || ownerId == NetworkInstanceId.Invalid || epoch <= 0 || revision <= 0 || group <= 0 || Obsolete) return;
        if (latest.TryGetValue(Key, out int seen) && revision < seen) return;
        newestEpoch[runId] = epoch;
        latest[Key] = revision;
        expires = Time.realtimeSinceStartup + 30f;
        pending[Key] = this;
        Pump();
    }
    private static void Release(OwnerSnapshot snapshot)
    {
        var target = Util.FindNetworkObject(snapshot.targetId);
        if (target && !target.GetComponent<InstanceHandler>()) PendingOwnership.Resolve(target);
        pending.Remove(snapshot.Key);
    }
    public static void Pump()
    {
        foreach (var snapshot in pending.Values.ToArray())
        {
            if (snapshot.Obsolete || Time.realtimeSinceStartup > snapshot.expires) { Release(snapshot); continue; }
            // A valid packet can precede Run, stage SyncVars, scene activation,
            // target spawn, or owner spawn. None blocks a different entity.
            if (!snapshot.CurrentSession) continue;
            appliedEpoch[snapshot.runId] = snapshot.epoch;
            var target = Util.FindNetworkObject(snapshot.targetId);
            var owner = Util.FindNetworkObject(snapshot.ownerId)?.GetComponent<PlayerCharacterMasterController>();
            if (target && !owner) PendingOwnership.Mark(target);
            if (!target || !owner) continue;
            var handler = target.GetComponent<InstanceHandler>() ?? target.AddComponent<InstanceHandler>();
            handler.Assign(owner, snapshot.group, snapshot.revision);
            pending.Remove(snapshot.Key);
        }
    }
    public void SendToAll() { if (NetworkServer.active && Run.instance) this.Send(NetworkDestination.Clients); }
    public static void SceneChanged()
    {
        if (NetworkServer.active) { serverEpoch++; return; }
        if (Run.instance && appliedEpoch.TryGetValue(Run.instance.netId, out int applied))
            minimumEpoch[Run.instance.netId] = applied + 1;
        // Preserve packets for the incoming scene that arrived before scene activation.
        Pump();
    }
    public static void RunChanged()
    {
        if (NetworkServer.active) { serverEpoch++; return; }
        foreach (var snapshot in pending.Values.ToArray())
            if (Run.instance && snapshot.runId != Run.instance.netId) Release(snapshot);
        Pump();
    }
    public static void Clear()
    {
        foreach (var snapshot in pending.Values.ToArray()) Release(snapshot);
        latest.Clear();
        newestEpoch.Clear(); appliedEpoch.Clear(); minimumEpoch.Clear();
    }
}
