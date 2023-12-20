using InstancedLoot.Configuration.Attributes;
using AliasesAttribute = InstancedLoot.Configuration.Attributes.ObjectTypeAliasesAttribute;
using DisableInstanceModesAttribute = InstancedLoot.Configuration.Attributes.ObjectTypeDisableInstanceModesAttribute;

namespace InstancedLoot.Enums;

public enum InstanceMode
{
    Default,
    None,
    InstanceObject,
    InstanceItems,
    InstanceBoth,
    InstanceObjectForOwnerOnly,
    InstanceItemForOwnerOnly,
    InstanceBothForOwnerOnly,
    InstancePreferred
}

public enum ObjectInstanceMode
{
    None, // Object cannot/shouldn't be instanced at all
    CopyObject, // Object can be instanced by making multiple copies of the object
    InstancedObject // Object handles instancing gracefully
}

public static class ObjectType
{
    [Description("Small chest")]
    [Aliases(new []{ ObjectAlias.Chests, ObjectAlias.ChestsSmall })]
    public const string Chest1 = "Chest1";
    [Description("Big chest")]
    [Aliases(new []{ ObjectAlias.Chests, ObjectAlias.ChestsBig })]
    public const string Chest2 = "Chest2";
    [Description("Legendary/golden chest")]
    [Aliases(new []{ ObjectAlias.Chests })]
    public const string GoldChest = "GoldChest";
    [Description("Stealth chest")]
    [Aliases(new []{ ObjectAlias.Chests, ObjectAlias.ChestsSmall })]
    public const string Chest1StealthedVariant = "Chest1StealthedVariant";
    
    [Description("Adaptive chest")]
    [Aliases(new []{ ObjectAlias.Chests})]
    public const string CasinoChest = "CasinoChest";
    [Description("Shrine of chance")]
    [Aliases(new []{ ObjectAlias.Shrines })]
    public const string ShrineChance = "ShrineChance";
    
    [Description("Equipment barrel\nNote: Equipment itself currently cannot be instanced due to swapping behavior")]
    [Aliases(new []{ ObjectAlias.Equipment })]
    public const string EquipmentBarrel = "EquipmentBarrel";
    
    [Description("Multishop/Triple shop")]
    [Aliases(new []{ ObjectAlias.Shops })]
    public const string TripleShop = "TripleShop";
    [Description("Large Multishop/Triple shop")]
    [Aliases(new []{ ObjectAlias.Shops })]
    public const string TripleShopLarge = "TripleShopLarge";
    [Description("Equipment Multishop/Triple shop\nNote: Equipment itself currently cannot be instanced due to swapping behavior")]
    [Aliases(new []{ ObjectAlias.Shops, ObjectAlias.Equipment })]
    [ObjectTypeDisableInstanceModes(new [] { InstanceMode.InstanceItems, InstanceMode.InstanceBoth})]
    public const string TripleShopEquipment = "TripleShopEquipment";
    
    [Description("Scrapper")]
    // [Aliases(new []{ ObjectAlias.Chests, ObjectAlias.ChestsSmall })]
    public const string Scrapper = "Scrapper";
    [Description("3D Printer (White items)")]
    [Aliases(new []{ ObjectAlias.Printers })]
    public const string Duplicator = "Duplicator";
    [Description("3D Printer (Green items)")]
    [Aliases(new []{ ObjectAlias.Printers })]
    public const string DuplicatorLarge = "DuplicatorLarge";
    [Description("Mili-tech printer (Red items)")]
    [Aliases(new []{ ObjectAlias.Printers })]
    public const string DuplicatorMilitary = "DuplicatorMilitary";
    [Description("Overgrown 3D printer (Yellow items)")]
    [Aliases(new []{ ObjectAlias.Printers })]
    public const string DuplicatorWild = "DuplicatorWild";
    
