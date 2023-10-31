using System;
using System.Linq;
using System.Reflection;
using InstancedLoot.Components;
using RoR2;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace InstancedLoot.Hooks;

public class PickupPickerControllerHandler : AbstractHookHandler
{
    public override void RegisterHooks()
    {
        On.RoR2.PickupPickerController.Awake += On_PickupPickerController_Awake;
        On.RoR2.PickupPickerController.GetInteractability += On_PickupPickerController_GetInteractability;
        On.RoR2.PickupPickerController.CreatePickup_PickupIndex += On_PickupPickerController_CreatePickup_PickupIndex;
    }

    public override void UnregisterHooks()
    {
        On.RoR2.PickupPickerController.Awake -= On_PickupPickerController_Awake;
        On.RoR2.PickupPickerController.GetInteractability -= On_PickupPickerController_GetInteractability;
        On.RoR2.PickupPickerController.CreatePickup_PickupIndex -= On_PickupPickerController_CreatePickup_PickupIndex;
    }

    private UnityAction<int> GenerateHandleDestroy(PickupPickerController self)
    {
        return HandleDestroy;
        
        void HandleDestroy(int pickupIndex)
        {
            InstanceHandler instanceHandler = self.GetComponent<InstanceHandler>();
            
            if (instanceHandler != null)
            {
                if (instanceHandler.Players.Count > 0)
                    return;
            }
            
            UnityEngine.Object.Destroy(self.gameObject);
        }
    }

    private void On_PickupPickerController_Awake(On.RoR2.PickupPickerController.orig_Awake orig, PickupPickerController self)
    {
        orig(self);

        if (NetworkServer.active)
        {
            Plugin.HandleInstancingNextTick(self.gameObject, null);
            
            var onPickupSelected = self.onPickupSelected;
            int eventCount = onPickupSelected.GetPersistentEventCount();
            
            bool hasDestroySelf = false;
            
            for (int i = 0; i < eventCount; i++)
            {
                if (onPickupSelected.GetPersistentTarget(i) is EventFunctions target &&
                    onPickupSelected.GetPersistentMethodName(i) == "DestroySelf")
                {
                    onPickupSelected.SetPersistentListenerState(i, UnityEventCallState.Off);
                    
                    hasDestroySelf = true;
                    break;
                }
            }
            
            if (hasDestroySelf)
            {
                onPickupSelected.AddListener(GenerateHandleDestroy(self));
            }
        }
    }

    private Interactability On_PickupPickerController_GetInteractability(On.RoR2.PickupPickerController.orig_GetInteractability orig, PickupPickerController self, Interactor activator)
    {
        var interactability = orig(self, activator);

        var body = activator.GetComponent<CharacterBody>();
        if (body)
        {
            var player = body.master.GetComponent<PlayerCharacterMasterController>();
            var instanceHandler = self.GetComponent<InstanceHandler>();
            if (player && instanceHandler)
            {
                if (!instanceHandler.Players.Contains(player))
                    interactability = Interactability.Disabled;
            }
        }

        return interactability;
    }

    private void On_PickupPickerController_CreatePickup_PickupIndex(On.RoR2.PickupPickerController.orig_CreatePickup_PickupIndex orig, PickupPickerController self, PickupIndex pickupindex)
    {
        InstanceHandler instanceHandler = self.GetComponent<InstanceHandler>();
        InstanceInfoTracker instanceInfoTracker = self.GetComponent<InstanceInfoTracker>();
        
        if (NetworkServer.active && instanceHandler != null && instanceInfoTracker != null)
        {
            CharacterMaster master = self.networkUIPromptController.currentParticipantMaster;
            if (master == null) return;

            PlayerCharacterMasterController player = master.playerCharacterMasterController;
            if (player == null) return;

            if (!instanceHandler.Players.Contains(player)) return;
            
            InstanceInfoTracker.InstanceOverrideInfo info = instanceInfoTracker.Info;
            if (Plugin.ModConfig.SharePickupPickers.Value)
            {
                info.PlayerOverride = instanceHandler.Players.ToArray();
                instanceHandler.Players.Clear();
            }
            else
            {
                info.PlayerOverride = new[] { player };
                instanceHandler.RemovePlayer(player);
            }
            
            GenericPickupControllerHandler genericPickupControllerHandler =
                hookManager.GetHandler<GenericPickupControllerHandler>();
            genericPickupControllerHandler.InstanceOverrideInfo = info;
            orig(self, pickupindex);
            genericPickupControllerHandler.InstanceOverrideInfo = null;

            if (self.networkUIPromptController is var networkUIPromptController && networkUIPromptController != null)
            {
                networkUIPromptController.ClearParticipant();
            }
        }
        else if (instanceInfoTracker != null)
        {
            GenericPickupControllerHandler genericPickupControllerHandler =
                hookManager.GetHandler<GenericPickupControllerHandler>();
            genericPickupControllerHandler.InstanceOverrideInfo = instanceInfoTracker.Info;
            orig(self, pickupindex);
            genericPickupControllerHandler.InstanceOverrideInfo = null;
        }
        else
        {
            orig(self, pickupindex);
        }
    }
}