using PokeNET.Audio.Models;

namespace PokeNET.Audio.Abstractions;

/// <summary>
/// High-level orchestration interface for managing all audio subsystems.
/// SOLID PRINCIPLE: Dependency Inversion - Depends on abstractions (IMusicPlayer, ISoundEffectPlayer).
/// SOLID PRINCIPLE: Open/Closed - Extensible through composition of audio players.
/// </summary>
/// <remarks>
/// The AudioManager coordinates between music, sound effects, and procedural generation.
/// It enforces business rules like ducking music during SFX playback and managing
/// audio priority. This follows the Facade pattern to simplify audio system usage.
/// </remarks>
public interface IAudioManager
{
    /// <summary>
    /// Gets the music player for background music management.
    /// </summary>
    IMusicPlayer MusicPlayer { get; }

    /// <summary>
    /// Gets the sound effect player for one-shot audio.
    /// </summary>
    ISoundEffectPlayer SoundEffectPlayer { get; }

    /// <summary>
    /// Gets the audio mixer for volume and channel control.
    /// </summary>
    IAudioMixer Mixer { get; }

    /// <summary>
    /// Gets the audio configuration settings.
    /// </summary>
    IAudioConfiguration Configuration { get; }

    /// <summary>
    /// Initializes the audio system asynchronously.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>A task representing the initialization operation.</returns>
    /// <exception cref="AudioException">Thrown when initialization fails.</exception>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Shuts down the audio system and releases all resources.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>A task representing the shutdown operation.</returns>
    Task ShutdownAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Pauses all audio playback (music and sound effects).
    /// </summary>
    void PauseAll();

    /// <summary>
    /// Resumes all paused audio playback.
    /// </summary>
    void ResumeAll();

    /// <summary>
    /// Stops all audio playback immediately.
    /// </summary>
    void StopAll();

    /// <summary>
    /// Mutes all audio output.
    /// </summary>
    void MuteAll();

    /// <summary>
    /// Unmutes all audio output.
    /// </summary>
    void UnmuteAll();

    /// <summary>
    /// Event raised when the audio system state changes.
    /// </summary>
    event EventHandler<AudioStateChangedEventArgs>? StateChanged;

    /// <summary>
    /// Event raised when an audio error occurs.
    /// </summary>
    event EventHandler<AudioErrorEventArgs>? ErrorOccurred;
}

/// <summary>
/// Event arguments for audio state changes.
/// </summary>
public class AudioStateChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the previous state.
    /// </summary>
    public PlaybackState PreviousState { get; init; }

    /// <summary>
    /// Gets the new state.
    /// </summary>
    public PlaybackState NewState { get; init; }

    /// <summary>
    /// Gets the timestamp of the state change.
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Event arguments for audio errors.
/// </summary>
public class AudioErrorEventArgs : EventArgs
{
    /// <summary>
    /// Gets the error message.
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Gets the exception that caused the error, if available.
    /// </summary>
    public Exception? Exception { get; init; }

    /// <summary>
    /// Gets the timestamp when the error occurred.
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
