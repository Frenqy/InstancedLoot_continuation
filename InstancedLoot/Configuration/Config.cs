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
    
    public ConfigEntry<InstanceMode> instancedChests;
    public ConfigEntry<InstanceMode> instancedItems;
    
    public ConfigEntry<InstanceMode> instancedPrinters;
    public ConfigEntry<InstanceMode> instancedScrappers;
    //TODO: Generic config for instancing specific objects?
    public ConfigEntry<InstanceMode> instancedLockboxes;

    public Dictionary<ItemSource, ConfigEntry<InstanceMode>> ItemSourceMapper;

    public bool Ready => true;
    public event Action OnConfigReady;
    public ConfigMigrator migrator;

    public class AcceptableValueNoOwnerOnly : AcceptableValueBase
    {
        public AcceptableValueNoOwnerOnly() : base(typeof(InstanceMode))
        {
        }

        public SortedSet<InstanceMode> AcceptableValues = new SortedSet<InstanceMode>()
        {
            InstanceMode.Default, InstanceMode.FullInstancing, InstanceMode.NoInstancing
        };

        public override object Clamp(object value)
        {
            if (IsValid(value)) return value;
            return InstanceMode.Default;
        }

        public override bool IsValid(object value)
        {
            return AcceptableValues.Contains((InstanceMode)value);
        }

        public override string ToDescriptionString() => "# Acceptable values: " +
                                                        string.Join(", ",
                                                            AcceptableValues.Select(x => x.ToString()).ToArray());
    }
    
    public delegate void Names(ISet<string> names);

    public delegate void InstanceModes(string source, ISet<InstanceModeNew> modes);

    public event Names GenerateSources;
    public event Names GenerateExtraNames;
    public event InstanceModes LimitInstanceModes;

    public Dictionary<string, ConfigEntry<InstanceModeNew>> ConfigEntriesForNames;
    public Dictionary<string, SortedSet<string>> ExtraNames;
    public Dictionary<string, ConfigPreset> ConfigPresets;

    private Dictionary<string, InstanceModeNew> CachedInstanceModes;

    public Config(InstancedLoot plugin, ManualLogSource _logger)
    {
        Plugin = plugin;
        logger = _logger;

        migrator = new(config, this);

        GenerateSources += DefaultGenerateSources;
        GenerateExtraNames += DefaultGenerateExtraNames;

        var noBuyerAcceptableValues = new Config.AcceptableValueNoOwnerOnly();
        
        instancedChests = config.Bind("General", "InstancedChests", InstanceMode.FullInstancing,
             new ConfigDescription("Should chests be able to be opened separately by each players?", noBuyerAcceptableValues));
        instancedItems = config.Bind("General", "InstancedItems", InstanceMode.NoInstancing,
            new ConfigDescription(
                "Should items be able to be picked up separately by each player?\nNote: if chests are instanced, and items are set to Default, items bought from those chests will be limited to the buyer only.",
                noBuyerAcceptableValues));
        
        instancedPrinters = config.Bind("General", "InstancedPrinters", InstanceMode.Default,
             "Should items from printers be able to be picked up separately by each players?");
        instancedScrappers = config.Bind("General", "InstancedScrappers", InstanceMode.NoInstancing,
             "Should items from scrappers be able to be picked up separately by each players?");
        
        instancedLockboxes = config.Bind("General", "InstancedLockboxes", InstanceMode.NoInstancing,
             "Should lockboxes be able to be opened separately by each players?");
        
        ItemSourceMapper = new()
        {
            {ItemSource.TierItem, instancedPrinters},
            {ItemSource.Scrapper, instancedScrappers},
            {ItemSource.SpecificItem, instancedLockboxes},
            {ItemSource.PersonalDrop, instancedLockboxes},
        };
        
        config.SettingChanged += ConfigOnSettingChanged;

        // OnTeleport.OnReady += CheckReadyStatus;
        // OnDrop.OnReady += CheckReadyStatus;
        OnConfigReady += DoMigrationIfReady;
        
        DoMigrationIfReady();
    }

    public ConfigPreset GetPreset()
    {
        string presetName = SelectedPreset.Value ?? "";
        return ConfigPresets.TryGetValue(presetName, out var preset) ? preset : null;
    }

    public SortedSet<string> GetExtraNames(string source)
    {
        return ExtraNames.TryGetValue(source, out var extraNames) ? extraNames : null;
    }

    public void MergeInstanceModes(ref InstanceModeNew orig, InstanceModeNew other)
    {
        if (other != InstanceModeNew.Default)
            orig = other;
    }
    
    public InstanceModeNew GetInstanceMode(string source)
    {
        if (CachedInstanceModes.TryGetValue(source, out var mode))
            return mode;
        
        ConfigPreset preset = GetPreset();
        SortedSet<string> extraNames = GetExtraNames(source);

        InstanceModeNew result = InstanceModeNew.None;
        
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
        
    }
    
    private void DefaultGenerateExtraNames(ISet<string> names)
    {
        
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