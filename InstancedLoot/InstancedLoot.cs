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

//TODO: Test ReduceSacrificeSpawnChance
//TODO: Test ReduceInteractibleBudget
//TODO: Handle disconnected players?
//      Compatibility with https://thunderstore.io/package/Moffein/Fix_Playercount/
//      Teleporter drop counting is going to be off and give items to the wrong players
//TODO: Instance drones (duh), perhaps later though - need to handle drones that broke correctly.
//TODO: Lunar pods are fixed, but rely on coroutine running next frame.
//TODO: Instancing effects isn't complete, some effects don't work, some effects seem to use a different system.
//TODO: Instance pickup droplets for dithering - Dithering doesn't work, problem with networking, scrapping idea for now.
//TODO: PickupPickerControllerHandler needs coroutine, due to PickupPickerController only having Awake.
//TODO: Better way to forfeit items. Currently have only chat commands.

#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618 // Type or member is obsolete

namespace InstancedLoot;

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
        
        NetworkingAPI.RegisterMessageType<SyncInstances>();
    }

    public static List<SyncInstances.InstanceHandlerEntry[]> FailedSyncs = new();

    public void Start()
    {
        ModConfig.Init();
    }

    public void OnEnable()
    {
        Instance = this;
        
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
        HookEndpointManager.RemoveAllOwnedBy(HookEndpointManager.GetOwner((Action)OnDisable));
        
        foreach (var component in FindObjectsOfType<InstancedLootBehaviour>())
        {
            if(component != null)
                Destroy(component);
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

    public void HandleInstancing(GameObject obj, InstanceInfoTracker.InstanceOverrideInfo? overrideInfo = null)
    {
        Logger.LogDebug($"Called for {obj}");
        InstanceInfoTracker instanceInfoTracker = obj.GetComponent<InstanceInfoTracker>();

        // Unity overrides null comparison, shouldn't matter here, but the IDE keeps yelling at me, so just to be safe...
        if (instanceInfoTracker == null) instanceInfoTracker = null;

        string objectType = overrideInfo?.ObjectType ?? instanceInfoTracker?.ObjectType;
        PlayerCharacterMasterController owner = overrideInfo?.Owner ?? instanceInfoTracker?.Owner;
        Logger.LogDebug($"objectType: {objectType}, owner: {owner?.GetDisplayName()}");

        if (instanceInfoTracker == null && objectType == null)
            return;
        
        InstanceMode instanceMode = ModConfig.GetInstanceMode(objectType ?? instanceInfoTracker.ObjectType);
        
        if (instanceMode == InstanceMode.None)
            return;

        Logger.LogDebug($"Handling: {obj} as {instanceMode}");
        
        bool shouldInstance = false;
        bool ownerOnly = false;
        bool isSimple = false;

        if (instanceInfoTracker == null)
        {
            switch (instanceMode)
            {
                case InstanceMode.InstanceBoth:
                case InstanceMode.InstanceObject:
                    shouldInstance = true;
                    ownerOnly = false;
                    break;
                case InstanceMode.InstanceBothForOwnerOnly:
                case InstanceMode.InstanceObjectForOwnerOnly:
                    shouldInstance = true;
                    ownerOnly = true;
                    break;
            }
        }

        if (//(instanceInfoTracker != null) && (
                (obj.GetComponent<GenericPickupController>() is var pickupController && pickupController != null && (PickupCatalog.GetPickupDef(pickupController.pickupIndex)?.itemIndex ?? ItemIndex.None) != ItemIndex.None)
                || (obj.GetComponent<PickupPickerController>() != null)
                // || (obj.GetComponent<PickupDropletController>() != null)
            )
           //)
        {
            isSimple = true;
            Logger.LogDebug($"It's an item!");
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

        if (!isSimple && shouldInstance)
        {
            shouldInstance = ObjectHandlerManager.CanInstanceObject(objectType, obj);
        }

        if (instanceInfoTracker == null && overrideInfo != null)
        {
            Logger.LogDebug($"Propagating overrideInfo");
            overrideInfo.Value.AttachTo(obj);
        } else if (instanceInfoTracker != null && owner != null)
        {
            Logger.LogDebug($"Fixing owner on InstanceInfoTracker");
            instanceInfoTracker.Info.Owner = owner;
        }
        
        Logger.LogDebug($"ShouldInstance: {shouldInstance}, OwnerOnly: {ownerOnly}");
        
        if (shouldInstance)
        {
            Logger.LogDebug($"Instancing!");
            //If instancing should happen only for owner but owner is missing, don't instance to avoid duplication exploits
            if (ownerOnly && owner == null && instanceInfoTracker?.PlayerOverride == null)
                return;

            HashSet<PlayerCharacterMasterController> players;

            if (instanceInfoTracker?.PlayerOverride != null)
            {
                players = new HashSet<PlayerCharacterMasterController>(instanceInfoTracker.PlayerOverride);
            }
            else if (ownerOnly)
            {
                players = new HashSet<PlayerCharacterMasterController> { owner };
            }
            else
            {
                players = ModConfig.GetValidPlayersSet();
            }

            if (isSimple)
            {
                InstanceHandler handler = obj.AddComponent<InstanceHandler>();

                handler.SharedInfo = new InstanceHandler.SharedInstanceInfo
                {
                    ObjectInstanceMode = ObjectInstanceMode.InstancedObject,
                };

                Logger.LogDebug($"Instancing {obj} as simple object for {String.Join(", ", players.Select(player => player.GetDisplayName()))}");
                handler.SetPlayers(players);
            }
            else
            {
                Logger.LogDebug($"Instancing {obj} as object for {String.Join(", ", players.Select(player => player.GetDisplayName()))}");
                ObjectHandlerManager.InstanceObject(objectType, obj, players.ToArray());
            }
        }
    }

    public static bool CanUninstance(InstanceHandler instanceHandler, PlayerCharacterMasterController player)
    {
        if (!instanceHandler.Players.Contains(player)) return false;
        if (instanceHandler.AllPlayers.Count == 1) return true;
        if (instanceHandler.GetComponent<CreatePickupInfoTracker>()) return true;

        return false;
    }

    public void HandleUninstancing(InstanceHandler instanceHandler, PlayerCharacterMasterController player)
    {
        if (!instanceHandler.Players.Contains(player)) return;

        if (instanceHandler.AllPlayers.Count == 1) // This is only instanced for one specific player
        {
            Destroy(instanceHandler);
            return;
        }

        if (instanceHandler.GetComponent<CreatePickupInfoTracker>() is var createPickupInfoTracker && createPickupInfoTracker)
        {
            Vector3 awayVector = instanceHandler.transform.position -
                                 (player.body != null ? player.body.transform.position : Vector3.zero);
            awayVector.y = 0.01f;
            awayVector.Normalize();

            PickupDropletController.CreatePickupDroplet(createPickupInfoTracker.CreatePickupInfo,
                instanceHandler.transform.position + Vector3.up * 3, Vector3.up * 10 + awayVector * 5);
            
            instanceHandler.RemovePlayer(player);
            return;
        }

        // if (instanceHandler.GetComponent<GenericPickupController>() is var pickupController && pickupController)
        // {
        //     Vector3 awayVector = pickupController.transform.position -
        //                          (player.body != null ? player.body.transform.position : Vector3.zero);
        //     awayVector.z = 0.01f;
        //     awayVector.Normalize();
        //
        //     PickupDropletController.CreatePickupDroplet(pickupController.pickupIndex,
        //         pickupController.transform.position, Vector3.Normalize(Vector3.up * 3 + awayVector * 2));
        //     return;
        // }
    }
}