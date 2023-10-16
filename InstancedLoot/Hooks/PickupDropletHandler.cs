using System;
using InstancedLoot.Components;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using UnityEngine;

namespace InstancedLoot.Hooks;

/// <summary>
///     Handler for cases where PickupDropletController.CreatePickupDroplet is used.
///     Patches RoR2.PickupDropletController.CreatePickupDroplet.
///     Uses a global field to override the target.
/// </summary>
public class PickupDropletHandler : AbstractHookHandler
{
    public InstanceInfoTracker.InstanceOverrideInfo? InstanceOverrideInfo;

    public override void RegisterHooks()
    {
        IL.RoR2.PickupDropletController.CreatePickupDroplet_CreatePickupInfo_Vector3_Vector3 +=
            IL_PickupDropletController_CreatePickupDroplet;
        IL.RoR2.PickupDropletController.OnCollisionEnter += ModifyDropletCollision;
    }

    public override void UnregisterHooks()
    {
        IL.RoR2.PickupDropletController.CreatePickupDroplet_CreatePickupInfo_Vector3_Vector3 -=
            IL_PickupDropletController_CreatePickupDroplet;
        IL.RoR2.PickupDropletController.OnCollisionEnter -= ModifyDropletCollision;
    }

    private void IL_PickupDropletController_CreatePickupDroplet(ILContext il)
    {
        var cursor = new ILCursor(il);

        cursor.GotoNext(MoveType.After, i => i.MatchCall<UnityEngine.Object>("Instantiate"));
        cursor.Emit(OpCodes.Dup);
        cursor.EmitDelegate<Action<GameObject>>(obj =>
        {
            if (InstanceOverrideInfo.HasValue)
            {
                Plugin.HandleInstancing(obj, InstanceOverrideInfo);
                // InstanceOverrideInfo.Value.AttachTo(obj);
            }
        });
    }
    
    private void ModifyDropletCollision(ILContext il)
    {
        var cursor = new ILCursor(il);

        cursor.GotoNext(MoveType.After, i => i.MatchCall<GenericPickupController>("CreatePickup"));
        cursor.Emit(OpCodes.Dup);
        cursor.Emit(OpCodes.Ldarg_0);
        cursor.EmitDelegate<Action<GenericPickupController, PickupDropletController>>(
            (pickupController, self) =>
            {
                var instanceOverride = self.GetComponent<InstanceInfoTracker>();
                if (instanceOverride)
                {
                    Plugin._logger.LogWarning($"(InstanceInfo: {instanceOverride.ItemSource}, {instanceOverride.Owner}, {instanceOverride.SourceItemIndex})");
                    Plugin.HandleInstancing(pickupController.gameObject, instanceOverride.Info);
                    // instanceOverride.Info.AttachTo(pickupController.gameObject);
                }
            });
    }
}