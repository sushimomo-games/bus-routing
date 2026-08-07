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

    public Commute CurrentItinerary { get; private set; }

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
        _infoPopup = GetNode<Control>("InfoPopup");
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

        var transitSegments = Pathfinder.CalculateBestRoute(startStop, validDestinations);

        if (transitSegments != null)
        {
            transitSegments.Insert(0, new WalkSegment(this, startStop));
            
            CurrentItinerary = new Commute(transitSegments);
            IsChecked = true;
        }
        else
        {
            CurrentItinerary = null;
            IsChecked = false;
        }
    }

    private void _on_area_2d_mouse_entered()
    {
        if (_infoPopup == null)
        {
            _infoPopup = _infoPopupScene.Instantiate<Control>();
            var canvasLayer = GetTree().CurrentScene.GetNode<CanvasLayer>("CanvasLayer"); 
            canvasLayer.AddChild(_infoPopup);
            _infoPopup.GetNode<Label>("Label").Text = $"Bus Usage Probability: {BusUsageProbability:P1}\nItinerary: {(CurrentItinerary != null ? string.Join("\n", CurrentItinerary.GetDirections()) : "No route available")}";
            _infoPopup.Modulate = Modulate;
        }

        if (CurrentItinerary != null)
        {
            foreach (var segment in CurrentItinerary.Itinerary.OfType<RideSegment>())
            {
                segment.Line.Visual.HighlightPath(segment.GetPathNodes());
            }
        }
    }

    private void _on_area_2d_mouse_exited()
    {
        _infoPopup?.QueueFree();
        _infoPopup = null;

        if (CurrentItinerary != null)
        {
            foreach (var segment in CurrentItinerary.Itinerary.OfType<RideSegment>())
            {
                segment.Line.Visual.ClearHighlight();
            }
        }
    }
}
