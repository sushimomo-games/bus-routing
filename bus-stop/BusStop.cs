using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class BusStop : RoadNode
{
    [Export] public Godot.Collections.Array<RoadEdge> ConnectedEdges = [];
    public Area2D BusStopArea => GetNode<Area2D>("BusStopArea");
    public Area2D WalkRadius => GetNode<Area2D>("WalkRadius");
    private Sprite2D _walkRadiusSprite => WalkRadius.GetNode<Sprite2D>("WalkRadiusSprite");

    private bool _isHovered = false;
    private bool _isPersistent = false;

    public override void _Ready()
    {
        base._Ready();
        LevelState.AllBusStops.Add(this);
    }

    /// <summary>
    /// Determines if the walk radius should persist, depending on whether
    /// it has been selected by the user or not. Also slightly deepens the color
    /// so as to indicate which stop is selected while other stops may be
    /// hovered over.
    /// </summary>
    public void SetWalkRadiusPersistence(bool persistent)
    {
        _isPersistent = persistent;
        if (_isPersistent)
        {
            WalkRadius.Modulate = new Color(0.35686275f, 1, 2, 1);
        }
        else
        {
            WalkRadius.Modulate = new Color(0.35686275f, 1f, 1, 0.3529412f);
        }

        _walkRadiusSprite.Visible = _isHovered || _isPersistent;
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

    // Signals
    private void _on_bus_stop_area_mouse_entered()
    {
        _isHovered = true;
        _walkRadiusSprite.Visible = true;
    }

    private void _on_bus_stop_area_mouse_exited()
    {
        _isHovered = false;
        if (!_isPersistent)
        {
            _walkRadiusSprite.Visible = false;
        }
    }


    /// <summary>
    /// Counts how many houses are able to access bus stop.
    /// Uses ReachableBusStop to check which houses are in radius of current Bus Stop.
    /// Output is currently to terminal - will be hooked up with Information Panel.
    /// </summary>
    private void _on_bus_stop_clicked(Node viewport,
        InputEvent @event,
        long shapeIdx)
    {

        int _houseCount = 0;

        foreach (House house in LevelState.AllHouses)
        {
            if (house.ReachableBusStop == this)
            {
                _houseCount++;
            }
        }

        if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed && mouseEvent.ButtonIndex == MouseButton.Left)
        {
            House.DeselectHouses();
            LevelState.InformationPanel.ShowBusStop(this, _houseCount);
        }

    }
    public int GetHouseCount() /// allows outside classes to use this method and logic of counting the houses. Done by Malachi
    {
        int houseCount = 0;

        foreach (House house in LevelState.AllHouses)
        {
            if (house.ReachableBusStop == this)
            {
                houseCount++;
            }
        }

        return houseCount;
    }

    public override void _ExitTree()
    {
        LevelState.AllBusStops.Remove(this);
        LevelState.AllRoadNodes.Remove(this);
        LevelState.Budget -= Cost.BusStopRemoval;
    }


}
