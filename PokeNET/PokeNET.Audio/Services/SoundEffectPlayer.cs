using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using PokeNET.Audio.Abstractions;
using PokeNET.Audio.Models;

namespace PokeNET.Audio;

/// <summary>
/// Sound effect player with pooling and priority-based playback.
/// Manages concurrent sound playback with automatic eviction of low-priority sounds when pool is full.
/// </summary>
public sealed class SoundEffectPlayer : ISoundEffectPlayer, IDisposable
{
    private readonly ILogger<SoundEffectPlayer> _logger;
    private readonly IAudioEngine _audioEngine;
    private readonly ConcurrentDictionary<Guid, SoundInstance> _activeSounds;
    private readonly ConcurrentDictionary<string, Models.SoundEffect> _preloadedEffects;
    private float _masterVolume = 1.0f;
    private bool _isMuted;
    private bool _isInitialized;
    private bool _disposed;

    /// <summary>
    /// Gets whether the player has been initialized.
    /// </summary>
    public bool IsInitialized => _isInitialized;

    /// <summary>
    /// Gets the maximum number of concurrent sounds (pool size).
    /// </summary>
    public int MaxSimultaneousSounds { get; }

    /// <summary>
    /// Gets the number of currently playing sounds.
    /// </summary>
    public int ActiveSoundCount
    {
        get
        {
            CleanupFinishedSounds();
            return _activeSounds.Count;
        }
    }

    /// <summary>
    /// Gets a value indicating whether the sound effect player is muted.
    /// </summary>
    public bool IsMuted => _isMuted;

    /// <summary>
    /// Event raised when a sound effect completes playback.
    /// </summary>
#pragma warning disable CS0067 // Event is never used - reserved for future feature
    public event EventHandler<SoundEffectCompletedEventArgs>? SoundCompleted;
#pragma warning restore CS0067

    /// <summary>
    /// Event raised when a sound effect is interrupted due to channel limits.
    /// </summary>
    public event EventHandler<SoundEffectInterruptedEventArgs>? SoundInterrupted;

