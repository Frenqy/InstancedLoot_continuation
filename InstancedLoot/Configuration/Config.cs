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

/*
 * Terminology:
 * 
 * name: A configurable set of instancables, includes object types and aliases.
 *       Configuration can be queried either for individual object types, or for aliases, names cover both cases.
 * 
 * object type/ObjectType: An object that can be instanced and/or a source of items which can be instanced.
 *                         Includes things such as chests, shrines of blood, and the artifact of sacrifice.
 * 
 * aliases: Extra names for object types, which can be shared by multiple object types.
 *          They serve the purpose of making configuration easier, by letting you change behavior for a class of objects.
 *          The main example is Chests, which includes different sizes, cloaked chests, and category chests.
 */

public class Config
{
    public readonly InstancedLoot Plugin;
    private readonly ManualLogSource logger;

    public ConfigEntry<string> SelectedPreset;
    public ConfigEntry<bool> SharePickupPickers;

    public bool Ready => true;
    public event Action OnConfigReady;
    public ConfigMigrator Migrator;

    public delegate void ObjectTypesDelegate(ISet<string> objectTypes);
    public delegate void DescribeObjectTypeDelegate(string objectType, List<string> descriptions);
    public delegate void GenerateAliasesDelegate(string objectType, ISet<string> aliases);
    public delegate void InstanceModesDelegate(string objectType, ISet<InstanceMode> modes);
    public delegate void ObjectInstanceModesDelegate(string objectType, ISet<ObjectInstanceMode> modes);
    public delegate void DescribeAliasesDelegate(string alias, List<string> descriptions);

    public event ObjectTypesDelegate GenerateObjectTypes;
    public event DescribeObjectTypeDelegate DescribeObjectTypes;
    public event GenerateAliasesDelegate GenerateAliases;
    public event InstanceModesDelegate LimitInstanceModes;
    public event ObjectInstanceModesDelegate GenerateObjectInstanceModes;
    public event DescribeAliasesDelegate DescribeAliases;

    public Dictionary<string, string[]> DefaultAliasesForObjectType = new();
    public Dictionary<string, string> DefaultDescriptionsForObjectType = new();
    public Dictionary<string, InstanceMode[]> DefaultInstanceModesForObjectType = new();
    public Dictionary<string, string> DefaultDescriptionsForAliases = new();

    public Dictionary<string, ConfigEntry<InstanceMode>> ConfigEntriesForNames = new();
    public Dictionary<string, SortedSet<string>> AliasesForObjectType = new();
    public Dictionary<string, ObjectInstanceMode> ObjectInstanceModeForObject = new();
    public Dictionary<string, ConfigPreset> ConfigPresets = new();

    private Dictionary<string, InstanceMode> CachedInstanceModes = new();

