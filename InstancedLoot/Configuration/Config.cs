using System;
using InstancedLoot.Enums;
using BepInEx.Configuration;
using BepInEx.Logging;
using RoR2;

namespace InstancedLoot.Configuration;

public class Config
{
    public readonly InstancedLoot Plugin;
    private readonly ManualLogSource logger;
    
    public ConfigEntry<bool> instancedItems;

    public bool Ready => true;
    public event Action OnConfigReady;
    public ConfigMigrator migrator;

    public Config(InstancedLoot plugin, ManualLogSource _logger)
    {
        Plugin = plugin;
        logger = _logger;

        migrator = new(config, this);

        instancedItems = config.Bind("General", "DistributeToDeadPlayers", true,
            "Should items be distributed to dead players?");
        
        // config.SettingChanged += ConfigOnSettingChanged;

        // OnTeleport.OnReady += CheckReadyStatus;
        // OnDrop.OnReady += CheckReadyStatus;
        OnConfigReady += DoMigrationIfReady;
        
        DoMigrationIfReady();
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

    // private void ConfigOnSettingChanged(object sender, SettingChangedEventArgs e)
    // {
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
    // }
}