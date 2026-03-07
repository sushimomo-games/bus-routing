using Godot;
using System;
using static EditorState;

public partial class InfoWindow : Control
{
    /// <summary>
    /// The route that this info window is displaying information about.
    /// </summary>
    public Route Route { get; set; }

    /// <summary>
    /// Whether or not this info window is currently being dragged by the user.
    /// </summary>
    private bool _isDragging = false;

    /// <summary>
    /// The offset from the mouse position to the top-left corner of the info
    /// window.
    /// </summary>
    private Vector2 _dragOffset = Vector2.Zero;

    public override void _Ready()
    {
        Position = new Vector2(100, 100);
    }

    public override void _Input(InputEvent @event)
    {
        // Handle dragging the info window around the screen.
        if (@event is InputEventMouseButton mouseButton)
        {
            if (mouseButton.ButtonIndex == MouseButton.Left)
            {
                _isDragging = mouseButton.Pressed;
                if (_isDragging)
                    _dragOffset = GetGlobalMousePosition() - GlobalPosition;
            }
        }
        else if (@event is InputEventMouseMotion && _isDragging)
        {
            GlobalPosition = GetGlobalMousePosition() - _dragOffset;
        }
    }

    public override void _ExitTree()
    {
        OpenWindows.Remove(Route.ID);
    }
}