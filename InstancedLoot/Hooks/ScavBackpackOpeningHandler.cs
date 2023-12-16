using System;
using EntityStates.ScavBackpack;
using InstancedLoot.Enums;
using MonoMod.Cil;
using RoR2;

namespace InstancedLoot.Hooks;

public class ScavBackpackOpeningHandler : AbstractHookHandler
{
    public override void RegisterHooks()
    {
        IL.EntityStates.ScavBackpack.Opening.OnEnter += IL_Opening_OnEnter;
    }

    public override void UnregisterHooks()
    {
        IL.EntityStates.ScavBackpack.Opening.OnEnter -= IL_Opening_OnEnter;
    }

    private void IL_Opening_OnEnter(ILContext il)
    {
        ILCursor cursor = new ILCursor(il);

        cursor.GotoNext(MoveType.After, i => i.MatchLdsfld<Opening>("maxItemDropCount"));
        cursor.EmitDelegate<Func<int, int>>(maxItemDropCount =>
        {
            if (ModConfig.ReduceScavengerSackDrops.Value &&
                Utils.IncreasesItemCount(ModConfig.GetInstanceMode(ObjectType.ScavBackpack)))
                maxItemDropCount /= Run.instance.participatingPlayerCount;
            
            return maxItemDropCount;
        });
    }
}