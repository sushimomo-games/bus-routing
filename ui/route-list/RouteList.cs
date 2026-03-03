using Godot;
using System;
using System.Collections.Generic;
using static EditorState;

public partial class RouteList : ItemList
{
    private Dictionary<uint, Control> _openWindows = new();

    private PackedScene InfoWindowScene = GD.Load<PackedScene>(Path.InfoWindowScene);

    public override void _UnhandledInput(InputEvent @event)
    {
        if (SelectedRoute != null && @event.IsActionPressed("ui_text_delete")) // "ui_cancel" is usually the Escape or Delete key
        {
            var colorInfo = new KeyValuePair<string, Color>(SelectedRoute.ColorName, SelectedRoute.Color);
            DeleteRoute(SelectedRoute);
            LevelState.ReturnRouteColor(colorInfo);
            AcceptEvent(); 
        }
    }

    public void DeleteRoute(Route route)
    {
        GD.Print($"Deleting route: {route.ColorName}");

        int itemIndex = LevelState.AllRoutes.IndexOf(route);
        if (itemIndex != -1)
        {
            RemoveItem(itemIndex);
        }

        route.Visual?.QueueFree();
        LevelState.AllRoutes.Remove(route);
        route.QueueFree();

        SelectedRoute = null;
        LevelState.UpdateAllHouseStatuses();
    }

    private void _on_item_selected(int index)
    {
        if (index >= 0 && index < LevelState.AllRoutes.Count)
        {
            SelectedRoute = LevelState.AllRoutes[index];
            GD.Print($"Time to complete: {SelectedRoute.TimeToComplete} minutes");

            if (_openWindows.TryGetValue(SelectedRoute.ID, out var existing))
            {
                // This logic is mostly to test if this if statement is working but should be changedd
                existing.QueueFree();
                _openWindows.Remove(SelectedRoute.ID);
            }
            else
            {
                var window = InfoWindowScene.Instantiate<Control>();
                window.Position = new Vector2(100, 100);
                var canvasLayer = GetTree().CurrentScene.GetNode<CanvasLayer>("EditorUI"); 
                canvasLayer.AddChild(window);
                _openWindows[SelectedRoute.ID] = window;
            }

        }
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsLeftMouseClick())
        {
            // Check if the click was outside the item list to deselect the current route
            var mouseEvent = (InputEventMouseButton)@event;
            Rect2 itemListRect = GetGlobalRect();
            if (!itemListRect.HasPoint(mouseEvent.GlobalPosition))
            {
                DeselectAll();
                if (SelectedRoute != null)
                {
                    SelectedRoute = null;
                }
            }
        }
    }
}
