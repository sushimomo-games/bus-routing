using Godot; 
using System.Collections.Generic; 
using System.Linq; 

public static class Pathfinder 
{ 
    /// <summary>
    /// Uses Dijkstra's algorithm to find the lowest-cost route.
    /// </summary>
    public static List<CommuteSegment> CalculateBestRoute(BusStop start, HashSet<Destination> validDestinations) 
    { 
        var validDestinationStops = validDestinations.Select(d => (BusStop)d.ReachableBusStop).ToHashSet(); 
        var priorityQueue = new PriorityQueue<BusStop, float>(); 
        var costs = new Dictionary<BusStop, float>(); 
        var lineageMap = new Dictionary<BusStop, (BusStop Parent, CommuteSegment Segment)>(); 
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

            EvaluateBusConnections(current, costs, lineageMap, priorityQueue);
            EvaluateWalkConnections(current, costs, lineageMap, priorityQueue);
        } 
        
        return null; 
    } 

    private static void EvaluateBusConnections(
        BusStop current, 
        Dictionary<BusStop, float> costs, 
        Dictionary<BusStop, (BusStop Parent, CommuteSegment Segment)> lineageMap, 
        PriorityQueue<BusStop, float> priorityQueue)
    {
        foreach (var busLine in LevelState.AllBusLines) 
        { 
            int currentIndex = busLine.Path.IndexOf(current); 
            if (currentIndex == -1) continue; 
            
            for (int i = currentIndex + 1; i < busLine.Path.Count; i++) 
            { 
                if (busLine.Path[i] is BusStop nextNode) 
                { 
                    var routePath = busLine.Path.Select(node => (Node2D)node).ToList();
                    float rideCost = CalculateDirectionalRideCost(routePath, currentIndex, i); 
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
    }

    private static void EvaluateWalkConnections(
        BusStop current, 
        Dictionary<BusStop, float> costs, 
        Dictionary<BusStop, (BusStop Parent, CommuteSegment Segment)> lineageMap, 
        PriorityQueue<BusStop, float> priorityQueue)
    {
        foreach (var nearbyStop in current.GetNearbyBusStops()) 
        { 
            if (!LevelState.AllBusLines.Any(r => r.Path.Contains(nearbyStop))) continue; 
            
            var walkSegment = new WalkSegment(current, nearbyStop);
            float newCost = costs[current] + walkSegment.Cost; 
            
            if (!costs.ContainsKey(nearbyStop) || newCost < costs[nearbyStop]) 
            { 
                costs[nearbyStop] = newCost; 
                lineageMap[nearbyStop] = (current, walkSegment); 
                priorityQueue.Enqueue(nearbyStop, newCost); 
            } 
        } 
    }

    internal static float CalculateDirectionalRideCost(List<Node2D> path, int startIndex, int endIndex) 
    { 
        float totalDistance = 0f; 
        for (int i = startIndex; i < endIndex; i++) 
        { 
            totalDistance += path[i].GlobalPosition.DistanceTo(path[i + 1].GlobalPosition); 
        } 
        return totalDistance; 
    } 

    internal static List<CommuteSegment> BuildTransitItinerary( 
        BusStop start, 
        BusStop end, 
        Destination finalDestination, 
        Dictionary<BusStop, (BusStop Parent, CommuteSegment Segment)> lineageMap) 
    { 
        var segments = new List<CommuteSegment>(); 
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