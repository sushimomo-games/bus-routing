using Godot;
using System;
using System.Collections.Generic;
using static EditorState;

public partial class BusLineList : ItemList
{
    private PackedScene InfoWindowScene = GD.Load<PackedScene>(Path.InfoWindowScene);

    public override void _UnhandledInput(InputEvent @event)
    {
        if (SelectedBusLine != null && @event.IsActionPressed("ui_text_delete")) // "ui_cancel" is usually the Escape or Delete key
        {
            var colorInfo = new KeyValuePair<string, Color>(SelectedBusLine.ColorName, SelectedBusLine.Color);
            DeleteBusLine(SelectedBusLine);
            LevelState.ReturnBusLineColor(colorInfo);
            AcceptEvent(); 
        }
    }

    public void DeleteBusLine(BusLine busLine)
    {
        GD.Print($"Deleting busLine: {busLine.ColorName}");

        int itemIndex = LevelState.AllBusLines.IndexOf(busLine);
        if (itemIndex != -1)
        {
            RemoveItem(itemIndex);
        }

        busLine.Visual?.QueueFree();
        LevelState.AllBusLines.Remove(busLine);
        busLine.QueueFree();

        SelectedBusLine = null;
        LevelState.UpdateAllHouseStatuses();
    }

    private void _on_item_selected(int index)
    {
        if (index >= 0 && index < LevelState.AllBusLines.Count)
        {
            SelectedBusLine = LevelState.AllBusLines[index];

            if (OpenWindows.ContainsKey(SelectedBusLine.ID))
                return;

            var window = InfoWindowScene.Instantiate<InfoWindow>();
            window.BusLine = SelectedBusLine;
            var canvasLayer = GetTree().CurrentScene.GetNode<CanvasLayer>("CanvasLayer"); 
            canvasLayer.AddChild(window);
            OpenWindows[SelectedBusLine.ID] = window;
        }
    }

    public override void _Input(InputEvent @event)
    {
        // if (@event.IsLeftMouseClick())
        // {
        //     // Check if the click was outside the item list to deselect the current busLine
        //     var mouseEvent = (InputEventMouseButton)@event;
        //     Rect2 itemListRect = GetGlobalRect();
        //     if (!itemListRect.HasPoint(mouseEvent.GlobalPosition))
        //     {
        //         DeselectAll();
        //         if (SelectedBusLine != null)
        //         {
        //             SelectedBusLine = null;
        //         }
        //     }
        // }
    }
}
