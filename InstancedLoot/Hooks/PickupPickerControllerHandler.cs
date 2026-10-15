using System.Collections;
using System.Linq;
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
        On.RoR2.PickupPickerController.CreatePickup_UniquePickup += On_PickupPickerController_CreatePickup_UniquePickup;
    }

    public override void UnregisterHooks()
    {
        On.RoR2.PickupPickerController.Awake -= On_PickupPickerController_Awake;
        On.RoR2.PickupPickerController.GetInteractability -= On_PickupPickerController_GetInteractability;
        On.RoR2.PickupPickerController.CreatePickup_UniquePickup -= On_PickupPickerController_CreatePickup_UniquePickup;
    }

    private void ReplaceDestroySelf<T>(PickupPickerController self, UnityEvent<T> onPickupSelected)
    {
        if (onPickupSelected == null) return;

        for (int i = 0; i < onPickupSelected.GetPersistentEventCount(); i++)
            if (onPickupSelected.GetPersistentTarget(i) is EventFunctions &&
                onPickupSelected.GetPersistentMethodName(i) == "DestroySelf")
            {
                onPickupSelected.SetPersistentListenerState(i, UnityEventCallState.Off);
                onPickupSelected.AddListener(HandleDestroy);
                break;
            }
        
        void HandleDestroy(T pickup)
        {
            InstanceHandler instanceHandler = self.GetComponent<InstanceHandler>();
            
            if (instanceHandler != null)
                if (instanceHandler.Players.Count > 0)
                    return;

            UnityEngine.Object.Destroy(self.gameObject);
        }
    }

    private void On_PickupPickerController_Awake(On.RoR2.PickupPickerController.orig_Awake orig, PickupPickerController self)
    {
        orig(self);

        if (NetworkServer.active)
        {
            IEnumerator coroutine()
            {
                yield return 0;
                Plugin.HandleInstancing(self.gameObject, isObject: false);
            }

            self.StartCoroutine(coroutine());

            ReplaceDestroySelf(self, self.onUniquePickupSelected);
#pragma warning disable CS0618 // Older prefabs can still have a serialized DestroySelf listener here.
            ReplaceDestroySelf(self, self.onPickupSelected);
#pragma warning restore CS0618
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
                if (!instanceHandler.Players.Contains(player))
                    interactability = Interactability.Disabled;
        }

        return interactability;
    }

    private void On_PickupPickerController_CreatePickup_UniquePickup(On.RoR2.PickupPickerController.orig_CreatePickup_UniquePickup orig, PickupPickerController self, UniquePickup pickup)
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
                instanceHandler.SetPlayers(System.Array.Empty<PlayerCharacterMasterController>());
            }
            else
            {
                info.PlayerOverride = new[] { player };
                instanceHandler.RemovePlayer(player);
            }
            
            GenericPickupControllerHandler genericPickupControllerHandler =
                hookManager.GetHandler<GenericPickupControllerHandler>();
            var previousInfo = genericPickupControllerHandler.InstanceOverrideInfo;
            try
            {
                genericPickupControllerHandler.InstanceOverrideInfo = info;
                orig(self, pickup);
            }
            finally
            {
                genericPickupControllerHandler.InstanceOverrideInfo = previousInfo;
            }

            if (self.networkUIPromptController is var networkUIPromptController && networkUIPromptController != null) networkUIPromptController.ClearParticipant();
        }
        else if (instanceInfoTracker != null)
        {
            GenericPickupControllerHandler genericPickupControllerHandler =
                hookManager.GetHandler<GenericPickupControllerHandler>();
            var previousInfo = genericPickupControllerHandler.InstanceOverrideInfo;
            try
            {
                genericPickupControllerHandler.InstanceOverrideInfo = instanceInfoTracker.Info;
                orig(self, pickup);
            }
            finally
            {
                genericPickupControllerHandler.InstanceOverrideInfo = previousInfo;
            }
        }
        else
        {
            orig(self, pickup);
        }
    }
}
