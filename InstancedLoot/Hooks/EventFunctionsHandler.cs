using InstancedLoot.Components;
using RoR2;

namespace InstancedLoot.Hooks;

public class EventFunctionsHandler : AbstractHookHandler
{
    public override void RegisterHooks()
    {
        // On.RoR2.EventFunctions.DestroySelf += On_EventFunctions_DestroySelf;
    }

    public override void UnregisterHooks()
    {
        // On.RoR2.EventFunctions.DestroySelf -= On_EventFunctions_DestroySelf;
    }

    private void On_EventFunctions_DestroySelf(On.RoR2.EventFunctions.orig_DestroySelf orig, EventFunctions self)
    {
        Plugin._logger.LogDebug("DestroySelf called");
        
        var instanceHandler = self.GetComponent<InstanceHandler>();
        if (instanceHandler && instanceHandler.Players.Count > 0)
        {
            Plugin._logger.LogDebug("DestroySelf - object is instanced");
            //Don't immediately destroy PickupPickerControllers that are still instanced
            if (self.GetComponent<PickupPickerController>()) return;
        }

        Plugin._logger.LogDebug("DestroySelf destroying object");
        orig(self);
    }
}