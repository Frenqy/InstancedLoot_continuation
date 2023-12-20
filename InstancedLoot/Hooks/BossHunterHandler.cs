using InstancedLoot.Components;
using InstancedLoot.Enums;
using RoR2;

namespace InstancedLoot.Hooks;

public class BossHunterHandler : AbstractHookHandler
{
    public override void RegisterHooks()
    {
        On.RoR2.EquipmentSlot.FireBossHunter += On_EquipmentSlot_FireBossHunter;
    }

    public override void UnregisterHooks()
    {
        On.RoR2.EquipmentSlot.FireBossHunter -= On_EquipmentSlot_FireBossHunter;
    }

    private bool On_EquipmentSlot_FireBossHunter(On.RoR2.EquipmentSlot.orig_FireBossHunter orig, EquipmentSlot self)
    {
        PlayerCharacterMasterController owner = null;

        if (self != null && self.characterBody != null)
        {
            var characterMaster = self.characterBody.master;

            if (characterMaster != null) owner = characterMaster.playerCharacterMasterController;
        }
        
        PickupDropletControllerHandler pickupDropletControllerHandler = hookManager.GetHandler<PickupDropletControllerHandler>();
        
        pickupDropletControllerHandler.InstanceOverrideInfo =
            new InstanceInfoTracker.InstanceOverrideInfo(ObjectType.HuntersTricorn, owner);
        bool result = orig(self);
        pickupDropletControllerHandler.InstanceOverrideInfo = null;
        
        return result;
    }
}