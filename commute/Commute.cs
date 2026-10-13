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

    /// <summary>
    /// The total distance in miles that a resident walks during a commute.
    /// This includes the walk to the first bus stop, any walking transfers
    /// between stops, and the walk from the last stop to the destination.
    /// </summary>
    public float MilesWalked => Itinerary.OfType<WalkSegment>().Sum(segment => segment.DistanceMiles);

    public int TransferCount => Itinerary.OfType<WalkSegment>().Count() - 1;

    /// <summary>
    /// Returns a list of sequential instructions for the UI.
    /// </summary>
    public List<string> GetDirections()
    {
        return [.. Itinerary.Select(segment => segment.GetInstruction())];
    }
}