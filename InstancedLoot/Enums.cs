using System;
using System.Collections.Generic;
using InstancedLoot.Configuration.Attributes;
using DescriptionAttribute = InstancedLoot.Configuration.Attributes.ObjectTypeDescriptionAttribute;
using AliasesAttribute = InstancedLoot.Configuration.Attributes.ObjectTypeAliasesAttribute;

namespace InstancedLoot.Enums;

public enum InstanceMode
{
    Default,
    None,
    InstanceObject,
    InstanceItems,
    InstanceBoth,
    InstanceItemForOwnerOnly
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
    [Aliases(new []{ "Chests", "ChestsSmall" })]
    public const string Chest1 = "Chest1";
    [Description("Big chest")]
    [Aliases(new []{ "Chests", "ChestsBig" })]
    public const string Chest2 = "Chest2";
    [Description("Legendary/golden chest")]
    [Aliases(new []{ "Chests" })]
    public const string GoldChest = "GoldChest";
    [Description("Stealth chest")]
    [Aliases(new []{ "Chests", "ChestsSmall" })]
    public const string Chest1StealthedVariant = "Chest1StealthedVariant";
    
    [Description("Adaptive chest")]
    [Aliases(new []{ "Chests"})]
    public const string CasinoChest = "CasinoChest";
    [Description("Shrine of chance")]
    [Aliases(new []{ "Shrines" })]
    public const string ShrineChance = "ShrineChance";
    
    [Description("Equipment barrel\nNote: Equipment itself currently cannot be instanced due to swapping behavior")]
    [Aliases(new []{ "Equipment" })]
    public const string EquipmentBarrel = "EquipmentBarrel";
    
    [Description("Multishop/Triple shop")]
    [Aliases(new []{ "Shops" })]
    public const string TripleShop = "TripleShop";
    [Description("Large Multishop/Triple shop")]
    [Aliases(new []{ "Shops" })]
    public const string TripleShopLarge = "TripleShopLarge";
    [Description("Equipment Multishop/Triple shop\nnNote: Equipment itself currently cannot be instanced due to swapping behavior")]
    [Aliases(new []{ "Shops", "Equipment" })]
    public const string TripleShopEquipment = "TripleShopEquipment";
    
    [Description("Scrapper")]
    // [Aliases(new []{ "Chests", "ChestsSmall" })]
    public const string Scrapper = "Scrapper";
    [Description("3D Printer (White items)")]
    [Aliases(new []{ "Printers" })]
    public const string Duplicator = "Duplicator";
    [Description("3D Printer (Green items)")]
    [Aliases(new []{ "Printers" })]
    public const string DuplicatorLarge = "DuplicatorLarge";
    [Description("Mili-tech printer (Red items)")]
    [Aliases(new []{ "Printers" })]
    public const string DuplicatorMilitary = "DuplicatorMilitary";
    [Description("Overgrown 3D printer (Yellow items)")]
    [Aliases(new []{ "Printers" })]
    public const string DuplicatorWild = "DuplicatorWild";
    // public const string Barrel1 = "Barrel1"; // Barrels give benefits to everybody, let's not do this one
    [Description("Lunar coin")]
    // [Aliases(new []{ "Chests", "ChestsSmall" })]
    public const string PickupLunarCoin = "PickupLunarCoin";
    
    [Description("Rusty lockbox (Rusted key lockbox)")]
    [Aliases(new []{ "ItemSpawned", "PaidWithItem" })]
    public const string TreasureCache = "TreasureCache";
    [Description("Lunar pod")]
    [Aliases(new []{ "Lunar" })]
    public const string LunarChest = "LunarChest"; //Lunar pod, ChestBehavior
    [Description("Shipping Request Form delivery")]
    [Aliases(new []{ "ItemSpawned" })]
    public const string FreeChestMultiShop = "FreeChestMultiShop";
    
