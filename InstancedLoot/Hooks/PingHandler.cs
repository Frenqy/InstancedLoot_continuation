using System;
using System.Linq;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using RoR2.UI;
using UnityEngine;

namespace InstancedLoot.Hooks;

public class PingHandler : AbstractHookHandler
{
    public override void RegisterHooks()
    {
        IL.RoR2.UI.PingIndicator.RebuildPing += IL_PingIndicator_RebuildPing;
    }
    
    public override void UnregisterHooks()
    {
        IL.RoR2.UI.PingIndicator.RebuildPing -= IL_PingIndicator_RebuildPing;
    }

    private void IL_PingIndicator_RebuildPing(ILContext il)
    {
        var cursor = new ILCursor(il);

        while (cursor.TryGotoNext(MoveType.Before, i => i.MatchCallOrCallvirt(typeof(Chat), "AddMessage")))
        {
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Func<string, PingIndicator, string>>((str, self) =>
            {
                GameObject target = self.pingTarget;
                InstanceHandler instanceHandler = target.GetComponent<InstanceHandler>();

                if (instanceHandler)
                {
                    str =
                        $"{str} (Instanced: {string.Join(", ", instanceHandler.Players.Select(player => player.GetDisplayName()))})";
                }

                InstanceInfoTracker instanceInfoTracker = target.GetComponent<InstanceInfoTracker>();

                if (instanceInfoTracker)
                {
                    str = $"{str} (InstanceInfo: {instanceInfoTracker.ItemSource}, {instanceInfoTracker.Owner}, {instanceInfoTracker.SourceItemIndex})";
                }
                
                return str;
            });
            cursor.Index++;
        }
    }
}