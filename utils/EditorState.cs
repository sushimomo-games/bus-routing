using Godot;
using System.Collections.Generic;

/// <summary>
/// Represents all possible states that the route editor can be in.
/// </summary>
public enum EditorTool
{
    None,
    NewRoute
}

/// <summary>
/// Represents the steps involved in creating a new route.
/// </summary>
public enum RouteCreationStep
{
    NotCreating,
    AddingFirstStop,
    AddingSubsequentStops,
    BeginningEdit,
    ContinuingEdit
}

/// <summary>
/// Holds the current state of the route editor and routes.
/// </summary>
public partial class EditorState : Node
{
    private static RouteCreationStep? _currentRouteCreationStep = RouteCreationStep.NotCreating;
    public static RouteCreationStep? CurrentRouteCreationStep 
    { 
        get => _currentRouteCreationStep; 
        set => _currentRouteCreationStep = value; 
    }

    private static EditorTool _activeTool = EditorTool.None;
    public static EditorTool ActiveTool
    {
        get => _activeTool;
        set
        {
            _activeTool = value;
            if (_activeTool == EditorTool.NewRoute)
            {
                CurrentRouteCreationStep = RouteCreationStep.AddingFirstStop;
            }
            else
            {
                CurrentRouteCreationStep = null;
            }
        }
    }

    /// <summary>
    /// The route currently selected in the editor for inspection.
    /// </summary>
    public static Route SelectedRoute { get; set; }

    public static bool IsEditingFromStart { get; set; }

    /// <summary>
    /// Keeps track of all open windows, keyed by their ID. Prevents multiple
    /// of the same window from being opened.
    /// </summary>
    public static Dictionary<uint, InfoWindow> OpenWindows { get; } = new();
}