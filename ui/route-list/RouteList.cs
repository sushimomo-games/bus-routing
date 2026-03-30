using Godot;
using System;
using System.Collections.Generic;
using static EditorState;

public partial class RouteList : ItemList
{
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

            if (OpenWindows.ContainsKey(SelectedRoute.ID))
                return;

            var window = InfoWindowScene.Instantiate<InfoWindow>();
            window.Route = SelectedRoute;
            var canvasLayer = GetTree().CurrentScene.GetNode<CanvasLayer>("CanvasLayer"); 
            canvasLayer.AddChild(window);
            OpenWindows[SelectedRoute.ID] = window;
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
