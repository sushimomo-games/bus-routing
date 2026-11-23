using System.Threading;
using Godot;
using static LevelState;
using System.Linq;
using static Path;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

public partial class BusStopDeletor : Area2D
{

    public void DeleteBusStop()
    {
        if (Budget < Cost.BusStopRemoval)
        {
            var errorMessage = GetTree().CurrentScene.GetNode<ErrorMessage>(
                ErrorMessageNode
            );
            errorMessage.DisplayMessage("Insufficient budget to remove bus stop.");
            return;
        }

        var busStop = GetParent<BusStop>();
        if (BusLineEditor.BusLineInProgress != null && BusLineEditor.DraftLineSegments.Any(segment => segment.Nodes.Contains(busStop)))
        {
            var errorMessage = GetTree().CurrentScene.GetNode<ErrorMessage>(
                ErrorMessageNode
            );
            errorMessage.DisplayMessage("Cannot Remove bus stops on lines currently being edited");
            return;
        }

        var budgetBefore = Budget;
        var position = busStop.GlobalPosition;

        // var affectedRoutes = AllBusLines.Where(busLine => busLine.ContainsNode(busStop)).ToList();
        // foreach (var busLine in affectedRoutes)
        // {
        //     busLine.RemoveExcessLineSegments(busStop);

        //     if (busLine.Path.OfType<BusStop>().Count() <= 1)
        //     {
        //         busLine.Delete();
        //     }
        // }

        var affectedRoutes = AllBusLines
            .Where(busLine => busLine.ContainsNode(busStop))
            .ToList();

        var linePathsBefore = affectedRoutes.ToDictionary(
            line => line,
            line => new List<RoadNode>(line.Path)
        );

        foreach (var busLine in affectedRoutes)
        {
            busLine.RemoveExcessLineSegments(busStop);
        }

        var A = busStop.Neighbors[0];
        var B = busStop.Neighbors[1];

        A.AddNeighbor(B);
        B.AddNeighbor(A);

        A.RemoveNeighbor(busStop);
        B.RemoveNeighbor(busStop);

        for (int i = busStop.ConnectedEdges.Count - 1; i >= 0; i--)
        {
            busStop.ConnectedEdges[i]?.QueueFree();
        }

        foreach (var line in AllBusLines.ToList())
        {
            if (line.ContainsNode(busStop))
            {
                line.RemoveNode(busStop);
            }
        }

        busStop.QueueFree();

        var roadEdgeScene = GD.Load<PackedScene>(RoadEdgeScene);
        var edge = roadEdgeScene.Instantiate<RoadEdge>();
        CurrentLevel.AddChild(edge);
        edge.SetEndpoints(A, B);

        UpdateAllHouseStatuses();

        EditorState.History.LogEdit(new BusStopEdit(
            false,
            budgetBefore,
            position,
            A,
            B,
            busStop,
            linePathsBefore: linePathsBefore,
            upgrades: busStop.PurchasedUpgrades.ToList()
        ));

    }
}