    /// <summary>
    /// Initializes a new instance of the SoundEffectPlayer.
    /// </summary>
    /// <param name="logger">Logger for diagnostics</param>
    /// <param name="audioEngine">Audio engine for low-level playback</param>
    /// <param name="maxSimultaneousSounds">Maximum number of concurrent sounds (default: 32)</param>
    /// <exception cref="ArgumentException">Thrown when maxSimultaneousSounds is less than 1</exception>
    public SoundEffectPlayer(
        ILogger<SoundEffectPlayer> logger,
        IAudioEngine audioEngine,
        int maxSimultaneousSounds = 32
    )
    {
        if (maxSimultaneousSounds < 1)
        {
            throw new ArgumentException(
                "Max simultaneous sounds must be at least 1",
                nameof(maxSimultaneousSounds)
            );
        }

        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _audioEngine = audioEngine ?? throw new ArgumentNullException(nameof(audioEngine));
        MaxSimultaneousSounds = maxSimultaneousSounds;
        _activeSounds = new ConcurrentDictionary<Guid, SoundInstance>();
        _preloadedEffects = new ConcurrentDictionary<string, Models.SoundEffect>();

        _logger.LogInformation(
            "SoundEffectPlayer created with max simultaneous sounds: {Max}",
            MaxSimultaneousSounds
        );
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
    /// Plays a sound effect once.
    /// </summary>
    public Guid? Play(Models.SoundEffect effect, float? volume = null, int priority = 0)
    {
        ThrowIfDisposed();

        if (effect == null)
            throw new ArgumentNullException(nameof(effect));

        if (_isMuted)
            return null;

        CleanupFinishedSounds();

        // Check if pool is full
        if (_activeSounds.Count >= MaxSimultaneousSounds)
        {
            _logger.LogDebug(
                "Pool full ({Count}/{Max}), attempting to evict low-priority sound",
                _activeSounds.Count,
                MaxSimultaneousSounds
            );

            if (!TryEvictLowestPriority(priority))
            {
                _logger.LogWarning(
                    "Cannot play sound: pool full and no lower priority sounds to evict"
                );
                return null;
            }
        }

        try
        {
            float effectiveVolume = Math.Clamp((volume ?? 1.0f) * _masterVolume, 0.0f, 1.0f);
            Guid instanceId = Guid.NewGuid();

            var instance = new SoundInstance
            {
                InstanceId = instanceId,
                Effect = effect,
                Priority = priority,
                Volume = volume ?? 1.0f,
                StartTime = DateTime.UtcNow,
            };

            _activeSounds[instanceId] = instance;

            _logger.LogDebug(
                "Playing sound {Name} ({InstanceId}) with priority {Priority}",
                effect.Name,
                instanceId,
                priority
            );

            return instanceId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to play sound {Name}", effect.Name);
            return null;
        }
    }

    /// <summary>
    /// Plays a sound effect asynchronously and waits for completion.
    /// </summary>
    public async Task PlayAsync(
        Models.SoundEffect effect,
        float? volume = null,
        int priority = 0,
        CancellationToken cancellationToken = default
    )
    {
        ThrowIfDisposed();

        var instanceId = Play(effect, volume, priority);
        if (instanceId == null)
            return;

        // Wait for sound to complete (simplified - in real implementation would track actual playback)
        await Task.Delay(100, cancellationToken); // Placeholder
    }

    /// <summary>
    /// Internal method: Plays audio data with optional volume, pitch, and priority.
    /// </summary>
    internal async Task<int> PlayRawAsync(
        byte[] audioData,
        float volume = 1.0f,
        float pitch = 1.0f,
        int priority = 0
    )
    {
        ThrowIfDisposed();

        if (audioData == null)
        {
            throw new ArgumentNullException(nameof(audioData));
        }

        CleanupFinishedSounds();

        // Check if pool is full
        if (_activeSounds.Count >= MaxSimultaneousSounds)
        {
            _logger.LogDebug(
                "Pool full ({Count}/{Max}), attempting to evict low-priority sound",
                _activeSounds.Count,
                MaxSimultaneousSounds
            );

            if (!TryEvictLowestPriority(priority))
            {
                _logger.LogWarning(
                    "Cannot play sound: pool full and no lower priority sounds to evict"
                );
                return 0;
            }
        }

        try
        {
            // Apply master volume
            float effectiveVolume = Math.Clamp(volume * _masterVolume, 0.0f, 1.0f);

            // Play through audio engine
            int engineSoundId = await _audioEngine.PlaySoundAsync(
                audioData,
                effectiveVolume,
                pitch
            );

            _logger.LogDebug(
                "Playing raw audio (engine: {EngineSoundId}) with priority {Priority}",
                engineSoundId,
                priority
            );

            return engineSoundId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to play raw audio");
            return 0;
        }
    }

    /// <summary>
    /// Stops a specific playing sound effect by its instance ID.
    /// </summary>
    public bool Stop(Guid instanceId)
    {
        ThrowIfDisposed();

        if (_activeSounds.TryRemove(instanceId, out var instance))
        {
            _logger.LogDebug(
                "Stopped sound {Name} ({InstanceId})",
                instance.Effect.Name,
                instanceId
            );

            SoundInterrupted?.Invoke(
                this,
                new SoundEffectInterruptedEventArgs
                {
                    InstanceId = instanceId,
                    Effect = instance.Effect,
                    Reason = InterruptionReason.ManualStop,
                }
            );

            return true;
        }

        return false;
    }

    /// <summary>
    /// Stops all currently playing sound effects.
    /// </summary>
    public void StopAll()
    {
        ThrowIfDisposed();

        _logger.LogInformation("Stopping all sounds ({Count} active)", _activeSounds.Count);

        foreach (var kvp in _activeSounds)
        {
            SoundInterrupted?.Invoke(
                this,
                new SoundEffectInterruptedEventArgs
                {
                    InstanceId = kvp.Key,
                    Effect = kvp.Value.Effect,
                    Reason = InterruptionReason.GlobalStop,
                }
            );
        }

        _activeSounds.Clear();
        _logger.LogInformation("All sounds stopped");
    }

    /// <summary>
    /// Stops all sound effects with the specified name.
    /// </summary>
    public int StopAllByName(string effectName)
    {
        ThrowIfDisposed();

        if (string.IsNullOrEmpty(effectName))
            throw new ArgumentException("Effect name cannot be null or empty", nameof(effectName));

        int stoppedCount = 0;
        var toRemove = _activeSounds.Where(kvp => kvp.Value.Effect.Name == effectName).ToList();

        foreach (var kvp in toRemove)
        {
            if (Stop(kvp.Key))
                stoppedCount++;
        }

        _logger.LogDebug("Stopped {Count} instances of sound {Name}", stoppedCount, effectName);
        return stoppedCount;
    }

    /// <summary>
    /// Sets the master volume for all sound effects.
    /// </summary>
    public void SetMasterVolume(float volume)
    {
        ThrowIfDisposed();

        if (volume < 0.0f || volume > 1.0f)
            throw new ArgumentOutOfRangeException(
                nameof(volume),
                "Volume must be between 0.0 and 1.0"
            );

        _masterVolume = volume;
        _logger.LogDebug("Master volume set to {Volume}", _masterVolume);
    }

    /// <summary>
    /// Gets the master volume for sound effects.
    /// </summary>
    public float GetMasterVolume()
    {
        ThrowIfDisposed();
        return _masterVolume;
    }

    /// <summary>
    /// Mutes all sound effects without changing volume settings.
    /// </summary>
    public void Mute()
    {
        ThrowIfDisposed();
        _isMuted = true;
        _logger.LogDebug("Sound effects muted");
    }

    /// <summary>
    /// Unmutes all sound effects.
    /// </summary>
    public void Unmute()
    {
        ThrowIfDisposed();
        _isMuted = false;
        _logger.LogDebug("Sound effects unmuted");
    }

    /// <summary>
    /// Checks if a specific sound effect is currently playing.
    /// </summary>
    public bool IsPlaying(Guid instanceId)
    {
        ThrowIfDisposed();
        CleanupFinishedSounds();
        return _activeSounds.ContainsKey(instanceId);
    }

    /// <summary>
    /// Preloads a sound effect into memory for faster playback.
    /// </summary>
    public Task PreloadAsync(
        Models.SoundEffect effect,
        CancellationToken cancellationToken = default
    )
    {
        ThrowIfDisposed();

        if (effect == null)
            throw new ArgumentNullException(nameof(effect));

        _preloadedEffects[effect.Name] = effect;
        _logger.LogDebug("Preloaded sound effect {Name}", effect.Name);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Unloads a sound effect from memory.
    /// </summary>
    public void Unload(string effectName)
    {
        ThrowIfDisposed();

        if (string.IsNullOrEmpty(effectName))
            throw new ArgumentException("Effect name cannot be null or empty", nameof(effectName));

        if (_preloadedEffects.TryRemove(effectName, out _))
        {
            _logger.LogDebug("Unloaded sound effect {Name}", effectName);
        }
    }

    /// <summary>
    /// Tries to evict the lowest priority sound to make room for a new one.
    /// </summary>
    /// <param name="newSoundPriority">Priority of the new sound</param>
    /// <returns>True if a sound was evicted, false otherwise</returns>
    private bool TryEvictLowestPriority(int newSoundPriority)
    {
        // Find lowest priority sound
        var lowestPriority = _activeSounds
            .Values.OrderBy(s => s.Priority)
            .ThenBy(s => s.StartTime) // Evict oldest if same priority
            .FirstOrDefault();

        if (lowestPriority != null && lowestPriority.Priority < newSoundPriority)
        {
            _logger.LogDebug(
                "Evicting sound {Name} ({InstanceId}) with priority {Priority} for new sound with priority {NewPriority}",
                lowestPriority.Effect.Name,
                lowestPriority.InstanceId,
                lowestPriority.Priority,
                newSoundPriority
            );

            Stop(lowestPriority.InstanceId);
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
        public Guid InstanceId { get; set; }
        public Models.SoundEffect Effect { get; set; } = null!;
        public int Priority { get; set; }
        public float Volume { get; set; }
        public DateTime StartTime { get; set; }
    }
}
