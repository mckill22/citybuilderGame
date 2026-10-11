using UnityEngine;

public class BuildingInventorySetup : MonoBehaviour
{
    public BuildingType buildingType;

    private BuildingInventory inventory;

    void Awake()
    {
        inventory = GetComponent<BuildingInventory>();

        ConfigureInventory();
    }

    void ConfigureInventory()
    {
        if (inventory == null)
            return;

        inventory.allowedResources.Clear();

        switch (buildingType)
        {
            case BuildingType.Farm:

                inventory.storageCapacity = 100;

                inventory.allowedResources.Add(
                    ResourceType.Farming);

                break;

            case BuildingType.OilExtractor:

                inventory.storageCapacity = 100;

                inventory.allowedResources.Add(
                    ResourceType.Oil);

                break;

            case BuildingType.Mine:

                inventory.storageCapacity = 100;

                inventory.allowedResources.Add(
                    ResourceType.Minerals);

                break;

            case BuildingType.LumberYard:

                inventory.storageCapacity = 100;

                inventory.allowedResources.Add(
                    ResourceType.Wood);

                break;

            case BuildingType.IronMine:

                inventory.storageCapacity = 100;

                inventory.allowedResources.Add(
                    ResourceType.Iron);

                break;

            case BuildingType.PowerPlant:

                inventory.storageCapacity = 200;

                inventory.allowedResources.Add(
                    ResourceType.Oil);

                break;

            case BuildingType.Commercial:

                inventory.storageCapacity = 200;

                inventory.allowedResources.Add(
                    ResourceType.CivilianGoods);

                break;

            case BuildingType.Industrial:

                inventory.storageCapacity = 300;

                inventory.allowedResources.Add(
                    ResourceType.Wood);

                inventory.allowedResources.Add(
                    ResourceType.Iron);

                inventory.allowedResources.Add(
                    ResourceType.Farming);

                inventory.allowedResources.Add(
                    ResourceType.Oil);

                inventory.allowedResources.Add(
                    ResourceType.Minerals);

                inventory.allowedResources.Add(
                    ResourceType.CivilianGoods);

                inventory.allowedResources.Add(
                    ResourceType.HeavySupplies);

                inventory.allowedResources.Add(
                    ResourceType.MilitarySupplies);

                break;
            case BuildingType.HeavyComplex:

                inventory.storageCapacity = 400;

                inventory.allowedResources.Add(
                    ResourceType.HeavySupplies);

                inventory.allowedResources.Add(
                    ResourceType.Trucks);

                break;

            case BuildingType.OilStorage:

                // Oil & Gas Storage — stores both Oil and Gasoline
                inventory.storageCapacity = 500;

                inventory.allowedResources.Add(
                    ResourceType.Oil);

                inventory.allowedResources.Add(
                    ResourceType.Gasoline);

                break;

            case BuildingType.OilRefinery:

                // Converts Oil → Gasoline
                inventory.storageCapacity = 300;

                inventory.allowedResources.Add(
                    ResourceType.Oil);

                inventory.allowedResources.Add(
                    ResourceType.Gasoline);

                break;

            case BuildingType.GasStation:

                // Receives Gasoline, sells to population
                inventory.storageCapacity = 200;

                inventory.allowedResources.Add(
                    ResourceType.Gasoline);

                break;
        }
    }
}