using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class BusStop : RoadNode
{
    [Export] public Godot.Collections.Array<RoadEdge> ConnectedEdges = [];
    public Area2D BusStopArea => GetNode<Area2D>("BusStopArea");
    public Area2D WalkRadius => GetNode<Area2D>("WalkRadius");

    public override void _Ready()
    {
        base._Ready();
        LevelState.AllBusStops.Add(this);
    }

    /// <summary>
    /// Returns all bus stops that are within walking distance of this bus stop.
    /// Uses the WalkRadius Area2D to detect nearby BusStopArea collisions.
    /// </summary>
    public List<BusStop> GetNearbyBusStops()
    {
        return WalkRadius.GetOverlappingAreas()
            .Select(area => area.GetParent())
            .OfType<BusStop>()
            .Where(stop => stop != this)
            .ToList();
    }

    public override void _ExitTree()
    {
        LevelState.AllBusStops.Remove(this);
        LevelState.AllRoadNodes.Remove(this);
        LevelState.Budget -= Cost.BusStopRemoval;
    }
}
