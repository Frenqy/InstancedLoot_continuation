using System;
using System.Linq;
using System.Reflection;
using InstancedLoot.Components;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using RoR2.ConVar;
using RoR2.UI;
using UnityEngine;

namespace InstancedLoot.Hooks;

/*
 * Info:
 * PingerController pings an object, unpinging it if it's disabled, creates and directs PingIndicator
 * PingIndicator is part of prefab with Highlight, directs Highlight to Renderer on target
 * Highlight is used in OutlineHightlight.RenderHighlights
 * RenderHighlights actually renders the highlights around target Renderer
 *
 * Some notes:
 * PingerController refuses to leave a PingIndicator if pinging a disabled/spent purchasable
 * PingerController is networked
 * PingIndicator handles sending the chat message, should be adjusted to local copy
 * Highlight only has one target
 * Checking for instances in RenderHighlights might be inefficient, might be viable with some caching
 *
 * Some ideas:
 * Could redirect ping in PingerController - make sure it's networked properly, but locally actually direct it elsewhere
 * Might need to adjust the check in PingerController to allow pinging spent interactables
 * Could redirect ping in PingIndicator - avoids networking shenanigans
 * Those options won't work properly for splitscreen
 * 
 * Could use FadeBehavior to redirect PingerController.pingIndicator.pingHighlight in PreRender
 * If it works, very simple, works with splitscreen, avoids messing with a lot of code
 * Probably need to tweak PingerController to allow pinging spent interactables
 * Also hide PingIndicator if current player's instance is spent 
 */

public class PingHandler : AbstractHookHandler
{
    public override void RegisterHooks()
    {
        On.RoR2.PingerController.RebuildPing += On_PingerController_RebuildPing;
        IL.RoR2.UI.PingIndicator.RebuildPing += IL_PingIndicator_RebuildPing;
        IL.RoR2.UI.PingIndicator.Update += IL_PingIndicator_Update;
        On.RoR2.PositionIndicator.UpdatePositions += On_PositionIndicator_UpdatePositions;
    }

    public override void UnregisterHooks()
    {
        On.RoR2.PingerController.RebuildPing -= On_PingerController_RebuildPing;
        IL.RoR2.UI.PingIndicator.RebuildPing -= IL_PingIndicator_RebuildPing;
        IL.RoR2.UI.PingIndicator.Update -= IL_PingIndicator_Update;
        On.RoR2.PositionIndicator.UpdatePositions -= On_PositionIndicator_UpdatePositions;
    }

    private void On_PingerController_RebuildPing(On.RoR2.PingerController.orig_RebuildPing orig, PingerController self, PingerController.PingInfo pingInfo)
    {
        if (self.GetComponent<PingerControllerRenderBehaviour>() == null) self.gameObject.AddComponent<PingerControllerRenderBehaviour>();

        orig(self, pingInfo);
    }