    [Description("Rusty lockbox")]
    [Aliases(new []{ ObjectAlias.ItemSpawned, ObjectAlias.PaidWithItem })]
    public const string Lockbox = "Lockbox";
    [Description("Lunar pod")]
    [Aliases(new []{ ObjectAlias.Lunar })]
    public const string LunarChest = "LunarChest"; //Lunar pod, ChestBehavior
    [Description("Shipping Request Form delivery")]
    [Aliases(new []{ ObjectAlias.ItemSpawned })]
    public const string FreeChestMultiShop = "FreeChestMultiShop";
    
    [Description("Lunar cauldron (3 White -> 1 Green)")]
    [Aliases(new []{ ObjectAlias.Cauldrons })]
    public const string LunarCauldronWhiteToGreen = "LunarCauldron_WhiteToGreen";
    [Description("Lunar cauldron (5 Green -> 1 Red)")]
    [Aliases(new []{ ObjectAlias.Cauldrons })]
    public const string LunarCauldronGreenToRed = "LunarCauldron_GreenToRed";
    [Description("Lunar cauldron (1 Red -> 3 White)")]
    [Aliases(new []{ ObjectAlias.Cauldrons })]
    public const string LunarCauldronRedToWhite = "LunarCauldron_RedToWhite";
    [Description("Cleansing pool")]
    [Aliases(new []{ ObjectAlias.PaidWithItem })]
    public const string ShrineCleanse = "ShrineCleanse"; //ShopTerminalBehavior //Cleansing pool
    
    //TODO: Actually handle this one
    [Description("Bazaar shop terminal\nNote: when instanced as an object, rerolling the shop rerolls it for all players")]
    // [Aliases(new []{ ObjectAlias.Chests, ObjectAlias.ChestsSmall })]
    public const string LunarShopTerminal = "LunarShopTerminal"; //ShopTerminalBehavior
    
    [Description("Small damage chest")]
    [Aliases(new []{ ObjectAlias.Chests, ObjectAlias.ChestsSmall, ObjectAlias.ChestsDamage })]
    public const string CategoryChestDamage = "CategoryChestDamage";
    [Description("Small healing chest")]
    [Aliases(new []{ ObjectAlias.Chests, ObjectAlias.ChestsSmall, ObjectAlias.ChestsHealing })]
    public const string CategoryChestHealing = "CategoryChestHealing";
    [Description("Small utility chest")]
    [Aliases(new []{ ObjectAlias.Chests, ObjectAlias.ChestsSmall, ObjectAlias.ChestsUtility })]
    public const string CategoryChestUtility = "CategoryChestUtility";
    [Description("Big damage chest")]
    [Aliases(new []{ ObjectAlias.Chests, ObjectAlias.ChestsBig, ObjectAlias.ChestsDamage })]
    public const string CategoryChest2Damage = "CategoryChest2Damage";
    [Description("Big healing chest")]
    [Aliases(new []{ ObjectAlias.Chests, ObjectAlias.ChestsBig, ObjectAlias.ChestsHealing })]
    public const string CategoryChest2Healing = "CategoryChest2Healing";
    [Description("Big utility chest")]
    [Aliases(new []{ ObjectAlias.Chests, ObjectAlias.ChestsBig, ObjectAlias.ChestsUtility })]
    public const string CategoryChest2Utility = "CategoryChest2Utility";
    
    [Description("Void cradle")]
    [Aliases(new []{ ObjectAlias.Void })]
    public const string VoidChest = "VoidChest";
    [Description("Void potential (The interactible that costs health, not the orb pickup)")]
    [Aliases(new []{ ObjectAlias.Void })]
    public const string VoidTriple = "VoidTriple"; // Should probably be handled in a different, special way
    [Description("Encrusted lockbox")]
    [Aliases(new []{ ObjectAlias.Void, ObjectAlias.ItemSpawned, ObjectAlias.PaidWithItem })]
    public const string LockboxVoid = "LockboxVoid"; //OptionChestBehavior
    
    [Description("Scavenger's sack")]
    public const string ScavBackpack = "ScavBackpack";

