using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class House : Building
{
    private Sprite2D _checkSprite;
    private bool _isChecked;

    protected override Color HighlightFactor => new(1.4f, 1.4f, 1.4f, 1.0f);

    public bool IsChecked
    {
        get => _isChecked;
        private set
        {
            _isChecked = value;
            _checkSprite.Visible = value;
        }
    }

    public override void _Ready()
    {
        base._Ready(); // Calls _Ready() of the base class, Building.
        _checkSprite = GetNode<Sprite2D>("Check");
        LevelState.AllHouses.Add(this);
    }

    public void UpdateCheckStatus()
    {
        if (ReachableBusStop is not BusStop startStop)
        {
            IsChecked = false;
            return;
        }

        var validDestinationStops = LevelState.AllDestinations
            .Where(destination => destination.Modulate == Modulate
            && destination.ReachableBusStop is BusStop)
            .Select(destination => (BusStop)destination.ReachableBusStop)
            .ToHashSet();

        if (validDestinationStops.Count == 0)
        {
            IsChecked = false;
            return;
        }

        IsChecked = CanReachAnyDestination(startStop, validDestinationStops);
    }

    /// <summary>
    /// Uses BFS to determine if any destination bus stop is reachable from the start
    /// bus stop via routes and walking transfers between nearby bus stops.
    /// </summary>
    private bool CanReachAnyDestination(BusStop start, HashSet<BusStop> destinations)
    {
        var visited = new HashSet<BusStop>();
        var queue = new Queue<BusStop>();
        
        queue.Enqueue(start);
        visited.Add(start);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            if (destinations.Contains(current))
            {
                return true;
            }

            // Find all bus stops reachable via routes from current stop
            foreach (var route in LevelState.AllRoutes)
            {
                if (!route.Path.Contains(current)) continue;
                
                foreach (var node in route.Path.OfType<BusStop>())
                {
                    if (visited.Add(node))
                    {
                        queue.Enqueue(node);
                    }
                }
            }

            // Find all bus stops reachable via walking (transfer)
            foreach (var nearbyStop in current.GetNearbyBusStops())
            {
                if (visited.Add(nearbyStop))
                {
                    queue.Enqueue(nearbyStop);
                }
            }
        }

        return false;
    }
}
