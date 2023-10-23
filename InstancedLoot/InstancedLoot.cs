using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Security.Permissions;
using InstancedLoot.Configuration;
using InstancedLoot.Enums;
using InstancedLoot.Hooks;
using BepInEx;
using BepInEx.Logging;
using InstancedLoot.Components;
using InstancedLoot.Networking;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour.HookGen;
using R2API.Networking;
using R2API.Utils;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace InstancedLoot;

//TODO: Instance effects on use/open
//TODO: Instance pickup droplets for dithering
#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618 // Type or member is obsolete

[BepInPlugin("com.kuberoot.instancedloot", "InstancedLoot", "1.0.0")]
[NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.EveryoneNeedSameModVersion)]
[BepInDependency(NetworkingAPI.PluginGUID)]
public class InstancedLoot : BaseUnityPlugin
{
    public HookManager HookManager;
    public ObjectHandlerManager ObjectHandlerManager;

    internal ManualLogSource _logger => Logger;
    public static InstancedLoot Instance { get; private set; }
    public Config ModConfig { get; private set; }

    public void Awake()
    {
        ModConfig = new Config(this, Logger);
        HookManager = new HookManager(this);
        ObjectHandlerManager = new ObjectHandlerManager(this);
        NetworkingAPI.RegisterMessageType<SyncInstanceHandlerSet>();
    }

    public void Start()
    {
        ModConfig.Init();
    }

    public void OnEnable()
    {
        Instance = this;
        // Debug code for local multiplayer testing
        On.RoR2.Networking.NetworkManagerSystemSteam.OnClientConnect += (s, u, t) => { };
        
        PlayerCharacterMasterController.onPlayerAdded += OnPlayerAdded;

        HookManager.RegisterHooks();
    }

    public void OnDisable()
    {
        if (Instance == this) Instance = null;
        
        HookManager.UnregisterHooks();
        
        PlayerCharacterMasterController.onPlayerAdded -= OnPlayerAdded;

        // TODO: Check if this works for non-hooks
        // Cleanup any leftover hooks
        HookEndpointManager.RemoveAllOwnedBy(
            HookEndpointManager.GetOwner((Action)OnDisable));
        
        foreach (var instanceHandler in FindObjectsOfType<InstanceHandler>())
        {
            Destroy(instanceHandler);
        }
    }
    
    private void OnPlayerAdded(PlayerCharacterMasterController player)
    {
        if (!NetworkServer.active) return;
        
        if (player == null || player.networkUser == null || player.networkUser.connectionToClient == null)
            return;
        
        HashSet<InstanceHandler> instancesToSend = new();

        foreach (var instanceHandler in InstanceHandler.Instances)
        {
            InstanceHandler main = instanceHandler.LinkedHandlers?[0] ?? instanceHandler;

            if (!instancesToSend.Contains(main))
            {
                instancesToSend.Add(main);
                main.SyncToPlayer(player);
            }
        }
    }

    public void HandleInstancingNextTick(GameObject obj, InstanceInfoTracker.InstanceOverrideInfo? overrideInfo)
    {
        StartCoroutine(HandleInstancingNextTick_Internal(obj, overrideInfo));
    }

    private IEnumerator HandleInstancingNextTick_Internal(GameObject obj, InstanceInfoTracker.InstanceOverrideInfo? overrideInfo)
    {
        yield return 0;
        HandleInstancing(obj, overrideInfo);
    }

    public void HandleInstancing(GameObject obj, InstanceInfoTracker.InstanceOverrideInfo? overrideInfo = null)
    {
        Logger.LogWarning($"Called for {obj}");
        InstanceInfoTracker instanceInfoTracker = obj.GetComponent<InstanceInfoTracker>();

        // Unity overrides null comparison, shouldn't matter here, but the IDE keeps yelling at me, so just to be safe...
        if (instanceInfoTracker == null) instanceInfoTracker = null;

        string source = overrideInfo?.ItemSource ?? instanceInfoTracker?.ItemSource;
        PlayerCharacterMasterController owner = overrideInfo?.Owner ?? instanceInfoTracker?.Owner;
        Logger.LogWarning($"source: {source}, owner: {owner?.GetDisplayName()}");

        if (instanceInfoTracker == null && source == null)
            return;
        
        InstanceMode instanceMode = ModConfig.GetInstanceMode(source ?? instanceInfoTracker.ItemSource);
        
        if (instanceMode == InstanceMode.None)
            return;

        Logger.LogWarning($"Handling: {obj} as {instanceMode}");
        
        bool shouldInstance = false;
        bool ownerOnly = false;
        bool isItem = false;

        if (instanceInfoTracker == null)
        {
            switch (instanceMode)
            {
                case InstanceMode.InstanceBoth:
                case InstanceMode.InstanceObject:
                    shouldInstance = true;
                    ownerOnly = false;
                    break;
            }
        }

        if (instanceInfoTracker != null && obj.GetComponent<GenericPickupController>() is var pickupController && pickupController != null)
        {
            isItem = true;
            Logger.LogWarning($"It's an item!");
            switch (instanceMode)
            {
                case InstanceMode.InstanceObject:
                    shouldInstance = false;
                    break;
                case InstanceMode.InstanceBoth:
                case InstanceMode.InstanceItemForOwnerOnly:
                    shouldInstance = true;
                    ownerOnly = true;
                    break;
                case InstanceMode.InstanceItems:
                    shouldInstance = true;
                    ownerOnly = false;
                    break;
            }
        }

        if (!isItem && shouldInstance)
        {
            shouldInstance = ObjectHandlerManager.CanInstanceObject(source, obj);
        }

        if (instanceInfoTracker == null && overrideInfo != null)
        {
            Logger.LogWarning($"Propagating overrideInfo");
            overrideInfo.Value.AttachTo(obj);
        } else if (instanceInfoTracker != null && owner != null)
        {
            Logger.LogWarning($"Fixing owner on InstanceInfoTracker");
            instanceInfoTracker.Info.Owner = owner;
        }
        
        Logger.LogWarning($"ShouldInstance: {shouldInstance}, OwnerOnly: {ownerOnly}");
        
        if (shouldInstance)
        {
            Logger.LogWarning($"Instancing!");
            //If instancing should happen only for owner but owner is missing, don't instance to avoid duplication exploits
            if (ownerOnly && owner == null)
                return;

            HashSet<PlayerCharacterMasterController> players;

            if (ownerOnly)
            {
                players = new HashSet<PlayerCharacterMasterController> { owner };
            }
            else
            {
                players = ModConfig.GetValidPlayersSet();
            }

            if (isItem)
            {
                InstanceHandler handler = obj.AddComponent<InstanceHandler>();

                Logger.LogWarning($"Instancing {obj} as item for {players}");
                handler.SetPlayers(players);
            }
            else
            {
                Logger.LogWarning($"Instancing {obj} as object for {players}");
                ObjectHandlerManager.InstanceObject(source, obj, players.ToArray());
            }
        }
    }
}