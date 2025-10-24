using PokeNET.Audio.Abstractions;
using PokeNET.Audio.Models;

namespace PokeNET.Audio.Services.Music;

/// <summary>
/// Handles music track transitions including crossfades and smooth transitions.
/// Coordinates transitions between tracks with configurable effects.
/// </summary>
public interface IMusicTransitionHandler
{
    /// <summary>
    /// Event raised when a track transition begins.
    /// </summary>
    event EventHandler<TrackTransitionEventArgs>? TrackTransitioning;

    /// <summary>
    /// Gets or sets the crossfade duration.
    /// </summary>
    TimeSpan CrossfadeDuration { get; set; }

    /// <summary>
    /// Performs a transition from the current track to a new track.
    /// </summary>
    /// <param name="fromTrack">Current track</param>
    /// <param name="toTrack">New track to transition to</param>
    /// <param name="useCrossfade">Whether to use crossfade effect</param>
    /// <param name="playNewTrackAsync">Async function to play the new track</param>
    /// <param name="fadeOutAsync">Async function to fade out current track</param>
    /// <param name="fadeInAsync">Async function to fade in new track</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task TransitionAsync(
        AudioTrack? fromTrack,
        AudioTrack toTrack,
        bool useCrossfade,
        Func<AudioTrack, CancellationToken, Task> playNewTrackAsync,
        Func<TimeSpan, CancellationToken, Task> fadeOutAsync,
        Func<TimeSpan, CancellationToken, Task> fadeInAsync,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs a crossfade between tracks.
    /// </summary>
    /// <param name="currentVolume">Current volume level</param>
    /// <param name="targetVolume">Target volume level</param>
    /// <param name="crossfadeDurationMs">Crossfade duration in milliseconds</param>
    /// <param name="playNewTrackAsync">Function to play new track</param>
    /// <param name="fadeVolumeAsync">Function to fade volume</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task CrossfadeAsync(
        float currentVolume,
        float targetVolume,
        int crossfadeDurationMs,
        Func<Task> playNewTrackAsync,
        Func<float, float, int, CancellationToken, Task> fadeVolumeAsync,
        CancellationToken cancellationToken = default);
}
