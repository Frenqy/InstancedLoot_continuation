using System;
using System.Reflection;
using EntityStates.Scrapper;
using InstancedLoot.Components;
using InstancedLoot.Enums;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;

namespace InstancedLoot.Hooks;

/// <summary>
///     Handler for scrappers.
///     Patches EntityStates.Scrapper.ScrappingToIdle.OnEnter.
///     Uses CreatePickupDropletHandler to affect CreatePickupDroplet.
/// </summary>
public class ScrapperTargetHandler : AbstractHookHandler
{
    internal static FieldInfo Field_ScrapperBaseState_scrapperController = typeof(ScrapperBaseState).GetField(
        "scrapperController",
        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

    internal static FieldInfo Field_ScrapperController_interactor = typeof(ScrapperController).GetField(
        "interactor",
        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

    public override void RegisterHooks()
    {
        IL.EntityStates.Scrapper.ScrappingToIdle.OnEnter += IL_ScrappingToIdle_OnEnter;
    }

    public override void UnregisterHooks()
    {
        IL.EntityStates.Scrapper.ScrappingToIdle.OnEnter -= IL_ScrappingToIdle_OnEnter;
    }

    private void IL_ScrappingToIdle_OnEnter(ILContext il)
    {
        var cursor = new ILCursor(il);

        cursor.GotoNext(MoveType.Before, i => i.MatchCall<PickupDropletController>("CreatePickupDroplet"));
        cursor.Emit(OpCodes.Ldarg_0);
        cursor.Emit(OpCodes.Ldfld, Field_ScrapperBaseState_scrapperController);
        cursor.Emit(OpCodes.Ldfld, Field_ScrapperController_interactor);
        cursor.EmitDelegate<Action<Interactor>>(interactor =>
        {
            var body = interactor.GetComponent<CharacterBody>();
            var target = body.master;

            if (target)
                hookManager.GetHandler<PickupDropletHandler>().InstanceOverrideInfo =
                    new InstanceInfoTracker.InstanceOverrideInfo(ItemSource.Scrapper, target.GetComponent<PlayerCharacterMasterController>());
        });

        cursor.Index++;
        cursor.EmitDelegate(() => { hookManager.GetHandler<PickupDropletHandler>().InstanceOverrideInfo = null; });
    }
}