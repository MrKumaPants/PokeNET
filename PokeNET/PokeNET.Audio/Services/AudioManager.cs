using System.IO;
using Microsoft.Extensions.Logging;
using PokeNET.Audio.Abstractions;
using PokeNET.Audio.Models;
using PokeNET.Audio.Services.Managers;
using SoundEffect = PokeNET.Audio.Models.SoundEffect;

namespace PokeNET.Audio.Services;

/// <summary>
/// Central audio management service that coordinates music and sound effect playback.
/// Implements the Facade pattern to provide a unified interface to the audio subsystem.
/// Acts as an orchestrator that delegates to specialized managers.
/// Refactored to comply with Single Responsibility Principle (SRP).
/// </summary>
public sealed class AudioManager : IAudioManager, IDisposable
{
    private readonly ILogger<AudioManager> _logger;
    private readonly IAudioCache _cache;
    private readonly IMusicPlayer _musicPlayer;
    private readonly ISoundEffectPlayer _sfxPlayer;

    // Specialized managers for single responsibilities
    private readonly IAudioVolumeManager _volumeManager;
    private readonly IAudioStateManager _stateManager;
    private readonly IAudioCacheCoordinator _cacheCoordinator;
    private readonly IAmbientAudioManager _ambientManager;

    private bool _disposed;

    /// <summary>
    /// Event raised when the audio system state changes.
    /// </summary>
    public event EventHandler<AudioStateChangedEventArgs>? StateChanged
    {
        add => _stateManager.StateChanged += value;
        remove => _stateManager.StateChanged -= value;
    }

    /// <summary>
    /// Event raised when an audio error occurs.
    /// </summary>
    public event EventHandler<AudioErrorEventArgs>? ErrorOccurred
    {
        add => _stateManager.ErrorOccurred += value;
        remove => _stateManager.ErrorOccurred -= value;
    }

    /// <summary>
    /// Gets a value indicating whether the audio system is initialized.
    /// </summary>
    public bool IsInitialized => _stateManager.IsInitialized;

    /// <summary>
    /// Gets a value indicating whether music is currently playing.
    /// </summary>
    public bool IsMusicPlaying => _musicPlayer.IsPlaying;

    /// <summary>
    /// Gets a value indicating whether music is currently paused.
    /// </summary>
    public bool IsMusicPaused => _musicPlayer.State == PlaybackState.Paused;

    /// <summary>
    /// Gets the current music track name.
    /// </summary>
    public string CurrentMusicTrack => _stateManager.CurrentMusicTrack;

    /// <summary>
    /// Gets the current master volume (0.0 to 1.0).
    /// </summary>
    public float MasterVolume => _volumeManager.MasterVolume;

    /// <summary>
    /// Gets the current music volume (0.0 to 1.0).
    /// </summary>
    public float MusicVolume => _volumeManager.MusicVolume;

    /// <summary>
    /// Gets the current sound effects volume (0.0 to 1.0).
    /// </summary>
    public float SfxVolume => _volumeManager.SfxVolume;

    /// <summary>
    /// Gets the music player for background music management.
    /// </summary>
    public IMusicPlayer MusicPlayer => _musicPlayer;

    /// <summary>
    /// Gets the sound effect player for one-shot audio.
    /// </summary>
    public ISoundEffectPlayer SoundEffectPlayer => _sfxPlayer;

    /// <summary>
    /// Gets the audio mixer for volume and channel control.
    /// </summary>
    public IAudioMixer Mixer =>
        throw new NotImplementedException("Audio mixer not yet implemented");

    /// <summary>
    /// Gets the audio configuration settings.
    /// </summary>
    public IAudioConfiguration Configuration =>
        throw new NotImplementedException("Audio configuration not yet implemented");

