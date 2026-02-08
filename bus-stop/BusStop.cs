using Godot;

public partial class BusStop : RoadNode
{
    [Export] public Godot.Collections.Array<RoadEdge> ConnectedEdges = [];
    public Area2D BusStopArea => GetNode<Area2D>("BusStopArea");

    public override void _Ready()
    {
        base._Ready();
        LevelState.AllBusStops.Add(this);
    }

    public override void _ExitTree()
    {
        LevelState.AllBusStops.Remove(this);
        LevelState.AllRoadNodes.Remove(this);
        LevelState.Budget -= Cost.BusStopRemoval;
    }
}
