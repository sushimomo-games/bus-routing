using Godot;

/// <summary>
/// An abstract base class for all edit operations made via the bus line editor tool.
/// </summary>
public abstract partial class AbstractEdit : Node
{
    /// <summary>
    /// Gets the name of the edit operation.
    /// </summary>
    /// <remarks>
    /// This property is used for debugging purposes and is not intended to be modified by the user.
    /// Name is derived from the class name of the implementing class at runtime. 
    /// For example, if a class named "StopEdit" implements AbstractEdit, the Name property will return "StopEdit".
    /// </remarks>
    private string Name { 
        get{
            return GetType().Name;
        } 
    }
    /// <summary>
    /// Reverts the last player action in the undo history.
    /// </summary>
    public abstract void Revert();
    /// <summary>
    /// Applies the player action asociated with the instance.
    /// </summary>
    /// <remarks>
    /// This method is called when the player wants to redo an action that was previously undone.
    /// </remarks>
    
    
    // public abstract void Apply();
}
