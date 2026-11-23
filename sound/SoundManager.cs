using Godot;
using System.Collections.Generic;

/// <summary>
/// Singleton (autoload) that manages sound effects and music in the game.
/// </summary>
public partial class SoundManager : Node
{
    public static SoundManager Instance { get; private set; }

    /// <summary>
    /// The default sound effects to use for UI interactions, such as button presses.
    /// </summary>
    [Export]
    public SoundEffectSet DefaultSounds = new SoundEffectSet
    {
        MouseDown = GD.Load<AudioStream>("res://assets/audio/mouse-down.wav"),
        MouseUp = GD.Load<AudioStream>("res://assets/audio/mouse-up.wav")
    };

    /// <summary>
    /// The name of the audio bus that this sound manager will use to play sounds.
    /// </summary>
    [Export] public StringName BusName { get; set; } = "UI"; // falls back to Master if missing

    /// <summary>
    /// The number of audio players to keep in the pool. This allows for multiple
    /// sound effects to be played simultaneously without cutting each other off.
    /// </summary>    
    private const int PoolSize = 8;

    /// <summary>
    /// The pool of audio players that are used to play sound effects.
    /// Each player is reused round-robin to allow for multiple sounds to be played at once.
    /// </summary>
    private readonly List<AudioStreamPlayer> _pool = new();

    /// <summary>
    /// The index of the next audio player in the pool to use for playing a sound effect.
    /// </summary>
    private int _next;

    public override void _Ready()
    {
        Instance = this;
        ProcessMode = ProcessModeEnum.Always; // keeps working while the game is paused
        for (int i = 0; i < PoolSize; i++)
        {
            var player = new AudioStreamPlayer { Bus = BusName };
            AddChild(player);
            _pool.Add(player);
        }
    }

    /// <summary>
    /// Plays the given audio stream using one of the pooled audio players.
    /// </summary>
    /// <param name="stream"></param>
    public void Play(AudioStream stream)
    {
        if (stream == null) return;
        var player = _pool[_next];
        _next = (_next + 1) % PoolSize;
        player.Stream = stream;
        player.Play();
    }
}