using Godot;
using System;

public partial class Camera : Camera2D
{
    /// <summary>
    /// Indicates whether the camera is currently being dragged.
    /// </summary>
    private bool isDragging = false;

    /// <summary>
    /// The position where the drag started.
    /// </summary>
    private Vector2 dragStartPosition;

    /// <summary>
    /// Zooms the camera in by the specified factor.
    /// </summary>
    /// <param name="factor">The factor by which to zoom in.</param>
    private void ZoomIn(float factor)
    {
        float z = Math.Min(5f, Zoom.X * factor);
        Zoom = new Vector2(z, z);
    }

    /// <summary>
    /// Zooms the camera out by the specified factor.
    /// </summary>
    /// <param name="factor">The factor by which to zoom out.</param>
    private void ZoomOut(float factor)
    {
        float z = Math.Max(0.2f, Zoom.X * factor);
        Zoom = new Vector2(z, z);
    }

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

        if (@event.IsMouseWheelDown())
        {
            ZoomOut(0.95f);
        }
        else if (@event.IsMouseWheelUp())
        {
            ZoomIn(1.05f);
        }
    }
}
