using System;
using System.Collections.Generic;
using System.Linq;
using InstancedLoot.Components;
using InstancedLoot.Networking;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace InstancedLoot.ObjectInstancing;

/// <summary>Server-only lifetime of independently consumable native objects.</summary>
public sealed class ObjectInstanceService
{
    private readonly InstancedLoot plugin;
    private readonly Dictionary<GameObject, PendingCopy> pending = new();
    private readonly HashSet<GameObject> initialized = new();
    private readonly List<(ObjectAdapter adapter, GameObject source, string type)> sources = new();
    private readonly HashSet<GameObject> pendingChildren = new();
    private Run run;
    private int nextGroup;

    private sealed class PendingCopy
    {
        public Action<GameObject> ApplyState;
        public ObjectAdapter Adapter;
        public PlayerCharacterMasterController Owner;
        public int Group;
        public GameObject[] OwnedObjects;
    }

    public ObjectInstanceService(InstancedLoot plugin) { this.plugin = plugin; }

    // Called before an adapter's native Start. Only copies skip native randomization;
    // their Awake has already run and must not be repeated.
    public bool InitializeCopy(GameObject target)
    {
        CheckRun();
        if (!NetworkServer.active || !pending.TryGetValue(target, out var copy)) return false;
        try
        {
            if (!copy.Owner) throw new InvalidOperationException("Copy owner has left.");
            copy.ApplyState(target);
            AssignTree(copy.Adapter, target, copy.Owner, copy.Group);
        }
        catch (Exception error)
        {
            plugin._logger.LogError($"Copy initialization failed for {target.name}: {error}");
            DestroyCopy(copy);
        }
        finally
        {
            pending.Remove(target);
            foreach (var child in copy.OwnedObjects) pendingChildren.Remove(child);
        }
        return true;
    }

    public bool IsPendingChild(GameObject target) => pendingChildren.Contains(target);

    private void CheckRun()
    {
        if (run == Run.instance) return;
        Clear();
        run = Run.instance;
        OwnerSnapshot.RunChanged();
    }

    // Called once after native source initialization. The source stays native and is
    // assigned to the first player; each additional player gets a SpawnCard entity.
    public void InitializeSource(ObjectAdapter adapter, GameObject source, string objectType)
    {
        CheckRun();
        if (!NetworkServer.active || !source || !initialized.Add(source)) return;
        if (!plugin.ModConfig.IsEnabled(objectType)) return;
        foreach (var target in adapter.OwnedObjects(source)) PendingOwnership.Mark(target);
        sources.Add((adapter, source, objectType));
    }

    public void Pump()
    {
        CheckRun();
        if (!NetworkServer.active) return;
        foreach (var request in sources.ToArray())
        {
            if (!request.source) { sources.Remove(request); continue; }
            if (!request.adapter.IsReady(request.source)) continue;
            sources.Remove(request);
            try { CreateCopies(request.adapter, request.source); }
            catch (Exception error)
            {
                ResolveTree(request.adapter, request.source);
                plugin._logger.LogError($"Object instancing failed for {request.source.name}: {error}");
            }
        }
    }

    private void CreateCopies(ObjectAdapter adapter, GameObject source)
    {
        var players = PlayerCharacterMasterController.instances.Where(player => player).ToArray();
        if (players.Length == 0) { ResolveTree(adapter, source); return; }
        int group = ++nextGroup;
        var applyState = adapter.CaptureState(source);
        var created = new List<GameObject>();
        try
        {
            for (int i = 1; i < players.Length; i++)
            {
                GameObject target = SpawnCopy(source);
                if (!target)
                    throw new InvalidOperationException("SpawnCard did not create all player copies; keeping the source vanilla.");
                created.Add(target);
                var owned = adapter.OwnedObjects(target).ToArray();
                pending.Add(target, new PendingCopy { ApplyState = applyState, Adapter = adapter, Owner = players[i], Group = group, OwnedObjects = owned });
                foreach (var child in owned)
                {
                    PendingOwnership.Mark(child);
                    if (child != target) pendingChildren.Add(child);
                }
            }
            AssignTree(adapter, source, players[0], group);
        }
        catch
        {
            foreach (var previous in created)
            {
                if (pending.TryGetValue(previous, out var copy))
                {
                    DestroyCopy(copy);
                    foreach (var child in copy.OwnedObjects) pendingChildren.Remove(child);
                    pending.Remove(previous);
                }
                else
                {
                    foreach (var target in adapter.OwnedObjects(previous).Reverse()) if (target) NetworkServer.Destroy(target);
                }
            }
            throw;
        }
    }

    private void AssignTree(ObjectAdapter adapter, GameObject root, PlayerCharacterMasterController owner, int group)
    {
        int slot = 0;
        var assigned = new List<InstanceHandler>();
        try
        {
            foreach (var target in adapter.OwnedObjects(root))
            {
                if (!target) continue;
                var handler = target.GetComponent<InstanceHandler>() ?? target.AddComponent<InstanceHandler>();
                assigned.Add(handler);
                handler.Assign(owner, checked(group * 16 + slot++));
            }
        }
        catch
        {
            foreach (var handler in assigned) if (handler) UnityEngine.Object.Destroy(handler);
            throw;
        }
        // A transport failure must not turn a fully initialized object into a
        // partially rolled-back purchase. Periodic snapshots retry delivery.
        foreach (var handler in assigned)
            try { new OwnerSnapshot(handler).SendToAll(); }
            catch (Exception error) { plugin._logger.LogError($"Owner snapshot send failed for {handler.name}: {error}"); }
    }

    public GameObject SpawnCopy(GameObject source)
    {
        if (!NetworkServer.active || !source) return null;
        var card = source.GetComponent<SpawnCardTracker>()?.SpawnCard;
        if (!card)
            card = Resources.FindObjectsOfTypeAll<InteractableSpawnCard>().FirstOrDefault(candidate =>
                candidate.prefab && candidate.prefab.name == source.name.Replace("(Clone)", "").Trim());
        if (!card)
        {
            plugin._logger.LogError($"No SpawnCard for {source.name}; additional player copies were not created.");
            return null;
        }
        var rule = new DirectorPlacementRule
        {
            placementMode = DirectorPlacementRule.PlacementMode.DirectWithoutRandomRotation,
            position = source.transform.position,
            rotation = source.transform.rotation
        };
        var result = card.DoSpawn(source.transform.position, source.transform.rotation,
            new DirectorSpawnRequest(card, rule, new Xoroshiro128Plus(0)));
        if (result.spawnedInstance)
        {
            result.spawnedInstance.transform.position = source.transform.position;
            result.spawnedInstance.transform.rotation = source.transform.rotation;
            result.spawnedInstance.transform.localScale = source.transform.localScale;
        }
        return result.spawnedInstance;
    }

    public void Clear()
    {
        // A queued clone must never fall through to native Start after a transition.
        if (NetworkServer.active)
            foreach (var copy in pending.Values) DestroyCopy(copy);
        foreach (var source in sources)
            if (source.source) ResolveTree(source.adapter, source.source);
        pending.Clear();
        pendingChildren.Clear();
        sources.Clear();
        initialized.Clear();
    }

    private static void DestroyCopy(PendingCopy copy)
    {
        // MultiShop terminals are separate network entities, not transform children.
        foreach (var target in copy.OwnedObjects.AsEnumerable().Reverse())
            if (target) NetworkServer.Destroy(target);
    }

    private static void ResolveTree(ObjectAdapter adapter, GameObject source)
    {
        foreach (var target in adapter.OwnedObjects(source)) if (target) PendingOwnership.Resolve(target);
    }
}
