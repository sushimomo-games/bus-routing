using Godot;
using System.Collections.Generic;
using System.Linq;
using static PathGeometry;

/// <summary>
/// Manages the visual representation of a busLine using a Line2D node.
/// Handles offset calculations to prevent overlapping when multiple busLines
/// share the same road segments.
/// </summary>
public partial class BusLineVisual : Node2D
{
    private BusLine _busLine;
    private Line2D _line;
    private Line2D _highlightLine;
    private Tween _highlightTween;
    
    /// <summary>
    /// The width of the busLine line.
    /// </summary>
    public float LineWidth { get; set; } = 8.0f;

    /// <summary>
    /// The spacing between parallel busLine lines when they share a segment.
    /// </summary>
    public float LineSpacing { get; set; } = 8.0f;

    public BusLineVisual(BusLine busLine)
    {
        _busLine = busLine;
        _line = new Line2D
        {
            Width = LineWidth,
            DefaultColor = busLine.Color
        };
        AddChild(_line);

        _highlightLine = new Line2D
        {
            Width = LineWidth + 4.0f,
            DefaultColor = busLine.Color.Lightened(0.4f),
            ZIndex = 0,
            Modulate = new Color(1, 1, 1, 0),
            BeginCapMode = Line2D.LineCapMode.Round,
            EndCapMode = Line2D.LineCapMode.Round
        };
        AddChild(_highlightLine);
    }

    public override void _Ready()
    {
        UpdateVisual();
    }

    /// <summary>
    /// Rebuilds the entire visual from the busLine's current path, applying
    /// offsets to prevent overlapping with other busLines on shared segments.
    /// </summary>
    public void UpdateVisual()
    {
        _line.ClearPoints();
        
        var path = _busLine.Path;
        if (path.Count == 0) return;
        if (path.Count == 1)
        {
            _line.AddPoint(path[0].GlobalPosition);
            return;
        }

        // For each segment, calculate the offset and add points
        for (int i = 0; i < path.Count; i++)
        {
            Vector2 currentPos = path[i].GlobalPosition;
            
            if (i == 0)
            {
                // First point: offset based on first segment only
                Vector2 dir = (path[1].GlobalPosition - currentPos).Normalized();
                Vector2 perp = new Vector2(-dir.Y, dir.X);
                float offset = LevelState.CalculateSegmentOffsetAmount(_busLine, path[0], path[1], LineSpacing);
                _line.AddPoint(currentPos + perp * offset);
            }
            else if (i == path.Count - 1)
            {
                // Last point: offset based on last segment only
                Vector2 dir = (currentPos - path[i - 1].GlobalPosition).Normalized();
                Vector2 perp = new Vector2(-dir.Y, dir.X);
                float offset = LevelState.CalculateSegmentOffsetAmount(_busLine, path[i - 1], path[i], LineSpacing);
                _line.AddPoint(currentPos + perp * offset);
            }
            else
            {
                // Middle point: may need two points if offsets differ
                AddPointsAtIntersection(i);
            }
        }
    }

    /// <summary>
    /// Adds points at an intersection node. If the offset changes between
    /// incoming and outgoing segments, adds two points to create a clean transition.
    /// </summary>
    private void AddPointsAtIntersection(int nodeIndex)
    {
        var path = _busLine.Path;
        Vector2 currentPos = path[nodeIndex].GlobalPosition;
        
        Vector2 prevPos = path[nodeIndex - 1].GlobalPosition;
        Vector2 nextPos = path[nodeIndex + 1].GlobalPosition;
        
        Vector2 dirBefore = (currentPos - prevPos).Normalized();
        Vector2 dirAfter = (nextPos - currentPos).Normalized();
        
        Vector2 perpBefore = new Vector2(-dirBefore.Y, dirBefore.X);
        Vector2 perpAfter = new Vector2(-dirAfter.Y, dirAfter.X);
        
        float offsetBefore = LevelState.CalculateSegmentOffsetAmount(_busLine, path[nodeIndex - 1], path[nodeIndex], LineSpacing);
        float offsetAfter = LevelState.CalculateSegmentOffsetAmount(_busLine, path[nodeIndex], path[nodeIndex + 1], LineSpacing);

        bool isCollinear = Mathf.Abs(dirBefore.Cross(dirAfter)) < 0.001f;
        
        bool sameOffset = Mathf.Abs(offsetBefore - offsetAfter) < 0.001f;

        if (isCollinear)
        {
            if (sameOffset)
            {
                // Straight line with same offset - single point
                _line.AddPoint(currentPos + perpBefore * offsetBefore);
            }
            else
            {
                // Different offsets - add two points at the intersection
                _line.AddPoint(currentPos + perpBefore * offsetBefore);
                _line.AddPoint(currentPos + perpAfter * offsetAfter);
            }
        }
        else
        {
            // Corner - use line intersection
            Vector2 intersection = CalculateIntersection(prevPos, dirBefore, offsetBefore, currentPos, dirAfter, offsetAfter, currentPos);
            _line.AddPoint(intersection);
        }
    }

    /// <summary>
    /// Adds a point to the end of the visual line.
    /// Note: For proper offset handling, prefer calling UpdateVisual() after
    /// modifying the busLine path.
    /// </summary>
    public void AppendPoint(Vector2 position)
    {
        // When appending, we need to recalculate to handle offsets properly
        UpdateVisual();
    }

    /// <summary>
    /// Adds a point to the beginning of the visual line.
    /// Note: For proper offset handling, prefer calling UpdateVisual() after
    /// modifying the busLine path.
    /// </summary>
    public void PrependPoint(Vector2 position)
    {
        // When prepending, we need to recalculate to handle offsets properly
        UpdateVisual();
    }

