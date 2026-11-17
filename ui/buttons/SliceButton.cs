using Godot;
using System.Linq;

public partial class SliceButton : TextureButton
{
    /// <summary>
    /// Turns slice mode on or off. It only works while a bus line is being edited.
    /// </summary>
    private void _on_pressed() => BusLineEditor.ToggleSlicing();
    // Grays out the button when there is nothing to slice.
    public override void _Process(double delta)
    {
        var step = EditorState.CurrentBusLineCreationStep;

        // Slice mode is on, so keep the button usable to turn it off.
        bool isSlicing = step == BusLineCreationStep.Slicing;

        // Edit was clicked on a line, and Slice will start the edit.
        bool canStartEdit = step == BusLineCreationStep.BeginningEdit
            && GodotObject.IsInstanceValid(EditorState.SelectedBusLine)
            && EditorState.SelectedBusLine.Path.Count >= 2;

        // Editing a line that still has at least one road to cut.
        bool hasRoadToCut = BusLineEditor.IsInEditingMode
            && BusLineEditor.DraftLineSegments.Any(s => s.Nodes.Count >= 2);

        Disabled = !(isSlicing || canStartEdit || hasRoadToCut);
        Modulate = Disabled ? new Color(0.2f, 0.2f, 0.2f, 0.25f) : Colors.White;
    }
}
