using Godot;
using System.Collections.Generic;
using System.Linq;

[Tool]
public partial class House : Building
{
    private Sprite2D _checkSprite;
    private bool _isChecked;
    private PackedScene _infoPopupScene = GD.Load<PackedScene>(Path.InfoPopupScene);
    private Control _infoPopup;

    public Route CurrentItinerary { get; private set; }

    public bool IsChecked
    {
        get => _isChecked;
        private set
        {
            _isChecked = value;
            if (_checkSprite != null)
            {
                _checkSprite.Visible = value;
            }
            if (value && _busUsageProbability < 0.05f)
            {
                _busUsageProbability = 0.05f;
            }
            else if (!value)
            {
                _busUsageProbability = 0.0f;
            }
        }
    }

    private float _busUsageProbability = 0.0f;
    
    public float BusUsageProbability
    {
        get => _busUsageProbability;
        set => _busUsageProbability = Mathf.Clamp(value, 0.0f, 1.0f);
    }

    public override void _Ready()
    {
        base._Ready(); // Calls _Ready() of the base class, Building. Yes, we need this.
        _checkSprite = GetNode<Sprite2D>("Check");
        _checkSprite.Visible = _isChecked;
        LevelState.AllHouses.Add(this);
    }

    public override void _Process(double delta)
    {
        base._Process(delta); // Yes, we need this too.

        if (_infoPopup != null)
        {
            _infoPopup.GlobalPosition = GetViewport().GetMousePosition() + new Vector2(15, 15);
        }
    }

    public void UpdateCheckStatus()
    {
        if (ReachableBusStop is not BusStop startStop)
        {
            IsChecked = false;
            CurrentItinerary = null;
            return;
        }

        var validDestinations = LevelState.AllDestinations
            .Where(destination => destination.Modulate == Modulate
            && destination.ReachableBusStop is BusStop)
            .ToHashSet();

        if (validDestinations.Count == 0)
        {
            IsChecked = false;
            CurrentItinerary = null;
            return;
        }

        CurrentItinerary = GenerateRoute(startStop, validDestinations);
        IsChecked = CurrentItinerary != null;
    }

    private void _on_area_2d_mouse_entered()
    {
        if (_infoPopup == null)
        {
            _infoPopup = _infoPopupScene.Instantiate<Control>();
            var canvasLayer = GetTree().CurrentScene.GetNode<CanvasLayer>("CanvasLayer"); 
            canvasLayer.AddChild(_infoPopup);
            _infoPopup.GetNode<Label>("Label").Text = $"Bus Usage Probability: {BusUsageProbability:P1}\n Itinerary: {(CurrentItinerary != null ? string.Join("\n", CurrentItinerary.GetDirections()) : "No route available")}";
            _infoPopup.Modulate = Modulate;
        }
    }

    private void _on_area_2d_mouse_exited()
    {
        _infoPopup?.QueueFree();
        _infoPopup = null;
    }

    /// <summary>
    /// Uses BFS to determine the optimal route to any destination bus stop from the start
    /// bus stop via bus lines and walking transfers between nearby bus stops.
    /// </summary>
    private Route GenerateRoute(BusStop start, HashSet<Destination> validDestinations)
    {
        var validDestinationStops = validDestinations.Select(d => (BusStop)d.ReachableBusStop).ToHashSet();

        var visited = new HashSet<BusStop>();
        var queue = new Queue<BusStop>();
        
        var lineageMap = new Dictionary<BusStop, (BusStop Parent, RouteSegment Segment)>();
        
        queue.Enqueue(start);
        visited.Add(start);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            if (validDestinationStops.Contains(current))
            {
                var segments = new List<RouteSegment>();
                var backtrackNode = current;
                
                while (backtrackNode != start)
                {
                    var lineage = lineageMap[backtrackNode];
                    segments.Add(lineage.Segment);
                    backtrackNode = lineage.Parent;
                }
                
                segments.Reverse();
                
                segments.Insert(0, new WalkSegment(this, start));
                
                var actualDestination = validDestinations.First(d => d.ReachableBusStop == current);
                segments.Add(new WalkSegment(current, actualDestination));
                
                return new Route(segments);
            }

            // Find all bus stops reachable via bus line from current stop
            foreach (var busLine in LevelState.AllBusLines)
            {
                if (!busLine.Path.Contains(current)) continue;
                
                foreach (var node in busLine.Path.OfType<BusStop>())
                {
                    if (visited.Add(node))
                    {
                        lineageMap[node] = (current, new RideSegment(busLine, current, node));
                        queue.Enqueue(node);
                    }
                }
            }

            // Find all bus stops reachable via walking (transfer)
            foreach (var nearbyStop in current.GetNearbyBusStops())
            {
                // Only allow walking to stops that have at least one busLine attached
                if (!LevelState.AllBusLines.Any(r => r.Path.Contains(nearbyStop)))
                {
                    continue;
                }

                if (visited.Add(nearbyStop))
                {
                    lineageMap[nearbyStop] = (current, new WalkSegment(current, nearbyStop));
                    queue.Enqueue(nearbyStop);
                }
            }
        }

        return null;
    }
}
