using System.Collections.Generic;
using System.Linq;
using InstancedLoot.Enums;
using RoR2;
using RoR2.UI;
using UnityEngine;

namespace InstancedLoot.Components;

public class PingerControllerRenderBehaviour : InstancedLootBehaviour
{
    public static readonly List<PingerControllerRenderBehaviour> InstancesList = new();
    
    private PingerController pingerController;
    
    static PingerControllerRenderBehaviour()
    {
        SceneCamera.onSceneCameraPreCull += PreCullAll;
        SceneCamera.onSceneCameraPreRender += PreRenderAll;
    }

    public static void PreCullAll(SceneCamera camera)
    {
        RefreshAllInstances(camera, true);
    }

    public static void PreRenderAll(SceneCamera camera)
    {
        RefreshAllInstances(camera, false);
    }

    public static void RefreshAllInstances(SceneCamera camera, bool isPreCull)
    {
        CameraRigController cameraRigController = camera.cameraRigController;
        if (!cameraRigController) return;

        CharacterBody body = cameraRigController.targetBody;
        if (!body) return;

        PlayerCharacterMasterController player = body.master != null ? body.master.playerCharacterMasterController : null;
        if (!player) return;

        if(isPreCull)
            foreach (var pingerControllerRenderBehaviour in InstancesList)
                pingerControllerRenderBehaviour.PreCull(player);
        else
            foreach (var pingerControllerRenderBehaviour in InstancesList)
                pingerControllerRenderBehaviour.PreRender(player);
    }

    public void Awake()
    {
        pingerController = GetComponent<PingerController>();
    }

    public void OnEnable()
    {
        InstancesList.Add(this);
    }

    public void OnDisable()
    {
        InstancesList.Remove(this);
    }

    private PlayerCharacterMasterController lastPlayer;
    private PingIndicator lastPingIndicator;
    private GameObject lastPingTarget;
    private Renderer[] cachedRenderers;

    public void PreCull(PlayerCharacterMasterController player)
    {
        if (pingerController == null)
        {
            Destroy(this);
            return;
        }

        PingIndicator pingIndicator = pingerController.pingIndicator;
        GameObject pingTarget = pingIndicator != null ? pingIndicator.pingTarget : null;

        if (lastPlayer == player && lastPingIndicator == pingIndicator && lastPingTarget == pingTarget)
            return;

        if (pingIndicator != null)
        {
            bool shouldRender = true;

            if (pingTarget != null
                && !pingTarget.GetComponent<GenericPickupController>() // For some reason this breaks pinging items
                && pingTarget.GetComponent<InstanceHandler>() is var instanceHandler && instanceHandler)
                shouldRender = instanceHandler.AllPlayers.Contains(player);

            if(lastPingIndicator != pingIndicator || cachedRenderers == null)
                cachedRenderers = pingIndicator.GetComponentsInChildren<Renderer>();

            foreach (var renderer in cachedRenderers)
            {
                renderer.enabled = shouldRender;
            }
        }
    }

    public void PreRender(PlayerCharacterMasterController player)
    {
        if (pingerController == null)
            return;
        
        PingIndicator pingIndicator = pingerController.pingIndicator;
        GameObject pingTarget = pingIndicator != null ? pingIndicator.pingTarget : null;

        if (lastPlayer == player && lastPingIndicator == pingIndicator && lastPingTarget == pingTarget)
            return;

        if (pingIndicator != null && pingTarget != null)
        {
            InstanceHandler instanceHandler = pingTarget.GetComponent<InstanceHandler>();

            if (instanceHandler != null && instanceHandler.ObjectInstanceMode == ObjectInstanceMode.CopyObject)
            {
                //Highlight highlight = pingIndicator.pingHighlight;

                Renderer targetRenderer = null;
                
                InstanceHandler targetInstanceHandler = instanceHandler.LinkedHandlers.Where(handler => handler.Players.Contains(player)).FirstOrDefault(handler =>
                {
                    if (handler.GetComponent<IInteractable>() is var interactable && interactable != null
                        && player.body is var body && body
                        && body.GetComponent<Interactor>() is var interactor && interactor)
                    {
                        if (interactable.GetInteractability(interactor) == Interactability.Disabled)
                            return false;
                    }
                    
                    return true;
                });

                if (targetInstanceHandler != null)
                {
                    ModelLocator modelLocator = targetInstanceHandler.GetComponent<ModelLocator>();
                    
                    targetRenderer = modelLocator != null
                        ? modelLocator.modelTransform.GetComponentInChildren<Renderer>()
                        : pingTarget.GetComponentInChildren<Renderer>();
                }

                //highlight.targetRenderer = targetRenderer;
            }
        }

        lastPlayer = player;
        lastPingIndicator = pingIndicator;
        lastPingTarget = pingTarget;
    }
}