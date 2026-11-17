using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

/// <summary>
/// Persists player-owned level state in Godot's writable user data directory.
/// Scene-authored buildings and intersections remain defined by the level scene.
/// </summary>
public static class SaveGame
{
	private const int CurrentSchemaVersion = 1;
	private const string SaveFilePath = "user://bus-routing-save.json";
	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		WriteIndented = true,
		PropertyNameCaseInsensitive = true
	};

	public static bool SaveCurrentLevel()
	{
		if (LevelState.CurrentLevel is not Level level)
			return false;

		try
		{
			var data = Capture(level);
			var json = JsonSerializer.Serialize(data, JsonOptions);
			using var file = FileAccess.Open(SaveFilePath, FileAccess.ModeFlags.Write);
			if (file is null)
			{
				GD.PushError($"Could not open save file for writing: {FileAccess.GetOpenError()}");
				return false;
			}

			file.StoreString(json);
			file.Flush();
			return file.GetError() == Error.Ok;
		}
		catch (Exception exception)
		{
			GD.PushError($"Could not save the current level: {exception.Message}");
			return false;
		}
	}

	/// <summary>
	/// Restores the save for this level after its authored road graph is ready.
	/// Returns false when no applicable, valid save exists; the scene remains fresh.
	/// </summary>
	public static bool LoadCurrentLevel(Level level)
	{
		if (!FileAccess.FileExists(SaveFilePath))
			return false;

		try
		{
			using var file = FileAccess.Open(SaveFilePath, FileAccess.ModeFlags.Read);
			if (file is null)
			{
				GD.PushWarning($"Could not open save file: {FileAccess.GetOpenError()}");
				return false;
			}

			var data = JsonSerializer.Deserialize<LevelSaveData>(file.GetAsText(), JsonOptions);
			if (data is null || data.LevelId != GetLevelId(level))
				return false;

			var authoredNodes = GetAuthoredNodeMap(level);
			if (!Validate(data, authoredNodes.Keys))
			{
				GD.PushWarning("The saved level data is invalid or uses an unsupported format. Starting a fresh level.");
				return false;
			}

			Restore(level, data, authoredNodes);
			return true;
		}
		catch (Exception exception)
		{
			GD.PushWarning($"Could not load the saved level. Starting fresh: {exception.Message}");
			return false;
		}
	}

	private static LevelSaveData Capture(Level level)
	{
		var nodes = LevelState.AllRoadNodes
			.Where(IsUsableNode)
			.Distinct()
			.ToList();
		var nodeIds = new Dictionary<RoadNode, string>();
		var ids = new HashSet<string>(StringComparer.Ordinal);

		foreach (var node in nodes)
		{
			var id = GetNodeId(level, node);
			if (!ids.Add(id))
				throw new InvalidOperationException($"Duplicate road node ID '{id}'.");
			nodeIds.Add(node, id);
		}

		var data = new LevelSaveData
		{
			SchemaVersion = CurrentSchemaVersion,
			LevelId = GetLevelId(level),
			Budget = LevelState.Budget
		};

		foreach (var stop in nodes.OfType<BusStop>())
		{
			var localPosition = level.ToLocal(stop.GlobalPosition);
			data.BusStops.Add(new BusStopSaveData
			{
				Id = nodeIds[stop],
				X = localPosition.X,
				Y = localPosition.Y
			});
		}

		var edgeKeys = new HashSet<string>(StringComparer.Ordinal);
		foreach (var node in nodes)
		{
			foreach (var neighbor in node.Neighbors)
			{
				if (!IsUsableNode(neighbor) || !nodeIds.TryGetValue(neighbor, out var neighborId))
					continue;

				var nodeId = nodeIds[node];
				if (nodeId == neighborId)
					continue;

				var (first, second) = SortIds(nodeId, neighborId);
				if (edgeKeys.Add($"{first}\n{second}"))
				{
					data.RoadEdges.Add(new RoadEdgeSaveData
					{
						NodeAId = first,
						NodeBId = second
					});
				}
			}
		}

		foreach (var line in LevelState.AllBusLines.Where(IsUsableLine))
		{
			data.BusLines.Add(new BusLineSaveData
			{
				Id = line.ID,
				ColorName = line.ColorName,
				Red = line.Color.R,
				Green = line.Color.G,
				Blue = line.Color.B,
				Alpha = line.Color.A,
				PathNodeIds = line.Path.Select(node =>
				{
					if (!nodeIds.TryGetValue(node, out var id))
						throw new InvalidOperationException($"Bus line {line.ID} references a road node that is not in the level.");
					return id;
				}).ToList()
			});
		}

		return data;
	}

	private static Dictionary<string, RoadNode> GetAuthoredNodeMap(Level level)
	{
		var result = new Dictionary<string, RoadNode>(StringComparer.Ordinal);
		foreach (var node in LevelState.AllRoadNodes.Where(IsUsableNode).OfType<RoadNode>())
		{
			if (node is BusStop)
				continue;

			var id = GetNodeId(level, node);
			if (!result.TryAdd(id, node))
				throw new InvalidOperationException($"Duplicate authored road node ID '{id}'.");
		}
		return result;
	}

	private static bool Validate(LevelSaveData data, IEnumerable<string> authoredIds)
	{
		if (data.SchemaVersion != CurrentSchemaVersion)
			return false;

		var nodeIds = new HashSet<string>(authoredIds, StringComparer.Ordinal);
		foreach (var stop in data.BusStops)
		{
			if (string.IsNullOrWhiteSpace(stop.Id) || !stop.Id.StartsWith("stop:", StringComparison.Ordinal)
				|| !float.IsFinite(stop.X) || !float.IsFinite(stop.Y) || !nodeIds.Add(stop.Id))
				return false;
		}

		var edgeKeys = new HashSet<string>(StringComparer.Ordinal);
		foreach (var edge in data.RoadEdges)
		{
			if (string.IsNullOrWhiteSpace(edge.NodeAId) || string.IsNullOrWhiteSpace(edge.NodeBId)
				|| edge.NodeAId == edge.NodeBId || !nodeIds.Contains(edge.NodeAId) || !nodeIds.Contains(edge.NodeBId))
				return false;

			var (first, second) = SortIds(edge.NodeAId, edge.NodeBId);
			if (!edgeKeys.Add($"{first}\n{second}"))
				return false;
		}

		var lineIds = new HashSet<uint>();
		foreach (var line in data.BusLines)
		{
			if (line.Id == 0 || line.Id == uint.MaxValue || !lineIds.Add(line.Id)
				|| string.IsNullOrWhiteSpace(line.ColorName) || line.PathNodeIds.Count < 2
				|| !float.IsFinite(line.Red) || !float.IsFinite(line.Green)
				|| !float.IsFinite(line.Blue) || !float.IsFinite(line.Alpha)
				|| line.PathNodeIds.Any(id => !nodeIds.Contains(id)))
				return false;

			for (var index = 0; index < line.PathNodeIds.Count - 1; index++)
			{
				var (first, second) = SortIds(line.PathNodeIds[index], line.PathNodeIds[index + 1]);
				if (!edgeKeys.Contains($"{first}\n{second}"))
					return false;
			}
		}

		return data.RoadEdges.Count > 0;
	}

	private static void Restore(Level level, LevelSaveData data, Dictionary<string, RoadNode> nodes)
	{
		var stopScene = GD.Load<PackedScene>(Path.BusStopScene);
		foreach (var stopData in data.BusStops)
		{
			var stop = stopScene.Instantiate<BusStop>();
			stop.PersistentId = stopData.Id;
			stop.Position = new Vector2(stopData.X, stopData.Y);
			level.AddChild(stop);
			nodes.Add(stopData.Id, stop);
		}

		foreach (var node in nodes.Values)
			node.Neighbors.Clear();

		foreach (var edgeData in data.RoadEdges)
		{
			var nodeA = nodes[edgeData.NodeAId];
			var nodeB = nodes[edgeData.NodeBId];
			nodeA.AddNeighbor(nodeB);
			nodeB.AddNeighbor(nodeA);
		}

		foreach (var oldEdge in LevelState.AllRoadEdges.ToArray())
			oldEdge.QueueFree();
		LevelState.AllRoadEdges.Clear();

		var roadEdgeScene = GD.Load<PackedScene>(Path.RoadEdgeScene);
		foreach (var edgeData in data.RoadEdges)
		{
			var edge = roadEdgeScene.Instantiate<RoadEdge>();
			level.AddChild(edge);
			edge.SetEndpoints(nodes[edgeData.NodeAId], nodes[edgeData.NodeBId]);
		}

		LevelState.AllBusLines.Clear();
		LevelState.ResetAvailableColors();
		foreach (var lineData in data.BusLines)
		{
			var line = new BusLine(
				lineData.Id,
				lineData.ColorName,
				new Color(lineData.Red, lineData.Green, lineData.Blue, lineData.Alpha));
			line.Name = $"BusLine_{lineData.Id}";
			line.SetPath(lineData.PathNodeIds.Select(id => nodes[id]).ToList());
			level.AddChild(line);
			LevelState.AllBusLines.Add(line);
			LevelState.ReserveBusLineColor(new KeyValuePair<string, Color>(line.ColorName, line.Color));
		}

		if (data.BusLines.Count > 0)
			BusLine.EnsureNextId(data.BusLines.Max(line => line.Id) + 1);

		level.GetNodeOrNull<BusLineList>(Path.BusLineListNode)?.RebuildFromState();
		LevelState.UpdateAllHouseStatuses();
		LevelState.RefreshAllBusLineVisuals();
		LevelState.Budget = data.Budget;
		level.RefreshDerivedStateAfterLoad();
	}

	private static string GetNodeId(Level level, RoadNode node)
	{
		if (node is BusStop)
		{
			if (string.IsNullOrWhiteSpace(node.PersistentId))
				node.PersistentId = $"stop:{Guid.NewGuid():N}";
			return node.PersistentId;
		}

		return $"scene:{level.GetPathTo(node)}";
	}

	private static string GetLevelId(Level level) =>
		string.IsNullOrWhiteSpace(level.SceneFilePath) ? level.Name.ToString() : level.SceneFilePath;

	private static bool IsUsableNode(RoadNode node) =>
		GodotObject.IsInstanceValid(node) && !node.IsQueuedForDeletion();

	private static bool IsUsableLine(BusLine line) =>
		GodotObject.IsInstanceValid(line) && !line.IsQueuedForDeletion();

	private static (string First, string Second) SortIds(string first, string second) =>
		string.CompareOrdinal(first, second) <= 0 ? (first, second) : (second, first);
	
	public static void ResetGame()
{
	// Delete save file
	if (FileAccess.FileExists(SaveFilePath))
	{
		DirAccess.RemoveAbsolute(
			ProjectSettings.GlobalizePath(SaveFilePath)
		);
	}

	// Delete all bus lines
	foreach (var line in LevelState.AllBusLines.ToArray())
	{
		line.Delete();
	}

	LevelState.AllBusLines.Clear();

	// Reset route colors
	LevelState.ResetAvailableColors();

	// Refresh bus line list UI
	LevelState.CurrentLevel
		.GetNodeOrNull<BusLineList>(Path.BusLineListNode)?
		.RebuildFromState();

	// Restore budget
	LevelState.Budget = Cost.InitialBudget;

	LevelState.RefreshAllBusLineVisuals();

	GD.Print("Game Reset");
}
}//end

public sealed class LevelSaveData
{
	public int SchemaVersion { get; set; }
	public string LevelId { get; set; } = string.Empty;
	public uint Budget { get; set; }
	public List<BusStopSaveData> BusStops { get; set; } = [];
	public List<RoadEdgeSaveData> RoadEdges { get; set; } = [];
	public List<BusLineSaveData> BusLines { get; set; } = [];
}

public sealed class BusStopSaveData
{
	public string Id { get; set; } = string.Empty;
	public float X { get; set; }
	public float Y { get; set; }
}

public sealed class RoadEdgeSaveData
{
	public string NodeAId { get; set; } = string.Empty;
	public string NodeBId { get; set; } = string.Empty;
}

public sealed class BusLineSaveData
{
	public uint Id { get; set; }
	public string ColorName { get; set; } = string.Empty;
	public float Red { get; set; }
	public float Green { get; set; }
	public float Blue { get; set; }
	public float Alpha { get; set; } = 1.0f;
	public List<string> PathNodeIds { get; set; } = [];
}
