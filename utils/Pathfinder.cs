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
        
        var visited = new HashSet<BusStop>(); 

        priorityQueue.Enqueue(start, 0f);
        costs[start] = 0f;

        while (priorityQueue.Count > 0)
        {
            var current = priorityQueue.Dequeue();

            // Skip stale queue entries 
            if (!visited.Add(current)) continue;

            if (validDestinationStops.Contains(current))
            {
                var destinationNode = validDestinations.First(d => d.ReachableBusStop == current);
                return BuildTransitItinerary(start, current, destinationNode, lineageMap);
            }

            foreach (var busLine in LevelState.AllBusLines)
            {
                int currentIndex = busLine.Path.IndexOf(current);
                if (currentIndex == -1) continue;

                for (int i = currentIndex + 1; i < busLine.Path.Count; i++)
                {
                    if (busLine.Path[i] is BusStop nextNode)
                    {
                        float rideCost = CalculateDirectionalRideCost(busLine.Path, currentIndex, i);
                        float newCost = costs[current] + rideCost;

                        if (!costs.ContainsKey(nextNode) || newCost < costs[nextNode])
                        {
                            costs[nextNode] = newCost;
                            lineageMap[nextNode] = (current, new RideSegment(busLine, current, nextNode));
                            priorityQueue.Enqueue(nextNode, newCost);
                        }
                    }
                }
            }

            // Evaluate Walk Connections (Transfers)
            foreach (var nearbyStop in current.GetNearbyBusStops())
            {
                if (!LevelState.AllBusLines.Any(r => r.Path.Contains(nearbyStop))) continue;

                float walkCost = CalculateWalkCost(current, nearbyStop);
                float newCost = costs[current] + walkCost;

                // Changed <= to < 
                if (!costs.ContainsKey(nearbyStop) || newCost < costs[nearbyStop])
                {
                    costs[nearbyStop] = newCost;
                    lineageMap[nearbyStop] = (current, new WalkSegment(current, nearbyStop));
                    priorityQueue.Enqueue(nearbyStop, newCost);
                }
            }
        }

        return null; 
    }

    private static float CalculateWalkCost(BusStop start, BusStop end)
    {
        float distance = start.GlobalPosition.DistanceTo(end.GlobalPosition);
        return distance * 2.5f; 
    }

    private static float CalculateDirectionalRideCost(List<RoadNode> path, int startIndex, int endIndex)
    {
        float totalDistance = 0f;
        for (int i = startIndex; i < endIndex; i++)
        {
            totalDistance += path[i].GlobalPosition.DistanceTo(path[i + 1].GlobalPosition);
        }
        return totalDistance;
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