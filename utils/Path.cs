/// <summary>
/// Holds constant paths used throughout the project.
/// Using these constants avoids needing to change hardcoded path strings
/// in multiple files in the case the paths change.
/// </summary>
public static class Path
{
    // Scenes
    public const string BusStopScene = "res://bus-stop/bus-stop.tscn";
    public const string InfoPopupScene = "res://ui/info-popup/info-popup.tscn";
    public const string InfoWindowScene = "res://ui/info-window/info-window.tscn";
    public const string PreviewBusStopScene = "res://bus-stop/preview/preview-bus-stop.tscn";
    public const string RoadEdgeScene = "res://road/edge/road-edge.tscn";

    // Nodes
    public const string ErrorMessageNode = "CanvasLayer/LevelUI/ErrorMessage";
    public const string RouteListNode = "CanvasLayer/LevelUI/BusLinesContainer/RouteList";
}