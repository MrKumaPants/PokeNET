using PokeNET.Audio.Abstractions;

namespace PokeNET.Audio.Models;

/// <summary>
/// Represents the current state of the music player.
/// SOLID PRINCIPLE: Single Responsibility - Encapsulates music playback state.
/// </summary>
public class MusicState
{
    /// <summary>
    /// Gets or sets the current track being played.
    /// </summary>
    public AudioTrack? CurrentTrack { get; set; }

    /// <summary>
    /// Gets or sets the next track in the queue.
    /// </summary>
    public AudioTrack? NextTrack { get; set; }

    /// <summary>
    /// Gets or sets the current playback state.
    /// </summary>
    public PlaybackState State { get; set; }

    /// <summary>
    /// Gets or sets the current playback position.
    /// </summary>
    public TimeSpan Position { get; set; }

    /// <summary>
    /// Gets or sets the current volume level (0.0 to 1.0).
    /// </summary>
    public float Volume { get; set; } = 1.0f;

    /// <summary>
    /// Gets or sets a value indicating whether the current track is looping.
    /// </summary>
    public bool IsLooping { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether music is muted.
    /// </summary>
    public bool IsMuted { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether a transition is in progress.
    /// </summary>
    public bool IsTransitioning { get; set; }

    /// <summary>
    /// Gets or sets the transition progress (0.0 to 1.0) if transitioning.
    /// </summary>
    public float TransitionProgress { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the current state was updated.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the playback speed multiplier (1.0 = normal speed).
    /// </summary>
    public float PlaybackSpeed { get; set; } = 1.0f;

    /// <summary>
    /// Gets or sets the current tempo in BPM (for procedural music).
    /// </summary>
    public int? Tempo { get; set; }

    /// <summary>
    /// Gets or sets the current energy level (for adaptive music).
    /// </summary>
    public float? Energy { get; set; }

    /// <summary>
    /// Gets or sets the current mood (for adaptive music).
    /// </summary>
    public string? Mood { get; set; }

    /// <summary>
    /// Gets or sets custom state data.
    /// </summary>
    public Dictionary<string, object> CustomData { get; set; } = new();

    /// <summary>
    /// Creates a snapshot copy of the current music state.
    /// </summary>
    /// <returns>A new MusicState instance with copied values.</returns>
    public MusicState Snapshot()
    {
        return new MusicState
        {
            CurrentTrack = CurrentTrack?.Clone(),
            NextTrack = NextTrack?.Clone(),
            State = State,
            Position = Position,
            Volume = Volume,
            IsLooping = IsLooping,
            IsMuted = IsMuted,
            IsTransitioning = IsTransitioning,
            TransitionProgress = TransitionProgress,
            Timestamp = DateTime.UtcNow,
            PlaybackSpeed = PlaybackSpeed,
            Tempo = Tempo,
            Energy = Energy,
            Mood = Mood,
            CustomData = new Dictionary<string, object>(CustomData)
        };
    }

    /// <summary>
    /// Returns a string representation of the music state.
    /// </summary>
    /// <returns>String describing the current state.</returns>
    public override string ToString()
    {
        var trackInfo = CurrentTrack?.ToString() ?? "No track";
        return $"{State}: {trackInfo} at {Position:mm\\:ss} (Volume: {Volume:P0})";
    }
}
