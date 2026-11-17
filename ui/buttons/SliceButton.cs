using Godot;

public partial class SliceButton : TextureButton
{
    /// <summary>
    /// Turns slice mode on or off. It only works while a bus line is being edited.
    /// </summary>
    private void _on_pressed() => BusLineEditor.ToggleSlicing();
}
