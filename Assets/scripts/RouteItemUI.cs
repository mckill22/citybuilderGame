using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RouteItemUI : MonoBehaviour
{
    public TMP_Text routeLabel;
    public TMP_Text truckCountText;
    public TMP_Text freeTrucksText;
    public Button plusButton;
    public Button minusButton;
    public Button deleteButton;

    LogisticsRoute route;
    LogisticsUIManager uiManager;
    BuildingLogistics logistics;

    public void Setup(
        LogisticsRoute r,
        LogisticsUIManager manager,
        BuildingLogistics ownerLogistics)
    {
        route = r;
        uiManager = manager;
        logistics = ownerLogistics;

        uiManager.RefreshAllRouteItems();

        plusButton.onClick.AddListener(AddTruck);
        minusButton.onClick.AddListener(RemoveTruck);
        deleteButton.onClick.AddListener(DeleteRoute);
    }

    void Refresh()
    {
        if (route == null) return;

        // Show building type + city name + building number
        // e.g. "Oil → PowerPlant #2 — Baghdad"
        string dest = "???";
        if (route.destination != null)
        {
            string type = route.destination.tileData.buildingType.ToString();
            int cityId = route.destination.tileData.cityId;
            int num = route.destination.buildingNumber;

            string cityName = "";
            if (CityManager.Instance != null && cityId >= 0 &&
                cityId < CityManager.Instance.cities.Count)
                cityName = " — " + CityManager.Instance.cities[cityId].cityName;

            dest = num > 0
                ? $"{type} #{num}{cityName}"
                : $"{type}{cityName}";
        }

        routeLabel.text = $"{route.resourceType} → {dest}";
        truckCountText.text = route.assignedTrucks.ToString();
    }

    void AddTruck()
    {
        // Need both a free truck AND a free worker
        if (GameManager.Instance.freeTrucks <= 0) return;
        if (GameManager.Instance.computedFreeWorkers <= 0) return;

        route.assignedTrucks++;
        GameManager.Instance.UpdateGlobalResourceTotals();
        uiManager.RefreshAllRouteItems();
        uiManager.RefreshFreeTrucksText();
    }

    void RemoveTruck()
    {
        if (route.assignedTrucks <= 1) return;

        route.assignedTrucks--;
        GameManager.Instance.UpdateGlobalResourceTotals();
        uiManager.RefreshAllRouteItems();
        uiManager.RefreshFreeTrucksText();
    }

    void DeleteRoute()
    {
        if (logistics == null || route == null) return;

        route.active = false;
        logistics.RemoveRoute(route);

        if (LogisticsManager.Instance != null)
            LogisticsManager.Instance.RunLogisticsTick();
        RefreshFreeTrucks();
        Destroy(gameObject);
    }

    void RefreshFreeTrucks()
    {
        if (freeTrucksText == null || GameManager.Instance == null) return;
        freeTrucksText.text = "Free Trucks: " + GameManager.Instance.freeTrucks;
    }

    public void RefreshExternal()
    {
        Refresh();
        RefreshFreeTrucks();
    }
}