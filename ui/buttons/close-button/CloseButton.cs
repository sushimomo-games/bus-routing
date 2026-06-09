using Godot;
using System;

public partial class CloseButton : Button
{
    private void _on_pressed()
    {
        GD.Print("Close button pressed, closing info window. i am stupid.");
        GetParent().GetParent().GetParent().QueueFree();
    }
}
