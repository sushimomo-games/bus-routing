using System.Collections.Generic;

/// <summary>
/// A slice made with the slice tool while editing a bus line: the player clicked a
/// draft segment and it was deleted.
/// </summary>
/// <remarks>
/// Stores the draft segments before and after the slice, the same way <see cref="LineEdit"/> does.
/// Undo restores the before snapshot and redo restores the after snapshot, both through
/// <see cref="BusLineEditor.RestoreSegments"/>. It is a separate class so the edit history
/// logs show slices apart from drawing. Log it to <see cref="EditorState.DraftHistory"/>.
/// </remarks>
public partial class SliceEdit : LineEdit
{
    /// <param name="before">The draft segments before the slice, from <see cref="LineEdit.Snapshot"/>.</param>
    /// <param name="after">The draft segments after the slice, from <see cref="LineEdit.Snapshot"/>.</param>
    public SliceEdit(List<List<RoadNode>> before, List<List<RoadNode>> after)
        : base(before, after) { }
}
