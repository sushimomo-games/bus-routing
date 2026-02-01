using Godot;

/// <summary>
/// Base class for buildings (houses and workplaces) that can be serviced by bus stops.
/// Handles visual highlighting.
/// </summary>
public abstract partial class Building : Node2D
{
    private BusStopDetector _busStopDetector;
    private Sprite2D _buildingSprite;
    private Color _originalColor;
    private Color _targetColor;
    private float _lerpSpeed = 10f;

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
        _busStopDetector = GetNode<BusStopDetector>("BusStopDetector");
        _buildingSprite = GetNode<Sprite2D>("Sprite2D");
        _originalColor = _buildingSprite.Modulate;
    }

    public override void _PhysicsProcess(double delta)
    {
        bool shouldHighlight = false;
        
        if (ReachableBusStop != null)
        {
            if (ReachableBusStop is BusStop)
            {
                shouldHighlight = true;
            }
            else if (ReachableBusStop is PreviewBusStop)
            {
                shouldHighlight = LevelState.BusStopPlacement?.IsValidPlacement == true;
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
}