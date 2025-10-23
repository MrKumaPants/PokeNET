using PokeNET.Audio.Abstractions;
using PokeNET.Audio.Configuration;
using PokeNET.Audio.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Melanchall.DryWetMidi.Core;

namespace PokeNET.Audio.Services;

/// <summary>
/// Central audio management service that coordinates music and sound effect playback.
/// Implements the Facade pattern to provide a unified interface to the audio subsystem.
/// NOTE: Full IAudioManager interface implementation pending - currently provides basic functionality.
/// </summary>
public sealed class AudioManager // TODO: Implement IAudioManager interface
{
    private readonly ILogger<AudioManager> _logger;
    private readonly AudioSettings _settings;
    private readonly AudioCache _cache;
    private readonly Lazy<MusicPlayer> _musicPlayerLazy;
    private readonly Lazy<SoundEffectPlayer> _soundEffectPlayerLazy;

    private float _masterVolume;
    private bool _isMuted;
    private bool _initialized;
    private bool _disposed;

    public MusicPlayer MusicPlayer
    {
        get
        {
            ThrowIfDisposed();
            return _musicPlayerLazy.Value;
        }
    }

    public SoundEffectPlayer SoundEffectPlayer
    {
        get
        {
            ThrowIfDisposed();
            return _soundEffectPlayerLazy.Value;
        }
    }

    public float MasterVolume
    {
        get => _masterVolume;
        set
        {
            ThrowIfDisposed();
            _masterVolume = Math.Clamp(value, 0.0f, 1.0f);

            // Update child players
            if (_musicPlayerLazy.IsValueCreated)
            {
                MusicPlayer.Volume = _settings.MusicVolume * _masterVolume;
            }

            if (_soundEffectPlayerLazy.IsValueCreated)
            {
                // TODO: Implement Volume property in SoundEffectPlayer
            }

            _logger.LogInformation("Master volume set to {Volume}", _masterVolume);
        }
    }

    public bool IsMuted
    {
        get => _isMuted;
        set
        {
            ThrowIfDisposed();
            _isMuted = value;

            if (_isMuted)
            {
                _logger.LogInformation("Audio muted");
                PauseAll();
            }
            else
            {
                _logger.LogInformation("Audio unmuted");
                ResumeAll();
            }
        }
    }

    public AudioManager(
        ILogger<AudioManager> logger,
        IOptions<AudioSettings> settings,
        ILoggerFactory loggerFactory)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (settings?.Value == null)
        {
            throw new ArgumentNullException(nameof(settings));
        }

        if (loggerFactory == null)
        {
            throw new ArgumentNullException(nameof(loggerFactory));
        }

        _settings = settings.Value;
        _settings.Validate();

        _masterVolume = _settings.MasterVolume;
        _isMuted = false;
        _initialized = false;

        // Initialize cache
        _cache = new AudioCache(
            loggerFactory.CreateLogger<AudioCache>(),
            _settings.MaxCacheSizeMB);

        // Lazy initialization of players (only created when accessed)
        _musicPlayerLazy = new Lazy<MusicPlayer>(() =>
        {
            _logger.LogDebug("Initializing MusicPlayer");
            return new MusicPlayer(
                loggerFactory.CreateLogger<MusicPlayer>(),
                settings,
                _cache);
        });

        _soundEffectPlayerLazy = new Lazy<SoundEffectPlayer>(() =>
        {
            _logger.LogDebug("Initializing SoundEffectPlayer");
            return new SoundEffectPlayer(
                loggerFactory.CreateLogger<SoundEffectPlayer>(),
                settings,
                _cache);
        });

        _logger.LogInformation("AudioManager created");
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (_initialized)
        {
            _logger.LogWarning("AudioManager already initialized");
            return;
        }

        _logger.LogInformation("Initializing AudioManager...");