    public Config(InstancedLoot plugin, ManualLogSource _logger)
    {
        Plugin = plugin;
        logger = _logger;

        Migrator = new(config, this);

        GenerateObjectTypes += DefaultGenerateObjectTypes;
        DescribeObjectTypes += DefaultDescribeObjectTypes;
        GenerateAliases += DefaultGenerateAliases;
        LimitInstanceModes += DefaultLimitInstanceModes;
        GenerateObjectInstanceModes += DefaultGenerateObjectInstanceModes;
        DescribeAliases += DefaultDescribeAliases;
        
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
        
        DefaultDescriptionsForObjectType.Clear();
        DefaultAliasesForObjectType.Clear();
        DefaultInstanceModesForObjectType.Clear();
        foreach (var field in typeof(ObjectType).GetFields(BindingFlags.Static | BindingFlags.Public))
        {
            DescriptionAttribute descriptionAttribute =
                field.GetCustomAttributes<DescriptionAttribute>().FirstOrDefault();
            ObjectTypeAliasesAttribute aliasesAttribute =
                field.GetCustomAttributes<ObjectTypeAliasesAttribute>().FirstOrDefault();
            ObjectTypeDisableInstanceModesAttribute disableInstanceModesAttribute =
                field.GetCustomAttributes<ObjectTypeDisableInstanceModesAttribute>().FirstOrDefault();

            if (field.GetValue(null) is not string objectType) continue;
            if (descriptionAttribute != null)
            {
                DefaultDescriptionsForObjectType.Add(objectType, descriptionAttribute.Description);
            }

            if (aliasesAttribute != null)
            {
                DefaultAliasesForObjectType.Add(objectType, aliasesAttribute.Aliases);
            }

            if (disableInstanceModesAttribute != null)
            {
                DefaultInstanceModesForObjectType.Add(objectType, disableInstanceModesAttribute.DisabledInstanceModes);
            }
        }
        
        DefaultDescriptionsForAliases.Clear();
        foreach (var field in typeof(ObjectAlias).GetFields(BindingFlags.Static | BindingFlags.Public))
        {
            DescriptionAttribute descriptionAttribute =
                field.GetCustomAttributes<DescriptionAttribute>().FirstOrDefault();

            if (field.GetValue(null) is not string objectAlias) continue;
            if (descriptionAttribute != null)
            {
                DefaultDescriptionsForAliases.Add(objectAlias, descriptionAttribute.Description);
            }
        }
        
        SortedSet<InstanceMode> defaultInstanceModes = new SortedSet<InstanceMode>
        {
            InstanceMode.Default, InstanceMode.None, InstanceMode.InstanceBoth, InstanceMode.InstanceItems,
            InstanceMode.InstanceObject, InstanceMode.InstanceItemForOwnerOnly
        };
        
        SortedSet<ObjectInstanceMode> defaultObjectInstanceModes = new SortedSet<ObjectInstanceMode>
        {
            ObjectInstanceMode.None // ObjectInstanceMode needs to be explicitly enabled
        };

        SortedSet<string> objectTypes = new();
        SortedSet<string> allAliases = new();
        Dictionary<string, SortedSet<InstanceMode>> instanceModeLimits = new();
        
        GenerateObjectTypes!(objectTypes);

        foreach (var objectType in objectTypes)
        {
            SortedSet<ObjectInstanceMode> objectInstanceModes = new(defaultObjectInstanceModes);
            GenerateObjectInstanceModes!(objectType, objectInstanceModes);
            ObjectInstanceModeForObject[objectType] = objectInstanceModes.Max;
            SortedSet<InstanceMode> instanceModes = instanceModeLimits[objectType] = new(defaultInstanceModes);
            LimitInstanceModes!(objectType, instanceModes);
            if (instanceModes.Count == 0) continue;
            SortedSet<string> aliases = AliasesForObjectType[objectType] = new SortedSet<string>();
            GenerateAliases!(objectType, aliases);

            List<string> extraDescriptions = new();
            DescribeObjectTypes!(objectType, extraDescriptions);

            string description = "Configure instancing for specific raw objectType";
            if (extraDescriptions.Count > 0)
                description += $"\n{String.Join("\n", extraDescriptions)}";
            
            ConfigEntriesForNames[objectType] = config.Bind("ObjectTypes", objectType, InstanceMode.Default,
                new ConfigDescription(description,
                    new AcceptableValuesInstanceMode(instanceModes)));

            foreach (var alias in aliases)
            {
                SortedSet<InstanceMode> aliasInstanceModes;
                if (!instanceModeLimits.ContainsKey(alias))
                {
                    aliasInstanceModes = instanceModeLimits[alias] = new(instanceModes);
                }
                else
                {
                    aliasInstanceModes = instanceModeLimits[alias];
                    aliasInstanceModes.IntersectWith(instanceModes);
                }
                
                LimitInstanceModes!(alias, aliasInstanceModes);

                allAliases.Add(alias);
            }
        }

        foreach (var alias in allAliases)
        {
            var instanceModeLimit = instanceModeLimits[alias];
            if (instanceModeLimit.Count > 0)
            {
                string description = "Configure instancing for alias/group of sources";
                
                List<string> extraDescriptions = new List<string>();
                DescribeAliases!(alias, extraDescriptions);

                if (extraDescriptions.Count > 0)
                    description += $"\n{String.Join("\n", extraDescriptions)}";
                
                ConfigEntriesForNames[alias] = config.Bind("ObjectAliases", alias, InstanceMode.Default,
                    new ConfigDescription(description,
                        new AcceptableValuesInstanceMode(instanceModeLimits[alias])));
            }
        }
    }

