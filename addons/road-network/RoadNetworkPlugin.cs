#if TOOLS
using Godot;
using System.Linq;

[Tool]
public partial class RoadNetworkPlugin : EditorPlugin
{
    private PackedScene _intersectionScene;
    private OptionButton _modeSelector;
    private Button _toggleBtn;
    private bool _roadModeActive = false;
    private RoadNode _connectionSource = null;
    private enum ToolMode { Place, Connect }

    public override bool _Handles(GodotObject @object) => _roadModeActive;
    public override void _EnterTree()
    {
        _intersectionScene = GD.Load<PackedScene>("res://road/intersection/intersection-node.tscn");
        var container = new HBoxContainer();

        _toggleBtn = new Button { Text = "Road Tool", ToggleMode = true };
        _toggleBtn.Toggled += OnToolToggled;

        _modeSelector = new OptionButton();
        _modeSelector.AddItem("Mode: Place Nodes", (int)ToolMode.Place);
        _modeSelector.AddItem("Mode: Connect Nodes", (int)ToolMode.Connect);

        container.AddChild(_toggleBtn);
        container.AddChild(_modeSelector);

        AddControlToContainer(CustomControlContainer.CanvasEditorMenu, container);
    }
    public override void _ExitTree()
    {
        if (_toggleBtn != null)
        {
            _toggleBtn.Toggled -= OnToolToggled;
        }
        _modeSelector?.GetParent()?.QueueFree();
    }
    private void OnToolToggled(bool toggled)
    {
        _roadModeActive = toggled;
        _connectionSource = null;
        GD.Print(_roadModeActive ? "Road Mode: ON" : "Road Mode: OFF");
    }
    public override bool _ForwardCanvasGuiInput(InputEvent @event)
    {
        if (!_roadModeActive) return false;

        if (@event is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left && mb.CtrlPressed)
        {

            var sceneRoot = EditorInterface.Singleton.GetEditedSceneRoot();
            if (sceneRoot == null) return false;

            Vector2 worldPos = EditorInterface.Singleton.GetEditorViewport2D().GetFinalTransform().AffineInverse() * mb.Position;
            var clickedNode = FindNodeAtPosition(sceneRoot, worldPos);
            if (_modeSelector.Selected == (int)ToolMode.Place)
            {
                if (clickedNode == null) CreateNode(sceneRoot, worldPos);
            }
            else
            {
                if (clickedNode != null) HandleConnection(clickedNode);
            }

            return true;
        }
        return false;
    }

    private void CreateNode(Node sceneRoot, Vector2 worldPos)
    {
        Node container = sceneRoot.FindChild("IntersectionNodes", true, false);

        if (container == null)
        {
            container = new Node { Name = "IntersectionNodes" };
            sceneRoot.AddChild(container);
            container.Owner = sceneRoot;
            GD.Print("Created 'IntersectionNodes' container.");
        }

        if (_intersectionScene == null)
        {
            GD.PrintErr("CRITICAL: Intersection.tscn could not be loaded at res://Intersection.tscn");
            return;
        }

        var newNode = _intersectionScene.Instantiate<IntersectionNode>();

        container.AddChild(newNode);
        newNode.Owner = sceneRoot;
        newNode.GlobalPosition = worldPos;

        GD.Print($"Placed new Node under {container.Name}.");
    }

    private void HandleConnection(RoadNode clickedNode)
    {
        if (_connectionSource == null)
        {
            _connectionSource = clickedNode;
            GD.Print($"Selected Start: {clickedNode.Name}. Ctrl+Click another to Toggle Connection.");
        }
        else if (_connectionSource == clickedNode)
        {
            _connectionSource = null;
            GD.Print("Selection cleared.");
        }
        else
        {
            if (_connectionSource.Neighbors.Contains(clickedNode))
            {
                _connectionSource.Neighbors.Remove(clickedNode);
                clickedNode.Neighbors.Remove(_connectionSource);
                GD.Print($"Removed connection between {_connectionSource.Name} and {clickedNode.Name}");
            }
            else
            {
                _connectionSource.Neighbors.Add(clickedNode);
                clickedNode.Neighbors.Add(_connectionSource);
                GD.Print($"Connected {_connectionSource.Name} and {clickedNode.Name}");
            }

            _connectionSource.QueueRedraw();
            clickedNode.QueueRedraw();

            _connectionSource = null;
        }
    }

    private RoadNode FindNodeAtPosition(Node root, Vector2 pos)
    {
        return root.FindChildren("*", "Node2D", true, false)
            .OfType<RoadNode>()
            .FirstOrDefault(n => n.GlobalPosition.DistanceTo(pos) < 20f);
    }
}
#endif