        try
        {
            // Validate settings
            _settings.Validate();

            // Ensure asset base path exists
            if (!Directory.Exists(_settings.AssetBasePath))
            {
                _logger.LogWarning("Asset base path does not exist, creating: {Path}", _settings.AssetBasePath);
                Directory.CreateDirectory(_settings.AssetBasePath);
            }

            // Preload common assets if enabled
            if (_settings.PreloadCommonAssets && _settings.PreloadAssets)
            {
                _logger.LogInformation("Preloading common audio assets");
                // TODO: Implement asset preloading when asset list is available
            }

            // Force initialization of players if needed
            if (_settings.PreloadCommonAssets)
            {
                _ = MusicPlayer;
                _ = SoundEffectPlayer;
            }

            _initialized = true;
            _logger.LogInformation("AudioManager initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize AudioManager");
            throw new AudioException("Failed to initialize AudioManager", ex);
        }
    }

    public void StopAll()
    {
        ThrowIfDisposed();

        _logger.LogInformation("Stopping all audio playback");

        try
        {
            if (_musicPlayerLazy.IsValueCreated)
            {
                MusicPlayer.Stop();
            }

            if (_soundEffectPlayerLazy.IsValueCreated)
            {
                SoundEffectPlayer.StopAll();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping all audio");
            throw new AudioException("Failed to stop all audio", ex);
        }
    }

    public void PauseAll()
    {
        ThrowIfDisposed();

        _logger.LogInformation("Pausing all audio playback");

        try
        {
            if (_musicPlayerLazy.IsValueCreated && MusicPlayer.IsPlaying)
            {
                MusicPlayer.Pause();
            }

            if (_soundEffectPlayerLazy.IsValueCreated)
            {
                // TODO: Implement PauseAll when ISoundEffectPlayer interface is fully implemented
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error pausing all audio");
            throw new AudioException("Failed to pause all audio", ex);
        }
    }

    public void ResumeAll()
    {
        ThrowIfDisposed();

        _logger.LogInformation("Resuming all audio playback");

        try
        {
            if (_musicPlayerLazy.IsValueCreated && MusicPlayer.IsPaused)
            {
                MusicPlayer.Resume();
            }

            if (_soundEffectPlayerLazy.IsValueCreated)
            {
                // TODO: Implement ResumeAll when ISoundEffectPlayer interface is fully implemented
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resuming all audio");
            throw new AudioException("Failed to resume all audio", ex);
        }
    }

    public void ClearCache()
    {
        ThrowIfDisposed();

        _logger.LogInformation("Clearing audio cache");

        try
        {
            _cache.Clear();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing audio cache");
            throw new AudioException("Failed to clear audio cache", ex);
        }
    }

    public IDictionary<string, int> GetCacheStatistics()
    {
        ThrowIfDisposed();

        try
        {
            var stats = _cache.GetStatistics();
            stats["TotalCount"] = _cache.Count;
            stats["CacheSizeMB"] = (int)(_cache.CurrentSize / (1024 * 1024));

            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache statistics");
            throw new AudioException("Failed to get cache statistics", ex);
        }
    }

    /// <summary>
    /// Preloads a list of audio assets into the cache.
    /// </summary>
    private async Task PreloadAssetsAsync(List<string> assetPaths, CancellationToken cancellationToken)
    {
        var tasks = new List<Task>();

        foreach (var assetPath in assetPaths)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            tasks.Add(PreloadAssetAsync(assetPath, cancellationToken));
        }

        try
        {
            await Task.WhenAll(tasks);
            _logger.LogInformation("Preloaded {Count} audio assets", assetPaths.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Some assets failed to preload");
        }
    }

    /// <summary>
    /// Preloads a single audio asset.
    /// </summary>
    private async Task PreloadAssetAsync(string assetPath, CancellationToken cancellationToken)
    {
        try
        {
            var fullPath = Path.Combine(_settings.AssetBasePath, assetPath);
            var extension = Path.GetExtension(fullPath).ToLowerInvariant();

            if (extension == ".mid" || extension == ".midi")
            {
                // Preload MIDI file
                var midiFile = MidiFile.Read(fullPath);
                var fileInfo = new FileInfo(fullPath);
                _cache.Set(assetPath, midiFile, fileInfo.Length);
                _logger.LogDebug("Preloaded MIDI asset: {AssetPath}", assetPath);
            }
            else if (extension == ".wav" || extension == ".ogg")
            {
                // Preload sound effect
                // TODO: Load MonoGame SoundEffect when available
                await Task.CompletedTask;
                _logger.LogDebug("Preloaded sound effect asset: {AssetPath}", assetPath);
            }
            else
            {
                _logger.LogWarning("Unknown audio format for preload: {AssetPath}", assetPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to preload asset: {AssetPath}", assetPath);
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(AudioManager));
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _logger.LogInformation("Disposing AudioManager");

        try
        {
            // Stop all playback
            StopAll();

            // Dispose players if they were created
            if (_musicPlayerLazy.IsValueCreated)
            {
                MusicPlayer.Dispose();
            }

            if (_soundEffectPlayerLazy.IsValueCreated)
            {
                SoundEffectPlayer.Dispose();
            }

            // Dispose cache
            _cache.Dispose();

            _disposed = true;
            _logger.LogInformation("AudioManager disposed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during AudioManager disposal");
        }
    }
}
