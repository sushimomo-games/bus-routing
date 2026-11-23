using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using static LevelState;


/// <summary>
/// Represents a finalized bus line running in the simulation.
/// </summary>
public partial class BusLine : Node
{
	private static uint _nextID = 1;

	public uint ID { get; private set; }
	public List<RoadNode> Path { get; set; } = [];
	public string ColorName { get; set; }
	public Color Color { get; set; }
	public BusLineVisual Visual { get; private set; }

	public event Action OnPathChanged;
	public event Action OnDeleted;

	public float TimeToComplete
	{
		get
		{
			float total = 0f;
			for (int i = 0; i < Path.Count - 1; i++)
			{
				total += Path[i].GlobalPosition.DistanceTo(Path[i + 1].GlobalPosition);
			}
			return total;
		}
	}

	public BusLine()
	{
		ID = _nextID++;
		
		var colorInfo = LevelState.GetNextBusLineColor();
		if (colorInfo.HasValue)
		{
			ColorName = colorInfo.Value.Key;
			Color = colorInfo.Value.Value;
		}
		else
		{
			ColorName = "Default";
			Color = Colors.White;
		}

		InitializeVisual();
	}

	/// <summary>
	/// Creates a line from persisted values without consuming a palette color.
	/// </summary>
	public BusLine(uint id, string colorName, Color color)
	{
		ID = id;
		ColorName = colorName;
		Color = color;
		if (id >= _nextID && id < uint.MaxValue)
			_nextID = id + 1;

		InitializeVisual();
	}

	private void InitializeVisual()
	{
		Visual = new BusLineVisual(this);
		AddChild(Visual);
	}

	internal static void EnsureNextId(uint nextId)
	{
		if (nextId > _nextID)
			_nextID = nextId;
	}

	public void SetPath(List<RoadNode> newPath)
	{
		Path = new List<RoadNode>(newPath);
		Visual?.UpdateVisual();
		OnPathChanged?.Invoke();
	}

	public void ClearPath()
	{
		Path.Clear();
		Visual?.ClearPoints();
		OnPathChanged?.Invoke();
	}

	public bool ContainsNode(RoadNode node) => Path.Contains(node);

	/// <summary>
	/// Detaches a line for undo while returning its color to the available pool.
	/// The line node remains alive so the history entry can restore it.
	/// </summary>
	

/// <summary>
/// Represents a finalized bus line running in the simulation.
/// </summary>

	
	

	
	public void RemoveNode(RoadNode node)
	{
		if (ContainsNode(node))
		{
			var newPath = Path.Where(n => n != node).ToList();
			SetPath(newPath);
		}
	}

	/// <summary>
	/// Removes the busLine from the level and returns its color to the pool,
	/// but keeps the node alive (detached from the scene tree) so the removal
	/// can be undone.
	/// </summary>
	public void Detach()
	{
		LevelState.AllBusLines.Remove(this);
		LevelState.ReturnBusLineColor(new KeyValuePair<string, Color>(ColorName, Color));
		GetParent()?.RemoveChild(this);
		UpdateAllHouseStatuses();

		OnDeleted?.Invoke();
	}

	/// <summary>
	/// Removes the bus line from the level as a player action.
	/// </summary>
	/// <param name="undoable">
	/// If true, the line is detached and a <see cref="DeleteBusLineEdit"/> is logged so the delete can be undone.
	/// If false, the line is freed immediately, for example when deleting a bus stop invalidates it.
	/// </param>
	public void DeleteFromLevel(bool undoable = false)
	{
		int index = LevelState.AllBusLines.IndexOf(this);

		if (undoable)
		{
			Detach();
			EditorState.History.LogEdit(new DeleteBusLineEdit(this, index));
		}
		else
		{
			Delete();
		}

		if (EditorState.SelectedBusLine == this) EditorState.SelectedBusLine = null;
		UpdateAllHouseStatuses();
	}

	/// <summary>
	/// Use this instead of QueueFree() directly on the bus line to properly clean
	/// up the busLine and return its color to the pool.
	/// </summary>
	
	public void Delete()
	{
		LevelState.AllBusLines.Remove(this);
		LevelState.ReturnBusLineColor(new KeyValuePair<string, Color>(ColorName, Color));
		Visual?.QueueFree();
		UpdateAllHouseStatuses(); 
		
		OnDeleted?.Invoke();
		QueueFree();
	}
}

	
