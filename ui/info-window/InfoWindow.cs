using Godot;
using System;
using static EditorState;

public partial class InfoWindow : Control
{
    /// <summary>
    /// The route that this info window is displaying information about.
    /// </summary>
    public Route Route { get; set; }

    public override void _Ready()
    {
        Position = new Vector2(100, 100);
    }

    public override void _ExitTree()
    {
        OpenWindows.Remove(Route.ID);
    }
}