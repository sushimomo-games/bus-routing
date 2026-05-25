using Godot;

/// <summary>
/// Abstract base class representing a segment of a route,
/// which can be either a walk or a bus ride.
/// </summary>
public abstract class RouteSegment
{
    // The estimated time this specific step takes
    public abstract float EstimatedTime { get; }
    
    // Generates the human-readable instruction (e.g. "Walk to X")
    public abstract string GetInstruction();
}
