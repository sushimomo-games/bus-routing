using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class Route
{
    public List<RouteSegment> Itinerary { get; private set; } = new List<RouteSegment>();

    public float TotalTime => Itinerary.Sum(segment => segment.EstimatedTime);
    public int TransferCount => Itinerary.OfType<WalkSegment>().Count() - 1;

    public Route(List<RouteSegment> segments)
    {
        Itinerary = segments;
    }

    /// <summary>
    /// Returns a list of sequential instructions for the UI.
    /// </summary>
    public List<string> GetDirections()
    {
        return Itinerary.Select(segment => segment.GetInstruction()).ToList();
    }
}