using Godot;

public partial class RedoButton : TextureButton
{
    /// <summary>
    /// Redoes the last undone edit. While a line is being drawn or edited, this redoes
    /// its segment changes. Otherwise it redoes bus stop and finished bus line changes.
    /// </summary>
    private void _on_pressed()
    {
        if (BusLineEditor.BusLineInProgress != null)
            EditorState.DraftHistory.Redo();
        else
            EditorState.History.Redo();
    }

    public override void _Process(double delta)
    {
        // It chooses draft stack while drawing a line, main stack otherwise.
        var history = BusLineEditor.BusLineInProgress != null
            ? EditorState.DraftHistory
            : EditorState.History;

        Disabled = !history.CanRedo;
        Modulate = Disabled ? new Color(0.2f, 0.2f, 0.2f, 0.25f) : Colors.White;
    }
}
