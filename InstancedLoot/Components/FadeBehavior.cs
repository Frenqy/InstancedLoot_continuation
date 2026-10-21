using System.Collections.Generic;
using System.Linq;
using RoR2;
using UnityEngine;

namespace InstancedLoot.Components;

/// <summary>Render ownership is evaluated separately for each SceneCamera.</summary>
public sealed class FadeBehavior : InstancedLootBehaviour
{
    private static readonly int Fade = Shader.PropertyToID("_Fade");
    public float FadeLevel = 0f;
    public readonly HashSet<GameObject> ExtraGameObjects = new();
    public static readonly List<FadeBehavior> InstancesList = new();
    private readonly HashSet<Renderer> renderers = new();
    private readonly HashSet<Behaviour> effects = new();
    private MaterialPropertyBlock properties;
    private bool dirty = true;
    private readonly Dictionary<Renderer, bool> originalForce = new();
    private readonly Dictionary<Behaviour, bool> originalEffects = new();
    private readonly Dictionary<Renderer, MaterialPropertyBlock> originalProperties = new();

    static FadeBehavior()
    {
        SceneCamera.onSceneCameraPreCull += camera => RefreshAll(camera, true);
        SceneCamera.onSceneCameraPreRender += camera => RefreshAll(camera, false);
        SceneCamera.onSceneCameraPostRender += _ => { foreach (var view in InstancesList) view.RestoreAfterCamera(); };
    }
    private static void RefreshAll(SceneCamera camera, bool cull)
    {
        var body = camera.cameraRigController ? camera.cameraRigController.targetBody : null;
        var player = body && body.master ? body.master.playerCharacterMasterController : null;
        foreach (var view in InstancesList)
            if (cull) view.RefreshForPreCull(player); else view.RefreshForPreRender(player);
    }
    public void Awake() { properties = new MaterialPropertyBlock(); }
    public void OnEnable() { InstancesList.Add(this); }
    public void OnDisable() { RestoreAfterCamera(); InstancesList.Remove(this); }
    public void OnDestroy()
    {
        RestoreAfterCamera();
        InstancesList.Remove(this);
    }
    public void Refresh() { dirty = true; }
    public void RefreshNextFrame() { Refresh(); }
    public void RefreshComponentLists()
    {
        dirty = false;
        renderers.Clear();
        effects.Clear();
        ExtraGameObjects.RemoveWhere(target => !target);
        var targets = new HashSet<GameObject>(ExtraGameObjects) { gameObject };
        foreach (var locator in GetComponentsInChildren<ModelLocator>())
            if (locator.modelTransform != null) targets.Add(locator.modelTransform.gameObject);
        foreach (var target in targets.ToArray())
            foreach (var hologram in target.GetComponentsInChildren<CostHologramContent>())
                if (hologram.targetTextMesh) targets.Add(hologram.targetTextMesh.gameObject);
        foreach (var target in targets)
        {
            renderers.UnionWith(target.GetComponentsInChildren<Renderer>());
            effects.UnionWith(target.GetComponentsInChildren<Highlight>());
            effects.UnionWith(target.GetComponentsInChildren<Light>());
        }
        // A MultiShop root can contain terminal models. Each renderer belongs to
        // its nearest view, otherwise nested views restore different baselines.
        renderers.RemoveWhere(renderer => renderer && renderer.GetComponentInParent<FadeBehavior>() is var view && view && view != this);
        effects.RemoveWhere(effect => effect && effect.GetComponentInParent<FadeBehavior>() is var view && view && view != this);
    }
    private bool Visible(PlayerCharacterMasterController player)
    {
        if (PendingOwnership.IsPending(gameObject)) return false;
        var owner = GetComponent<InstanceHandler>();
        // Spectator cameras have no player: show native objects without carrying
        // over another split-screen camera's renderer state.
        return !owner || !player || owner.IsInstancedFor(player);
    }
    public float GetFadeLevelForCameraRigController(CameraRigController camera)
    {
        var body = camera ? camera.targetBody : null;
        return Visible(body && body.master ? body.master.playerCharacterMasterController : null) ? 1f : FadeLevel;
    }
    public void RefreshForPreCull(PlayerCharacterMasterController player)
    {
        RestoreAfterCamera();
        if (dirty) RefreshComponentLists();
        bool visible = Visible(player);
        foreach (var renderer in renderers)
        {
            if (!renderer) continue;
            originalForce[renderer] = renderer.forceRenderingOff;
            renderer.forceRenderingOff = renderer.forceRenderingOff || !visible;
        }
        foreach (var effect in effects)
        {
            if (!effect) continue;
            originalEffects[effect] = effect.enabled;
            if (!visible) effect.enabled = false;
        }
    }
    public void RefreshForPreRender(PlayerCharacterMasterController player)
    {
        if (dirty) RefreshComponentLists();
        float fade = Visible(player) ? 1f : FadeLevel;
        foreach (var renderer in renderers)
        {
            if (!renderer) continue;
            if (!originalProperties.ContainsKey(renderer))
            {
                var original = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(original);
                originalProperties[renderer] = original;
            }
            renderer.GetPropertyBlock(properties);
            properties.SetFloat(Fade, fade);
            renderer.SetPropertyBlock(properties);
        }
    }
    public void RestoreAfterCamera()
    {
        foreach (var pair in originalForce) if (pair.Key) pair.Key.forceRenderingOff = pair.Value;
        foreach (var pair in originalEffects) if (pair.Key) pair.Key.enabled = pair.Value;
        foreach (var pair in originalProperties) if (pair.Key) pair.Key.SetPropertyBlock(pair.Value);
        originalForce.Clear(); originalEffects.Clear(); originalProperties.Clear();
    }
    public static FadeBehavior Attach(GameObject target)
    {
        var view = target.GetComponent<FadeBehavior>() ?? target.AddComponent<FadeBehavior>();
        view.Refresh();
        return view;
    }
}
