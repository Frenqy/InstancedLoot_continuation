using System;
using MonoMod.Cil;
using RoR2;

namespace InstancedLoot.Hooks;

public class SceneDirectorHandler : AbstractHookHandler
{
    public override void RegisterHooks()
    {
        IL.RoR2.SceneDirector.Start += IL_SceneDirector_Start;
    }

    public override void UnregisterHooks()
    {
        IL.RoR2.SceneDirector.Start -= IL_SceneDirector_Start;
    }

    private void IL_SceneDirector_Start(ILContext il)
    {
        ILCursor cursor = new ILCursor(il);

        cursor.GotoNext(MoveType.After, i => i.MatchCallOrCallvirt<Run>("get_participatingPlayerCount"));
        cursor.EmitDelegate<Func<int, int>>(playerCount =>
        {
            if (ModConfig.ReduceInteractibleBudget.Value)
                return 1;

            return playerCount;
        });
    }
}