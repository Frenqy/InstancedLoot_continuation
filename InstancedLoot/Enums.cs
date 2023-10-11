using System;

namespace InstancedLoot.Enums;

public enum Cause
{
    Teleport,
    Drop
}

public enum Mode
{
    Sequential,
    Random,
    Closest,
    LeastItems
}

public enum InstanceMode
{
    Default,
    None,
    InstanceObject,
    InstanceItems,
    InstanceBoth,
    InstanceItemForOwnerOnly
}

public static class ItemSource
{
    public static string Chest1 = "Chest1";
    public static string Chest2 = "Chest2";
    public static string GoldChest = "GoldChest";
    public static string CasinoChest = "CasinoChest";
    public static string ShrineChance = "ShrineChance";
    
    public static string EquipmentBarrel = "EquipmentBarrel";
    
    public static string TripleShop = "TripleShop";
    public static string TripleShopLarge = "TripleShopLarge";
    public static string TripleShopEquipment = "TripleShopEquipment";
    
    public static string Scrapper = "Scrapper";
    public static string Barrel1 = "Barrel1";
    public static string PickupLunarCoin = "PickupLunarCoin";
    
    public static string TreasureCache = "TreasureCache";
    public static string LunarChest = "LunarChest";
    public static string FreeChestMultiShop = "FreeChestMultiShop";
    
    public static string LunarCauldronWhiteToGreen = "LunarCauldron_WhiteToGreen";
    public static string LunarCauldronGreenToRed = "LunarCauldron_GreenToRed";
    public static string LunarCauldronRedToWhite = "LunarCauldron_RedToWhite";
    public static string LunarRecycler = "LunarRecycler";
    public static string LunarShopTerminal = "LunarShopTerminal";
    
    public static string CategoryChestDamage = "CategoryChestDamage";
    public static string CategoryChestHealing = "CategoryChestHealing";
    public static string CategoryChestUtility = "CategoryChestUtility";
    public static string CategoryChest2Damage = "CategoryChest2Damage";
    public static string CategoryChest2Healing = "CategoryChest2Healing";
    public static string CategoryChest2Utility = "CategoryChest2Utility";
    public static string VoidChest = "VoidChest";
    public static string VoidTriple = "VoidTriple";
    public static string LockboxVoid = "LockboxVoid";
    public static string VoidBarrel = "VoidBarrel";
    
    // public static string PickupOrbOnUse = "PickupOrbOnUse";
    // public static string OptionPickup = "OptionPickup";

    public static string Sacrifice = "Sacrifice";
    public static string ShrineBoss = "ShrineBoss";
}