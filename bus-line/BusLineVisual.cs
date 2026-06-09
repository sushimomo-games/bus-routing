using Godot;
using System.Collections.Generic;
using System.Linq;

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
                float offset = CalculateSegmentOffsetAmount(path[0], path[1]);
                _line.AddPoint(currentPos + perp * offset);
            }
            else if (i == path.Count - 1)
            {
                // Last point: offset based on last segment only
                Vector2 dir = (currentPos - path[i - 1].GlobalPosition).Normalized();
                Vector2 perp = new Vector2(-dir.Y, dir.X);
                float offset = CalculateSegmentOffsetAmount(path[i - 1], path[i]);
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
        
        float offsetBefore = CalculateSegmentOffsetAmount(path[nodeIndex - 1], path[nodeIndex]);
        float offsetAfter = CalculateSegmentOffsetAmount(path[nodeIndex], path[nodeIndex + 1]);

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
    /// Calculates the intersection point of two offset line segments.
    /// Used to create clean continuous corners when offset lines change direction.
    /// </summary>
    private Vector2 CalculateIntersection(Vector2 p1, Vector2 dir1, float offset1, Vector2 p2, Vector2 dir2, float offset2, Vector2 currentPos)
    {
        Vector2 perp1 = new Vector2(-dir1.Y, dir1.X);
        Vector2 perp2 = new Vector2(-dir2.Y, dir2.X);

        Vector2 line1Start = p1 + perp1 * offset1;
        Vector2 line2Start = p2 + perp2 * offset2;

        float det = dir1.Cross(dir2);

        // If lines are nearly parallel, fallback
        if (Mathf.Abs(det) < 0.001f)
        {
            return currentPos + perp1 * offset1;
        }

        Vector2 diff = line2Start - line1Start;
        float t = diff.Cross(dir2) / det;
        
        Vector2 intersection = line1Start + dir1 * t;

        // Clamp extreme corners
        if (intersection.DistanceTo(currentPos) > 30.0f)
        {
             // Fallback to non-intersecting point avoiding extreme spikes
             return currentPos + perp1 * offset1;
        }

        return intersection;
    }

    /// <summary>
    /// Determines the canonical direction of a segment between two nodes.
    /// This ensures we always calculate offsets based on a consistent direction,
    /// regardless of which way the bus line travels the segment.
    /// </summary>
    private bool IsCanonicalDirection(RoadNode nodeA, RoadNode nodeB)
    {
        // First compare X, then Y as tie-breaker
        if (Mathf.Abs(nodeA.GlobalPosition.X - nodeB.GlobalPosition.X) > 0.001f)
        {
            return nodeA.GlobalPosition.X < nodeB.GlobalPosition.X;
        }
        return nodeA.GlobalPosition.Y < nodeB.GlobalPosition.Y;
    }

    /// <summary>
    /// Calculates just the offset amount (scalar) for a specific segment.
    /// </summary>
    private float CalculateSegmentOffsetAmount(RoadNode nodeA, RoadNode nodeB)
    {
        var busLinesOnSegment = GetBusLinesOnSegment(nodeA, nodeB);
        
        if (busLinesOnSegment.Count <= 1)
        {
            return 0f;
        }

        int slotIndex = busLinesOnSegment.IndexOf(_busLine);
        if (slotIndex < 0) return 0f;

        float totalWidth = (busLinesOnSegment.Count - 1) * LineSpacing;
        float baseOffset = -totalWidth / 2.0f + (slotIndex * LineSpacing);

        bool isCanonical = IsCanonicalDirection(nodeA, nodeB);
        return isCanonical ? baseOffset : -baseOffset;
    }

    /// <summary>
    /// Gets all busLines that pass through a given segment, sorted by busLine ID
    /// for consistent slot assignment.
    /// </summary>
    private List<BusLine> GetBusLinesOnSegment(RoadNode nodeA, RoadNode nodeB)
    {
        var busLines = new List<BusLine>();
        
        foreach (var busLine in LevelState.AllBusLines)
        {
            if (BusLineContainsSegment(busLine, nodeA, nodeB))
            {
                busLines.Add(busLine);
            }
        }

        // Sort by busLine ID to ensure consistent slot assignment
        busLines = busLines.OrderBy(r => r.ID).ToList();
        return busLines;
    }

    /// <summary>
    /// Checks if a busLine contains a segment between two nodes (in either direction).
    /// </summary>
    private bool BusLineContainsSegment(BusLine busLine, RoadNode nodeA, RoadNode nodeB)
    {
        var path = busLine.Path;
        for (int i = 0; i < path.Count - 1; i++)
        {
            if ((path[i] == nodeA && path[i + 1] == nodeB) ||
                (path[i] == nodeB && path[i + 1] == nodeA))
            {
                return true;
            }
        }
        return false;
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
        float offset = CalculateSegmentOffsetAmount(nodeA, nodeB);

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
                float offset = CalculateSegmentOffsetAmount(pathNodes[0], pathNodes[1]);
                _highlightLine.AddPoint(currentPos + perp * offset);
            }
            else if (i == pathNodes.Count - 1)
            {
                Vector2 dir = (currentPos - pathNodes[i - 1].GlobalPosition).Normalized();
                Vector2 perp = new Vector2(-dir.Y, dir.X);
                float offset = CalculateSegmentOffsetAmount(pathNodes[i - 1], pathNodes[i]);
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
        
        float offsetBefore = CalculateSegmentOffsetAmount(path[nodeIndex - 1], path[nodeIndex]);
        float offsetAfter = CalculateSegmentOffsetAmount(path[nodeIndex], path[nodeIndex + 1]);

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