    public ConfigPreset GetPreset()
    {
        // string presetName = SelectedPreset.Value ?? "";
        // return ConfigPresets.TryGetValue(presetName, out var preset) ? preset : null;
        return null;
    }

    public SortedSet<string> GetAliases(string objectType)
    {
        return AliasesForObjectType.TryGetValue(objectType, out var extraNames) ? extraNames : null;
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
        SortedSet<string> aliases = GetAliases(objectType);

        InstanceMode result = InstanceMode.None;
        
        if (preset != null)
        {
            if (aliases != null)
            {
                foreach (var alias in aliases)
                {
                    MergeInstanceModes(ref result, preset.GetPresetForName(alias));
                }
            }
            MergeInstanceModes(ref result, preset.GetPresetForName(objectType));
        }
        
        if (aliases != null)
        {
            foreach (var alias in aliases)
            {
                MergeInstanceModes(ref result, ConfigEntriesForNames[alias].Value);
            }
        }
        MergeInstanceModes(ref result, ConfigEntriesForNames[objectType].Value);

        CachedInstanceModes[objectType] = result;
        
        return result;
    }

    private void DefaultGenerateObjectTypes(ISet<string> objectTypes)
    {
        objectTypes.UnionWith(Plugin.ObjectHandlerManager.HandlersForObjectType.Keys);
    }

    private void DefaultDescribeObjectTypes(string objectType, List<string> descriptions)
    {
        if (DefaultDescriptionsForObjectType.TryGetValue(objectType, out var description))
            descriptions.Add(description);
    }
    
    private void DefaultGenerateAliases(string objectType, ISet<string> names)
    {
        if(DefaultAliasesForObjectType.TryGetValue(objectType, out var aliases))
            names.UnionWith(aliases);
    }

    private void DefaultLimitInstanceModes(string objectType, ISet<InstanceMode> modes)
    {
        if (ObjectInstanceModeForObject.TryGetValue(objectType, out var objectInstanceMode))
        {
            if (objectInstanceMode == ObjectInstanceMode.None)
            {
                modes.Remove(InstanceMode.InstanceBoth);
                modes.Remove(InstanceMode.InstanceObject);
            }
        }

        if (DefaultInstanceModesForObjectType.TryGetValue(objectType, out var instanceModes))
        {
            modes.IntersectWith(instanceModes);
        }

        if (Plugin.ObjectHandlerManager.HandlersForObjectType.TryGetValue(objectType, out var objectHandler) && objectHandler.CanObjectBeOwned)
        {
            if (modes.Contains(InstanceMode.InstanceBoth))
                modes.Add(InstanceMode.InstanceBothForOwnerOnly);
            if (modes.Contains(InstanceMode.InstanceObject))
                modes.Add(InstanceMode.InstanceObjectForOwnerOnly);
        }
    }

    private void DefaultGenerateObjectInstanceModes(string objectType, ISet<ObjectInstanceMode> modes)
    {
        if (Plugin.ObjectHandlerManager.HandlersForObjectType.TryGetValue(objectType, out var objectHandler))
        {
            modes.Add(objectHandler.ObjectInstanceMode);
        }
    }

    private void DefaultDescribeAliases(string alias, List<string> descriptions)
    {
        if (DefaultDescriptionsForAliases.TryGetValue(alias, out var description))
            descriptions.Add(description);

        SortedSet<string> baseNames = new();
        foreach (var entry in AliasesForObjectType)
        {
            if (entry.Value.Contains(alias))
                baseNames.Add(entry.Key);
        }
        
        descriptions.Add($"Full list of included object types:\n{String.Join(", ", baseNames)}");
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