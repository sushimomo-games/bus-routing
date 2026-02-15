using Godot;
using System;
using System.Collections.Generic;
using static EditorState;

public partial class RouteList : ItemList
{
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
            EditorState.SelectedRoute = LevelState.AllRoutes[index];
            GD.Print($"Selected route for editing: {EditorState.SelectedRoute.ColorName}");
        }
    }

    private new void DeselectAll()
    {
        // Deselect in the UI
        DeselectAll();
        // Clear the selected route from the state
        if (EditorState.SelectedRoute != null)
        {
            GD.Print($"Deselected route: {EditorState.SelectedRoute.ColorName}");
            EditorState.SelectedRoute = null;
        }
    }
}
