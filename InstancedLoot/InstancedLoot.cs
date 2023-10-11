using System;
using System.Collections.Generic;
using System.Reflection;
using InstancedLoot.Configuration;
using InstancedLoot.Enums;
using InstancedLoot.Hooks;
using BepInEx;
using BepInEx.Logging;
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

[BepInPlugin("com.kuberoot.instancedloot", "InstancedLoot", "1.0.0")]
[NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.EveryoneNeedSameModVersion)]
[BepInDependency(NetworkingAPI.PluginGUID)]
public class InstancedLoot : BaseUnityPlugin
{
    private HookManager hookManager;

    internal ManualLogSource _logger => Logger;
    public static InstancedLoot Instance { get; private set; }
    public Config ModConfig { get; private set; }

    public void OnEnable()
    {
        Instance = this;
        // Debug code for local multiplayer testing
        On.RoR2.Networking.NetworkManagerSystemSteam.OnClientConnect += (s, u, t) => { };

        ModConfig = new Config(this, Logger);

        hookManager = new HookManager(this);
        hookManager.RegisterHooks();

        NetworkingAPI.RegisterMessageType<SyncInstanceTracker>();
    }

    public void OnDisable()
    {
        if (Instance == this) Instance = null;
        
        hookManager.UnregisterHooks();

        // TODO: Check if this works for non-hooks
        // Cleanup any leftover hooks
        HookEndpointManager.RemoveAllOwnedBy(
            HookEndpointManager.GetOwner((Action)OnDisable));
    }

    public void HandleInstancing(string source, GameObject obj)
    {
        InstanceModeNew instanceMode = InstanceModeNew.None;
    }

    public void MakeInstanced(GameObject obj)
    {
        Logger.LogWarning($"Considering instancing of {obj}");
        InstanceOverride instanceOverride = obj.GetComponent<InstanceOverride>();

        bool shouldInstance = true;
        bool ownerOnly = false;
        PlayerCharacterMasterController owner = null;
        
        if (instanceOverride)
        {
            GenericPickupController pickup = obj.GetComponent<GenericPickupController>();

            if (ModConfig.ItemSourceMapper.TryGetValue(instanceOverride.ItemSource, out var instanceConfig))
            {
                switch (instanceConfig.Value)
                {
                    case InstanceMode.NoInstancing:
                        shouldInstance = false;
                        break;
                    case InstanceMode.FullInstancing:
                        shouldInstance = true;
                        ownerOnly = false;
                        break;
                    case InstanceMode.OwnerOnly:
                        shouldInstance = true;
                        ownerOnly = true;
                        owner = instanceOverride.Owner;
                        break;
                }
            }
            
            //TODO: Confirm if this is the right place and time
            Destroy(pickup);
        }

        if (obj.GetComponent<GenericPickupController>() is var pickupController && pickupController != null)
        {
            switch (ModConfig.instancedItems.Value)
            {
                case InstanceMode.NoInstancing:
                    shouldInstance = false;
                    break;
                case InstanceMode.FullInstancing:
                    shouldInstance = true;
                    ownerOnly = false;
                    break;
            }
        }

        if (shouldInstance)
        {
            InstanceTracker tracker = obj.AddComponent<InstanceTracker>();

            HashSet<PlayerCharacterMasterController> players;

            if (ownerOnly)
            {
                players = new HashSet<PlayerCharacterMasterController> { owner };
            }
            else
            {
                players = ModConfig.GetValidPlayersSet();
            }
            
            Logger.LogWarning($"Instancing {obj} for {players}");
            tracker.SetPlayers(players);
        }
    }
}