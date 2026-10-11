using UnityEngine;

public enum TerrainType
{
    Clear,
    Forest,
    Water,
    Mountain,
    Sand
}

public enum BuildingType
{
    None,

    Residential,

    Industrial,
    HeavyComplex,

    Farm,
    OilExtractor,
    Mine,
    LumberYard,
    IronMine,

    PowerPlant,
    Commercial,

    GeneralStorage,
    OilStorage,
    IndustrialStorage,
    HardwareStorage,
    Hospital,
    CollegeCampus,
    PoliceStation,

    TownHall,          // city anchor building

    // ── NEW ──
    OilRefinery,        // converts Oil → Gasoline
    GasStation          // sells Gasoline to population
}

public enum RoadType
{
    None,
    Road
}

public enum ProductionType
{
    None,
    Civilian,
    HeavySupplies,  // renamed from Military — Iron + Wood → Heavy Supplies
    Supplies
}

public enum StorageType
{
    General,
    Oil,
    Industrial,
    Hardware
}

// =============================================
// GOVERNMENT TYPES
// Mechanical modifiers live in GovernmentConfig
// =============================================
public enum GovernmentType
{
    Democracy,
    Dictatorship,
    Monarchy,
    Communism,
    Fascism
}