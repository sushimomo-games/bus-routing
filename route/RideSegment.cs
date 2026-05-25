using Godot;
using System.Linq;

/// <summary>
/// Represents a segment of a route where the user takes a bus.
/// Contains details about the bus line, boarding and alighting stops,
/// and calculates estimated time and instructions for this segment.
/// </summary>
public class RideSegment : RouteSegment
{
    public BusLine Line { get; private set; }
    public BusStop BoardingStop { get; private set; }
    public BusStop AlightingStop { get; private set; }

    public int StopsTraveled { get; private set; }

    public RideSegment(BusLine line, BusStop boardingStop, BusStop alightingStop)
    {
        Line = line;
        BoardingStop = boardingStop;
        AlightingStop = alightingStop;
        
        CalculateRideStats();
    }

    private void CalculateRideStats()
    {
        // Filter the BusLine's path to only actual BusStops
        var stopsOnLine = Line.Path.OfType<BusStop>().ToList();
        
        int startIndex = stopsOnLine.IndexOf(BoardingStop);
        int endIndex = stopsOnLine.IndexOf(AlightingStop);
        
        StopsTraveled = Mathf.Abs(endIndex - startIndex);
    }
    
    // For RideSegment, distance along nodes
    public override float EstimatedTime 
    { 
        get 
        {
            float totalDist = 0;
            bool counting = false;
            foreach(var node in Line.Path)
            {
                if(node == BoardingStop || node == AlightingStop)
                {
                    if(counting)
                    {
                        // We hit the end node
                        break;
                    }
                    else
                    {
                        // We hit the start node
                        counting = true;
                    }
                }
                
                if(counting)
                {
                    var nextNode = Line.Path[Line.Path.IndexOf(node) + 1];
                    totalDist += node.GlobalPosition.DistanceTo(nextNode.GlobalPosition);
                }
            }
            return totalDist;
        } 
    }

    public override string GetInstruction()
    {
        string plural = StopsTraveled == 1 ? "stop" : "stops";
        // E.g., "Take the Orange Line for 3 stops."
        return $"Take the {Line.ColorName} line for {StopsTraveled} {plural}.";
    }
}
