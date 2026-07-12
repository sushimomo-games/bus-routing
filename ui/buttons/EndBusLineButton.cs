using Godot;

public partial class EndBusLineButton : Button
{
    private void _on_pressed()
    {
        BusLineEditor.FinalizeBusLineCreation();
    }
}