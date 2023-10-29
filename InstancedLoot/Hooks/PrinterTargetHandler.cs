using InstancedLoot.Enums;
using RoR2;

namespace InstancedLoot.Hooks;

public class PrinterTargetHandler : AbstractHookHandler
{
    public override void RegisterHooks()
    {
        On.RoR2.ShopTerminalBehavior.DropPickup += On_ShopTerminalBehavior_DropPickup;
    }

    public override void UnregisterHooks()
    {
        On.RoR2.ShopTerminalBehavior.DropPickup -= On_ShopTerminalBehavior_DropPickup;
    }

    private void On_ShopTerminalBehavior_DropPickup(On.RoR2.ShopTerminalBehavior.orig_DropPickup orig,
        ShopTerminalBehavior self)
    {
        var interaction = self.GetComponent<PurchaseInteraction>();

        if (interaction && CostTypeCatalog.GetCostTypeDef(interaction.costType)?.itemTier != ItemTier.NoTier)
        {
            var interactor = interaction.lastActivator;
            var body = interactor != null ? interactor.GetComponent<CharacterBody>() : null;
            var target = body != null ? body.master : null;

            // if (target)
            //     HookManager.GetHandler<PickupDropletHandler>().InstanceOverrideInfo =
            //         new InstanceInfoTracker.InstanceOverrideInfo(ObjectType.TierItem, target.playerCharacterMasterController);
        }

        orig(self);
        hookManager.GetHandler<PickupDropletHandler>().InstanceOverrideInfo = null;
    }
}