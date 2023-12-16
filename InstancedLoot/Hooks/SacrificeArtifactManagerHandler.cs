using System;
using InstancedLoot.Components;
using InstancedLoot.Enums;
using MonoMod.Cil;
using RoR2;

namespace InstancedLoot.Hooks;

public class SacrificeArtifactManagerHandler : AbstractHookHandler
{
    public override void RegisterHooks()
    {
        On.RoR2.Artifacts.SacrificeArtifactManager.OnServerCharacterDeath +=
            On_SacrificeArtifactManager_OnServerCharacterDeath;
        IL.RoR2.Artifacts.SacrificeArtifactManager.OnServerCharacterDeath +=
            IL_SacrificeArtifactManager_OnServerCharacterDeath;
    }

    public override void UnregisterHooks()
    {
        On.RoR2.Artifacts.SacrificeArtifactManager.OnServerCharacterDeath -=
            On_SacrificeArtifactManager_OnServerCharacterDeath;
        IL.RoR2.Artifacts.SacrificeArtifactManager.OnServerCharacterDeath +=
            IL_SacrificeArtifactManager_OnServerCharacterDeath;
    }

    private void On_SacrificeArtifactManager_OnServerCharacterDeath(On.RoR2.Artifacts.SacrificeArtifactManager.orig_OnServerCharacterDeath orig, DamageReport damagereport)
    {
        PlayerCharacterMasterController owner = null;
        
        var attackerMaster = damagereport.attackerOwnerMaster;
        if (attackerMaster == null) attackerMaster = damagereport.attackerMaster;
        if (attackerMaster != null && attackerMaster.playerCharacterMasterController != null)
        {
            owner = attackerMaster.playerCharacterMasterController;
        }
        var pickupDropletHandler = hookManager.GetHandler<PickupDropletControllerHandler>();
        pickupDropletHandler.InstanceOverrideInfo = new InstanceInfoTracker.InstanceOverrideInfo(ObjectType.Sacrifice, owner: owner);
        orig(damagereport);
        pickupDropletHandler.InstanceOverrideInfo = null;
    }

    private void IL_SacrificeArtifactManager_OnServerCharacterDeath(ILContext il)
    {
        ILCursor cursor = new ILCursor(il);

        cursor.GotoNext(MoveType.After,
            i => i.MatchCallOrCallvirt(typeof(RoR2.Util), "GetExpAdjustedDropChancePercent"));

        cursor.EmitDelegate<Func<float, float>>((dropChance) =>
        {
            if (ModConfig.ReduceSacrificeSpawnChance.Value &&
                Utils.IncreasesItemCount(ModConfig.GetInstanceMode(ObjectType.Sacrifice)))
                dropChance /= Run.instance.participatingPlayerCount;

            return dropChance;
        });
    }
}