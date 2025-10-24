using PokeNET.Audio.Models;

namespace PokeNET.Audio.Abstractions;

/// <summary>
/// Manages audio system state and event coordination.
/// SOLID PRINCIPLE: Single Responsibility - Handles only state tracking and events.
/// </summary>
public interface IAudioStateManager
{
    /// <summary>
    /// Gets a value indicating whether the audio system is initialized.
    /// </summary>
    bool IsInitialized { get; }

    /// <summary>
    /// Gets the current music track name.
    /// </summary>
    string CurrentMusicTrack { get; }

    /// <summary>
    /// Gets the current ambient track name.
    /// </summary>
    string CurrentAmbientTrack { get; }

    /// <summary>
    /// Event raised when the audio system state changes.
    /// </summary>
    event EventHandler<AudioStateChangedEventArgs>? StateChanged;

    /// <summary>
    /// Event raised when an audio error occurs.
    /// </summary>
    event EventHandler<AudioErrorEventArgs>? ErrorOccurred;

    /// <summary>
    /// Sets the initialized state.
    /// </summary>
    /// <param name="initialized">True if initialized, false otherwise.</param>
    void SetInitialized(bool initialized);

    /// <summary>
    /// Updates the current music track.
    /// </summary>
    /// <param name="trackName">The track name.</param>
    void SetCurrentMusicTrack(string trackName);

    /// <summary>
    /// Updates the current ambient track.
    /// </summary>
    /// <param name="trackName">The track name.</param>
    void SetCurrentAmbientTrack(string trackName);

    /// <summary>
    /// Raises the StateChanged event.
    /// </summary>
    /// <param name="previousState">The previous playback state.</param>
    /// <param name="newState">The new playback state.</param>
    void RaiseStateChanged(PlaybackState previousState, PlaybackState newState);

    /// <summary>
    /// Raises the ErrorOccurred event.
    /// </summary>
    /// <param name="message">Error message.</param>
    /// <param name="exception">Optional exception.</param>
    void RaiseError(string message, Exception? exception = null);

    /// <summary>
    /// Pauses all audio (music and effects) via coordinated players.
    /// </summary>
    void PauseAll();

    /// <summary>
    /// Resumes all paused audio via coordinated players.
    /// </summary>
    void ResumeAll();

    /// <summary>
    /// Stops all audio playback via coordinated players.
    /// </summary>
    void StopAll();
}
