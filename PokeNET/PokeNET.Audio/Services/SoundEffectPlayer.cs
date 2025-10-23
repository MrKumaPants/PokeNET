using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using PokeNET.Audio.Abstractions;

namespace PokeNET.Audio;

/// <summary>
/// Sound effect player with pooling and priority-based playback.
/// Manages concurrent sound playback with automatic eviction of low-priority sounds when pool is full.
/// </summary>
public sealed class SoundEffectPlayer : IDisposable
{
    private readonly ILogger<SoundEffectPlayer> _logger;
    private readonly IAudioEngine _audioEngine;
    private readonly ConcurrentDictionary<int, SoundInstance> _activeSounds;
    private int _nextSoundId = 1;
    private float _volume = 1.0f;
    private bool _isInitialized;
    private bool _disposed;

    /// <summary>
    /// Gets whether the player has been initialized.
    /// </summary>
    public bool IsInitialized => _isInitialized;

    /// <summary>
    /// Gets the maximum number of concurrent sounds (pool size).
    /// </summary>
    public int PoolSize { get; }

    /// <summary>
    /// Gets the number of currently playing sounds.
    /// </summary>
    public int ActiveSounds
    {
        get
        {
            CleanupFinishedSounds();
            return _activeSounds.Count;
        }
    }

    /// <summary>
    /// Gets or sets the master volume (0.0 to 1.0).
    /// </summary>
    public float Volume
    {
        get => _volume;
        private set => _volume = Math.Clamp(value, 0.0f, 1.0f);
    }

    /// <summary>
    /// Initializes a new instance of the SoundEffectPlayer.
    /// </summary>
    /// <param name="logger">Logger for diagnostics</param>
    /// <param name="audioEngine">Audio engine for low-level playback</param>
    /// <param name="poolSize">Maximum number of concurrent sounds (default: 32)</param>
    /// <exception cref="ArgumentException">Thrown when poolSize is less than 1</exception>
    public SoundEffectPlayer(ILogger<SoundEffectPlayer> logger, IAudioEngine audioEngine, int poolSize = 32)
    {
        if (poolSize < 1)
        {
            throw new ArgumentException("Pool size must be at least 1", nameof(poolSize));
        }

        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _audioEngine = audioEngine ?? throw new ArgumentNullException(nameof(audioEngine));
        PoolSize = poolSize;
        _activeSounds = new ConcurrentDictionary<int, SoundInstance>();

        _logger.LogInformation("SoundEffectPlayer created with pool size: {PoolSize}", PoolSize);
    }

    /// <summary>
    /// Initializes the audio engine asynchronously.
    /// </summary>
    public async Task InitializeAsync()
    {
        ThrowIfDisposed();

        if (_isInitialized)
        {
            _logger.LogWarning("SoundEffectPlayer already initialized");
            return;
        }

        _logger.LogInformation("Initializing SoundEffectPlayer...");
        await _audioEngine.InitializeAsync();
        _isInitialized = true;
        _logger.LogInformation("SoundEffectPlayer initialized successfully");
    }

    /// <summary>
    /// Plays audio data with optional volume, pitch, and priority.
    /// </summary>
    /// <param name="audioData">Raw audio data to play</param>
    /// <param name="volume">Volume level (0.0 to 1.0, default: 1.0)</param>
    /// <param name="pitch">Pitch adjustment (1.0 = normal, default: 1.0)</param>
    /// <param name="priority">Priority level (higher = more important, default: 0)</param>
    /// <returns>Unique sound ID, or 0 if playback failed</returns>
    public async Task<int> PlayAsync(byte[] audioData, float volume = 1.0f, float pitch = 1.0f, int priority = 0)
    {
        ThrowIfDisposed();

        if (audioData == null)
        {
            throw new ArgumentNullException(nameof(audioData));
        }

        CleanupFinishedSounds();

        // Check if pool is full
        if (_activeSounds.Count >= PoolSize)
        {
            _logger.LogDebug("Pool full ({Count}/{PoolSize}), attempting to evict low-priority sound",
                _activeSounds.Count, PoolSize);

            if (!TryEvictLowestPriority(priority))
            {
                _logger.LogWarning("Cannot play sound: pool full and no lower priority sounds to evict");
                return 0;
            }
        }

        try
        {
            // Apply master volume
            float effectiveVolume = Math.Clamp(volume * _volume, 0.0f, 1.0f);

            // Play through audio engine
            int engineSoundId = await _audioEngine.PlaySoundAsync(audioData, effectiveVolume, pitch);

            // Track sound instance
            int soundId = Interlocked.Increment(ref _nextSoundId);
            var instance = new SoundInstance
            {
                Id = soundId,
                EngineSoundId = engineSoundId,
                Priority = priority,
                Volume = volume,
                StartTime = DateTime.UtcNow
            };

            _activeSounds[soundId] = instance;

            _logger.LogDebug("Playing sound {SoundId} (engine: {EngineSoundId}) with priority {Priority}",
                soundId, engineSoundId, priority);

            return soundId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to play sound");
            return 0;
        }
    }

