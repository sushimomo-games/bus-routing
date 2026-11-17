using Godot;

public partial class UndoButton : TextureButton
{
    private void _on_pressed()
    {
        // If a line is being drawn or edited, undo its segments (pieces) only.
        // Else, undo bus stops and finished bus lines lines.
        if (BusLineEditor.BusLineInProgress != null)
        {
            GD.Print($"[DEBUG - Undo] Undo pressed (draft). CanUndo = {EditorState.DraftHistory.CanUndo}");
            EditorState.DraftHistory.Undo();
        }
        else
        {
            GD.Print($"[DEBUG - Undo] Undo pressed (main). CanUndo = {EditorState.History.CanUndo}");
            EditorState.History.Undo();
        }
    }

    public override void _Process(double delta)
    {
        // It chooses draft stack while drawing a line, main stack otherwise.
        var history = BusLineEditor.BusLineInProgress != null
            ? EditorState.DraftHistory
            : EditorState.History;

        Disabled = !history.CanUndo;
        Modulate = Disabled ? new Color(0.2f, 0.2f, 0.2f, 0.25f) : Colors.White;
    }
}