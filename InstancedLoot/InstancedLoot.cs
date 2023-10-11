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

        NetworkingAPI.RegisterMessageType<SyncInstanceHandler>();
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

    public void HandleInstancing(GameObject obj, InstanceInfoTracker.InstanceOverrideInfo? overrideInfo = null)
    {
        Logger.LogWarning($"Called for {obj}");
        InstanceInfoTracker instanceInfoTracker = obj.GetComponent<InstanceInfoTracker>();

        // Unity overrides null comparison, shouldn't matter here, but the IDE keeps yelling at me, so just to be safe...
        if (instanceInfoTracker == null) instanceInfoTracker = null;

        string source = overrideInfo?.ItemSource ?? instanceInfoTracker?.ItemSource;
        PlayerCharacterMasterController owner = overrideInfo?.Owner ?? instanceInfoTracker?.Owner;

        if (instanceInfoTracker == null && source == null)
            return;
        
        InstanceMode instanceMode = ModConfig.GetInstanceMode(source ?? instanceInfoTracker?.ItemSource);
        

        if (instanceMode == InstanceMode.None)
            return;

        Logger.LogWarning($"Handling: {obj} as {instanceMode}");
        
        bool shouldInstance = false;
        bool ownerOnly = false;

        if (instanceInfoTracker != null && obj.GetComponent<GenericPickupController>() is var pickupController && pickupController != null)
        {
            Logger.LogWarning($"It's an item!");
            switch (instanceMode)
            {
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
            
            InstanceHandler handler = obj.AddComponent<InstanceHandler>();

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
            handler.SetPlayers(players);
        }
    }
}