    /// <summary>
    /// Stops a specific playing sound.
    /// </summary>
    /// <param name="soundId">ID of the sound to stop</param>
    public async Task StopAsync(int soundId)
    {
        ThrowIfDisposed();

        if (_activeSounds.TryRemove(soundId, out var instance))
        {
            try
            {
                await _audioEngine.StopSoundAsync(instance.EngineSoundId);
                _logger.LogDebug("Stopped sound {SoundId}", soundId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to stop sound {SoundId}", soundId);
            }
        }
    }

    /// <summary>
    /// Stops all currently playing sounds.
    /// </summary>
    public async Task StopAllAsync()
    {
        ThrowIfDisposed();

        _logger.LogInformation("Stopping all sounds ({Count} active)", _activeSounds.Count);

        try
        {
            await _audioEngine.StopAllSoundsAsync();
            _activeSounds.Clear();
            _logger.LogInformation("All sounds stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop all sounds");
        }
    }

    /// <summary>
    /// Sets the master volume for all sounds.
    /// </summary>
    /// <param name="volume">Volume level (0.0 to 1.0)</param>
    public void SetVolume(float volume)
    {
        ThrowIfDisposed();
        Volume = volume;
        _logger.LogDebug("Master volume set to {Volume}", _volume);
    }

    /// <summary>
    /// Sets the volume for a specific playing sound.
    /// </summary>
    /// <param name="soundId">ID of the sound</param>
    /// <param name="volume">Volume level (0.0 to 1.0)</param>
    public async Task SetVolumeAsync(int soundId, float volume)
    {
        ThrowIfDisposed();

        if (_activeSounds.TryGetValue(soundId, out var instance))
        {
            try
            {
                float effectiveVolume = Math.Clamp(volume * _volume, 0.0f, 1.0f);
                await _audioEngine.SetSoundVolumeAsync(instance.EngineSoundId, effectiveVolume);
                instance.Volume = volume;
                _logger.LogDebug("Set volume for sound {SoundId} to {Volume}", soundId, volume);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set volume for sound {SoundId}", soundId);
            }
        }
    }

    /// <summary>
    /// Gets the number of available sound slots.
    /// </summary>
    /// <returns>Number of available slots in the pool</returns>
    public int GetAvailableSlots()
    {
        ThrowIfDisposed();
        CleanupFinishedSounds();
        return Math.Max(0, PoolSize - _activeSounds.Count);
    }

    /// <summary>
    /// Tries to evict the lowest priority sound to make room for a new one.
    /// </summary>
    /// <param name="newSoundPriority">Priority of the new sound</param>
    /// <returns>True if a sound was evicted, false otherwise</returns>
    private bool TryEvictLowestPriority(int newSoundPriority)
    {
        // Find lowest priority sound
        var lowestPriority = _activeSounds.Values
            .OrderBy(s => s.Priority)
            .ThenBy(s => s.StartTime) // Evict oldest if same priority
            .FirstOrDefault();

        if (lowestPriority != null && lowestPriority.Priority < newSoundPriority)
        {
            _logger.LogDebug("Evicting sound {SoundId} with priority {Priority} for new sound with priority {NewPriority}",
                lowestPriority.Id, lowestPriority.Priority, newSoundPriority);

            _ = StopAsync(lowestPriority.Id); // Fire and forget
            return true;
        }

        return false;
    }

    /// <summary>
    /// Removes finished sounds from the active sounds dictionary.
    /// Note: In a real implementation, this would check sound state from the audio engine.
    /// </summary>
    private void CleanupFinishedSounds()
    {
        // In a real implementation, we'd query the audio engine for finished sounds
        // For now, this is a placeholder that could be enhanced with callbacks
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(SoundEffectPlayer));
        }
    }

    /// <summary>
    /// Releases all resources used by the SoundEffectPlayer.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _logger.LogInformation("Disposing SoundEffectPlayer...");

        // Stop all sounds synchronously
        try
        {
            _audioEngine.StopAllSoundsAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping sounds during disposal");
        }

        _activeSounds.Clear();
        _audioEngine.Dispose();
        _disposed = true;

        _logger.LogInformation("SoundEffectPlayer disposed");
    }

    /// <summary>
    /// Represents a playing sound instance with tracking metadata.
    /// </summary>
    private class SoundInstance
    {
        public int Id { get; set; }
        public int EngineSoundId { get; set; }
        public int Priority { get; set; }
        public float Volume { get; set; }
        public DateTime StartTime { get; set; }
    }
}
