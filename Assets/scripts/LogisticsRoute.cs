using UnityEngine;

[System.Serializable]
public class LogisticsRoute
{
    public TileObject source;
    public TileObject destination;

    public ResourceType resourceType;

    public int assignedTrucks = 1;

    public bool active = true;
}