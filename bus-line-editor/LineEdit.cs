using Godot;
using System.Collections.Generic;

/// <summary>
/// Represents an edit operation that handles the addition or removal of segments in a bus line.
/// </summary>
/// <remarks>
/// This class is used to manage the state of a bus line before and after an edit operation
/// is performed. It allows for the reversion of changes made to the bus line through draft segments,
/// enabling undo functionality in the bus line editor.
/// </remarks>
public partial class LineEdit : AbstractEdit
{
    /// <summary>
    /// Before snapshot of saved state of the bus line segments before the user's edit operation.
    /// </summary>
    private List<DraftSegment> before;
    /// <summary>
    /// After snapshot of saved state of the bus line segments after the user's edit operation.
    /// </summary>
    private List<DraftSegment> after;

    public LineEdit(List<DraftSegment> before, List<DraftSegment> after)
    {
        this.before = before;
        this.after = after;
    }
    /// <summary>
    /// Reverts the last player action in the undo history by restoring the bus line segments to their state before the edit operation.
    /// </summary>
    /// <remarks>
    /// Updates the global DraftLineSegments to the state captured in the 'before' snapshot, effectively undoing the changes made by the user.
    /// </remarks>
    public override void Revert()
    {
       BusLineEditor.DraftLineSegments = before;
    }
}
