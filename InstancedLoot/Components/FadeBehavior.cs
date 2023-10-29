using System.Collections.Generic;
using System.Linq;
using InstancedLoot.Enums;
using RoR2;
using RoR2.Hologram;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace InstancedLoot.Components;

public class FadeBehavior : MonoBehaviour
{
    private static readonly int Fade = Shader.PropertyToID("_Fade");
    
    public float FadeLevel = 0.3f;
    private float lastFadeLevel = 1.0f;
    
    public HashSet<Renderer> Renderers;
    public HashSet<Renderer> DitherModelRenderers;
    public DitherModel[] DitherModels;
    private MaterialPropertyBlock propertyStorage;
    
    public Behaviour[] ComponentsForPreCull;
    public Behaviour[] ComponentsForPreRender;

    public static readonly List<FadeBehavior> InstancesList = new();
    
    //Stored for optimization purposes
    private CameraRigController lastCamera;
    private static SceneCamera lastCameraStaticPreCull;
    private static SceneCamera lastCameraStaticPreRender;

    static FadeBehavior()
    {
        SceneCamera.onSceneCameraPreCull += RefreshForPreCull;
        SceneCamera.onSceneCameraPreRender += RefreshForPreRender;
    }

    public static void RefreshForPreCull(SceneCamera sceneCamera)
    {
        if (lastCameraStaticPreCull == sceneCamera)
            return;
        lastCameraStaticPreCull = sceneCamera;
        
        RefreshAllInstances(sceneCamera, true);
    }

    public static void RefreshForPreRender(SceneCamera sceneCamera)
    {
        if (lastCameraStaticPreRender == sceneCamera)
            return;
        lastCameraStaticPreRender = sceneCamera;
        
        RefreshAllInstances(sceneCamera, false);
    }

    public static void RefreshAllInstances(SceneCamera sceneCamera, bool isPreCull)
    {
        CameraRigController cameraRigController = sceneCamera.cameraRigController;
        if (!cameraRigController) return;

        CharacterBody body = cameraRigController.targetBody;
        if (!body) return;

        PlayerCharacterMasterController player = body.master != null ? body.master.playerCharacterMasterController : null;
        if (!player) return;

        foreach (var fadeBehavior in InstancesList)
        {
            if (fadeBehavior == null)
            {
                Debug.LogError("fadeBehavior is null");
                return;
            }
        }
        
        if(isPreCull)
            foreach (var fadeBehavior in InstancesList)
                fadeBehavior.RefreshForPreCull(player);
        else
            foreach (var fadeBehavior in InstancesList)
                fadeBehavior.RefreshForPreRender(player);
    }

    public void RefreshInstanceForCamera(SceneCamera sceneCamera)
    {
        CameraRigController cameraRigController = sceneCamera.cameraRigController;
        if (!cameraRigController) return;

        CharacterBody body = cameraRigController.targetBody;
        if (!body) return;

        PlayerCharacterMasterController player = body.master != null ? body.master.playerCharacterMasterController : null;
        if (!player) return;
        
        RefreshForPreRender(player);
        RefreshForPreCull(player);
    }
    
    private void Awake()
    {
        propertyStorage = new();
        RefreshComponentLists();
    }

    private void Start()
    {
        RefreshComponentLists();

        // if (lastCameraStaticPreCull != null)
        //     RefreshInstanceForCamera(lastCameraStaticPreCull);
        
        RefreshForPreCull(PlayerCharacterMasterController.instances[0]);
        RefreshForPreRender(PlayerCharacterMasterController.instances[0]);
    }

    private void OnEnable()
    {
        InstancesList.Add(this);
    }

