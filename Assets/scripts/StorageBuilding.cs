using UnityEngine;
using System.Collections.Generic;

public class StorageBuilding : MonoBehaviour
{
    public StorageType storageType;

    private BuildingInventory inventory;

    void Awake()
    {
        inventory = GetComponent<BuildingInventory>();

        ConfigureStorage();
    }

    void ConfigureStorage()
    {
        if (inventory == null)
            return;

        inventory.allowedResources.Clear();

        switch (storageType)
        {
            case StorageType.General:

                inventory.storageCapacity = 1000;

                inventory.allowedResources.Add(ResourceType.Wood);

                inventory.allowedResources.Add(ResourceType.Farming);
                inventory.allowedResources.Add(ResourceType.Minerals);
                inventory.allowedResources.Add(ResourceType.Iron);

                break;

            case StorageType.Oil:

                inventory.storageCapacity = 1500;

                inventory.allowedResources.Add(ResourceType.Oil);
                inventory.allowedResources.Add(ResourceType.Gasoline);

                break;

            case StorageType.Industrial:

                inventory.storageCapacity = 1200;

                inventory.allowedResources.Add(ResourceType.CivilianGoods);
                inventory.allowedResources.Add(ResourceType.HeavySupplies);
                inventory.allowedResources.Add(ResourceType.MilitarySupplies);

                break;

            case StorageType.Hardware:

                inventory.storageCapacity = 500;

                inventory.allowedResources.Add(ResourceType.Trucks);
                inventory.allowedResources.Add(ResourceType.HeavyTrucks);
                inventory.allowedResources.Add(ResourceType.Tanks);

                break;
        }
    }
}