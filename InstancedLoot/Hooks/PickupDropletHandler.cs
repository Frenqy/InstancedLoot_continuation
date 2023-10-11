using System;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using UnityEngine;
using Object = UnityEngine.Object;

namespace InstancedLoot.Hooks;

/// <summary>
///     Handler for cases where PickupDropletController.CreatePickupDroplet is used.
///     Patches RoR2.PickupDropletController.CreatePickupDroplet.
///     Uses a global field to override the target.
/// </summary>
public class PickupDropletHandler : AbstractHookHandler
{
    public InstanceOverride.InstanceOverrideInfo? InstanceOverrideInfo;

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

        cursor.GotoNext(MoveType.After, i => i.MatchCall<Object>("Instantiate"));
        cursor.Emit(OpCodes.Dup);
        cursor.EmitDelegate<RuntimeILReferenceBag.FastDelegateInvokers.Action<GameObject>>(obj =>
        {
            if (InstanceOverrideInfo.HasValue)
            {
                InstanceOverrideInfo.Value.AttachTo(obj);
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
                var instanceOverride = self.GetComponent<InstanceOverride>();
                if (instanceOverride)
                    instanceOverride.Info.AttachTo(pickupController.gameObject);
            });
    }
}