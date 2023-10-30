using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using InstancedLoot.Enums;
using BepInEx.Configuration;
using BepInEx.Logging;
using InstancedLoot.Configuration.Attributes;
using RoR2;

namespace InstancedLoot.Configuration;

public class Config
{
    public readonly InstancedLoot Plugin;
    private readonly ManualLogSource logger;

    public ConfigEntry<string> SelectedPreset;
    public ConfigEntry<bool> SharePickupPickers;

    public bool Ready => true;
    public event Action OnConfigReady;
    public ConfigMigrator Migrator;

    public delegate void ObjectTypesDelegate(ISet<string> names);
    public delegate void DescribeObjectTypeDelegate(string objectType, SortedSet<string> description);
    public delegate void ExtraNamesDelegate(string objectType, ISet<string> names);
    public delegate void InstanceModesDelegate(string objectType, ISet<InstanceMode> modes);
    public delegate void ObjectInstanceModesDelegate(string objectType, ISet<ObjectInstanceMode> modes);

    public event ObjectTypesDelegate GenerateObjectTypes;
    public event DescribeObjectTypeDelegate DescribeObjectTypes;
    public event ExtraNamesDelegate GenerateExtraNames;
    public event InstanceModesDelegate LimitInstanceModes;
    public event ObjectInstanceModesDelegate GenerateObjectInstanceModes;

    public Dictionary<string, string[]> DefaultAliases = new();
    public Dictionary<string, string> DefaultDescriptions = new();

    public Dictionary<string, ConfigEntry<InstanceMode>> ConfigEntriesForNames = new();
    public Dictionary<string, SortedSet<string>> ExtraNames = new();
    public Dictionary<string, ConfigPreset> ConfigPresets = new();
    public Dictionary<string, ObjectInstanceMode> ObjectInstanceModes = new();

    private Dictionary<string, InstanceMode> CachedInstanceModes = new();

    public Config(InstancedLoot plugin, ManualLogSource _logger)
    {
        Plugin = plugin;
        logger = _logger;

        Migrator = new(config, this);

        GenerateObjectTypes += DefaultGenerateObjectTypes;
        DescribeObjectTypes += DefaultDescribeObjectTypes;
        GenerateExtraNames += DefaultGenerateExtraNames;
        LimitInstanceModes += DefaultLimitInstanceModes;
        GenerateObjectInstanceModes += DefaultGenerateObjectInstanceModes;
        
        config.SettingChanged += ConfigOnSettingChanged;

        // OnTeleport.OnReady += CheckReadyStatus;
        // OnDrop.OnReady += CheckReadyStatus;
        OnConfigReady += DoMigrationIfReady;
        
        DoMigrationIfReady();
    }

