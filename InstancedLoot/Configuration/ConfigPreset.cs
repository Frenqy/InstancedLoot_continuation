using System.Collections.Generic;
using InstancedLoot.Enums;

namespace InstancedLoot.Configuration;

public class ConfigPreset
{
    protected Dictionary<string, InstanceModeNew> Configuration;

    public ConfigPreset()
    {
    }

    public ConfigPreset(Dictionary<string, InstanceModeNew> configuration)
    {
        Configuration = configuration;
    }

    public virtual InstanceModeNew GetPresetForSource(string source)
    {
        return Configuration.TryGetValue(source, out var value) ? value : InstanceModeNew.None;
    }
}