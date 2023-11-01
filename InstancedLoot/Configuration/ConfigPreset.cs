using System.Collections.Generic;
using InstancedLoot.Enums;

namespace InstancedLoot.Configuration;

public class ConfigPreset
{
    protected Dictionary<string, InstanceMode> Configuration;
    public string Description = "Missing short description for preset";

    public ConfigPreset()
    {
    }

    public ConfigPreset(Dictionary<string, InstanceMode> configuration)
    {
        Configuration = configuration;
    }

    public virtual InstanceMode GetConfigForName(string name)
    {
        return Configuration.TryGetValue(name, out var value) ? value : InstanceMode.None;
    }
}