    /// <summary>
    /// Clears all points from the visual line.
    /// </summary>
    public void ClearPoints()
    {
        _line.ClearPoints();
    }

    /// <summary>
    /// Gets the Line2D node for direct manipulation if needed.
    /// </summary>
    public Line2D GetLine2D()
    {
        return _line;
    }

    /// <summary>
    /// Highlights a specific segment of the bus line with a glow overlay.
    /// </summary>
    public void HighlightSegment(RoadNode nodeA, RoadNode nodeB)
    {
        _highlightLine.ClearPoints();

        Vector2 posA = nodeA.GlobalPosition;
        Vector2 posB = nodeB.GlobalPosition;

        Vector2 dir = (posB - posA).Normalized();
        Vector2 perp = new Vector2(-dir.Y, dir.X);
        float offset = LevelState.CalculateSegmentOffsetAmount(_busLine, nodeA, nodeB, LineSpacing);

        _highlightLine.AddPoint(posA + perp * offset);
        _highlightLine.AddPoint(posB + perp * offset);

        _highlightTween?.Kill();
        _highlightTween = CreateTween();
        // Fade in alpha
        _highlightTween.TweenProperty(_highlightLine, "modulate", new Color(1, 1, 1, 1), 0.2f)
                       .SetTrans(Tween.TransitionType.Sine)
                       .SetEase(Tween.EaseType.Out);
    }

    /// <summary>
    /// Highlights a sequence of nodes on the bus line.
    /// </summary>
    public void HighlightPath(List<RoadNode> pathNodes)
    {
        if (pathNodes == null || pathNodes.Count < 2) return;

        _highlightLine.ClearPoints();

        for (int i = 0; i < pathNodes.Count; i++)
        {
            Vector2 currentPos = pathNodes[i].GlobalPosition;
            
            if (i == 0)
            {
                Vector2 dir = (pathNodes[1].GlobalPosition - currentPos).Normalized();
                Vector2 perp = new Vector2(-dir.Y, dir.X);
                float offset = LevelState.CalculateSegmentOffsetAmount(_busLine, pathNodes[0], pathNodes[1], LineSpacing);
                _highlightLine.AddPoint(currentPos + perp * offset);
            }
            else if (i == pathNodes.Count - 1)
            {
                Vector2 dir = (currentPos - pathNodes[i - 1].GlobalPosition).Normalized();
                Vector2 perp = new Vector2(-dir.Y, dir.X);
                float offset = LevelState.CalculateSegmentOffsetAmount(_busLine, pathNodes[i - 1], pathNodes[i], LineSpacing);
                _highlightLine.AddPoint(currentPos + perp * offset);
            }
            else
            {
                AddPointsAtIntersectionForHighlight(i, pathNodes);
            }
        }

        _highlightTween?.Kill();
        _highlightTween = CreateTween();
        _highlightTween.TweenProperty(_highlightLine, "modulate", new Color(1, 1, 1, 1), 0.2f)
                       .SetTrans(Tween.TransitionType.Sine)
                       .SetEase(Tween.EaseType.Out);
    }

    private void AddPointsAtIntersectionForHighlight(int nodeIndex, List<RoadNode> path)
    {
        Vector2 currentPos = path[nodeIndex].GlobalPosition;
    
        Vector2 prevPos = path[nodeIndex - 1].GlobalPosition;
        Vector2 nextPos = path[nodeIndex + 1].GlobalPosition;
        
        Vector2 dirBefore = (currentPos - prevPos).Normalized();
        Vector2 dirAfter = (nextPos - currentPos).Normalized();
        
        Vector2 perpBefore = new Vector2(-dirBefore.Y, dirBefore.X);
        Vector2 perpAfter = new Vector2(-dirAfter.Y, dirAfter.X);
        
        float offsetBefore = LevelState.CalculateSegmentOffsetAmount(_busLine, path[nodeIndex - 1], path[nodeIndex], LineSpacing);
        float offsetAfter = LevelState.CalculateSegmentOffsetAmount(_busLine, path[nodeIndex], path[nodeIndex + 1], LineSpacing);

        bool isCollinear = Mathf.Abs(dirBefore.Cross(dirAfter)) < 0.001f;
        bool sameOffset = Mathf.Abs(offsetBefore - offsetAfter) < 0.001f;

        if (isCollinear)
        {
            if (sameOffset)
            {
                _highlightLine.AddPoint(currentPos + perpBefore * offsetBefore);
            }
            else
            {
                _highlightLine.AddPoint(currentPos + perpBefore * offsetBefore);
                _highlightLine.AddPoint(currentPos + perpAfter * offsetAfter);
            }
        }
        else
        {
            Vector2 intersection = CalculateIntersection(prevPos, dirBefore, offsetBefore, currentPos, dirAfter, offsetAfter, currentPos);
            _highlightLine.AddPoint(intersection);
        }
    }

    /// <summary>
    /// Fades out the currently glowing highlight overlay segment.
    /// </summary>
    public void ClearHighlight()
    {
        if (_highlightLine.Points.Length == 0) return;

        _highlightTween?.Kill();
        _highlightTween = CreateTween();
        _highlightTween.TweenProperty(_highlightLine, "modulate", new Color(1, 1, 1, 0), 0.2f)
                       .SetTrans(Tween.TransitionType.Sine)
                       .SetEase(Tween.EaseType.Out);
        _highlightTween.TweenCallback(Callable.From(_highlightLine.ClearPoints));
    }
}