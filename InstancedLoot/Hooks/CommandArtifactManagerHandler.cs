using System;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using UnityEngine;
using UnityEngine.Networking;

namespace InstancedLoot.Hooks;

public class CommandArtifactManagerHandler : AbstractHookHandler
{
    public override void RegisterHooks()
    {
        IL.RoR2.Artifacts.CommandArtifactManager.OnDropletHitGroundServer +=
            IL_CommandArtifactManager_OnDropletHitGroundServer;
    }

    public override void UnregisterHooks()
    {
        IL.RoR2.Artifacts.CommandArtifactManager.OnDropletHitGroundServer -=
            IL_CommandArtifactManager_OnDropletHitGroundServer;
    }

    private void IL_CommandArtifactManager_OnDropletHitGroundServer(ILContext il)
    {
        ILCursor cursor = new ILCursor(il);

        cursor.GotoNext(MoveType.Before, i => i.MatchCallOrCallvirt<NetworkServer>("Spawn"));
        cursor.Emit(OpCodes.Dup);
        cursor.Index++;
        cursor.EmitDelegate<Action<GameObject>>(gameObject =>
        {
            var genericPickupControllerHandler = hookManager.GetHandler<GenericPickupControllerHandler>();
            if (genericPickupControllerHandler.InstanceOverrideInfo != null) Plugin.HandleInstancing(gameObject, genericPickupControllerHandler.InstanceOverrideInfo.Value);
        });
    }
}