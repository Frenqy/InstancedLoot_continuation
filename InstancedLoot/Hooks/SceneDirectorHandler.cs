using System;
using MonoMod.Cil;
using RoR2;

namespace InstancedLoot.Hooks;

public sealed class SceneDirectorHandler : AbstractHookHandler
{
    public override void RegisterHooks() { IL.RoR2.SceneDirector.Start += Budget; }
    public override void UnregisterHooks() { IL.RoR2.SceneDirector.Start -= Budget; }
    private void Budget(ILContext il)
    {
        var cursor = new ILCursor(il);
        cursor.GotoNext(MoveType.After, instruction => instruction.MatchCallOrCallvirt<Run>("get_participatingPlayerCount"));
        cursor.EmitDelegate<Func<int, int>>(count => ModConfig.ReduceInteractibleBudget.Value ? 1 : count);
    }
}