    [Description("Lunar cauldron (3 White -> 1 Green)")]
    [Aliases(new []{ "Cauldrons" })]
    public const string LunarCauldronWhiteToGreen = "LunarCauldron_WhiteToGreen";
    [Description("Lunar cauldron (5 Green -> 1 Red)")]
    [Aliases(new []{ "Cauldrons" })]
    public const string LunarCauldronGreenToRed = "LunarCauldron_GreenToRed";
    [Description("Lunar cauldron (1 Red -> 3 White)")]
    [Aliases(new []{ "Cauldrons" })]
    public const string LunarCauldronRedToWhite = "LunarCauldron_RedToWhite";
    [Description("Small chest")]
    [Aliases(new []{ "Chests", "ChestsSmall" })]
    public const string LunarRecycler = "LunarRecycler";
    [Description("Small chest")]
    [Aliases(new []{ "Chests", "ChestsSmall" })]
    public const string LunarShopTerminal = "LunarShopTerminal"; //ShopTerminalBehavior
    
    [Description("Small damage chest")]
    [Aliases(new []{ "Chests", "ChestsSmall", "ChestsDamage" })]
    public const string CategoryChestDamage = "CategoryChestDamage";
    [Description("Small healing chest")]
    [Aliases(new []{ "Chests", "ChestsSmall", "ChestsHealing" })]
    public const string CategoryChestHealing = "CategoryChestHealing";
    [Description("Small utility chest")]
    [Aliases(new []{ "Chests", "ChestsSmall", "ChestsUtility" })]
    public const string CategoryChestUtility = "CategoryChestUtility";
    [Description("Big damage chest")]
    [Aliases(new []{ "Chests", "ChestsBig", "ChestsDamage" })]
    public const string CategoryChest2Damage = "CategoryChest2Damage";
    [Description("Big healing chest")]
    [Aliases(new []{ "Chests", "ChestsBig", "ChestsHealing" })]
    public const string CategoryChest2Healing = "CategoryChest2Healing";
    [Description("Big utility chest")]
    [Aliases(new []{ "Chests", "ChestsBig", "ChestsUtility" })]
    public const string CategoryChest2Utility = "CategoryChest2Utility";
    
    [Description("Void cradle")]
    [Aliases(new []{ "Void" })]
    public const string VoidChest = "VoidChest";
    [Description("Void potential")]
    [Aliases(new []{ "Void" })]
    public const string VoidTriple = "VoidTriple"; // Should probably be handled in a different, special way
    [Description("Encrusted lockbox (Encrusted key lockbox)")]
    [Aliases(new []{ "Void", "ItemSpawned", "PaidWithItem" })]
    public const string LockboxVoid = "LockboxVoid"; //OptionChestBehavior
    // public const string VoidBarrel = "VoidBarrel"; // Barrels give benefits to everybody, let's not do this one
    
    // public const string PickupOrbOnUse = "PickupOrbOnUse";
    // public const string OptionPickup = "OptionPickup";

    [Description("Sacrifice item drop")]
    // [Aliases(new []{ "Special" })]
    public const string Sacrifice = "Sacrifice";
    [Description("Trophy Hunter's Tricorn ")]
    // [Aliases(new []{ "Special" })]
    public const string HuntersTricorn = "TrophyHuntersTricorn";
    // public const string ShrineBoss = "ShrineBoss"; //ShrineBossBehavior
    [Description("Shrine of Blood")]
    [Aliases(new []{ "Shrines" })]
    public const string ShrineBlood = "ShrineBlood"; //ShrineBloodBehavior
    [Description("Void fields item drop")]
    // [Aliases(new []{ "Chests", "ChestsSmall" })]
    public const string VoidFields = "VoidFields";
    [Description("Cleansing pool")]
    [Aliases(new []{ "PaidWithItem" })]
    public const string ShrineCleanse = "ShrineCleanse"; //ShopTerminalBehavior //Cleansing pool

    // public static SortedSet<string> AllSources = new()
    // {
    //     Chest1, Chest2, GoldChest,
    //     CasinoChest, ShrineChance,
    //     EquipmentBarrel,
    //     TripleShop, TripleShopLarge, TripleShopEquipment,
    //     Scrapper, Barrel1, PickupLunarCoin,
    //     TreasureCache, LunarChest, FreeChestMultiShop,
    //     LunarCauldronWhiteToGreen, LunarCauldronGreenToRed, LunarCauldronRedToWhite,
    //     LunarRecycler, LunarShopTerminal,
    //     CategoryChestDamage, CategoryChestHealing, CategoryChestUtility,
    //     CategoryChest2Damage, CategoryChest2Healing, CategoryChest2Utility,
    //     VoidChest, VoidTriple, LockboxVoid, VoidBarrel,
    //     Sacrifice, ShrineBoss,
    // };
}