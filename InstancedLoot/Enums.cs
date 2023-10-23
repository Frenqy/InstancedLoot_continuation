using System;
using System.Collections.Generic;

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

public static class ItemSource
{
    public const string Chest1 = "Chest1";
    public const string Chest2 = "Chest2";
    public const string GoldChest = "GoldChest";
    
    public const string CasinoChest = "CasinoChest";
    public const string ShrineChance = "ShrineChance";
    
    public const string EquipmentBarrel = "EquipmentBarrel";
    
    public const string TripleShop = "TripleShop";
    public const string TripleShopLarge = "TripleShopLarge";
    public const string TripleShopEquipment = "TripleShopEquipment";
    
    public const string Scrapper = "Scrapper";
    public const string Barrel1 = "Barrel1";
    public const string PickupLunarCoin = "PickupLunarCoin";
    
    public const string TreasureCache = "TreasureCache";
    public const string LunarChest = "LunarChest";
    public const string FreeChestMultiShop = "FreeChestMultiShop";
    
    public const string LunarCauldronWhiteToGreen = "LunarCauldron_WhiteToGreen";
    public const string LunarCauldronGreenToRed = "LunarCauldron_GreenToRed";
    public const string LunarCauldronRedToWhite = "LunarCauldron_RedToWhite";
    public const string LunarRecycler = "LunarRecycler";
    public const string LunarShopTerminal = "LunarShopTerminal";
    
    public const string CategoryChestDamage = "CategoryChestDamage";
    public const string CategoryChestHealing = "CategoryChestHealing";
    public const string CategoryChestUtility = "CategoryChestUtility";
    public const string CategoryChest2Damage = "CategoryChest2Damage";
    public const string CategoryChest2Healing = "CategoryChest2Healing";
    public const string CategoryChest2Utility = "CategoryChest2Utility";
    public const string VoidChest = "VoidChest";
    public const string VoidTriple = "VoidTriple";
    public const string LockboxVoid = "LockboxVoid";
    public const string VoidBarrel = "VoidBarrel";
    
    // public const string PickupOrbOnUse = "PickupOrbOnUse";
    // public const string OptionPickup = "OptionPickup";

    public const string Sacrifice = "Sacrifice";
    public const string HuntersTricorn = "HuntersTricorn";
    public const string ShrineBoss = "ShrineBoss";

    public static SortedSet<string> AllSources = new()
    {
        Chest1, Chest2, GoldChest,
        CasinoChest, ShrineChance,
        EquipmentBarrel,
        TripleShop, TripleShopLarge, TripleShopEquipment,
        Scrapper, Barrel1, PickupLunarCoin,
        TreasureCache, LunarChest, FreeChestMultiShop,
        LunarCauldronWhiteToGreen, LunarCauldronGreenToRed, LunarCauldronRedToWhite,
        LunarRecycler, LunarShopTerminal,
        CategoryChestDamage, CategoryChestHealing, CategoryChestUtility,
        CategoryChest2Damage, CategoryChest2Healing, CategoryChest2Utility,
        VoidChest, VoidTriple, LockboxVoid, VoidBarrel,
        Sacrifice, ShrineBoss,
    };
}