    private void IL_PingIndicator_RebuildPing(ILContext il)
    {
        var cursor = new ILCursor(il);
        
        var methodGetComponent = typeof(GameObject).GetMethods(
            BindingFlags.Instance | BindingFlags.Public
        ).First(method =>
            method.Name == nameof(GameObject.GetComponent)
            && method.IsGenericMethod
        );
        var methodGetComponentShopTerminalBehavior = methodGetComponent.MakeGenericMethod(typeof(ShopTerminalBehavior));
        var methodGetComponentPurchaseInteraction = methodGetComponent.MakeGenericMethod(typeof(PurchaseInteraction));

        while (cursor.TryGotoNext(MoveType.Before, i => i.MatchCallOrCallvirt(typeof(Chat), "AddMessage")))
        {
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Func<string, PingIndicator, string>>((str, self) =>
            {
                GameObject target = self.pingTarget;
                if (target)
                {
                    InstanceHandler instanceHandler = target.GetComponent<InstanceHandler>();

                    if (instanceHandler)
                        str +=
                            $" (Instanced: {string.Join(", ", instanceHandler.AllPlayers.Select(player => player.GetDisplayName()))})";

                    InstanceInfoTracker instanceInfoTracker = target.GetComponent<InstanceInfoTracker>();

                    if (instanceInfoTracker)
                        str =
                            $"{str} (InstanceInfo: {instanceInfoTracker.ObjectType}, {instanceInfoTracker.Owner?.GetDisplayName()}, {instanceInfoTracker.SourceItemIndex})";
                }
                
                return str;
            });
            cursor.Index++;
        }

        // cursor.Goto(0);
        cursor = new ILCursor(il);
        
        cursor.GotoNext(MoveType.Before, i => i.MatchCallOrCallvirt(methodGetComponentPurchaseInteraction));
        cursor.EmitDelegate<Func<GameObject, GameObject>>(pingTarget =>
        {
            InstanceHandler instanceHandler = pingTarget.GetComponent<InstanceHandler>();

            if (instanceHandler != null)
            {
                PlayerCharacterMasterController localPlayer = PlayerCharacterMasterController.instances.FirstOrDefault(player => player.hasAuthority);

                foreach (var linkedHandler in instanceHandler.LinkedHandlers)
                    if (linkedHandler.Players.Contains(localPlayer))
                    {
                        pingTarget = linkedHandler.gameObject;
                        break;
                    }
            }

            return pingTarget;
        });

        cursor.GotoNext(MoveType.Before, i => i.MatchCallOrCallvirt(methodGetComponentShopTerminalBehavior));
        cursor.EmitDelegate<Func<GameObject, GameObject>>(pingTarget =>
        {
            InstanceHandler instanceHandler = pingTarget.GetComponent<InstanceHandler>();

            if (instanceHandler != null)
            {
                PlayerCharacterMasterController localPlayer = PlayerCharacterMasterController.instances.FirstOrDefault(player => player.hasAuthority);

                foreach (var linkedHandler in instanceHandler.LinkedHandlers)
                    if (linkedHandler.Players.Contains(localPlayer))
                    {
                        pingTarget = linkedHandler.gameObject;
                        break;
                    }
            }

            return pingTarget;
        });
    }

    private void IL_PingIndicator_Update(ILContext il)
    {
        ILCursor cursor = new ILCursor(il);

        cursor.GotoNext(MoveType.Before, i => i.MatchLdfld<PingIndicator>("pingTargetPurchaseInteraction"),
            i => i.MatchLdfld<PurchaseInteraction>("available"));
        cursor.Index++;
        cursor.Remove();
        cursor.EmitDelegate((PurchaseInteraction purchaseInteraction) =>
        {
            bool shouldKeepAlive = false;

            InstanceHandler instanceHandler = purchaseInteraction.GetComponent<InstanceHandler>();

            if (instanceHandler == null)
                //Default behavior
                shouldKeepAlive = purchaseInteraction.available;
            else
                foreach (var instanceHandler2 in instanceHandler.LinkedHandlers)
                {
                    PurchaseInteraction purchaseInteraction2 = instanceHandler2.GetComponent<PurchaseInteraction>();

                    if (purchaseInteraction2 != null && purchaseInteraction2.available)
                    {
                        shouldKeepAlive = true;
                        break;
                    }
                }

            return shouldKeepAlive;
        });
    }

    private void On_PositionIndicator_UpdatePositions(On.RoR2.PositionIndicator.orig_UpdatePositions orig,
        UICamera uiCamera)
    {
        orig(uiCamera);


        if (uiCamera.cameraRigController is var cameraRigController && cameraRigController
                                                                    && cameraRigController.targetBody is var body &&
                                                                    body
                                                                    && body.master is var master && master
                                                                    && master.playerCharacterMasterController is var
                                                                        player && player)
        {
            foreach (var positionIndicator in PositionIndicator.instancesList)
            {
                if (positionIndicator.targetTransform is var transform && transform
                                                                       && !Utils.IsObjectInteractibleForPlayer(
                                                                           transform.gameObject, player)
                   )
                {
                    if(positionIndicator.insideViewObject)
                        positionIndicator.insideViewObject.SetActive(value: false);
                    if(positionIndicator.outsideViewObject)
			            positionIndicator.outsideViewObject.SetActive(value: false);
                    if(positionIndicator.alwaysVisibleObject)
			            positionIndicator.alwaysVisibleObject.SetActive(value: false);
                }
            }
        }
    }
}