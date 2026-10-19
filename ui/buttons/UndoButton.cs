using Godot;

public partial class UndoButton : TextureButton
{
    private void _on_pressed()
    {
        GD.Print($"[DEBUG - Undo] Undo pressed. CanUndo = {EditorState.History.CanUndo}");
        EditorState.History.Undo();
    }
}