using PokeNET.Audio.Models;

namespace PokeNET.Audio.Abstractions;

/// <summary>
/// Core audio service interface for unified audio playback control.
/// SOLID PRINCIPLE: Single Responsibility - Handles only basic playback operations.
/// SOLID PRINCIPLE: Interface Segregation - Minimal, focused interface.
/// </summary>
/// <remarks>
/// This interface represents the foundation of the audio system, providing
/// essential playback control without coupling to specific audio types.
/// Implementations should delegate to specialized players (Music, SFX) internally.
/// </remarks>
public interface IAudioService
{
    /// <summary>
    /// Gets the current playback state of the audio system.
    /// </summary>
    PlaybackState State { get; }

    /// <summary>
    /// Gets a value indicating whether any audio is currently playing.
    /// </summary>
    bool IsPlaying { get; }

    /// <summary>
    /// Plays the specified audio track.
    /// </summary>
    /// <param name="track">The audio track to play.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>A task representing the asynchronous play operation.</returns>
    /// <exception cref="AudioException">Thrown when playback fails.</exception>
    Task PlayAsync(AudioTrack track, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pauses the currently playing audio.
    /// </summary>
    /// <remarks>
    /// Calling Pause when no audio is playing should be a no-op.
    /// </remarks>
    void Pause();

    /// <summary>
    /// Resumes playback of paused audio.
    /// </summary>
    /// <remarks>
    /// Calling Resume when audio is not paused should be a no-op.
    /// </remarks>
    void Resume();

    /// <summary>
    /// Stops all audio playback and resets position.
    /// </summary>
    void Stop();

    /// <summary>
    /// Gets the current playback position.
    /// </summary>
    /// <returns>The current position as a TimeSpan.</returns>
    TimeSpan GetPosition();

    /// <summary>
    /// Seeks to the specified position in the current track.
    /// </summary>
    /// <param name="position">The target position.</param>
    /// <exception cref="AudioException">Thrown when seeking is not supported or fails.</exception>
    void Seek(TimeSpan position);
}

/// <summary>
/// Represents the current state of audio playback.
/// </summary>
public enum PlaybackState
{
    /// <summary>No audio is playing.</summary>
    Stopped,

    /// <summary>Audio is currently playing.</summary>
    Playing,

    /// <summary>Audio is paused.</summary>
    Paused,

    /// <summary>Audio is buffering.</summary>
    Buffering,

    /// <summary>An error occurred during playback.</summary>
    Error
}
