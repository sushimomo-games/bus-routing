using Godot;

/// <summary>
/// Represents a segment of a route where the user walks from one point to another.
/// </summary>
public class WalkSegment : RouteSegment
{
    public Node2D Origin { get; private set; }
    public Node2D Destination { get; private set; }

    public WalkSegment(Node2D origin, Node2D destination)
    {
        Origin = origin;
        Destination = destination;
    }

    public override float EstimatedTime => Origin.GlobalPosition.DistanceTo(Destination.GlobalPosition); // You can apply a walking speed modifier here

    public override string GetInstruction()
    {
        string destName = Destination is BusStop ? "the bus stop" : "your destination";
        return $"Walk to {destName}.";
    }
}
