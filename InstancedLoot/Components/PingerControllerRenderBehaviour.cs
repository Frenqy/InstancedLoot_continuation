using System.Collections.Generic;
using RoR2;
using UnityEngine;

namespace InstancedLoot.Components;

/// <summary>Per-camera ping marker visibility; native target selection remains native.</summary>
public sealed class PingerControllerRenderBehaviour : InstancedLootBehaviour
{
    public static readonly List<PingerControllerRenderBehaviour> InstancesList = new();
    private readonly Dictionary<Renderer, bool> originalForce = new();
    private PingerController pinger;
    static PingerControllerRenderBehaviour()
    {
        SceneCamera.onSceneCameraPreCull += camera =>
        {
            var body = camera.cameraRigController ? camera.cameraRigController.targetBody : null;
            var player = body && body.master ? body.master.playerCharacterMasterController : null;
            foreach (var view in InstancesList) view.RefreshForPlayer(player);
        };
        SceneCamera.onSceneCameraPostRender += _ => { foreach (var view in InstancesList) view.RestoreAfterCamera(); };
    }
    public void Awake() { pinger = GetComponent<PingerController>(); }
    public void OnEnable() { InstancesList.Add(this); }
    public void OnDisable() { RestoreAfterCamera(); InstancesList.Remove(this); }
    public void OnDestroy() { RestoreAfterCamera(); InstancesList.Remove(this); }
    public void RefreshForPlayer(PlayerCharacterMasterController player)
    {
        RestoreAfterCamera();
        if (!pinger || !pinger.pingIndicator) return;
        var indicator = pinger.pingIndicator;
        bool visible = Utils.IsObjectInteractibleForPlayer(indicator.pingTarget, player);
        foreach (var renderer in indicator.GetComponentsInChildren<Renderer>())
        {
            originalForce[renderer] = renderer.forceRenderingOff;
            renderer.forceRenderingOff = renderer.forceRenderingOff || !visible;
        }
    }
    public void RestoreAfterCamera()
    {
        foreach (var pair in originalForce) if (pair.Key) pair.Key.forceRenderingOff = pair.Value;
        originalForce.Clear();
    }
}
