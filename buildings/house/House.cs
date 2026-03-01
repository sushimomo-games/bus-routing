using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class House : Building
{
    private Sprite2D _checkSprite;
    protected override Color HighlightFactor => new(1.4f, 1.4f, 1.4f, 1.0f);
    private bool _isChecked;
    private PackedScene _infoPopupScene = GD.Load<PackedScene>(Path.InfoPopupScene);
    private Control _infoPopup;

    public bool IsChecked
    {
        get => _isChecked;
        private set
        {
            _isChecked = value;
            _checkSprite.Visible = value;
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
        LevelState.AllHouses.Add(this);
        SetProcess(false);
    }

    public override void _Process(double delta)
    {
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

    private void _on_area_2d_mouse_entered()
    {
        if (_infoPopup == null)
        {
            _infoPopup = _infoPopupScene.Instantiate<Control>();
            var canvasLayer = GetTree().CurrentScene.GetNode<CanvasLayer>("EditorUI"); 
            canvasLayer.AddChild(_infoPopup);
            _infoPopup.GetNode<Label>("Label").Text = $"Bus Usage Probability: {BusUsageProbability:P1}";
            _infoPopup.Modulate = Modulate;
            SetProcess(true);
        }
    }

    private void _on_area_2d_mouse_exited()
    {
        if (_infoPopup != null)
        {
            _infoPopup.QueueFree();
            _infoPopup = null;
            SetProcess(false);
        }
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
