using System;
using MonoMod.Cil;
using RoR2;

namespace InstancedLoot.Hooks;

public class DeathRewardsHandler : AbstractHookHandler
{
    public override void RegisterHooks()
    {
        IL.RoR2.DeathRewards.OnKilledServer += IL_DeathRewards_OnKilledServer;
    }

    public override void UnregisterHooks()
    {
        IL.RoR2.DeathRewards.OnKilledServer -= IL_DeathRewards_OnKilledServer;
    }

    private void IL_DeathRewards_OnKilledServer(ILContext il)
    {
        ILCursor cursor = new ILCursor(il);

        cursor.GotoNext(MoveType.Before,
            i => i.MatchCallOrCallvirt<TeamManager>(nameof(TeamManager.GiveTeamMoney)));

        cursor.EmitDelegate<Func<uint, uint>>(money =>
        {
            if (ModConfig.ReduceMoneyDrops.Value)
                money /= (uint)Run.instance.participatingPlayerCount;

            return money;
        });
    }
}