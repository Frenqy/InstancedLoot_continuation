using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using InstancedLoot.Enums;
using RoR2;
using RoR2.Hologram;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace InstancedLoot.Components;

public class FadeBehavior : InstancedLootBehaviour
{
    private static readonly int Fade = Shader.PropertyToID("_Fade");
    
    public float FadeLevel = 0.3f;

    private bool needsRefresh = true;

    public HashSet<GameObject> ExtraGameObjects = new();
    
    public HashSet<Renderer> Renderers;
    public HashSet<Renderer> DitherModelRenderers;
    public DitherModel[] DitherModels;
    private MaterialPropertyBlock propertyStorage;
    
    public Behaviour[] ComponentsForPreCull;
    // public Behaviour[] ComponentsForPreRender;

    public static readonly List<FadeBehavior> InstancesList = new();
    
    private CameraRigController lastCameraRigController;
    private PlayerCharacterMasterController lastPlayer;
    private bool lastVisible;

    private bool isBeingDestroyed = false;

    static FadeBehavior()
    {
        SceneCamera.onSceneCameraPreCull += RefreshForPreCull;
        SceneCamera.onSceneCameraPreRender += RefreshForPreRender;
    }

    public void OnDestroy()
    {
        isBeingDestroyed = true;

        RefreshForPreCull(lastPlayer);
        RefreshForPreRender(lastPlayer);
    }

    public static void RefreshForPreCull(SceneCamera sceneCamera)
    {
        RefreshAllInstances(sceneCamera, true);
    }

    public static void RefreshForPreRender(SceneCamera sceneCamera)
    {
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
    }

    private void Start()
    {
        Refresh();
    }

    private void OnEnable()
    {
        InstancesList.Add(this);
    }

    private void OnDisable()
    {
        InstancesList.Remove(this);
    }

    public float GetFadeLevelForCameraRigController(CameraRigController cameraRigController)
    {
        if(needsRefresh)
            RefreshComponentLists();
        
        bool isVisible;

        if (lastCameraRigController == cameraRigController)
            isVisible = lastVisible;
        else
        {
            CharacterBody body = cameraRigController.targetBody;
            if (!body) return FadeLevel;

            PlayerCharacterMasterController player =
                body.master != null ? body.master.playerCharacterMasterController : null;
            if (!player) return FadeLevel;

            isVisible = GetComponent<InstanceHandler>().IsInstancedFor(player);
            lastVisible = isVisible;
        }

        lastCameraRigController = cameraRigController;

        return isVisible ? 1.0f : FadeLevel;
    }

    private static IEnumerable<T> CustomGetComponents<T>(IEnumerable<GameObject> gameObjects)
    {
        return gameObjects.SelectMany(obj => obj.GetComponentsInChildren<T>());
    }

    public void RefreshNextFrame()
    {
        StartCoroutine(RefreshNextFrameCoroutine());
        return;

        IEnumerator RefreshNextFrameCoroutine()
        {
            yield return 0;
            Refresh();
        }
    }

    public void Refresh()
    {
        needsRefresh = true;
    }

    public void RefreshComponentLists()
    {
        needsRefresh = false;
        lastPlayer = null;
        lastCameraRigController = null;

        ExtraGameObjects.RemoveWhere(obj => obj == null);
        
        HashSet<GameObject> gameObjects = [gameObject];
        gameObjects.UnionWith(ExtraGameObjects);
        
        ModelLocator[] modelLocators = GetComponentsInChildren<ModelLocator>();
        gameObjects.UnionWith(modelLocators.Select(modelLocator => modelLocator.modelTransform.gameObject));
        gameObjects.UnionWith(CustomGetComponents<CostHologramContent>(gameObjects).ToArray().Select(hologram => hologram.targetTextMesh.gameObject));
        
        DitherModels = CustomGetComponents<DitherModel>(gameObjects).ToArray();
        DitherModelRenderers = [..DitherModels.SelectMany(ditherModel => ditherModel.renderers)];
        Renderers =
            [..CustomGetComponents<Renderer>(gameObjects).Where(renderer => !DitherModelRenderers.Contains(renderer))];
        
        HashSet<Behaviour> componentsForPreCull = new(CustomGetComponents<Highlight>(gameObjects));
        componentsForPreCull.UnionWith(CustomGetComponents<Light>(gameObjects));
        
        ComponentsForPreCull = componentsForPreCull.ToArray();

        // HashSet<Behaviour> componentsForPreRender = new();
        
        // ComponentsForPreRender = componentsForPreRender.ToArray();

        foreach (var renderer in Renderers)
        {
            foreach (var material in renderer.materials)
            {
                material.EnableKeyword("DITHER");
            }
        }
    }

