using System;
using System.Security.Permissions;
using BepInEx;
using BepInEx.Logging;
using InstancedLoot.Components;
using InstancedLoot.Configuration;
using InstancedLoot.Hooks;
using InstancedLoot.Networking;
using InstancedLoot.ObjectInstancing;
using R2API.Networking;
using R2API.Utils;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618
namespace InstancedLoot;

[BepInPlugin("com.kuberoot.instancedloot", "InstancedLoot", "2.0.0")]
[NetworkCompatibility]
[BepInDependency(NetworkingAPI.PluginGUID)]
public sealed class InstancedLoot : BaseUnityPlugin
{
    public static InstancedLoot Instance { get; private set; }
    internal ManualLogSource _logger => Logger;
    public Config ModConfig { get; private set; }
    public HookManager HookManager { get; private set; }
    public ObjectInstanceService Objects { get; private set; }
    private float nextResend;
    public void Awake()
    {
        ModConfig = new Config(this, Logger);
        ModConfig.Init();
        Objects = new ObjectInstanceService(this);
        HookManager = new HookManager(this);
        NetworkingAPI.RegisterMessageType<OwnerSnapshot>();
    }
    public void OnEnable()
    {
        Instance = this;
        HookManager.RegisterHooks();
        SceneManager.activeSceneChanged += SceneChanged;
        PlayerCharacterMasterController.onPlayerAdded += PlayerAdded;
    }
    public void OnDisable()
    {
        SceneManager.activeSceneChanged -= SceneChanged;
        PlayerCharacterMasterController.onPlayerAdded -= PlayerAdded;
        HookManager.UnregisterHooks();
        Clear();
        foreach (var handler in FindObjectsOfType<InstancedLootBehaviour>()) Destroy(handler);
        Instance = null;
    }
    private void Clear() { Objects.Clear(); OwnerSnapshot.Clear(); }
    private void SceneChanged(Scene previous, Scene current) { Objects.Clear(); OwnerSnapshot.SceneChanged(); }
    private void PlayerAdded(PlayerCharacterMasterController player) { Resend(); }
    private void Resend()
    {
        if (!NetworkServer.active || !Run.instance) return;
        foreach (var handler in InstanceHandler.Instances)
            if (handler && handler.Owner) new OwnerSnapshot(handler).SendToAll();
    }
    public void Update()
    {
        Objects.Pump();
        OwnerSnapshot.Pump();
        if (Time.unscaledTime >= nextResend)
        {
            nextResend = Time.unscaledTime + 2f;
            Resend();
        }
    }
}