    [Description("Sacrifice item drop\nNote: the owner for sacrifice is the player delivering the final blow, as recorded in the DamageReport")]
    // [Aliases(new []{ ObjectAlias.Special })]
    public const string Sacrifice = "Sacrifice";
    [Description("Trophy Hunter's Tricorn")]
    // [Aliases(new []{ ObjectAlias.Special })]
    public const string HuntersTricorn = "TrophyHuntersTricorn";
    // public const string ShrineBoss = "ShrineBoss"; //ShrineBossBehavior
    [Description("Void fields item drop")]
    // [Aliases(new []{ ObjectAlias.Chests, ObjectAlias.ChestsSmall })]
    public const string VoidFields = "VoidFields";
    
    [Description("Shrine of Blood")]
    [Aliases(new []{ ObjectAlias.Shrines })]
    [DisableInstanceModes(new[] { InstanceMode.InstanceItems, InstanceMode.InstanceBoth, InstanceMode.InstanceItemForOwnerOnly })]
    public const string ShrineBlood = "ShrineBlood"; //ShrineBloodBehavior
    [Description("Shrine of Order")]
    [Aliases(new []{ ObjectAlias.Shrines })]
    [DisableInstanceModes(new[] { InstanceMode.InstanceItems, InstanceMode.InstanceBoth, InstanceMode.InstanceItemForOwnerOnly })]
    public const string ShrineRestack = "ShrineRestack"; //ShrineBloodBehavior
    [Description("Lunar coin")]
    // [Aliases(new []{ ObjectAlias.Chests, ObjectAlias.ChestsSmall })]
    [DisableInstanceModes(new[] { InstanceMode.InstanceItems, InstanceMode.InstanceBoth, InstanceMode.InstanceItemForOwnerOnly })]
    public const string PickupLunarCoin = "PickupLunarCoin";

    [Description("Teleporter boss drop")]
    public const string TeleporterBoss = "TeleporterBoss";
    [Description("Siren's call boss drop")]
    public const string SuperRoboBallEncounter = "SuperRoboBallEncounter";
    [Description("Unknown boss drop")]
    public const string BossGroup = "BossGroup";

    // public const string Barrel1 = "Barrel1"; // Barrels give benefits to everybody, let's not do this one
    // public const string VoidBarrel = "VoidBarrel"; // Barrels give benefits to everybody, let's not do this one
    // public const string PickupOrbOnUse = "PickupOrbOnUse"; // Weird thing, looks like brass shop filled with void goop
    // public const string OptionPickup = "OptionPickup"; // Handled as an item
}

public static class ObjectAlias
{
    [Description("All chests")]
    public const string Chests = "Chests";
    [Description("Small chests (primarily white items)")]
    public const string ChestsSmall = "ChestsSmall";
    [Description("Big chests (primarily green items)")]
    public const string ChestsBig = "ChestsBig";
    
    [Description("Damage chests (only drop damage items)")]
    public const string ChestsDamage = "ChestsDamage";
    [Description("Healing chests (only drop healing items)")]
    public const string ChestsHealing = "ChestsHealing";
    [Description("Utility chests (only drop utility items)")]
    public const string ChestsUtility = "ChestsUtility";
    
    [Description("Item shops - does not include equipment shops, due to equipment shops not supporting item instancing")]
    public const string Shops = "Shops";
    [Description("Item printers, does not include cauldrons")]
    public const string Printers = "Printers";
    [Description("Lunar cauldrons, effectively same as printers, but with exchange rates")]
    public const string Cauldrons = "Cauldrons";
    
    [Description("Sources of lunar items")]
    public const string Lunar = "Lunar";
    [Description("Void-related objects")]
    public const string Void = "Void";
    
    [Description("Spawned due to a player having an item. Instancing object for everybody not recommended.")]
    public const string ItemSpawned = "ItemSpawned";
    [Description("Objects that require item payment. Does not include printers for convenience. Examples include lockboxes and cleansing pools.")]
    public const string PaidWithItem = "PaidWithItem";
    
    [Description("All supported shrines (chance, blood)")]
    public const string Shrines = "Shrines";
    [Description("Sources of guaranteed equipment\nNote: Equipment itself currently cannot be instanced due to swapping behavior")]
    public const string Equipment = "Equipment";
}