    public void RefreshForPreCull(PlayerCharacterMasterController player)
    {
        if (needsRefresh)
            RefreshComponentLists();
        
        if (player == lastPlayer && !isBeingDestroyed)
            return;
        
        var instanceHandler = GetComponent<InstanceHandler>();
        bool isCopyObject = instanceHandler != null ? instanceHandler.ObjectInstanceMode == ObjectInstanceMode.CopyObject : true;

        if (isCopyObject)
        {
            bool isOrigForCurrent = isBeingDestroyed || instanceHandler.OrigPlayer == player;
            foreach (var renderer in Renderers)
            {
                if (renderer == null)
                {
                    if (isBeingDestroyed) continue;
                    RefreshComponentLists();
                    return;
                }
                renderer.enabled = isOrigForCurrent;
            }
            
            foreach (var renderer in DitherModelRenderers)
            {
                if (renderer == null)
                {
                    if (isBeingDestroyed) continue;
                    RefreshComponentLists();
                    return;
                }
                renderer.enabled = isOrigForCurrent;
            }
            
            foreach (var component in ComponentsForPreCull)
            {
                if (component == null)
                {
                    if (isBeingDestroyed) continue;
                    RefreshComponentLists();
                    return;
                }
                component.enabled = isOrigForCurrent;
            }
        }
    }
    
    public void RefreshForPreRender(PlayerCharacterMasterController player)
    {
        if (needsRefresh)
            RefreshComponentLists();
        
        if (player == lastPlayer && !isBeingDestroyed)
            return;
        
        var instanceHandler = GetComponent<InstanceHandler>();
        bool isForCurrentPlayer = isBeingDestroyed || instanceHandler.IsInstancedFor(player);
        float actualFadeLevel = isForCurrentPlayer ? 1.0f : FadeLevel;
        
        foreach (var renderer in Renderers)
        {
            if (renderer == null)
            {
                if (isBeingDestroyed) continue;
                RefreshComponentLists();
                return;
            }
            renderer.GetPropertyBlock(propertyStorage);
            propertyStorage.SetFloat(Fade, actualFadeLevel);
            renderer.SetPropertyBlock(propertyStorage);
        }

        // bool isCopyObject = instanceHandler != null ? instanceHandler.ObjectInstanceMode == ObjectInstanceMode.CopyObject : true;
        //
        // if (isCopyObject)
        // {
        //     bool isOrigForCurrent = isBeingDestroyed || instanceHandler.OrigPlayer == player;
        //     
        //     foreach (var component in ComponentsForPreRender)
        //     {
        //         if (component == null)
        //         {
        //             if (isBeingDestroyed) continue;
        //             RefreshComponentLists();
        //             return;
        //         }
        //         component.enabled = isOrigForCurrent;
        //     }
        // }

        lastPlayer = player;
    }

    public static FadeBehavior Attach(GameObject obj)
    {
        FadeBehavior fadeBehavior = obj.GetComponent<FadeBehavior>();
        if (fadeBehavior != null)
        {
            fadeBehavior.Refresh();
            return fadeBehavior;
        }

        fadeBehavior = obj.AddComponent<FadeBehavior>();
        return fadeBehavior;
    }
}