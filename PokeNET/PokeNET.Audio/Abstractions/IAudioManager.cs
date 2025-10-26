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
    /// <exception cref="Exception">Thrown when initialization fails.</exception>
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
    /// Gets whether music is currently playing.
    /// </summary>
    bool IsMusicPlaying { get; }

    /// <summary>
    /// Gets the current music track name.
    /// </summary>
    string CurrentMusicTrack { get; }

    /// <summary>
    /// Plays music from the specified track with optional looping.
    /// </summary>
    /// <param name="trackName">The name/path of the music track.</param>
    /// <param name="loop">Whether to loop the music. Default is true.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>A task representing the operation.</returns>
    Task PlayMusicAsync(
        string trackName,
        bool loop = true,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Plays music from the specified track with volume control.
    /// </summary>
    /// <param name="trackName">The name/path of the music track.</param>
    /// <param name="volume">Volume level (0.0 to 1.0).</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>A task representing the operation.</returns>
    Task PlayMusicAsync(
        string trackName,
        float volume,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Pauses music playback.
    /// </summary>
    /// <returns>A task representing the operation.</returns>
    Task PauseMusicAsync();

    /// <summary>
    /// Resumes paused music playback.
    /// </summary>
    /// <returns>A task representing the operation.</returns>
    Task ResumeMusicAsync();

    /// <summary>
    /// Stops music playback.
    /// </summary>
    /// <returns>A task representing the operation.</returns>
    Task StopMusicAsync();

    /// <summary>
    /// Plays a sound effect.
    /// </summary>
    /// <param name="sfxName">The name/path of the sound effect.</param>
    /// <param name="volume">Volume level (0.0 to 1.0).</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>A task representing the operation.</returns>
    Task PlaySoundEffectAsync(
        string sfxName,
        float volume,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Plays ambient audio (looping background sounds).
    /// </summary>
    /// <param name="ambientName">The name/path of the ambient audio.</param>
    /// <param name="volume">Volume level (0.0 to 1.0).</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>A task representing the operation.</returns>
    Task PlayAmbientAsync(
        string ambientName,
        float volume,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Pauses ambient audio playback.
    /// </summary>
    /// <returns>A task representing the operation.</returns>
    Task PauseAmbientAsync();

    /// <summary>
    /// Resumes paused ambient audio playback.
    /// </summary>
    /// <returns>A task representing the operation.</returns>
    Task ResumeAmbientAsync();

    /// <summary>
    /// Stops ambient audio playback.
    /// </summary>
    /// <returns>A task representing the operation.</returns>
    Task StopAmbientAsync();

    /// <summary>
    /// Ducks (lowers) music volume temporarily.
    /// </summary>
    /// <param name="duckAmount">Amount to duck (0.0 to 1.0, where 1.0 is full duck).</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>A task representing the operation.</returns>
    Task DuckMusicAsync(float duckAmount, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops ducking and restores normal music volume.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>A task representing the operation.</returns>
    Task StopDuckingAsync(CancellationToken cancellationToken = default);

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
