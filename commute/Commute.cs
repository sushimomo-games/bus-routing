using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class Commute
{
    public List<CommuteSegment> Itinerary { get; private set; } = new List<CommuteSegment>();

    public float TotalTime => Itinerary.Sum(segment => segment.Weight);
    public int TransferCount => Itinerary.OfType<WalkSegment>().Count() - 1;

    public Commute(List<CommuteSegment> segments)
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