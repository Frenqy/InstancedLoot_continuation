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

public enum InstanceModeNew
{
    Default,
    None,
    InstanceObject,
    InstanceItems,
    InstanceBoth,
    InstanceItemForOwnerOnly
}

public enum InstanceMode
{
    NoInstancing,
    OwnerOnly,
    FullInstancing,
    Default
}

public enum ItemSource
{
    Money, // Chests, shrines, etc.
    SpecialCoin, // Lunar coins, void coins
    TierItem, // Printers, cauldrons
    Scrapper, // Scrappers
    SpecificItem, // Lockboxes
    PersonalDrop, // Special Delivery Form pods
    Instanced // Obtained from an instanced object
}