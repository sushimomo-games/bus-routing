using Godot;

/// <summary>
/// Represents a segment of a commute where a passenger walks from one point to another.
/// </summary>
public class WalkSegment : CommuteSegment
{
    public Node2D Origin { get; private set; }
    public Node2D Destination { get; private set; }

    public WalkSegment(Node2D origin, Node2D destination)
    {
        Origin = origin;
        Destination = destination;
    }

    /// <summary>
    /// The spatial distance in world pixels.
    /// </summary>
    public override float Weight => Origin.GlobalPosition.DistanceTo(Destination.GlobalPosition);

    /// <summary>
    /// Estimated time in minutes required to walk this segment. Adjust the
    /// divisor to change the walking speed (pixels per minute).
    /// </summary>
    public override float TimeMinutes => Weight / 50.0f;

    public override string GetInstruction()
    {
        string destName = Destination is BusStop ? "the bus stop" : "your destination";
        
        int roundedMinutes = Mathf.Max(1, Mathf.RoundToInt(TimeMinutes));
        return $"Walk {roundedMinutes} min ({DistanceMiles:F1} mi).";
    }
}