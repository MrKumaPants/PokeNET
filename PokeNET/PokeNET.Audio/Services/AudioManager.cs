using Microsoft.Extensions.Logging;
using PokeNET.Audio.Abstractions;
using PokeNET.Audio.Models;
using SoundEffect = PokeNET.Audio.Models.SoundEffect;

namespace PokeNET.Audio.Services;

/// <summary>
/// Central audio management service that coordinates music and sound effect playback.
/// Implements the Facade pattern to provide a unified interface to the audio subsystem.
/// Acts as a delegation layer over IMusicPlayer, ISoundEffectPlayer, and IAudioCache.
/// </summary>
public sealed class AudioManager : IAudioManager, IDisposable
{
    private readonly ILogger<AudioManager> _logger;
    private readonly IAudioCache _cache;
    private readonly IMusicPlayer _musicPlayer;
    private readonly ISoundEffectPlayer _sfxPlayer;

    private float _masterVolume = 1.0f;
    private float _musicVolume = 1.0f;
    private float _sfxVolume = 1.0f;
    private float _ambientVolume = 1.0f;
    private float _originalMusicVolume = 1.0f;
    private bool _disposed;
    private string _currentMusicTrack = string.Empty;
    private string _currentAmbientTrack = string.Empty;

    /// <summary>
    /// Event raised when the audio system state changes.
    /// </summary>
    public event EventHandler<AudioStateChangedEventArgs>? StateChanged;

    /// <summary>
    /// Event raised when an audio error occurs.
    /// </summary>
    public event EventHandler<AudioErrorEventArgs>? ErrorOccurred;

    /// <summary>
    /// Gets a value indicating whether the audio system is initialized.
    /// </summary>
    public bool IsInitialized { get; private set; }

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
    public string CurrentMusicTrack => _currentMusicTrack;

    /// <summary>
    /// Gets the current master volume (0.0 to 1.0).
    /// </summary>
    public float MasterVolume => _masterVolume;

    /// <summary>
    /// Gets the current music volume (0.0 to 1.0).
    /// </summary>
    public float MusicVolume => _musicVolume;

    /// <summary>
    /// Gets the current sound effects volume (0.0 to 1.0).
    /// </summary>
    public float SfxVolume => _sfxVolume;

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
    public IAudioMixer Mixer => throw new NotImplementedException("Audio mixer not yet implemented");

    /// <summary>
    /// Gets the audio configuration settings.
    /// </summary>
    public IAudioConfiguration Configuration => throw new NotImplementedException("Audio configuration not yet implemented");

