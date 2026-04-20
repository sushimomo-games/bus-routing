#if TOOLS
using Godot;
using System.Collections.Generic;
using System.Linq;

[Tool]
public partial class MapEditorPlugin : EditorPlugin
{
    private Dictionary<ToolMode, PackedScene> _scenes = new();
    private OptionButton _modeSelector;
    private Button _toggleBtn;
    private bool _toolActive = false;
    private RoadNode _connectionSource = null;
    private enum ToolMode { PlaceRoad, ConnectRoad, PlaceHouse, PlaceDestination }

    public override bool _Handles(GodotObject @object) => _toolActive;
    public override void _EnterTree()
    {
        _scenes[ToolMode.PlaceRoad] = GD.Load<PackedScene>("res://road/intersection/intersection-node.tscn");
        _scenes[ToolMode.PlaceHouse] = GD.Load<PackedScene>("res://buildings/house/house.tscn");
        _scenes[ToolMode.PlaceDestination] = GD.Load<PackedScene>("res://buildings/destination/destination.tscn");

        var container = new HBoxContainer();
        _toggleBtn = new Button { Text = "Road Tool", ToggleMode = true };
        _toggleBtn.Toggled += OnToolToggled;

        _modeSelector = new OptionButton();
        _modeSelector.AddItem("Road: Place Nodes", (int)ToolMode.PlaceRoad);
        _modeSelector.AddItem("Road: Connect Nodes", (int)ToolMode.ConnectRoad);
        _modeSelector.AddItem("Building: Place House", (int)ToolMode.PlaceHouse);
        _modeSelector.AddItem("Building: Place Destination", (int)ToolMode.PlaceDestination);

        _modeSelector.Visible = false;

        container.AddChild(_toggleBtn);
        container.AddChild(_modeSelector);

        AddControlToContainer(CustomControlContainer.CanvasEditorMenu, container);
    }
    public override void _ExitTree()
    {
        if (IsInstanceValid(_toggleBtn))
        {
            if (_toggleBtn.IsConnected(Button.SignalName.Toggled, Callable.From<bool>(OnToolToggled)))
            {
                _toggleBtn.Toggled -= OnToolToggled;
            }
        }

        var parent = _modeSelector?.GetParent();
        if (IsInstanceValid(parent))
        {
            parent.QueueFree();
        }
    }
    private void OnToolToggled(bool toggled)
    {
        _toolActive = toggled;
        _connectionSource = null;
        if (_modeSelector != null)
        {
            _modeSelector.Visible = toggled;
        }
        GD.Print(_toolActive ? "Road Mode: ON" : "Road Mode: OFF");
    }

    public override bool _ForwardCanvasGuiInput(InputEvent @event)
    {
        if (!_toolActive) return false;
        if (@event is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left && mb.CtrlPressed)
        {
            var sceneRoot = EditorInterface.Singleton.GetEditedSceneRoot();
            if (sceneRoot == null) return false;
            Vector2 worldPos = EditorInterface.Singleton.GetEditorViewport2D().GetFinalTransform().AffineInverse() * mb.Position;

            ToolMode currentMode = (ToolMode)_modeSelector.Selected;

            if (currentMode == ToolMode.ConnectRoad)
            {
                HandleConnection(sceneRoot, worldPos);
            }
            else
            {
                PlaceScene(sceneRoot, worldPos, currentMode);
            }
            return true;
        }
        return false;
    }

    private void PlaceScene(Node sceneRoot, Vector2 worldPos, ToolMode mode)
    {
        if (!_scenes.ContainsKey(mode) || _scenes[mode] == null) return;
        string containerName = (mode == ToolMode.PlaceRoad) ? "IntersectionNodes" : "Buildings";
        Node container = sceneRoot.FindChild(containerName, true, false);

        if (container == null)
        {
            container = new Node2D { Name = containerName };
            sceneRoot.AddChild(container);
            container.Owner = sceneRoot;
            GD.Print($"Created {containerName} container.");
        }

        var instance = _scenes[mode].Instantiate<Node2D>();
        container.AddChild(instance);
        instance.Owner = sceneRoot;
        instance.GlobalPosition = worldPos;

        GD.Print($"Placed {mode} at {worldPos}");
    }

    private void HandleConnection(Node sceneRoot, Vector2 worldPos)
    {
        var clickedNode = FindNodeAtPosition(sceneRoot, worldPos);
        if (clickedNode == null) return;
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