using Godot;
using System;

public partial class CloseButton : Button
{
    private void _on_pressed()
    {
        GetParent().QueueFree();
    }
}
