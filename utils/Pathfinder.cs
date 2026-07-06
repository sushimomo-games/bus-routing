using Godot;
using System.Collections.Generic;
using System.Linq;

public static class Pathfinder
{
    /// <summary>
    /// Uses Dijkstra's algorithm to find the lowest-cost route.
    /// </summary>
    public static List<RouteSegment> CalculateBestRoute(BusStop start, HashSet<Destination> validDestinations)
    {
        var validDestinationStops = validDestinations.Select(d => (BusStop)d.ReachableBusStop).ToHashSet();

        var priorityQueue = new PriorityQueue<BusStop, float>();
        
        var costs = new Dictionary<BusStop, float>();
        var lineageMap = new Dictionary<BusStop, (BusStop Parent, RouteSegment Segment)>();

        priorityQueue.Enqueue(start, 0f);
        costs[start] = 0f;

        while (priorityQueue.Count > 0)
        {
            var current = priorityQueue.Dequeue();

            if (validDestinationStops.Contains(current))
            {
                var destinationNode = validDestinations.First(d => d.ReachableBusStop == current);
                return BuildTransitItinerary(start, current, destinationNode, lineageMap);
            }

            // Evaluate Bus Line Connections
            foreach (var busLine in LevelState.AllBusLines)
            {
                if (!busLine.Path.Contains(current)) continue;

                foreach (var nextNode in busLine.Path.OfType<BusStop>())
                {
                    if (nextNode == current) continue;

                    float rideCost = CalculateRideCost(current, nextNode);
                    float newCost = costs[current] + rideCost;

                    if (!costs.ContainsKey(nextNode) || newCost <= costs[nextNode])
                    {
                        costs[nextNode] = newCost;
                        lineageMap[nextNode] = (current, new RideSegment(busLine, current, nextNode));
                        priorityQueue.Enqueue(nextNode, newCost);
                    }
                }
            }

            // Evaluate Walk Connections (Transfers)
            foreach (var nearbyStop in current.GetNearbyBusStops())
            {
                if (!LevelState.AllBusLines.Any(r => r.Path.Contains(nearbyStop))) continue;

                float walkCost = CalculateWalkCost(current, nearbyStop);
                float newCost = costs[current] + walkCost;

                if (!costs.ContainsKey(nearbyStop) || newCost <= costs[nearbyStop])
                {
                    costs[nearbyStop] = newCost;
                    lineageMap[nearbyStop] = (current, new WalkSegment(current, nearbyStop));
                    priorityQueue.Enqueue(nearbyStop, newCost);
                }
            }
        }

        return null; // No route found
    }

    private static float CalculateRideCost(BusStop start, BusStop end)
    {
        return start.GlobalPosition.DistanceTo(end.GlobalPosition);
    }

    private static float CalculateWalkCost(BusStop start, BusStop end)
    {
        float distance = start.GlobalPosition.DistanceTo(end.GlobalPosition);
        return distance * 2.5f; 
    }

    private static List<RouteSegment> BuildTransitItinerary(
        BusStop start, 
        BusStop end, 
        Destination finalDestination,
        Dictionary<BusStop, (BusStop Parent, RouteSegment Segment)> lineageMap)
    {
        var segments = new List<RouteSegment>();
        var backtrackNode = end;

        while (backtrackNode != start)
        {
            var lineage = lineageMap[backtrackNode];
            segments.Add(lineage.Segment);
            backtrackNode = lineage.Parent;
        }

        segments.Reverse();

        segments.Add(new WalkSegment(end, finalDestination));

        return segments;
    }
}