    /// <summary>
    /// Initializes a new instance of the AudioManager class.
    /// </summary>
    /// <param name="logger">Logger for diagnostics.</param>
    /// <param name="cache">Audio cache for managing audio data.</param>
    /// <param name="musicPlayer">Music player for background music.</param>
    /// <param name="sfxPlayer">Sound effect player for short audio.</param>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    public AudioManager(
        ILogger<AudioManager> logger,
        IAudioCache cache,
        IMusicPlayer musicPlayer,
        ISoundEffectPlayer sfxPlayer)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _musicPlayer = musicPlayer ?? throw new ArgumentNullException(nameof(musicPlayer));
        _sfxPlayer = sfxPlayer ?? throw new ArgumentNullException(nameof(sfxPlayer));

        IsInitialized = true;
        _logger.LogInformation("AudioManager created with dependency injection");
    }

    /// <summary>
    /// Initializes the audio system asynchronously.
    /// Calls InitializeAsync on both music and sound effect players.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        _logger.LogInformation("Initializing AudioManager...");

        try
        {
            // Players are initialized via dependency injection
            IsInitialized = true;
            _logger.LogInformation("AudioManager initialized successfully");
            await Task.CompletedTask; // Keep method async for future extensibility
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize AudioManager");
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
            throw;
        }
    }

    /// <summary>
    /// Pauses all audio playback (music and sound effects).
    /// </summary>
    public void PauseAll()
    {
        ThrowIfDisposed();
        _musicPlayer.Pause();
        _logger.LogInformation("Paused all audio playback");
    }

    /// <summary>
    /// Resumes all paused audio playback.
    /// </summary>
    public void ResumeAll()
    {
        ThrowIfDisposed();
        _musicPlayer.Resume();
        _logger.LogInformation("Resumed all audio playback");
    }

    /// <summary>
    /// Stops all audio playback immediately.
    /// </summary>
    public void StopAll()
    {
        ThrowIfDisposed();
        _musicPlayer.Stop();
        _sfxPlayer.StopAll();
        _logger.LogInformation("Stopped all audio playback");
    }

    /// <summary>
    /// Mutes all audio output.
    /// </summary>
    public void MuteAll()
    {
        ThrowIfDisposed();
        SetMasterVolume(0.0f);
        _logger.LogInformation("Muted all audio");
    }

    /// <summary>
    /// Unmutes all audio output.
    /// </summary>
    public void UnmuteAll()
    {
        ThrowIfDisposed();
        SetMasterVolume(1.0f);
        _logger.LogInformation("Unmuted all audio");
    }

    /// <summary>
    /// Plays background music from the specified asset path.
    /// Loads audio data from cache and passes to music player.
    /// </summary>
    /// <param name="assetPath">Path to the music file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task PlayMusicAsync(string assetPath, CancellationToken cancellationToken = default)
    {
        await PlayMusicAsync(assetPath, true, cancellationToken);
    }

    /// <summary>
    /// Plays background music from the specified track with optional looping.
    /// </summary>
    /// <param name="trackName">The name/path of the music track.</param>
    /// <param name="loop">Whether to loop the music. Default is true.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task PlayMusicAsync(string trackName, bool loop = true, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            _logger.LogInformation("Loading music: {TrackName}, Loop: {Loop}", trackName, loop);

            // Load audio data from cache with loader function
            var audioData = await _cache.GetOrLoadAsync<AudioTrack>(trackName, () =>
            {
                // TODO: Implement actual file loading logic
                throw new FileNotFoundException($"Audio file not found: {trackName}");
            });

            if (audioData == null)
            {
                throw new FileNotFoundException($"Audio file not found: {trackName}");
            }

            // Play the audio data
            await _musicPlayer.PlayAsync(audioData, cancellationToken);

            _currentMusicTrack = trackName;
            _logger.LogInformation("Started playing music: {TrackName}", trackName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to play music: {TrackName}", trackName);
            throw;
        }
    }

    /// <summary>
    /// Plays background music from the specified track with volume control.
    /// </summary>
    /// <param name="trackName">The name/path of the music track.</param>
    /// <param name="volume">Volume level (0.0 to 1.0).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task PlayMusicAsync(string trackName, float volume, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            _logger.LogInformation("Loading music: {TrackName}, Volume: {Volume}", trackName, volume);

            // Set music volume first
            SetMusicVolume(volume);

            // Load audio data from cache with loader function
            var audioData = await _cache.GetOrLoadAsync<AudioTrack>(trackName, () =>
            {
                // TODO: Implement actual file loading logic
                throw new FileNotFoundException($"Audio file not found: {trackName}");
            });

            if (audioData == null)
            {
                throw new FileNotFoundException($"Audio file not found: {trackName}");
            }

            // Play the audio data
            await _musicPlayer.PlayAsync(audioData, cancellationToken);

            _currentMusicTrack = trackName;
            _logger.LogInformation("Started playing music: {TrackName}", trackName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to play music: {TrackName}", trackName);
            throw;
        }
    }

    /// <summary>
    /// Plays a sound effect from the specified asset path.
    /// </summary>
    /// <param name="sfxName">Path to the sound effect file.</param>
    /// <param name="volume">Volume level (0.0 to 1.0). Defaults to 1.0.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task PlaySoundEffectAsync(string sfxName, float volume = 1.0f, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            _logger.LogDebug("Loading sound effect: {SfxName}", sfxName);

            // Load sound effect from cache with loader function
            var soundEffect = await _cache.GetOrLoadAsync<SoundEffect>(sfxName, () =>
            {
                // TODO: Implement actual file loading logic
                throw new FileNotFoundException($"Sound effect file not found: {sfxName}");
            });

            if (soundEffect == null)
            {
                throw new FileNotFoundException($"Sound effect file not found: {sfxName}");
            }

            // Play the sound effect
            await _sfxPlayer.PlayAsync(soundEffect, volume, priority: 0, cancellationToken);

            _logger.LogDebug("Played sound effect: {SfxName}", sfxName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to play sound effect: {SfxName}", sfxName);
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
            throw;
        }
    }

    /// <summary>
    /// Plays ambient audio (looping background sounds).
    /// </summary>
    /// <param name="ambientName">The name/path of the ambient audio.</param>
    /// <param name="volume">Volume level (0.0 to 1.0).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task PlayAmbientAsync(string ambientName, float volume, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            _logger.LogInformation("Playing ambient audio: {AmbientName}, Volume: {Volume}", ambientName, volume);

            // Load ambient audio from cache
            var audioData = await _cache.GetOrLoadAsync<AudioTrack>(ambientName, () =>
            {
                // TODO: Implement actual file loading logic
                throw new FileNotFoundException($"Ambient audio file not found: {ambientName}");
            });

            if (audioData == null)
            {
                throw new FileNotFoundException($"Ambient audio file not found: {ambientName}");
            }

            // Play as looping sound effect
            var soundEffect = new SoundEffect
            {
                Name = ambientName,
                // TODO: Convert AudioTrack to SoundEffect if needed
            };

            await _sfxPlayer.PlayAsync(soundEffect, volume, priority: -1, cancellationToken);

            _currentAmbientTrack = ambientName;
            _ambientVolume = volume;

            _logger.LogInformation("Started ambient audio: {AmbientName}", ambientName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to play ambient audio: {AmbientName}", ambientName);
            throw;
        }
    }

    /// <summary>
    /// Pauses ambient audio playback.
    /// </summary>
    public Task PauseAmbientAsync()
    {
        ThrowIfDisposed();

        try
        {
            // TODO: Implement ambient pause logic
            _logger.LogInformation("Paused ambient audio");
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to pause ambient audio");
            throw;
        }
    }

    /// <summary>
    /// Resumes paused ambient audio playback.
    /// </summary>
    public Task ResumeAmbientAsync()
    {
        ThrowIfDisposed();

        try
        {
            // TODO: Implement ambient resume logic
            _logger.LogInformation("Resumed ambient audio");
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resume ambient audio");
            throw;
        }
    }

    /// <summary>
    /// Stops ambient audio playback.
    /// </summary>
    public Task StopAmbientAsync()
    {
        ThrowIfDisposed();

        try
        {
            // TODO: Implement ambient stop logic
            _currentAmbientTrack = string.Empty;
            _logger.LogInformation("Stopped ambient audio");
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop ambient audio");
            throw;
        }
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
            _originalMusicVolume = _musicVolume;
            var duckedVolume = _musicVolume * (1.0f - Math.Clamp(duckAmount, 0.0f, 1.0f));
            _musicPlayer.SetVolume(duckedVolume * _masterVolume);

            _logger.LogDebug("Ducked music volume by {DuckAmount}", duckAmount);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to duck music");
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
            _musicPlayer.SetVolume(_originalMusicVolume * _masterVolume);
            _logger.LogDebug("Restored music volume after ducking");
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop ducking");
            throw;
        }
    }

    /// <summary>
    /// Preloads an audio file into the cache.
    /// </summary>
    /// <param name="assetPath">Path to the audio file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task PreloadAudioAsync(string assetPath, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            _logger.LogDebug("Preloading audio: {AssetPath}", assetPath);

            // Load audio data and cache it
            var audioData = await _cache.GetOrLoadAsync<AudioTrack>(assetPath, () =>
            {
                // TODO: Implement actual file loading logic
                throw new FileNotFoundException($"Audio file not found: {assetPath}");
            });

            _logger.LogDebug("Preloaded audio: {AssetPath}", assetPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to preload audio: {AssetPath}", assetPath);
            throw;
        }
    }

    /// <summary>
    /// Preloads multiple audio files into the cache in parallel.
    /// </summary>
    /// <param name="assetPaths">Array of asset paths to preload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task PreloadMultipleAsync(string[] assetPaths, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (assetPaths == null || assetPaths.Length == 0)
        {
            return;
        }

        _logger.LogInformation("Preloading {Count} audio files", assetPaths.Length);

        try
        {
            // Preload all files in parallel
            var tasks = assetPaths.Select(path => PreloadAudioAsync(path, cancellationToken));
            await Task.WhenAll(tasks);

            _logger.LogInformation("Successfully preloaded {Count} audio files", assetPaths.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to preload multiple audio files");
            throw;
        }
    }

    /// <summary>
    /// Clears all cached audio data.
    /// </summary>
    public async Task ClearCacheAsync()
    {
        ThrowIfDisposed();

        try
        {
            await _cache.ClearAsync();
            _logger.LogInformation("Cleared audio cache");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear cache");
            throw;
        }
    }

    /// <summary>
    /// Gets the current size of the audio cache in bytes.
    /// </summary>
    /// <returns>Cache size in bytes.</returns>
    public long GetCacheSize()
    {
        ThrowIfDisposed();
        return _cache.GetSize();
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
    /// Automatically updates both music and SFX player volumes.
    /// </summary>
    /// <param name="volume">Volume level (0.0 to 1.0).</param>
    public void SetMasterVolume(float volume)
    {
        ThrowIfDisposed();
        _masterVolume = Math.Clamp(volume, 0.0f, 1.0f);
        UpdatePlayerVolumes();
        _logger.LogInformation("Set master volume to {Volume}", _masterVolume);
    }

    /// <summary>
    /// Sets the music volume.
    /// Automatically applies master volume multiplication.
    /// </summary>
    /// <param name="volume">Volume level (0.0 to 1.0).</param>
    public void SetMusicVolume(float volume)
    {
        ThrowIfDisposed();
        _musicVolume = Math.Clamp(volume, 0.0f, 1.0f);
        _musicPlayer.SetVolume(_musicVolume * _masterVolume);
        _logger.LogDebug("Set music volume to {Volume}", _musicVolume);
    }

    /// <summary>
    /// Sets the sound effects volume.
    /// Automatically applies master volume multiplication.
    /// </summary>
    /// <param name="volume">Volume level (0.0 to 1.0).</param>
    public void SetSfxVolume(float volume)
    {
        ThrowIfDisposed();
        _sfxVolume = Math.Clamp(volume, 0.0f, 1.0f);
        _sfxPlayer.SetMasterVolume(_sfxVolume * _masterVolume);
        _logger.LogDebug("Set SFX volume to {Volume}", _sfxVolume);
    }

    /// <summary>
    /// Updates all player volumes based on current volume settings and master volume.
    /// </summary>
    private void UpdatePlayerVolumes()
    {
        _musicPlayer.SetVolume(_musicVolume * _masterVolume);
        _sfxPlayer.SetMasterVolume(_sfxVolume * _masterVolume);
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
            // Dispose players if they implement IDisposable
            if (_musicPlayer is IDisposable disposableMusicPlayer)
            {
                disposableMusicPlayer.Dispose();
            }

            if (_sfxPlayer is IDisposable disposableSfxPlayer)
            {
                disposableSfxPlayer.Dispose();
            }

            // Dispose cache
            _cache?.Dispose();

            _disposed = true;
            _logger.LogInformation("AudioManager disposed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during AudioManager disposal");
        }
    }
}
