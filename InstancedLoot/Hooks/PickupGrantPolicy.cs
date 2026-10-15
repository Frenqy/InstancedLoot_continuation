using InstancedLoot.Components;
using RoR2;

namespace InstancedLoot.Hooks;

internal static class PickupGrantPolicy
{
    internal static bool CanGrant(GenericPickupController self, CharacterBody body)
    {
        var instanceHandler = self.GetComponent<InstanceHandler>();
        if (!instanceHandler) return true;

        var player = body && body.master ? body.master.playerCharacterMasterController : null;
        return player && instanceHandler.Players.Contains(player);
    }

    internal static void OnGranted(ref PickupDef.GrantContext context, GenericPickupController self, CharacterBody body)
    {
        if (!context.shouldDestroy || !body || !body.master) return;

        var player = body.master.playerCharacterMasterController;
        var instanceHandler = self.GetComponent<InstanceHandler>();
        if (player && instanceHandler)
        {
            instanceHandler.RemovePlayer(player);
            if (instanceHandler.AllPlayers.Count > 0)
                context.shouldDestroy = false;
        }
    }
}
