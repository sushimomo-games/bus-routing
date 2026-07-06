using Godot;

/// <summary>
/// Abstract base class representing a segment of a route,
/// which can be either a walk or a bus ride.
/// </summary>
public abstract class RouteSegment
{
    /// <summary>
    /// The raw distance between beginning and end of the segment
    /// </summary>
    public abstract float Weight { get; }
    
    /// <summary>
    /// The estimated time to traverse this segment calculated from Weight
    /// </summary>
    // public abstract float EstimatedTime { get; }

    // Generates the human-readable instruction (e.g. "Walk to X")
    public abstract string GetInstruction();
}
