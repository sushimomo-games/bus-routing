using Godot;
using System;

public partial class RoadNode : Node2D
{
	[Export] public Godot.Collections.Array<RoadNode> Neighbors = [];

	/// <summary>
	/// Stable identifier used for runtime-created nodes in save files.
	/// Authored nodes use their path relative to the level scene instead.
	/// </summary>
	public string PersistentId { get; set; } = string.Empty;

	public void AddNeighbor(RoadNode neighbor)
	{
		if (!Neighbors.Contains(neighbor))
		{
			Neighbors.Add(neighbor);
		}
	}

	public void RemoveNeighbor(RoadNode neighbor)
	{
		Neighbors.Remove(neighbor);
	}

	public override void _Ready()
	{
		LevelState.AllRoadNodes.Add(this);
	}
}
