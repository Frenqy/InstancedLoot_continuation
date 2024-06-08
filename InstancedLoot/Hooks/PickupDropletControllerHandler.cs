using System;
using InstancedLoot.Components;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace InstancedLoot.Hooks;

/// <summary>
///     Handler for cases where PickupDropletController.CreatePickupDroplet is used.
///     Patches RoR2.PickupDropletController.CreatePickupDroplet.
///     Uses a global field to override the target.
/// </summary>
public class PickupDropletControllerHandler : AbstractHookHandler
{
    public InstanceInfoTracker.InstanceOverrideInfo? InstanceOverrideInfo;

    public override void RegisterHooks()
    {
        IL.RoR2.PickupDropletController.CreatePickupDroplet_CreatePickupInfo_Vector3 +=
            IL_PickupDropletController_CreatePickupDroplet;
        On.RoR2.PickupDropletController.OnCollisionEnter += On_PickupDropletController_OnCollisionEnter;
        IL.RoR2.PickupDropletController.CreateCommandCube += IL_PickupDropletController_CreateCommandCube;
    }

    public override void UnregisterHooks()
    {
        IL.RoR2.PickupDropletController.CreatePickupDroplet_CreatePickupInfo_Vector3 -=
            IL_PickupDropletController_CreatePickupDroplet;
        On.RoR2.PickupDropletController.OnCollisionEnter -= On_PickupDropletController_OnCollisionEnter;
        IL.RoR2.PickupDropletController.CreateCommandCube -= IL_PickupDropletController_CreateCommandCube;
    }

    private void IL_PickupDropletController_CreatePickupDroplet(ILContext il)
    {
        var cursor = new ILCursor(il);

        cursor.GotoNext(MoveType.After, i => i.MatchCall<UnityEngine.Object>("Instantiate"));
        cursor.Emit(OpCodes.Dup);
        cursor.EmitDelegate<Action<GameObject>>(obj =>
        {
            if (InstanceOverrideInfo.HasValue) Plugin.HandleInstancing(obj, InstanceOverrideInfo, isObject: false);
        });
    }
    
    private void On_PickupDropletController_OnCollisionEnter(On.RoR2.PickupDropletController.orig_OnCollisionEnter orig, PickupDropletController self, Collision collision)
    {
        if (self.GetComponent<InstanceInfoTracker>() is var instanceInfoTracker && instanceInfoTracker != null)
        {
            GenericPickupControllerHandler otherHandler = hookManager.GetHandler<GenericPickupControllerHandler>();
            otherHandler.InstanceOverrideInfo = instanceInfoTracker.Info;
            orig(self, collision);
            otherHandler.InstanceOverrideInfo = null;
        }
        else
        {
            orig(self, collision);
        }
    }

    private void IL_PickupDropletController_CreateCommandCube(ILContext il)
    {
        ILCursor cursor = new ILCursor(il);

        cursor.GotoNext(MoveType.Before, i => i.MatchCallOrCallvirt<NetworkServer>("Spawn"));
        cursor.Emit(OpCodes.Dup);
        cursor.Index++;
        cursor.EmitDelegate<Action<GameObject>>(gameObject =>
        {
            var genericPickupControllerHandler = hookManager.GetHandler<GenericPickupControllerHandler>();
            if (genericPickupControllerHandler.InstanceOverrideInfo != null) Plugin.HandleInstancing(gameObject, genericPickupControllerHandler.InstanceOverrideInfo.Value, isObject: false);
        });
    }
}