/// <summary>
/// A state object for a finalized bus line deleted from the bus line list.
/// </summary>
/// <remarks>
/// The inverse of <see cref="FinalizeBusLineEdit"/>: reverting restores the
/// detached bus line (see <see cref="BusLine.Detach"/>) at its original position
/// and takes its color back out of the pool.
/// </remarks>
public partial class DeleteBusLineEdit : FinalizeBusLineEdit
{
    public DeleteBusLineEdit(BusLine line, int index)
    {
        AddToGroup(line);
        _indices[line] = index;
    }

    /// <summary>
    /// Undoes the delete: puts the bus line back in the level and takes its color from the pool.
    /// </summary>
    public override void Revert() => Restore();

    /// <summary>
    /// Redoes the delete: detaches the bus line again and returns its color to the pool.
    /// </summary>
    public override void Apply() => Remove();
}