    /// <summary>
    /// Initializes a new instance of the AudioManager class.
    /// </summary>
    /// <param name="logger">Logger for diagnostics.</param>
    /// <param name="cache">Audio cache for managing audio data.</param>
    /// <param name="musicPlayer">Music player for background music.</param>
    /// <param name="sfxPlayer">Sound effect player for short audio.</param>
    /// <param name="volumeManager">Volume manager for audio mixing.</param>
    /// <param name="stateManager">State manager for audio system state.</param>
    /// <param name="cacheCoordinator">Cache coordinator for preloading.</param>
    /// <param name="ambientManager">Ambient audio manager.</param>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    public AudioManager(
        ILogger<AudioManager> logger,
        IAudioCache cache,
        IMusicPlayer musicPlayer,
        ISoundEffectPlayer sfxPlayer,
        IAudioVolumeManager volumeManager,
        IAudioStateManager stateManager,
        IAudioCacheCoordinator cacheCoordinator,
        IAmbientAudioManager ambientManager
    )
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _musicPlayer = musicPlayer ?? throw new ArgumentNullException(nameof(musicPlayer));
        _sfxPlayer = sfxPlayer ?? throw new ArgumentNullException(nameof(sfxPlayer));
        _volumeManager = volumeManager ?? throw new ArgumentNullException(nameof(volumeManager));
        _stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
        _cacheCoordinator =
            cacheCoordinator ?? throw new ArgumentNullException(nameof(cacheCoordinator));
        _ambientManager = ambientManager ?? throw new ArgumentNullException(nameof(ambientManager));

        _stateManager.SetInitialized(true);
        _logger.LogInformation(
            "AudioManager created with dependency injection and specialized managers"
        );
    }

    /// <summary>
    /// Initializes the audio system asynchronously.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        _logger.LogInformation("Initializing AudioManager...");

        try
        {
            _stateManager.SetInitialized(true);
            _logger.LogInformation("AudioManager initialized successfully");
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize AudioManager");
            _stateManager.RaiseError("Initialization failed", ex);
            throw;
        }
    }

    /// <summary>
    /// Shuts down the audio system and releases all resources.
    /// </summary>
    public async Task ShutdownAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            _logger.LogInformation("Shutting down audio system");
            StopAll();
            await Task.CompletedTask;
            _logger.LogInformation("Audio system shutdown complete");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during audio system shutdown");
            _stateManager.RaiseError("Shutdown failed", ex);
            throw;
        }
    }

    /// <summary>
    /// Pauses all audio playback (music and sound effects).
    /// </summary>
    public void PauseAll()
    {
        ThrowIfDisposed();
        _stateManager.PauseAll();
    }

    /// <summary>
    /// Resumes all paused audio playback.
    /// </summary>
    public void ResumeAll()
    {
        ThrowIfDisposed();
        _stateManager.ResumeAll();
    }

    /// <summary>
    /// Stops all audio playback immediately.
    /// </summary>
    public void StopAll()
    {
        ThrowIfDisposed();
        _stateManager.StopAll();
    }

    /// <summary>
    /// Mutes all audio output.
    /// </summary>
    public void MuteAll()
    {
        ThrowIfDisposed();
        _volumeManager.SetMasterVolume(0.0f);
        _logger.LogInformation("Muted all audio");
    }

    /// <summary>
    /// Unmutes all audio output.
    /// </summary>
    public void UnmuteAll()
    {
        ThrowIfDisposed();
        _volumeManager.SetMasterVolume(1.0f);
        _logger.LogInformation("Unmuted all audio");
    }

    /// <summary>
    /// Plays background music from the specified asset path.
    /// </summary>
    /// <param name="assetPath">Path to the music file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task PlayMusicAsync(
        string assetPath,
        CancellationToken cancellationToken = default
    )
    {
        await PlayMusicAsync(assetPath, true, cancellationToken);
    }

    /// <summary>
    /// Plays background music from the specified track with optional looping.
    /// </summary>
    /// <param name="trackName">The name/path of the music track.</param>
    /// <param name="loop">Whether to loop the music. Default is true.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task PlayMusicAsync(
        string trackName,
        bool loop = true,
        CancellationToken cancellationToken = default
    )
    {
        ThrowIfDisposed();

        try
        {
            _logger.LogInformation("Loading music: {TrackName}, Loop: {Loop}", trackName, loop);

            var audioData = await _cache.GetOrLoadAsync<AudioTrack>(
                trackName,
                () =>
                {
                    throw new FileNotFoundException($"Audio file not found: {trackName}");
                }
            );

            if (audioData == null)
            {
                throw new FileNotFoundException($"Audio file not found: {trackName}");
            }

            await _musicPlayer.PlayAsync(audioData, cancellationToken);
            _stateManager.SetCurrentMusicTrack(trackName);

            _logger.LogInformation("Started playing music: {TrackName}", trackName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to play music: {TrackName}", trackName);
            _stateManager.RaiseError($"Failed to play music: {trackName}", ex);
            throw;
        }
    }

    /// <summary>
    /// Plays background music from the specified track with volume control.
    /// </summary>
    /// <param name="trackName">The name/path of the music track.</param>
    /// <param name="volume">Volume level (0.0 to 1.0).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task PlayMusicAsync(
        string trackName,
        float volume,
        CancellationToken cancellationToken = default
    )
    {
        ThrowIfDisposed();

        try
        {
            _logger.LogInformation(
                "Loading music: {TrackName}, Volume: {Volume}",
                trackName,
                volume
            );
            _volumeManager.SetMusicVolume(volume);

            var audioData = await _cache.GetOrLoadAsync<AudioTrack>(
                trackName,
                () =>
                {
                    throw new FileNotFoundException($"Audio file not found: {trackName}");
                }
            );

            if (audioData == null)
            {
                throw new FileNotFoundException($"Audio file not found: {trackName}");
            }

            await _musicPlayer.PlayAsync(audioData, cancellationToken);
            _stateManager.SetCurrentMusicTrack(trackName);

            _logger.LogInformation("Started playing music: {TrackName}", trackName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to play music: {TrackName}", trackName);
            _stateManager.RaiseError($"Failed to play music: {trackName}", ex);
            throw;
        }
    }

    /// <summary>
    /// Plays a sound effect from the specified asset path.
    /// </summary>
    /// <param name="sfxName">Path to the sound effect file.</param>
    /// <param name="volume">Volume level (0.0 to 1.0). Defaults to 1.0.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task PlaySoundEffectAsync(
        string sfxName,
        float volume = 1.0f,
        CancellationToken cancellationToken = default
    )
    {
        ThrowIfDisposed();

        try
        {
            _logger.LogDebug("Loading sound effect: {SfxName}", sfxName);

            var soundEffect = await _cache.GetOrLoadAsync<SoundEffect>(
                sfxName,
                () =>
                {
                    throw new FileNotFoundException($"Sound effect file not found: {sfxName}");
                }
            );

            if (soundEffect == null)
            {
                throw new FileNotFoundException($"Sound effect file not found: {sfxName}");
            }

            await _sfxPlayer.PlayAsync(soundEffect, volume, priority: 0, cancellationToken);
            _logger.LogDebug("Played sound effect: {SfxName}", sfxName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to play sound effect: {SfxName}", sfxName);
            _stateManager.RaiseError($"Failed to play sound effect: {sfxName}", ex);
            throw;
        }
    }

    /// <summary>
    /// Stops the currently playing music.
    /// </summary>
    public Task StopMusicAsync()
    {
        ThrowIfDisposed();

        try
        {
            _musicPlayer.Stop();
            _logger.LogInformation("Stopped music playback");
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop music");
            _stateManager.RaiseError("Failed to stop music", ex);
            throw;
        }
    }

    /// <summary>
    /// Pauses the currently playing music.
    /// </summary>
    public Task PauseMusicAsync()
    {
        ThrowIfDisposed();

        try
        {
            _musicPlayer.Pause();
            _logger.LogInformation("Paused music playback");
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to pause music");
            _stateManager.RaiseError("Failed to pause music", ex);
            throw;
        }
    }

    /// <summary>
    /// Resumes paused music playback.
    /// </summary>
    public Task ResumeMusicAsync()
    {
        ThrowIfDisposed();

        try
        {
            _musicPlayer.Resume();
            _logger.LogInformation("Resumed music playback");
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resume music");
            _stateManager.RaiseError("Failed to resume music", ex);
            throw;
        }
    }

    /// <summary>
    /// Plays ambient audio (looping background sounds).
    /// </summary>
    /// <param name="ambientName">The name/path of the ambient audio.</param>
    /// <param name="volume">Volume level (0.0 to 1.0).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task PlayAmbientAsync(
        string ambientName,
        float volume,
        CancellationToken cancellationToken = default
    )
    {
        ThrowIfDisposed();
        return _ambientManager.PlayAsync(ambientName, volume, cancellationToken);
    }

    /// <summary>
    /// Pauses ambient audio playback.
    /// </summary>
    public Task PauseAmbientAsync()
    {
        ThrowIfDisposed();
        return _ambientManager.PauseAsync();
    }

    /// <summary>
    /// Resumes paused ambient audio playback.
    /// </summary>
    public Task ResumeAmbientAsync()
    {
        ThrowIfDisposed();
        return _ambientManager.ResumeAsync();
    }

    /// <summary>
    /// Stops ambient audio playback.
    /// </summary>
    public Task StopAmbientAsync()
    {
        ThrowIfDisposed();
        return _ambientManager.StopAsync();
    }

    /// <summary>
    /// Ducks (lowers) music volume temporarily.
    /// </summary>
    /// <param name="duckAmount">Amount to duck (0.0 to 1.0, where 1.0 is full duck).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task DuckMusicAsync(float duckAmount, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            _volumeManager.DuckMusic(duckAmount);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to duck music");
            _stateManager.RaiseError("Failed to duck music", ex);
            throw;
        }
    }

    /// <summary>
    /// Stops ducking and restores normal music volume.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task StopDuckingAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            _volumeManager.StopDucking();
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop ducking");
            _stateManager.RaiseError("Failed to stop ducking", ex);
            throw;
        }
    }

    /// <summary>
    /// Preloads an audio file into the cache.
    /// </summary>
    /// <param name="assetPath">Path to the audio file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task PreloadAudioAsync(string assetPath, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        return _cacheCoordinator.PreloadAsync(assetPath, cancellationToken);
    }

    /// <summary>
    /// Preloads multiple audio files into the cache in parallel.
    /// </summary>
    /// <param name="assetPaths">Array of asset paths to preload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task PreloadMultipleAsync(
        string[] assetPaths,
        CancellationToken cancellationToken = default
    )
    {
        ThrowIfDisposed();
        return _cacheCoordinator.PreloadMultipleAsync(assetPaths, cancellationToken);
    }

    /// <summary>
    /// Clears all cached audio data.
    /// </summary>
    public Task ClearCacheAsync()
    {
        ThrowIfDisposed();
        return _cacheCoordinator.ClearAsync();
    }

    /// <summary>
    /// Gets the current size of the audio cache in bytes.
    /// </summary>
    /// <returns>Cache size in bytes.</returns>
    public long GetCacheSize()
    {
        ThrowIfDisposed();
        return _cacheCoordinator.GetSize();
    }

    /// <summary>
    /// Gets the current music playback position.
    /// </summary>
    /// <returns>Current position as TimeSpan.</returns>
    public TimeSpan GetMusicPosition()
    {
        ThrowIfDisposed();
        return _musicPlayer.GetPosition();
    }

    /// <summary>
    /// Sets the master volume for all audio.
    /// </summary>
    /// <param name="volume">Volume level (0.0 to 1.0).</param>
    public void SetMasterVolume(float volume)
    {
        ThrowIfDisposed();
        _volumeManager.SetMasterVolume(volume);
    }

    /// <summary>
    /// Sets the music volume.
    /// </summary>
    /// <param name="volume">Volume level (0.0 to 1.0).</param>
    public void SetMusicVolume(float volume)
    {
        ThrowIfDisposed();
        _volumeManager.SetMusicVolume(volume);
    }

    /// <summary>
    /// Sets the sound effects volume.
    /// </summary>
    /// <param name="volume">Volume level (0.0 to 1.0).</param>
    public void SetSfxVolume(float volume)
    {
        ThrowIfDisposed();
        _volumeManager.SetSfxVolume(volume);
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(AudioManager));
        }
    }

    /// <summary>
    /// Disposes the AudioManager and all its resources.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _logger.LogInformation("Disposing AudioManager");

        try
        {
            if (_musicPlayer is IDisposable disposableMusicPlayer)
            {
                disposableMusicPlayer.Dispose();
            }

            if (_sfxPlayer is IDisposable disposableSfxPlayer)
            {
                disposableSfxPlayer.Dispose();
            }

            _cache?.Dispose();

            _disposed = true;
            _stateManager.SetInitialized(false);
            _logger.LogInformation("AudioManager disposed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during AudioManager disposal");
        }
    }
}
