using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class Commute(List<CommuteSegment> segments)
{
    public List<CommuteSegment> Itinerary { get; private set; } = segments;

    /// <summary>
    /// The estimated time that a commute should take a resident in minutes.
    /// </summary>
    public float TotalTimeMinutes => Itinerary.Sum(segment => segment.TimeMinutes);
    
    public int TransferCount => Itinerary.OfType<WalkSegment>().Count() - 1;

    /// <summary>
    /// Returns a list of sequential instructions for the UI.
    /// </summary>
    public List<string> GetDirections()
    {
        return [.. Itinerary.Select(segment => segment.GetInstruction())];
    }
}