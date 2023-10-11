using System;
using System.Collections.Generic;
using System.Linq;
using InstancedLoot.Enums;
using BepInEx.Configuration;
using BepInEx.Logging;
using RoR2;

namespace InstancedLoot.Configuration;

public class Config
{
    public readonly InstancedLoot Plugin;
    private readonly ManualLogSource logger;

    public ConfigEntry<string> SelectedPreset;

    public bool Ready => true;
    public event Action OnConfigReady;
    public ConfigMigrator migrator;

    public delegate void SourcesGenerator(ISet<string> names);
    public delegate void ExtraNamesGenerator(string source, ISet<string> names);

    public delegate void InstanceModes(string source, ISet<InstanceMode> modes);

    public event SourcesGenerator GenerateSources;
    public event ExtraNamesGenerator GenerateExtraNames;
    public event InstanceModes LimitInstanceModes;

    public Dictionary<string, ConfigEntry<InstanceMode>> ConfigEntriesForNames = new();
    public Dictionary<string, SortedSet<string>> ExtraNames = new();
    public Dictionary<string, ConfigPreset> ConfigPresets = new();

    private Dictionary<string, InstanceMode> CachedInstanceModes = new();

    public Config(InstancedLoot plugin, ManualLogSource _logger)
    {
        Plugin = plugin;
        logger = _logger;

        migrator = new(config, this);

        GenerateSources += DefaultGenerateSources;
        GenerateExtraNames += DefaultGenerateExtraNames;

        SortedSet<InstanceMode> allInstanceModes = new SortedSet<InstanceMode>()
        {
            InstanceMode.Default, InstanceMode.None, InstanceMode.InstanceBoth, InstanceMode.InstanceItems,
            InstanceMode.InstanceObject, InstanceMode.InstanceItemForOwnerOnly
        };

        SortedSet<string> sources = new();
        Dictionary<string, SortedSet<InstanceMode>> instanceModeLimits = new();
        
        GenerateSources!(sources);

        foreach (var source in sources)
        {
            SortedSet<InstanceMode> instanceModes = instanceModeLimits[source] = new(allInstanceModes);
            LimitInstanceModes?.Invoke(source, instanceModes);
            SortedSet<string> extraNames = ExtraNames[source] = new SortedSet<string>();
            GenerateExtraNames!(source, extraNames);

            ConfigEntriesForNames[source] = config.Bind("Sources", source, InstanceMode.Default,
                new ConfigDescription("Configure instancing for specific raw source",
                    new AcceptableValuesInstanceMode(instanceModes)));

            foreach (var extraName in extraNames)
            {
                SortedSet<InstanceMode> extraNameInstanceModes;
                if (!instanceModeLimits.ContainsKey(extraName))
                {
                    extraNameInstanceModes = instanceModeLimits[extraName] = new(instanceModes);
                }
                else
                {
                    extraNameInstanceModes = instanceModeLimits[extraName];
                    extraNameInstanceModes.IntersectWith(instanceModes);
                }
                
                LimitInstanceModes?.Invoke(extraName, extraNameInstanceModes);

                ConfigEntriesForNames[extraName] = config.Bind("Aliases", extraName, InstanceMode.Default,
                    new ConfigDescription("Configure instancing for alias/group of sources",
                        new AcceptableValuesInstanceMode(extraNameInstanceModes)));
            }
        }

        // SelectedPreset = config.Bind("General", "Preset", "Items",
        //      new ConfigDescription($"Ready to use presets with sensible defaults.\nAvailable presets:\n{
        //          string.Join("\n", ConfigPresets.Select(pair => $"{pair.Key}: {pair.Value.Description}"))
        //      }", new AcceptableValueList<string>(ConfigPresets.Keys.ToArray())));
        // ;
        
        config.SettingChanged += ConfigOnSettingChanged;

        // OnTeleport.OnReady += CheckReadyStatus;
        // OnDrop.OnReady += CheckReadyStatus;
        OnConfigReady += DoMigrationIfReady;
        
        DoMigrationIfReady();
    }

    public ConfigPreset GetPreset()
    {
        // string presetName = SelectedPreset.Value ?? "";
        // return ConfigPresets.TryGetValue(presetName, out var preset) ? preset : null;
        return null;
    }

    public SortedSet<string> GetExtraNames(string source)
    {
        return ExtraNames.TryGetValue(source, out var extraNames) ? extraNames : null;
    }

    public void MergeInstanceModes(ref InstanceMode orig, InstanceMode other)
    {
        if (other != InstanceMode.Default)
            orig = other;
    }
    
    public InstanceMode GetInstanceMode(string source)
    {
        if (source == null) return InstanceMode.None;
        
        if (CachedInstanceModes.TryGetValue(source, out var mode))
            return mode;
        
        ConfigPreset preset = GetPreset();
        SortedSet<string> extraNames = GetExtraNames(source);

        InstanceMode result = InstanceMode.None;
        
        if (preset != null)
        {
            if (extraNames != null)
            {
                foreach (var name in extraNames)
                {
                    MergeInstanceModes(ref result, preset.GetPresetForSource(name));
                }
            }
            MergeInstanceModes(ref result, preset.GetPresetForSource(source));
        }
        
        if (extraNames != null)
        {
            foreach (var name in extraNames)
            {
                MergeInstanceModes(ref result, ConfigEntriesForNames[name].Value);
            }
        }
        MergeInstanceModes(ref result, ConfigEntriesForNames[source].Value);

        CachedInstanceModes[source] = result;
        
        return result;
    }

    private void DefaultGenerateSources(ISet<string> names)
    {
        names.UnionWith(new[]
        {
            "Chest1"
        });
    }

    private static Dictionary<string, string[]> defaultExtraNames = new()
    {
        {"Chest1", new[]{"Chests"}},
        {"Chest2", new[]{"Chests"}},
    };
    private void DefaultGenerateExtraNames(string source, ISet<string> names)
    {
        if(defaultExtraNames.TryGetValue(source, out var extraNames))
            names.UnionWith(extraNames);
    }

    private void CheckReadyStatus()
    {
        if (Ready)
            OnConfigReady?.Invoke();
    }

    private void DoMigrationIfReady()
    {
        if (Ready && migrator.NeedsMigration)
        {
            migrator.DoMigration();
        }
    }

    private ConfigFile config => Plugin.Config;

    public HashSet<PlayerCharacterMasterController> GetValidPlayersSet()
    {
        return new HashSet<PlayerCharacterMasterController>(PlayerCharacterMasterController.instances);
    }

    private void ConfigOnSettingChanged(object sender, SettingChangedEventArgs e)
    {
        CachedInstanceModes.Clear();
    //     if (e.ChangedSetting.SettingType == typeof(ItemSet))
    //     {
    //         var entry = (ConfigEntry<ItemSet>)e.ChangedSetting;
    //         var itemSet = entry.Value;
    //
    //         if (itemSet.ParseErrors?.Count > 0)
    //         {
    //             var error =
    //                 $"Errors found when parsing {entry.Definition.Key} for {entry.Definition.Section}:\n\t{string.Join("\n\t", itemSet.ParseErrors)}";
    //             logger.LogWarning(error);
    //         }
    //     }
    }
}