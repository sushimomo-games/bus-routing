using Godot;
using System;

public partial class Camera : Camera2D
{
    private bool isDragging = false;
    private Vector2 dragStartPosition;

    public override void _Input(InputEvent @event)
    {
        if (@event.IsMiddleMouseClick())
        {
            isDragging = true;
            dragStartPosition = GetGlobalMousePosition();
        }
        else if (@event.IsMiddleMouseRelease())
        {
            isDragging = false;
        }

        if (@event is InputEventMouseMotion mouseMotionEvent && isDragging)
        {
            Vector2 dragDelta = mouseMotionEvent.Relative;
            
            Position -= dragDelta / Zoom; // Division by Zoom adjusts movement speed based on zoom level
        }

        if (@event.IsMouseWheelUp())
        {
            Zoom = new Vector2(Math.Max(0.2f, Zoom.X - 0.1f), Math.Max(0.2f, Zoom.Y - 0.1f));
        }
        else if (@event.IsMouseWheelDown())
        {
            Zoom = new Vector2(Math.Min(5f, Zoom.X + 0.1f), Math.Min(5f, Zoom.Y + 0.1f));
        }
    }
}
