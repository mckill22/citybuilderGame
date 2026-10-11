using UnityEngine;
using System.Collections.Generic;

public class BuildingInventory : MonoBehaviour
{
    public int storageCapacity = 100;

    public List<ResourceType> allowedResources = new();

    public Dictionary<ResourceType, int> storedResources =
        new Dictionary<ResourceType, int>();

    void Awake()
    {
        foreach (ResourceType type in allowedResources)
        {
            storedResources[type] = 0;
        }
    }

    public bool CanStore(ResourceType type)
    {
        return allowedResources.Contains(type);
    }

    public int GetResource(ResourceType type)
    {
        return storedResources.TryGetValue(type, out int val) ? val : 0;
    }

    public int GetTotalStored()
    {
        int total = 0;
        foreach (var v in storedResources)
            total += v.Value;

        return total;
    }

    public bool HasSpace(int amount)
    {
        return GetTotalStored() + amount <= storageCapacity;
    }

    public bool AddResource(ResourceType type, int amount)
    {
        if (!CanStore(type)) return false;
        if (!HasSpace(amount)) return false;

        if (!storedResources.ContainsKey(type))
            storedResources[type] = 0;

        storedResources[type] += amount;
        return true;
    }

    public bool RemoveResource(ResourceType type, int amount)
    {
        if (!storedResources.ContainsKey(type)) return false;
        if (storedResources[type] < amount) return false;

        storedResources[type] -= amount;
        return true;
    }

    public int GetRemainingCapacity()
    {
        return storageCapacity - GetTotalStored();
    }
}