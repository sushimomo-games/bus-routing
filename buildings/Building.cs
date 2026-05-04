using Godot;
using System.Collections.Generic;

/// <summary>
/// Base class for buildings (houses and workplaces) that can be serviced by bus stops.
/// Handles visual highlighting.
/// </summary>

[Tool]
public abstract partial class Building : Node2D
{
    public static readonly Dictionary<string, Color> BuildingColorPalette = new()
    {
        { "Red", Colors.Red },
        { "Blue", Colors.DeepSkyBlue },
        { "Green", Colors.ForestGreen },
        { "Yellow", Colors.Gold },
        { "White", Colors.White }
    };
    private string _selectedColorName = "White";
    private BusStopDetector _busStopDetector;
    private Sprite2D _buildingSprite;
    private Color _originalColor;
    private Color _targetColor;
    private float _lerpSpeed = 10f;

    [Export(PropertyHint.Enum, "Red,Blue,Green,Yellow,White")]
    public string BuildingColor
    {
        get => _selectedColorName;
        set
        {
            _selectedColorName = value;
            UpdateSpriteColor();
        }
    }

    /// <summary>
    /// Gets the bus stop node that this building can reach.
    /// Returns null if no bus stop is currently in range.
    /// </summary>
    public Node ReachableBusStop => _busStopDetector?.ReachableBusStop;

    /// <summary>
    /// The color multiplier to apply when a bus stop is in range.
    /// Override in derived classes to customize highlighting intensity.
    /// </summary>
    protected abstract Color HighlightFactor { get; }


    public override void _Ready()
    {
        UpdateSpriteColor();

        if (!Engine.IsEditorHint())
        {
            _busStopDetector = GetNodeOrNull<BusStopDetector>("BusStopDetector");
            _buildingSprite = GetNodeOrNull<Sprite2D>("Sprite2D");
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (Engine.IsEditorHint()) return;
        bool shouldHighlight = false;

        if (ReachableBusStop != null)
        {
            if (ReachableBusStop is BusStop)
            {
                shouldHighlight = true;
            }
            else if (ReachableBusStop is PreviewBusStop)
            {
                shouldHighlight = LevelState.LevelUI?.IsValidPlacement == true;
            }
        }

        if (shouldHighlight)
        {
            _targetColor = _originalColor * HighlightFactor;
        }
        else
        {
            _targetColor = _originalColor;
        }

        _buildingSprite.Modulate = _buildingSprite.Modulate.Lerp(
           _targetColor, (float)(delta * _lerpSpeed)
       );
    }

    private void UpdateSpriteColor()
    {
        if (BuildingColorPalette.TryGetValue(_selectedColorName, out Color newColor))
        {
            _originalColor = newColor;
            this.Modulate = newColor;
            if (IsInsideTree())
            {
                if (_buildingSprite == null)
                {
                    _buildingSprite = GetNodeOrNull<Sprite2D>("Sprite2D");
                }
                QueueRedraw();
            }
        }
    }
}