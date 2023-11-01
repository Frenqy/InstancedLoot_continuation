using System.Collections.Generic;
using InstancedLoot.Enums;

namespace InstancedLoot.Configuration;

public static class DefaultPresets
{
    public static Dictionary<string, ConfigPreset> Presets = new()
    {
        { "None", new ConfigPreset("Do not instance anything, vanilla behavior", new()) },
        {
            "Default", new ConfigPreset("Instance most things, tries to be a sensible default.", new()
            {
                {ObjectAlias.Chests, InstanceMode.InstancePreferred},
                {ObjectAlias.Shops, InstanceMode.InstancePreferred},
                {ObjectAlias.Shrines, InstanceMode.InstancePreferred},
                {ObjectAlias.Equipment, InstanceMode.InstanceObject},
                {ObjectType.LunarChest, InstanceMode.InstancePreferred},
                {ObjectType.VoidTriple, InstanceMode.InstancePreferred}
            })
        },
        {
            "Selfish", new ConfigPreset("Instance things for owner where applicable. Doesn't increase total item/interactible count.", new()
            {
                {ObjectAlias.Chests, InstanceMode.InstanceItemForOwnerOnly},
                {ObjectAlias.Shops, InstanceMode.InstanceItemForOwnerOnly},
                {ObjectAlias.Shrines, InstanceMode.InstanceItemForOwnerOnly},
                {ObjectAlias.Equipment, InstanceMode.InstanceObject},
                {ObjectAlias.Void, InstanceMode.InstanceItemForOwnerOnly},
                {ObjectAlias.ItemSpawned, InstanceMode.InstanceItemForOwnerOnly},
                {ObjectAlias.PaidWithItem, InstanceMode.InstanceItemForOwnerOnly},
                {ObjectAlias.Printers, InstanceMode.InstanceItemForOwnerOnly},
                {ObjectAlias.Cauldrons, InstanceMode.InstanceItemForOwnerOnly},
                {ObjectType.LunarChest, InstanceMode.InstanceItemForOwnerOnly},
            })
        },
        { "EVERYTHING", new EverythingConfigPreset() }
    };

    public class EverythingConfigPreset : ConfigPreset
    {
        public EverythingConfigPreset()
        {
            Description = "Instance absolutely everything. Not recommended.";
        }
        
        public override InstanceMode GetConfigForName(string name)
        {
            return InstanceMode.InstancePreferred;
        }
    }
}