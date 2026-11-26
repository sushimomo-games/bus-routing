using Godot;
using System.Collections.Generic;
using static Godot.Node.ProcessModeEnum;

/// <summary>
/// Singleton (autoload) that manages sound effects and music in the game.
/// Allows for multiple sound effects to be played simultaneously without
/// cutting each other off.
/// </summary>
public partial class SoundManager : Node
{
    public static SoundManager Instance { get; private set; }

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
        for (int i = 0; i < PoolSize; i++)
        {
            var player = new AudioStreamPlayer
            {
                Bus = "UI", // refers to an audio bus, not city bus lmao
                ProcessMode = Always, // keeps working while the game is paused
            };
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