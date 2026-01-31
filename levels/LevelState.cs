using Godot;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System;

/// <summary>
/// Holds the state of the level, including all routes.
/// Shout out to my friends Mitch, Adam, and Erik for helping me name this.
/// </summary>
public partial class LevelState : Node
{
    public static Node CurrentLevel { get; set; }

    /// <summary>
    /// The bus stop placement UI component for the current level.
    /// </summary>
    public static BusStopPlacement BusStopPlacement { get; set; }
    public static List<Route> AllRoutes { get; set; } = [];
    public static List<House> AllHouses { get; set; } = [];
    public static List<Destination> AllDestinations { get; set; } = [];
    public static List<Node> AllBusStops { get; set; } = [];
    
    /// <summary>
    /// All road nodes in the current level which includes bus stops and
    /// intersections. This is mostly for debugging purposes.
    /// </summary>
    public static List<RoadNode> AllRoadNodes { get; set; } = [];
    public static List<RoadEdge> AllRoadEdges { get; set; } = [];
    public static event Action<uint> OnBudgetChanged;
    private static uint _budget;
    public static uint Budget
    {
        get => _budget;
        set
        {
            _budget = value;
            OnBudgetChanged?.Invoke(_budget);
        }
    }

    private static List<KeyValuePair<string, Color>> _availableColors = new(RouteColors.ColorList);

    public static KeyValuePair<string, Color>? GetNextRouteColor()
    {
        if (_availableColors.Count == 0)
        {
            GD.PrintErr("All available route colors have been used.");
            return null;
        }
        var colorInfo = _availableColors[0];
        _availableColors.RemoveAt(0);
        return colorInfo;
    }

    /// <summary>
    /// Returns a route color back to the pool of available colors.
    /// Call this when a route is deleted.
    /// </summary>
    public static void ReturnRouteColor(KeyValuePair<string, Color> colorInfo)
    {
        if (!RouteColors.ColorList.Contains(colorInfo) || _availableColors.Contains(colorInfo))
        {
            return;
        }
        _availableColors.Add(colorInfo);
    }

    /// <summary>
    /// Refreshes the visual representation of all routes.
    /// Call this when routes are added, removed, or modified to ensure
    /// proper offset calculations for shared road segments.
    /// </summary>
    public static void RefreshAllRouteVisuals()
    {
        foreach (var route in AllRoutes)
        {
            route.Visual?.UpdateVisual();
        }
    }

    public static bool IsLevelComplete()
    {
        return AllHouses.All(house => house.IsChecked);
    }
    
    public static void UpdateAllHouseStatuses()
    {
        foreach (var house in AllHouses)
        {
            house.UpdateCheckStatus();
        }
    }
}