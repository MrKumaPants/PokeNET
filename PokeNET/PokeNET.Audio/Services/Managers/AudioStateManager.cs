using Microsoft.Extensions.Logging;
using PokeNET.Audio.Abstractions;
using PokeNET.Audio.Models;

namespace PokeNET.Audio.Services.Managers;

/// <summary>
/// Manages audio system state and event coordination.
/// Implements single responsibility for state tracking and events.
/// </summary>
public sealed class AudioStateManager : IAudioStateManager
{
    private readonly ILogger<AudioStateManager> _logger;
    private readonly IMusicPlayer _musicPlayer;
    private readonly ISoundEffectPlayer _sfxPlayer;

    private bool _isInitialized;
    private string _currentMusicTrack = string.Empty;
    private string _currentAmbientTrack = string.Empty;

    /// <summary>
    /// Gets a value indicating whether the audio system is initialized.
    /// </summary>
    public bool IsInitialized => _isInitialized;

    /// <summary>
    /// Gets the current music track name.
    /// </summary>
    public string CurrentMusicTrack => _currentMusicTrack;

    /// <summary>
    /// Gets the current ambient track name.
    /// </summary>
    public string CurrentAmbientTrack => _currentAmbientTrack;

    /// <summary>
    /// Event raised when the audio system state changes.
    /// </summary>
    public event EventHandler<AudioStateChangedEventArgs>? StateChanged;

    /// <summary>
    /// Event raised when an audio error occurs.
    /// </summary>
    public event EventHandler<AudioErrorEventArgs>? ErrorOccurred;

    /// <summary>
    /// Initializes a new instance of the AudioStateManager class.
    /// </summary>
    /// <param name="logger">Logger for diagnostics.</param>
    /// <param name="musicPlayer">Music player for state queries.</param>
    /// <param name="sfxPlayer">Sound effect player for state queries.</param>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    public AudioStateManager(
        ILogger<AudioStateManager> logger,
        IMusicPlayer musicPlayer,
        ISoundEffectPlayer sfxPlayer)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _musicPlayer = musicPlayer ?? throw new ArgumentNullException(nameof(musicPlayer));
        _sfxPlayer = sfxPlayer ?? throw new ArgumentNullException(nameof(sfxPlayer));
    }

    /// <summary>
    /// Sets the initialized state.
    /// </summary>
    /// <param name="initialized">True if initialized, false otherwise.</param>
    public void SetInitialized(bool initialized)
    {
        _isInitialized = initialized;
        _logger.LogInformation("Audio system initialized state: {Initialized}", initialized);
    }

    /// <summary>
    /// Updates the current music track.
    /// </summary>
    /// <param name="trackName">The track name.</param>
    public void SetCurrentMusicTrack(string trackName)
    {
        _currentMusicTrack = trackName;
        _logger.LogDebug("Current music track: {TrackName}", trackName);
    }

    /// <summary>
    /// Updates the current ambient track.
    /// </summary>
    /// <param name="trackName">The track name.</param>
    public void SetCurrentAmbientTrack(string trackName)
    {
        _currentAmbientTrack = trackName;
        _logger.LogDebug("Current ambient track: {TrackName}", trackName);
    }

    /// <summary>
    /// Raises the StateChanged event.
    /// </summary>
    /// <param name="previousState">The previous playback state.</param>
    /// <param name="newState">The new playback state.</param>
    public void RaiseStateChanged(PlaybackState previousState, PlaybackState newState)
    {
        var args = new AudioStateChangedEventArgs
        {
            PreviousState = previousState,
            NewState = newState
        };

        StateChanged?.Invoke(this, args);
        _logger.LogInformation("Audio state changed: {PreviousState} -> {NewState}", previousState, newState);
    }

    /// <summary>
    /// Raises the ErrorOccurred event.
    /// </summary>
    /// <param name="message">Error message.</param>
    /// <param name="exception">Optional exception.</param>
    public void RaiseError(string message, Exception? exception = null)
    {
        var args = new AudioErrorEventArgs
        {
            Message = message,
            Exception = exception
        };

        ErrorOccurred?.Invoke(this, args);
        _logger.LogError(exception, "Audio error occurred: {Message}", message);
    }

    /// <summary>
    /// Pauses all audio (music and effects) via coordinated players.
    /// </summary>
    public void PauseAll()
    {
        _musicPlayer.Pause();
        _logger.LogInformation("Paused all audio playback");
    }

    /// <summary>
    /// Resumes all paused audio via coordinated players.
    /// </summary>
    public void ResumeAll()
    {
        _musicPlayer.Resume();
        _logger.LogInformation("Resumed all audio playback");
    }

    /// <summary>
    /// Stops all audio playback via coordinated players.
    /// </summary>
    public void StopAll()
    {
        _musicPlayer.Stop();
        _sfxPlayer.StopAll();
        _logger.LogInformation("Stopped all audio playback");
    }
}
