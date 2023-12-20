using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Configuration;
using BepInEx.Logging;
using InstancedLoot.Configuration.Attributes;
using InstancedLoot.Enums;
using RoR2;
using UnityEngine;

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
    public ConfigEntry<InstanceMode> PreferredInstanceMode;
    public ConfigEntry<bool> ReduceInteractibleBudget;
    public ConfigEntry<bool> ReduceSacrificeSpawnChance;
    public ConfigEntry<bool> ReduceBossDrops;
    public ConfigEntry<bool> ReduceScavengerSackDrops;
    // public ConfigEntry<bool> HideInstancedPickupDroplets;

    public bool Ready => true;
    public event Action OnConfigReady;
    public ConfigMigrator Migrator;

    public delegate void ObjectTypesDelegate(ISet<string> objectTypes);
    public delegate void DescribeObjectTypeDelegate(string objectType, List<string> descriptions);
    public delegate void GenerateAliasesDelegate(string objectType, ISet<string> aliases);
    public delegate void InstanceModesDelegate(string objectType, ISet<InstanceMode> modes);
    public delegate void ObjectInstanceModesDelegate(string objectType, ISet<ObjectInstanceMode> modes);
    public delegate void DescribeAliasesDelegate(string alias, List<string> descriptions);
    public delegate void GenerateConfigPresetsDelegate(Dictionary<string, ConfigPreset> presets);

    public event ObjectTypesDelegate GenerateObjectTypes;
    public event DescribeObjectTypeDelegate DescribeObjectTypes;
    public event GenerateAliasesDelegate GenerateAliases;
    public event InstanceModesDelegate LimitInstanceModes;
    public event ObjectInstanceModesDelegate GenerateObjectInstanceModes;
    public event DescribeAliasesDelegate DescribeAliases;
    public event GenerateConfigPresetsDelegate GenerateConfigPresets;

    public Dictionary<string, string[]> DefaultAliasesForObjectType = new();
    public Dictionary<string, string> DefaultDescriptionsForObjectType = new();
    public Dictionary<string, InstanceMode[]> DefaultDisabledInstanceModesForObjectType = new();
    public Dictionary<string, string> DefaultDescriptionsForAliases = new();

    public Dictionary<string, ConfigEntry<InstanceMode>> ConfigEntriesForNames = new();
    public Dictionary<string, SortedSet<string>> AliasesForObjectType = new();
    public Dictionary<string, ObjectInstanceMode> ObjectInstanceModeForObject = new();
    public Dictionary<string, ConfigPreset> ConfigPresets = new();

    public Dictionary<string, SortedSet<InstanceMode>> AvailableInstanceModesForObjectType = new();

    private readonly Dictionary<string, InstanceMode> CachedInstanceModes = new();

    public Config(InstancedLoot plugin, ManualLogSource _logger)
    {
        Plugin = plugin;
        logger = _logger;

        Migrator = new ConfigMigrator(config, this);

        GenerateObjectTypes += DefaultGenerateObjectTypes;
        DescribeObjectTypes += DefaultDescribeObjectTypes;
        GenerateAliases += DefaultGenerateAliases;
        LimitInstanceModes += DefaultLimitInstanceModes;
        GenerateObjectInstanceModes += DefaultGenerateObjectInstanceModes;
        DescribeAliases += DefaultDescribeAliases;
        GenerateConfigPresets += DefaultGenerateConfigPresets;
        
        config.SettingChanged += ConfigOnSettingChanged;

        // OnTeleport.OnReady += CheckReadyStatus;
        // OnDrop.OnReady += CheckReadyStatus;
        OnConfigReady += DoMigrationIfReady;
        
        DoMigrationIfReady();
    }

    public void Init()
    {
        PreferredInstanceMode = config.Bind("General", "PreferredInstanceMode", InstanceMode.InstanceObject,
            new ConfigDescription("Preferred instance mode used by some presets. When an entry specifies InstancePreferred for something, it is replaced with the value specified in this config.\n" +
                                  "Note: In some cases the mode set here is not available for a specific object type or alias. In those cases, the mode is \"reduced\" to the closest option.\n" +
                                  "This config entry also serves as an explanation for the available instance modes:\n" +
                                  "None: Self-explanatory, this object does not get instanced, nor do items spawned from it\n" +
                                  "Default: Do not override the preset/alias. If every value in the chain is Default, defaults to None.\n" +
                                  "InstancePreferred: Use the configuration for PreferredInstanceMode for this entry. Provided for convenience and/or experimentation.\n" +
                                  "InstanceObject: Spawn multiple copies of the object, one for each player, where each can only be opened by the owning player, and from which items can be picked up by any player\n" +
                                  "InstanceItems: Keep one copy of the object that can be opened by anybody, but instance the spawned item, such that each player can pick it up independently\n" +
                                  "InstanceBoth: Spawn multiple copies of the object, like InstanceObject, but also limit the resulting item such that it can only be picked up by the player who earned/bought it\n" +
                                  "InstanceItemForOwnerOnly: Keep one copy of the object, and limit the resulting item to only be picked up by the player who earned/bought it.\n" +
                                  "InstanceObjectForOwnerOnly: Keep one copy of the object, and limit opening it to only the owning player. This is only meaningful for objects that inherently belong to a player, like lockboxes. The resulting items are not instanced and can be picked up by any player.\n" +
                                  "InstanceBothForOwnerOnly: Similar to InstanceObjectForOwnerOnly, but the resulting item can only be picked up by the owning player.",
                new AcceptableValuesInstanceMode(new[] { InstanceMode.None, InstanceMode.InstanceItems, InstanceMode.InstanceBoth, InstanceMode.InstanceObject, InstanceMode.InstanceItemForOwnerOnly})));
        SharePickupPickers = config.Bind("General", "SharePickupPickers", false,
            "Should pickup pickers be shared?\nIf true, pickup pickers (such as void orbs and command essences) will be shared among the players they are instanced for.\nA shared pickup picker can only be opened by one player, and will then drop an item that can be picked up separately.\nIf a pickup picker is not shared, then the item can be selected separately by each player.");
        ReduceInteractibleBudget = config.Bind("General", "ReduceInteractibleBudget", true,
            "Should the interactible budget be reduced to singleplayer levels? (NOTE: Does not account for instance modes)\n" +
            "If enabled, the budget used to spawn interactibles (chests, shrines, etc.) in SceneDirector is no longer increased based on player count, and is instead overriden to act as though there's one player.\n" +
            "If disabled, you might end up having an increased amount of item drops, with each item drop multiplied by the number of players, causing you to become overpowered."); 
        ReduceSacrificeSpawnChance = config.Bind("General", "ReduceSacrificeSpawnChance", true,
            "Should the spawn chance be reduced by the amount of players, if sacrifice item instancing will yield extra items?\n" +
            "If enabled, the chance that an enemy drops an item is divided by the number of players in the game.\n" +
            "If disabled, you might end up having an increased amount of item drops, due to an increased amount of enemies when playing in multiplayer, combined with items being multiplied by the number of players.\n" +
            "Note: This is not an accurate method of keeping the amount of items and/or the power level the same as without the mod. I have not checked the formulas or tested the results to ensure fairness, use or don't use this at your own risk.");
        ReduceBossDrops = config.Bind("General", "ReduceBossDrops", true,
            "Should the boss drop count not scale with player count, if the instancing will yield extra items?\n" +
            "Applies to teleporter boss drops, as well as the extra boss on Siren's Call, and any other boss drops from BossGroup.\n" +
            "Recommended when instancing teleporter items, otherwise your boss item drop amount might get increased."); 
        ReduceScavengerSackDrops = config.Bind("General", "ReduceScavengerSackDrops", false,
            "Should the amount of items dropped from a Scavenger's Sack be reduced based on the number of players, if the amount of items is increased as an effect of instancing?\n" +
            "Note: The amount of items might still not be fair - you might get too few or too many items with this option enabled.");
        
        //Scrapped for the time being, had some networking issues
        // HideInstancedPickupDroplets = config.Bind("General", "HideInstancedPickupDroplets", false,
        //     "If enabled, pickup droplets that will result in an item that isn't available for you will be hidden for you.\n" +
        //     "That means if another player opens a chest, you won't visually see the droplet unless you can also pickup the resulting item.");
        
        DefaultDescriptionsForObjectType.Clear();
        DefaultAliasesForObjectType.Clear();
        DefaultDisabledInstanceModesForObjectType.Clear();
        foreach (var field in typeof(ObjectType).GetFields(BindingFlags.Static | BindingFlags.Public))
        {
            DescriptionAttribute descriptionAttribute =
                field.GetCustomAttributes<DescriptionAttribute>().FirstOrDefault();
            ObjectTypeAliasesAttribute aliasesAttribute =
                field.GetCustomAttributes<ObjectTypeAliasesAttribute>().FirstOrDefault();
            ObjectTypeDisableInstanceModesAttribute disableInstanceModesAttribute =
                field.GetCustomAttributes<ObjectTypeDisableInstanceModesAttribute>().FirstOrDefault();

            if (field.GetValue(null) is not string objectType) continue;
            if (descriptionAttribute != null) DefaultDescriptionsForObjectType.Add(objectType, descriptionAttribute.Description);

            if (aliasesAttribute != null) DefaultAliasesForObjectType.Add(objectType, aliasesAttribute.Aliases);

            if (disableInstanceModesAttribute != null) DefaultDisabledInstanceModesForObjectType.Add(objectType, disableInstanceModesAttribute.DisabledInstanceModes);
        }
        
        DefaultDescriptionsForAliases.Clear();
        foreach (var field in typeof(ObjectAlias).GetFields(BindingFlags.Static | BindingFlags.Public))
        {
            DescriptionAttribute descriptionAttribute =
                field.GetCustomAttributes<DescriptionAttribute>().FirstOrDefault();

            if (field.GetValue(null) is not string objectAlias) continue;
            if (descriptionAttribute != null) DefaultDescriptionsForAliases.Add(objectAlias, descriptionAttribute.Description);
        }
        
        SortedSet<InstanceMode> defaultInstanceModes = new SortedSet<InstanceMode>
        {
            InstanceMode.Default, InstanceMode.None, InstanceMode.InstanceBoth, InstanceMode.InstanceItems,
            InstanceMode.InstanceObject, InstanceMode.InstanceItemForOwnerOnly, InstanceMode.InstancePreferred
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
            SortedSet<InstanceMode> instanceModes = instanceModeLimits[objectType] = new SortedSet<InstanceMode>(defaultInstanceModes);
            LimitInstanceModes!(objectType, instanceModes);
            AvailableInstanceModesForObjectType[objectType] = instanceModes;
            if (instanceModes.Count == 0) continue;
            SortedSet<string> aliases = AliasesForObjectType[objectType] = new SortedSet<string>();
            GenerateAliases!(objectType, aliases);

            List<string> extraDescriptions = new();
            DescribeObjectTypes!(objectType, extraDescriptions);

            string description = "Configure instancing for specific raw objectType";
            if (extraDescriptions.Count > 0)
                description += $"\n{string.Join("\n", extraDescriptions)}";
            
            ConfigEntriesForNames[objectType] = config.Bind("ObjectTypes", objectType, InstanceMode.Default,
                new ConfigDescription(description,
                    new AcceptableValuesInstanceMode(instanceModes)));

            foreach (var alias in aliases)
            {
                SortedSet<InstanceMode> aliasInstanceModes;
                if (!instanceModeLimits.ContainsKey(alias))
                {
                    aliasInstanceModes = instanceModeLimits[alias] = new SortedSet<InstanceMode>(instanceModes);
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
                    description += $"\n{string.Join("\n", extraDescriptions)}";
                
                ConfigEntriesForNames[alias] = config.Bind("ObjectAliases", alias, InstanceMode.Default,
                    new ConfigDescription(description,
                        new AcceptableValuesInstanceMode(instanceModeLimits[alias])));
            }
        }

        ConfigPresets = new Dictionary<string, ConfigPreset>();
        GenerateConfigPresets!(ConfigPresets);
        
        SelectedPreset = config.Bind("General", "Preset", "Default",
             new ConfigDescription($"Ready to use presets, mostly with sensible defaults.\n" +
                                   $"Note: Presets can change in updates.\n" +
                                   $"Note: If you don't like something about a preset, you can override the instancing for specific aliases/object types\n" +
                                   $"Available presets:\n{
                 string.Join("\n", ConfigPresets.Select(pair => $"{pair.Key}: {pair.Value.Description}"))
             }", new AcceptableValueList<string>(ConfigPresets.Keys.ToArray())));
    }

    public ConfigPreset GetPreset()
    {
        string presetName = SelectedPreset.Value ?? "";
        return ConfigPresets.TryGetValue(presetName, out var preset) ? preset : null;
    }

    public SortedSet<string> GetAliases(string objectType)
    {
        return AliasesForObjectType.TryGetValue(objectType, out var extraNames) ? extraNames : null;
    }

    private static readonly Dictionary<InstanceMode, List<InstanceMode>> InstanceModeReduceMatrix;

    static Config()
    {
        Dictionary<InstanceMode, List<InstanceMode>> preferredInstanceModeReductions = new()
        {
            { InstanceMode.InstanceBothForOwnerOnly, new List<InstanceMode> { InstanceMode.InstanceObjectForOwnerOnly, InstanceMode.InstanceItemForOwnerOnly}},
            { InstanceMode.InstanceObjectForOwnerOnly, new List<InstanceMode> { InstanceMode.InstanceObject }},
            { InstanceMode.InstanceBoth, new List<InstanceMode> { InstanceMode.InstanceObject, InstanceMode.InstanceItems }},
            { InstanceMode.InstanceObject, new List<InstanceMode> { InstanceMode.InstanceItems }},
            { InstanceMode.InstanceItems, new List<InstanceMode> { InstanceMode.InstanceObject }}
        };

        InstanceModeReduceMatrix = new Dictionary<InstanceMode, List<InstanceMode>>();

        foreach (var preferredEntry in preferredInstanceModeReductions)
        {
            var fullReductions = preferredEntry.Value.ToList();

            for (int i = 0; i < fullReductions.Count; i++)
            {
                InstanceMode current = fullReductions[i];

                if (!preferredInstanceModeReductions.TryGetValue(current, out var preferredNextReductions))
                    continue;

                foreach (var next in preferredNextReductions)
                    if(!fullReductions.Contains(next))
                        fullReductions.Add(next);
            }

            InstanceModeReduceMatrix[preferredEntry.Key] = fullReductions;
        }
    }
    
    public InstanceMode ReduceInstanceModeForObjectType(string objectType, InstanceMode mode)
    {
        if (mode == InstanceMode.InstancePreferred) mode = PreferredInstanceMode.Value;
        if (mode == InstanceMode.Default) return mode;

        if (!AvailableInstanceModesForObjectType.TryGetValue(objectType, out var availableModes))
            return InstanceMode.None;

        if (availableModes.Contains(mode)) return mode;

        if (!InstanceModeReduceMatrix.TryGetValue(mode, out var reductions))
            return InstanceMode.None;

        foreach (var reduction in reductions)
            if (availableModes.Contains(reduction))
                return reduction;

        Debug.Log($"Failed to find reduction for {objectType}");

        return InstanceMode.None;
    }

    public void MergeInstanceModes(ref InstanceMode orig, InstanceMode other)
    {
        if (other == InstanceMode.InstancePreferred)
            other = PreferredInstanceMode.Value;
        if (other != InstanceMode.Default)
            orig = other;
    }
    
    public InstanceMode GetInstanceMode(string objectType)
    {
        if (objectType == null) return InstanceMode.None;
        
        if (CachedInstanceModes.TryGetValue(objectType, out var mode))
            return mode;

        if (!ConfigEntriesForNames.ContainsKey(objectType))
            return InstanceMode.None;
        
        ConfigPreset preset = GetPreset();
        SortedSet<string> aliases = GetAliases(objectType);

        InstanceMode result = InstanceMode.None;
        
        if (preset != null)
        {
            if (aliases != null)
                foreach (var alias in aliases) MergeInstanceModes(ref result, ReduceInstanceModeForObjectType(objectType, preset.GetConfigForName(alias)));
            MergeInstanceModes(ref result, ReduceInstanceModeForObjectType(objectType, preset.GetConfigForName(objectType)));
        }
        
        if (aliases != null)
            foreach (var alias in aliases) MergeInstanceModes(ref result, ReduceInstanceModeForObjectType(objectType, ConfigEntriesForNames[alias].Value));
        MergeInstanceModes(ref result, ReduceInstanceModeForObjectType(objectType, ConfigEntriesForNames[objectType].Value));

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
            if (objectInstanceMode == ObjectInstanceMode.None)
            {
                modes.Remove(InstanceMode.InstanceBoth);
                modes.Remove(InstanceMode.InstanceObject);
            }

        if (DefaultDisabledInstanceModesForObjectType.TryGetValue(objectType, out var instanceModes)) modes.ExceptWith(instanceModes);

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
        if (Plugin.ObjectHandlerManager.HandlersForObjectType.TryGetValue(objectType, out var objectHandler)) modes.Add(objectHandler.ObjectInstanceMode);
    }

    private void DefaultDescribeAliases(string alias, List<string> descriptions)
    {
        if (DefaultDescriptionsForAliases.TryGetValue(alias, out var description))
            descriptions.Add(description);

        SortedSet<string> baseNames = new();
        foreach (var entry in AliasesForObjectType)
            if (entry.Value.Contains(alias))
                baseNames.Add(entry.Key);

        descriptions.Add($"Full list of included object types:\n{string.Join(", ", baseNames)}");
    }

    private void DefaultGenerateConfigPresets(Dictionary<string, ConfigPreset> presets)
    {
        foreach (var entry in DefaultPresets.Presets) presets[entry.Key] = entry.Value;
    }

    private void CheckReadyStatus()
    {
        if (Ready)
            OnConfigReady?.Invoke();
    }

    private void DoMigrationIfReady()
    {
        if (Ready && Migrator.NeedsMigration) Migrator.DoMigration();
    }

    private ConfigFile config => Plugin.Config;

    public HashSet<PlayerCharacterMasterController> GetValidPlayersSet()
    {
        return new HashSet<PlayerCharacterMasterController>(PlayerCharacterMasterController.instances);
    }

    private void ConfigOnSettingChanged(object sender, SettingChangedEventArgs e)
    {
        if(e.ChangedSetting.SettingType == typeof(InstanceMode) || e.ChangedSetting == SelectedPreset)
            CachedInstanceModes.Clear();

        // if (e.ChangedSetting == HideInstancedPickupDroplets)
        // {
        //     foreach (var fadeBehavior in Object.FindObjectsOfType<FadeBehavior>())
        //     {
        //         fadeBehavior.Refresh();
        //     }
        // }
        
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

    private void DebugLogInstanceModeForAllObjectTypes()
    {
        Debug.Log("Logging instance modes for all object types:");
        foreach (var objectType in AvailableInstanceModesForObjectType.Keys) Debug.Log($"{objectType}: {GetInstanceMode(objectType)}");
    }
}