using System;
using System.Collections.Generic;
using System.Reflection;
using InstancedLoot.Configuration;
using InstancedLoot.Enums;
using InstancedLoot.Hooks;
using BepInEx;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour.HookGen;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace InstancedLoot;

[BepInPlugin("com.kuberoot.instancedloot", "InstancedLoot", "1.0.0")]
public class InstancedLoot : BaseUnityPlugin
{
    private HookManager hookManager;

    public Config ModConfig { get; private set; }

    public void OnEnable()
    {
        // Debug code for local multiplayer testing
        // On.RoR2.Networking.NetworkManagerSystemSteam.OnClientConnect += (s, u, t) => { };

        ModConfig = new Config(this, Logger);

        hookManager = new HookManager(this);
        hookManager.RegisterHooks();
    }

    public void OnDisable()
    {
        hookManager.UnregisterHooks();

        // TODO: Check if this works for non-hooks
        // Cleanup any leftover hooks
        HookEndpointManager.RemoveAllOwnedBy(
            HookEndpointManager.GetOwner((Action)OnDisable));
    }

}