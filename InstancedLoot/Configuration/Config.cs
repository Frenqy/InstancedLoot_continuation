using System.Collections.Generic;
using BepInEx.Configuration;
using BepInEx.Logging;
using InstancedLoot.Enums;
using InstancedLoot.ObjectInstancing;

namespace InstancedLoot.Configuration;

public sealed class Config
{
    private readonly InstancedLoot plugin;
    private readonly Dictionary<string, ConfigEntry<bool>> enabled = new();
    public ConfigEntry<InstanceMode> PreferredInstanceMode { get; private set; }
    public ConfigEntry<bool> ReduceInteractibleBudget { get; private set; }
    public ConfigEntry<bool> ReduceMoneyDrops { get; private set; }
    public Config(InstancedLoot plugin, ManualLogSource logger) { this.plugin = plugin; }
    public void Init()
    {
        PreferredInstanceMode = plugin.Config.Bind("General", "PreferredInstanceMode", InstanceMode.InstanceObject,
            "InstanceObject is the only supported allocation strategy. Every player owns a native purchase object.");
        ReduceInteractibleBudget = plugin.Config.Bind("General - Balance", "ReduceInteractibleBudget", true,
            "Use the single-player interactable budget. Does not change object prices or payment rules.");
        ReduceMoneyDrops = plugin.Config.Bind("General - Balance", "ReduceMoneyDrops", false,
            "Divide native team money rewards by participating player count. Purchases still charge the native payer.");
        foreach (string type in NativeAdapters.ObjectTypes)
        {
            string section = type.StartsWith("Shrine") ? "Shrines" : type == "Drone" || type == "Turret" ? "Repairs" : "Objects";
            enabled[type] = plugin.Config.Bind(section, type, true,
                "Create an independently usable purchase object per player. False keeps vanilla behavior. Applies to newly spawned objects.");
        }
    }
    public bool IsEnabled(string type) => enabled.TryGetValue(type, out var entry) && entry.Value;
}
