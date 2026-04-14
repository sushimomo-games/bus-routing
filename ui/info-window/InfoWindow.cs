using Godot;
using System;
using static EditorState;
using static LevelState;

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

    private ColorRect _topBarRect;
    private Button _deleteButton;
    private ItemList _routeList;

    public override void _Ready()
    {
        Position = new Vector2(100, 100);
        _topBarRect = GetNode<ColorRect>("VBoxContainer/TopBarRect");
        _deleteButton = GetNode<Button>("VBoxContainer/ButtonsRect/DeleteButton");
        _routeList = GetTree().CurrentScene.GetNode<ItemList>(Path.RouteListNode);
    }

    public override void _Input(InputEvent @event)
    {
        // Handle dragging the info window around the screen.
        if (@event is InputEventMouseButton mouseButton && mouseButton.ButtonIndex == MouseButton.Left)
        {
            if (mouseButton.Pressed && _topBarRect.GetGlobalRect().HasPoint(GetGlobalMousePosition()))
            {
                _isDragging = true;
                _dragOffset = GetGlobalMousePosition() - GlobalPosition;
                GetViewport().SetInputAsHandled(); // Prevent the click from interacting with other UI elements.
            }
            else
            {
                _isDragging = false;
            }
        }
        else if (@event is InputEventMouseMotion && _isDragging)
        {
            GlobalPosition = GetGlobalMousePosition() - _dragOffset;
        }
    }

    private void _on_delete_button_pressed()
    {
        _routeList.RemoveItem(AllRoutes.IndexOf(Route));
        Route.Delete();
        QueueFree();
    }

    private void _on_edit_button_pressed()
    {
        CurrentRouteCreationStep = RouteCreationStep.BeginningEdit;
        SelectedRoute = Route;
        GD.Print($"Clicked edit button. Current step: {CurrentRouteCreationStep}. Selected route: {SelectedRoute.ColorName}");
    }

    public override void _ExitTree()
    {
        OpenWindows.Remove(Route.ID);
    }
}