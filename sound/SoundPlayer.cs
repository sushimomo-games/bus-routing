using Godot;

/// <summary>
/// Plays sound effects when the left mouse button is pressed or released on the parent
/// <see cref="Control"/>. Works on any Control, including ones that only act like buttons.
/// </summary>
public partial class SoundPlayer : Node
{
    /// <summary>
    /// The sound effects to play on mouse down and mouse up.
    /// If null, the default sounds from SoundManager will be used.
    /// </summary>
    [Export] public SoundEffectSet SoundSet { get; set; }

    public override void _Ready()
    {
        if (GetParent() is not Control control)
        {
            GD.PushWarning($"{nameof(SoundPlayer)} needs a Control parent: {GetParent()?.Name}");
            return;
        }

        control.GuiInput += OnGuiInput;
    }

    /// <summary>
    /// Plays the mouse down or mouse up sound when the left mouse button is pressed or
    /// released on the parent Control.
    /// </summary>
    /// <param name="event">The input event received by the parent Control.</param>
    private void OnGuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton { ButtonIndex: MouseButton.Left } mouseEvent)
            PlaySound(mouseEvent.Pressed);
    }

    /// <summary>
    /// Plays the mouse down or mouse up sound from the assigned set, falling back to the
    /// SoundManager defaults when no set is assigned.
    /// </summary>
    /// <param name="down">True to play the mouse down sound, false for mouse up.</param>
    private void PlaySound(bool down)
    {
        var set = SoundSet;
        SoundManager.Instance.Play(down ? set?.MouseDown : set?.MouseUp);
    }

    /// <summary>
    /// The number of bus stops that existed when the current drag began, or -1 if the
    /// current drag is not a bus stop drag.
    /// </summary>
    private int _busStopCountAtDragStart = -1;

    public override void _Notification(int what)
    {
        if (what == Control.NotificationDragBegin)
        {
            var isBusStopDrag = GetViewport().GuiGetDragData().AsString() == "BusStop";
            _busStopCountAtDragStart = isBusStopDrag ? LevelState.AllBusStops.Count : -1;
        }
        else if (what == Control.NotificationDragEnd)
        {
            if (_busStopCountAtDragStart < 0)
                return;

            var placed = LevelState.AllBusStops.Count > _busStopCountAtDragStart;
            SoundManager.Instance.Play(placed ? SoundSet?.DropSuccess : SoundSet?.DropFail);
            _busStopCountAtDragStart = -1;
        }
    }
}