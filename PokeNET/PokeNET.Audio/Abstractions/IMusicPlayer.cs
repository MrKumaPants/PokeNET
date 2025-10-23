using PokeNET.Audio.Models;

namespace PokeNET.Audio.Abstractions;

/// <summary>
/// Specialized interface for background music playback with looping and crossfading.
/// SOLID PRINCIPLE: Single Responsibility - Handles only music-specific features.
/// SOLID PRINCIPLE: Interface Segregation - Separate from sound effects to avoid bloat.
/// </summary>
/// <remarks>
/// Music playback differs from sound effects in key ways:
/// - Supports looping and crossfading between tracks
/// - Manages longer-duration audio streams
/// - Handles track transitions smoothly
/// - Integrates with procedural music generation
/// </remarks>
public interface IMusicPlayer : IAudioService
{
    /// <summary>
    /// Gets the currently loaded music track.
    /// </summary>
    AudioTrack? CurrentTrack { get; }

    /// <summary>
    /// Gets the next track queued for playback.
    /// </summary>
    AudioTrack? NextTrack { get; }

    /// <summary>
    /// Gets the current music state.
    /// </summary>
    MusicState MusicState { get; }

    /// <summary>
    /// Gets or sets a value indicating whether the current track should loop.
    /// </summary>
    bool IsLooping { get; set; }

    /// <summary>
    /// Gets or sets the crossfade duration for track transitions.
    /// </summary>
    TimeSpan CrossfadeDuration { get; set; }

    /// <summary>
    /// Loads a music track without playing it.
    /// </summary>
    /// <param name="track">The track to load.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>A task representing the load operation.</returns>
    Task LoadAsync(AudioTrack track, CancellationToken cancellationToken = default);

    /// <summary>
    /// Transitions to a new track with optional crossfading.
    /// </summary>
    /// <param name="track">The new track to play.</param>
    /// <param name="useCrossfade">Whether to use crossfading for the transition.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>A task representing the transition operation.</returns>
    Task TransitionToAsync(AudioTrack track, bool useCrossfade = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fades out the current track and stops playback.
    /// </summary>
    /// <param name="duration">Duration of the fade-out effect.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>A task representing the fade operation.</returns>
    Task FadeOutAsync(TimeSpan duration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fades in the current track from silence.
    /// </summary>
    /// <param name="duration">Duration of the fade-in effect.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>A task representing the fade operation.</returns>
    Task FadeInAsync(TimeSpan duration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the volume level for music playback.
    /// </summary>
    /// <param name="volume">Volume level between 0.0 (silent) and 1.0 (full volume).</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when volume is not in valid range.</exception>
    void SetVolume(float volume);

    /// <summary>
    /// Gets the current volume level.
    /// </summary>
    /// <returns>Volume level between 0.0 and 1.0.</returns>
    float GetVolume();

    /// <summary>
    /// Event raised when a track finishes playing.
    /// </summary>
    event EventHandler<TrackCompletedEventArgs>? TrackCompleted;

    /// <summary>
    /// Event raised when a track transition begins.
    /// </summary>
    event EventHandler<TrackTransitionEventArgs>? TrackTransitioning;
}

/// <summary>
/// Event arguments for track completion.
/// </summary>
public class TrackCompletedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the track that completed.
    /// </summary>
    public AudioTrack Track { get; init; } = null!;

    /// <summary>
    /// Gets a value indicating whether the track will loop.
    /// </summary>
    public bool WillLoop { get; init; }

    /// <summary>
    /// Gets the timestamp when the track completed.
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Event arguments for track transitions.
/// </summary>
public class TrackTransitionEventArgs : EventArgs
{
    /// <summary>
    /// Gets the track being transitioned from.
    /// </summary>
    public AudioTrack? FromTrack { get; init; }

    /// <summary>
    /// Gets the track being transitioned to.
    /// </summary>
    public AudioTrack ToTrack { get; init; } = null!;

    /// <summary>
    /// Gets a value indicating whether crossfade is being used.
    /// </summary>
    public bool IsCrossfading { get; init; }

    /// <summary>
    /// Gets the transition duration.
    /// </summary>
    public TimeSpan Duration { get; init; }

    /// <summary>
    /// Gets the timestamp when the transition started.
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