    public void Init()
    {
        // SelectedPreset = config.Bind("General", "Preset", "Items",
        //      new ConfigDescription($"Ready to use presets with sensible defaults.\nAvailable presets:\n{
        //          string.Join("\n", ConfigPresets.Select(pair => $"{pair.Key}: {pair.Value.Description}"))
        //      }", new AcceptableValueList<string>(ConfigPresets.Keys.ToArray())));
        // ;

        SharePickupPickers = config.Bind("General", "SharePickupPickers", false,
            "Should pickup pickers be shared?\nIf true, pickup pickers (such as void orbs and command essences) will be shared among the players they are instanced for.\nA shared pickup picker can only be opened by one player, and will then drop an item that can be picked up separately.\nIf a pickup picker is not shared, then the item can be selected separately by each player.");
        
        DefaultDescriptions.Clear();
        DefaultAliases.Clear();
        foreach (var field in typeof(ObjectType).GetFields(BindingFlags.Static | BindingFlags.Public))
        {
            ObjectTypeDescriptionAttribute descriptionAttribute =
                field.GetCustomAttributes<ObjectTypeDescriptionAttribute>().FirstOrDefault();
            ObjectTypeAliasesAttribute aliasesAttribute =
                field.GetCustomAttributes<ObjectTypeAliasesAttribute>().FirstOrDefault();

            if (field.GetValue(null) is not string objectType) continue;
            if (descriptionAttribute != null)
            {
                DefaultDescriptions.Add(objectType, descriptionAttribute.Description);
            }

            if (aliasesAttribute != null)
            {
                DefaultAliases.Add(objectType, aliasesAttribute.Aliases);
            }
        }
        
        SortedSet<InstanceMode> allInstanceModes = new SortedSet<InstanceMode>
        {
            InstanceMode.Default, InstanceMode.None, InstanceMode.InstanceBoth, InstanceMode.InstanceItems,
            InstanceMode.InstanceObject, InstanceMode.InstanceItemForOwnerOnly
        };
        
        SortedSet<ObjectInstanceMode> defaultObjectInstanceModes = new SortedSet<ObjectInstanceMode>
        {
            ObjectInstanceMode.None // ObjectInstanceMode needs to be explicitly enabled
        };

        SortedSet<string> sources = new();
        SortedSet<string> allExtraNames = new();
        Dictionary<string, SortedSet<InstanceMode>> instanceModeLimits = new();
        
        GenerateObjectTypes!(sources);

        foreach (var source in sources)
        {
            SortedSet<ObjectInstanceMode> objectInstanceModes = new(defaultObjectInstanceModes);
            GenerateObjectInstanceModes!(source, objectInstanceModes);
            ObjectInstanceModes[source] = objectInstanceModes.Max;
            SortedSet<InstanceMode> instanceModes = instanceModeLimits[source] = new(allInstanceModes);
            LimitInstanceModes!(source, instanceModes);
            if (instanceModes.Count == 0) continue;
            SortedSet<string> extraNames = ExtraNames[source] = new SortedSet<string>();
            GenerateExtraNames!(source, extraNames);

            SortedSet<string> extraDescriptions = new();
            DescribeObjectTypes!(source, extraDescriptions);
            string extraDescription = String.Join("\n", extraDescriptions);

            string description = "Configure instancing for specific raw objectType";
            if (extraDescription != "")
                description += $"\n{extraDescription}";
            
            ConfigEntriesForNames[source] = config.Bind("Sources", source, InstanceMode.Default,
                new ConfigDescription(description,
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
                
                LimitInstanceModes!(extraName, extraNameInstanceModes);

                allExtraNames.Add(extraName);
            }
        }

        foreach (var extraName in allExtraNames)
        {
            var instanceModeLimit = instanceModeLimits[extraName];
            if(instanceModeLimit.Count > 0)
                ConfigEntriesForNames[extraName] = config.Bind("Aliases", extraName, InstanceMode.Default,
                    new ConfigDescription("Configure instancing for alias/group of sources",
                        new AcceptableValuesInstanceMode(instanceModeLimits[extraName])));
        }
    }

    public ConfigPreset GetPreset()
    {
        // string presetName = SelectedPreset.Value ?? "";
        // return ConfigPresets.TryGetValue(presetName, out var preset) ? preset : null;
        return null;
    }

    public SortedSet<string> GetExtraNames(string objectType)
    {
        return ExtraNames.TryGetValue(objectType, out var extraNames) ? extraNames : null;
    }

    public void MergeInstanceModes(ref InstanceMode orig, InstanceMode other)
    {
        if (other != InstanceMode.Default)
            orig = other;
    }
    
    public InstanceMode GetInstanceMode(string objectType)
    {
        if (objectType == null) return InstanceMode.None;
        
        if (CachedInstanceModes.TryGetValue(objectType, out var mode))
            return mode;
        
        ConfigPreset preset = GetPreset();
        SortedSet<string> extraNames = GetExtraNames(objectType);

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
            MergeInstanceModes(ref result, preset.GetPresetForSource(objectType));
        }
        
        if (extraNames != null)
        {
            foreach (var name in extraNames)
            {
                MergeInstanceModes(ref result, ConfigEntriesForNames[name].Value);
            }
        }
        MergeInstanceModes(ref result, ConfigEntriesForNames[objectType].Value);

        CachedInstanceModes[objectType] = result;
        
        return result;
    }

    private void DefaultGenerateObjectTypes(ISet<string> names)
    {
        names.UnionWith(Plugin.ObjectHandlerManager.HandlersForSource.Keys);
    }

    private void DefaultDescribeObjectTypes(string objectType, SortedSet<string> descriptions)
    {
        if (DefaultDescriptions.TryGetValue(objectType, out var description))
            descriptions.Add(description);
    }
    
    private void DefaultGenerateExtraNames(string objectType, ISet<string> names)
    {
        if(DefaultAliases.TryGetValue(objectType, out var aliases))
            names.UnionWith(aliases);
    }

    private void DefaultLimitInstanceModes(string source, ISet<InstanceMode> modes)
    {
        if (ObjectInstanceModes.TryGetValue(source, out var objectInstanceMode))
        {
            if (objectInstanceMode == ObjectInstanceMode.None)
            {
                modes.Remove(InstanceMode.InstanceBoth);
                modes.Remove(InstanceMode.InstanceObject);
            }
        }

        switch (source)
        {
            case ObjectType.TripleShopEquipment:
            case ObjectType.EquipmentBarrel:
                modes.Remove(InstanceMode.InstanceBoth);
                modes.Remove(InstanceMode.InstanceItems);
                break;
        }
    }

    private void DefaultGenerateObjectInstanceModes(string source, ISet<ObjectInstanceMode> modes)
    {
        if (Plugin.ObjectHandlerManager.HandlersForSource.TryGetValue(source, out var objectHandler))
        {
            modes.Add(objectHandler.ObjectInstanceMode);
        }
    }

    private void CheckReadyStatus()
    {
        if (Ready)
            OnConfigReady?.Invoke();
    }

    private void DoMigrationIfReady()
    {
        if (Ready && Migrator.NeedsMigration)
        {
            Migrator.DoMigration();
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