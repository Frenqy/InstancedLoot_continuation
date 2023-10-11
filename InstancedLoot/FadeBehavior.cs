using System;
using System.Collections.Generic;
using System.Linq;
using RoR2;
using UnityEngine;

namespace InstancedLoot;

public class FadeBehavior : MonoBehaviour
{
    public float FadeLevel = 0.3f;
    protected float lastFadeLevel = 1.0f;
    protected CameraRigController lastCamera = null;
    public List<Renderer> Renderers;
    public DitherModel[] DitherModels;
	private MaterialPropertyBlock propertyStorage;
    private static readonly int Fade = Shader.PropertyToID("_Fade");

    public static List<FadeBehavior> instancesList = new();
    public static SceneCamera lastCameraStatic = null;

    public static void RefreshAllInstances(SceneCamera sceneCamera)
    {
        if (lastCameraStatic == sceneCamera)
            return;
        lastCameraStatic = sceneCamera;
        
        CameraRigController cameraRigController = sceneCamera.cameraRigController;
        if (!cameraRigController) return;

        CharacterBody body = cameraRigController.targetBody;
        if (!body) return;

        PlayerCharacterMasterController player = body.master != null ? body.master.playerCharacterMasterController : null;
        if (!player) return;
        
        foreach (var fadeBehavior in instancesList)
        {
            fadeBehavior.RefreshFade(player);
        }
    }

    public void RefreshInstanceForCamera(SceneCamera sceneCamera)
    {
        CameraRigController cameraRigController = sceneCamera.cameraRigController;
        if (!cameraRigController) return;

        CharacterBody body = cameraRigController.targetBody;
        if (!body) return;

        PlayerCharacterMasterController player = body.master != null ? body.master.playerCharacterMasterController : null;
        if (!player) return;
        
        RefreshFade(player);
    }

    static FadeBehavior()
    {
        SceneCamera.onSceneCameraPreRender += RefreshAllInstances;
    }
    
    private void Awake()
    {
        propertyStorage = new();
    }

    private void Start()
    {
        RefreshRenderers();

        if (lastCameraStatic != null)
            RefreshInstanceForCamera(lastCameraStatic);
    }

    private void OnEnable()
    {
        instancesList.Add(this);
    }

    private void OnDisable()
    {
        instancesList.Remove(this);
    }

    public float GetFadeLevel(PlayerCharacterMasterController player)
    {
        var instanceHandler = GetComponent<InstanceHandler>();
        if (!instanceHandler) return FadeLevel;
        return instanceHandler.Players.Contains(player) ? 1.0f : FadeLevel;
    }

    public float GetFadeLevelForCameraRigController(CameraRigController cameraRigController)
    {
        if (lastCamera == cameraRigController) return lastFadeLevel;
        
        CharacterBody body = cameraRigController.targetBody;
        if (!body) return FadeLevel;

        PlayerCharacterMasterController player = body.master != null ? body.master.playerCharacterMasterController : null;
        if (!player) return FadeLevel;

        float fadeLevel = GetFadeLevel(player);
        lastFadeLevel = fadeLevel;
        lastCamera = cameraRigController;
        return fadeLevel;
    }

    public void RefreshRenderers()
    {
        DitherModels = GetComponentsInChildren<DitherModel>();
        var ditherModelRenderers = new HashSet<Renderer>(DitherModels.SelectMany(ditherModel => ditherModel.renderers));
        Renderers = new List<Renderer>(GetComponentsInChildren<Renderer>().Where(renderer => !ditherModelRenderers.Contains(renderer)));
    }

    public void RefreshFade(PlayerCharacterMasterController player)
    {
        var instanceHandler = GetComponent<InstanceHandler>();
        float actualFadeLevel = instanceHandler.Players.Contains(player) ? 1.0f : FadeLevel;
        
        foreach (var renderer in Renderers)
        {
			renderer.GetPropertyBlock(propertyStorage);
			propertyStorage.SetFloat(Fade, actualFadeLevel);
			renderer.SetPropertyBlock(propertyStorage);
        }
    }

    public static FadeBehavior Attach(GameObject obj)
    {
        FadeBehavior fadeBehavior = obj.GetComponent<FadeBehavior>();
        if (fadeBehavior != null)
            return fadeBehavior;
        fadeBehavior = obj.AddComponent<FadeBehavior>();
        fadeBehavior.FadeLevel = 0.3f;
        return fadeBehavior;
    }
}