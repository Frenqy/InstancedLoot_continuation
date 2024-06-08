using System;
using InstancedLoot.Components;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace InstancedLoot.Hooks;

public class GenericPickupControllerHandler : AbstractHookHandler
{
    public InstanceInfoTracker.InstanceOverrideInfo? InstanceOverrideInfo;
    
    public override void RegisterHooks()
    {
        On.RoR2.GenericPickupController.Start += On_GenericPickupController_Start;
        On.RoR2.GenericPickupController.GetInteractability += On_GenericPickupController_GetInteractability;
        On.RoR2.GenericPickupController.OnTriggerStay += On_GenericPickupController_OnTriggerStay;
        IL.RoR2.GenericPickupController.AttemptGrant += IL_GenericPickupController_AttemptGrant;
        IL.RoR2.GenericPickupController.CreatePickup += IL_GenericPickupController_CreatePickup;
    }

    public override void UnregisterHooks()
    {
        On.RoR2.GenericPickupController.Start -= On_GenericPickupController_Start;
        On.RoR2.GenericPickupController.GetInteractability -= On_GenericPickupController_GetInteractability;
        On.RoR2.GenericPickupController.OnTriggerStay -= On_GenericPickupController_OnTriggerStay;
        IL.RoR2.GenericPickupController.AttemptGrant -= IL_GenericPickupController_AttemptGrant;
        IL.RoR2.GenericPickupController.CreatePickup -= IL_GenericPickupController_CreatePickup;
    }

    private void On_GenericPickupController_Start(On.RoR2.GenericPickupController.orig_Start orig,
        GenericPickupController self)
    {
        if (NetworkServer.active) 
            Plugin.HandleInstancing(self.gameObject, isObject: false);

        orig(self);
    }

    private Interactability On_GenericPickupController_GetInteractability(On.RoR2.GenericPickupController.orig_GetInteractability orig, GenericPickupController self, Interactor activator)
    {
        var interactability = orig(self, activator);

        var body = activator.GetComponent<CharacterBody>();
        if (body)
        {
            var player = body.master.GetComponent<PlayerCharacterMasterController>();
            var instanceHandler = self.GetComponent<InstanceHandler>();
            if (player && instanceHandler)
                if (!instanceHandler.Players.Contains(player))
                    interactability = Interactability.Disabled;
        }

        return interactability;
    }

    private void On_GenericPickupController_OnTriggerStay(On.RoR2.GenericPickupController.orig_OnTriggerStay orig, GenericPickupController self, Collider other)
    {
        var interactor = other.GetComponent<Interactor>();

        if (interactor)
        {
            var interactability = self.GetInteractability(interactor);

            if (interactability != Interactability.Available) return;
        }

        orig(self, other);
    }

    private void IL_GenericPickupController_AttemptGrant(ILContext il)
    {
        var cursor = new ILCursor(il);

        cursor.GotoNext(MoveType.After, i => i.MatchLdfld<PickupDef.GrantContext>("shouldDestroy"));
        cursor.Emit(OpCodes.Ldarg_0);
        cursor.Emit(OpCodes.Ldarg_1);
        cursor.EmitDelegate<Func<bool, GenericPickupController, CharacterBody, bool>>((shouldDestroy, self, body) =>
        {
            var player = body.master.GetComponent<PlayerCharacterMasterController>();
            var instanceHandler = self.GetComponent<InstanceHandler>();
            if (player && instanceHandler)
            {
                instanceHandler.RemovePlayer(player);
                if(instanceHandler.AllPlayers.Count > 0)
                    shouldDestroy = false;
            }
            
            return shouldDestroy;
        });
    }

    private delegate void IL_GenericPickupController_CreatePickup_Delegate(GameObject gameObject, ref GenericPickupController.CreatePickupInfo createPickupInfo);

    private void IL_GenericPickupController_CreatePickup(ILContext il)
    {
        ILCursor cursor = new ILCursor(il);

        cursor.GotoNext(MoveType.After, i => i.MatchCallOrCallvirt<UnityEngine.Object>("Instantiate"));

        cursor.Emit(OpCodes.Dup);
        cursor.Emit(OpCodes.Ldarg_0);
        cursor.EmitDelegate<IL_GenericPickupController_CreatePickup_Delegate>((GameObject obj, ref GenericPickupController.CreatePickupInfo createPickupInfo) =>
        {
            InstanceOverrideInfo?.AttachTo(obj);

            if (obj != null)
            {
                CreatePickupInfoTracker createPickupInfoTracker =
                    obj.gameObject.AddComponent<CreatePickupInfoTracker>();
                createPickupInfoTracker.CreatePickupInfo = createPickupInfo;
            }
        });
    }
}