    private void OnDisable()
    {
        InstancesList.Remove(this);
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

    private static IEnumerable<T> CustomGetComponents<T>(IEnumerable<GameObject> gameObjects)
    {
        return gameObjects.SelectMany(obj => obj.GetComponentsInChildren<T>());
    }

    public void RefreshComponentLists()
    {
        HashSet<GameObject> gameObjects = new(){gameObject};
        ModelLocator[] modelLocators = GetComponentsInChildren<ModelLocator>();
        gameObjects.UnionWith(modelLocators.Select(modelLocator => modelLocator.modelTransform.gameObject));
        gameObjects.UnionWith(CustomGetComponents<CostHologramContent>(gameObjects).ToArray().Select(hologram => hologram.targetTextMesh.gameObject));
        
        DitherModels = CustomGetComponents<DitherModel>(gameObjects).ToArray();
        DitherModelRenderers = new HashSet<Renderer>(DitherModels.SelectMany(ditherModel => ditherModel.renderers));
        Renderers = new HashSet<Renderer>(CustomGetComponents<Renderer>(gameObjects).Where(renderer => !DitherModelRenderers.Contains(renderer)));
        
        
        HashSet<Behaviour> componentsForPreCull = new(CustomGetComponents<Highlight>(gameObjects));
        componentsForPreCull.UnionWith(CustomGetComponents<Light>(gameObjects));
        
        ComponentsForPreCull = componentsForPreCull.ToArray();

        HashSet<Behaviour> componentsForPreRender = new();//CustomGetComponents<CostHologramContent>(gameObjects).Select(hologram => hologram.targetTextMesh));
        // componentsForPreRender.UnionWith(CustomGetComponents<TextMeshPro>(gameObjects));
        
        ComponentsForPreRender = componentsForPreRender.ToArray();
        
        //To force refresh:
        lastCamera = null;
        if(lastCameraStaticPreCull)
            RefreshForPreCull(lastCameraStaticPreCull);
        if(lastCameraStaticPreRender)
            RefreshForPreRender(lastCameraStaticPreRender);
    }

    public void RefreshForPreCull(PlayerCharacterMasterController player)
    {
        // return;
        if (gameObject == null)
        {
            Debug.LogError("gameObject is null on Visibility");
            return;
        }
        var instanceHandler = GetComponent<InstanceHandler>();
        bool isCopyObject = instanceHandler.ObjectInstanceMode == ObjectInstanceMode.CopyObject;

        if (isCopyObject)
        {
            bool isOrigForCurrent = instanceHandler.OrigPlayer == player;
            foreach (var renderer in Renderers)
            {
                if (renderer == null)
                {
                    Debug.LogError("renderer is null on Fade");
                    RefreshComponentLists();
                    return;
                }
                renderer.enabled = isOrigForCurrent;
            }
            
            foreach (var renderer in DitherModelRenderers)
            {
                if (renderer == null)
                {
                    Debug.LogError("ditherModelRenderer is null on Fade");
                    RefreshComponentLists();
                    return;
                }
                renderer.enabled = isOrigForCurrent;
            }
            
            foreach (var component in ComponentsForPreCull)
            {
                if (component == null)
                {
                    Debug.LogError("renderingComponent is null on Fade");
                    RefreshComponentLists();
                    return;
                }
                component.enabled = isOrigForCurrent;
            }
        }
    }
    
    public void RefreshForPreRender(PlayerCharacterMasterController player)
    {
        // return;
        var instanceHandler = GetComponent<InstanceHandler>();
        bool isForCurrentPlayer = instanceHandler.Players.Contains(player);
        float actualFadeLevel = isForCurrentPlayer ? 1.0f : FadeLevel;
        
        foreach (var renderer in Renderers)
        {
            if (renderer == null)
            {
                Debug.LogError("renderer is null on Fade");
                RefreshComponentLists();
                return;
            }
            renderer.GetPropertyBlock(propertyStorage);
            propertyStorage.SetFloat(Fade, actualFadeLevel);
            renderer.SetPropertyBlock(propertyStorage);
        }
        
        bool isCopyObject = instanceHandler.ObjectInstanceMode == ObjectInstanceMode.CopyObject;

        if (isCopyObject)
        {
            bool isOrigForCurrent = instanceHandler.OrigPlayer == player;
            
            foreach (var component in ComponentsForPreRender)
            {
                if (component == null)
                {
                    Debug.LogError("renderingComponent is null on PreRender");
                    RefreshComponentLists();
                    return;
                }
                component.enabled = isOrigForCurrent;
            }
        }
    }

    public static FadeBehavior Attach(GameObject obj)
    {
        FadeBehavior fadeBehavior = obj.GetComponent<FadeBehavior>();
        if (fadeBehavior != null)
        {
            fadeBehavior.lastCamera = null;
            // if(lastCameraStaticPreCull != null)
            //     fadeBehavior.RefreshInstanceForCamera(lastCameraStaticPreCull);
            return fadeBehavior;
        }

        fadeBehavior = obj.AddComponent<FadeBehavior>();
        return fadeBehavior;
    }
}