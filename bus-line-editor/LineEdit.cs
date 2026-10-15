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
    
    public override void Revert()
    {
       
    }
}
