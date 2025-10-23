using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PokeNET.Audio.Abstractions;
using PokeNET.Audio.Configuration;
using PokeNET.Audio.Exceptions;
using PokeNET.Audio.Models;
using SoundEffect = PokeNET.Audio.Models.SoundEffect;

namespace PokeNET.Audio.Services;

/// <summary>
/// Sound effect player supporting WAV/OGG playback with concurrent instance management.
/// SOLID PRINCIPLE: Single Responsibility - Manages sound effect playback only.
/// SOLID PRINCIPLE: Dependency Inversion - Depends on abstractions (ILogger, IOptions).
/// </summary>
public sealed class SoundEffectPlayer : ISoundEffectPlayer
{
    private readonly ILogger<SoundEffectPlayer> _logger;
    private readonly AudioSettings _settings;
    private readonly AudioCache _cache;
    private readonly ConcurrentDictionary<Guid, PlayingSoundInstance> _activeInstances;
    private readonly ConcurrentDictionary<string, SoundEffect> _loadedEffects;
    private readonly SemaphoreSlim _instanceLock;

    private float _masterVolume;
    private bool _isMuted;
    private bool _disposed;

    public int MaxSimultaneousSounds => _settings.MaxConcurrentSounds;

    public int ActiveSoundCount
    {
        get
        {
            ThrowIfDisposed();
            CleanupFinishedInstances();
            return _activeInstances.Count(kvp => kvp.Value.IsPlaying);
        }
    }

    public bool IsMuted
    {
        get
        {
            ThrowIfDisposed();
            return _isMuted;
        }
    }

    public event EventHandler<SoundEffectCompletedEventArgs>? SoundCompleted;
    public event EventHandler<SoundEffectInterruptedEventArgs>? SoundInterrupted;

    public SoundEffectPlayer(
        ILogger<SoundEffectPlayer> logger,
        IOptions<AudioSettings> settings,
        AudioCache cache)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));

        _activeInstances = new ConcurrentDictionary<Guid, PlayingSoundInstance>();
        _loadedEffects = new ConcurrentDictionary<string, SoundEffect>();
        _instanceLock = new SemaphoreSlim(1, 1);
        _masterVolume = _settings.SoundEffectVolume;
        _isMuted = false;

        _logger.LogInformation("SoundEffectPlayer initialized with max concurrent sounds: {MaxSounds}",
            _settings.MaxConcurrentSounds);
    }

    public Guid? Play(SoundEffect effect, float? volume = null, int priority = 0)
    {
        ThrowIfDisposed();

        if (effect == null)
        {
            throw new ArgumentNullException(nameof(effect));
        }

        try
        {
            _instanceLock.Wait();

            _logger.LogDebug("Playing sound effect: {Name}, Volume: {Volume}, Priority: {Priority}",
                effect.Name, volume ?? effect.Volume, priority);

            // Clean up finished instances
            CleanupFinishedInstances();

            // Check if we've reached the max concurrent sounds limit
            if (_activeInstances.Count >= MaxSimultaneousSounds)
            {
                // Try to evict lowest priority sound
                if (!TryEvictLowestPriority(priority))
                {
                    _logger.LogWarning("Cannot play sound effect {Name}: channel limit reached and no lower priority sounds to evict", effect.Name);
                    return null;
                }
            }

            // Create instance
            var instanceId = Guid.NewGuid();
            var effectiveVolume = (volume ?? effect.Volume) * _masterVolume;

            if (_isMuted)
            {
                effectiveVolume = 0f;
            }

            var instance = new PlayingSoundInstance
            {
                Id = instanceId,
                Effect = effect,
                Volume = Math.Clamp(effectiveVolume, 0.0f, 1.0f),
                Priority = priority,
                StartTime = DateTime.UtcNow,
                IsPlaying = true
            };

            // TODO: When MonoGame is available:
            // var soundInstance = effect.CreateInstance();
            // soundInstance.Volume = instance.Volume;
            // soundInstance.Play();
            // instance.RuntimeInstance = soundInstance;

            _activeInstances.TryAdd(instanceId, instance);

            // Update effect statistics
            effect.PlayCount++;
            effect.LastPlayedAt = DateTime.UtcNow;

            _logger.LogInformation("Sound effect started: {Name}, InstanceId: {InstanceId}", effect.Name, instanceId);

            // Start monitoring for completion (TODO: integrate with MonoGame)
            _ = MonitorInstanceAsync(instanceId);

            return instanceId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to play sound effect: {Name}", effect.Name);
            return null;
        }
        finally
        {
            _instanceLock.Release();
        }
    }

    public async Task PlayAsync(SoundEffect effect, float? volume = null, int priority = 0, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (effect == null)
        {
            throw new ArgumentNullException(nameof(effect));
        }

        var instanceId = Play(effect, volume, priority);

        if (instanceId == null)
        {
            _logger.LogWarning("PlayAsync failed for effect {Name}", effect.Name);
            return;
        }

        // Wait for completion
        while (IsPlaying(instanceId.Value) && !cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(50, cancellationToken);
        }
    }

    public bool Stop(Guid instanceId)
    {
        ThrowIfDisposed();

        if (_activeInstances.TryRemove(instanceId, out var instance))
        {
            _logger.LogDebug("Stopping sound instance: {InstanceId}", instanceId);

            // TODO: When MonoGame is available:
            // if (instance.RuntimeInstance != null)
            // {
            //     instance.RuntimeInstance.Stop();
            //     instance.RuntimeInstance.Dispose();
            // }

            instance.IsPlaying = false;

            // Raise interruption event
            SoundInterrupted?.Invoke(this, new SoundEffectInterruptedEventArgs
            {
                InstanceId = instanceId,
                Effect = instance.Effect,
                Reason = InterruptionReason.ManualStop
            });

            _logger.LogInformation("Sound instance stopped: {InstanceId}", instanceId);
            return true;
        }

        return false;
    }

    public void StopAll()
    {
        ThrowIfDisposed();

        _logger.LogInformation("Stopping all sound effects");

        foreach (var instanceId in _activeInstances.Keys.ToList())
        {
            if (_activeInstances.TryRemove(instanceId, out var instance))
            {
                instance.IsPlaying = false;

                // Raise interruption event
                SoundInterrupted?.Invoke(this, new SoundEffectInterruptedEventArgs
                {
                    InstanceId = instanceId,
                    Effect = instance.Effect,
                    Reason = InterruptionReason.GlobalStop
                });
            }
        }
    }

    public int StopAllByName(string effectName)
    {
        ThrowIfDisposed();

        if (string.IsNullOrWhiteSpace(effectName))
        {
            throw new ArgumentException("Effect name cannot be null or whitespace", nameof(effectName));
        }

        _logger.LogInformation("Stopping all instances of sound effect: {EffectName}", effectName);

        var stoppedCount = 0;
        foreach (var kvp in _activeInstances.ToList())
        {
            if (kvp.Value.Effect.Name == effectName)
            {
                if (Stop(kvp.Key))
                {
                    stoppedCount++;
                }
            }
        }

        _logger.LogInformation("Stopped {Count} instances of {EffectName}", stoppedCount, effectName);
        return stoppedCount;
    }

    public void SetMasterVolume(float volume)
    {
        ThrowIfDisposed();

        if (volume < 0.0f || volume > 1.0f)
        {
            throw new ArgumentOutOfRangeException(nameof(volume), "Volume must be between 0.0 and 1.0");
        }

        _masterVolume = volume;

        // Apply volume to all active instances
        foreach (var instance in _activeInstances.Values)
        {
            UpdateInstanceVolume(instance);
        }

        _logger.LogDebug("Master sound effect volume set to {Volume}", _masterVolume);
    }

    public float GetMasterVolume()
    {
        ThrowIfDisposed();
        return _masterVolume;
    }

    public void Mute()
    {
        ThrowIfDisposed();

        _isMuted = true;

        // Mute all active instances
        foreach (var instance in _activeInstances.Values)
        {
            UpdateInstanceVolume(instance);
        }

        _logger.LogInformation("Sound effects muted");
    }

    public void Unmute()
    {
        ThrowIfDisposed();

        _isMuted = false;

        // Restore volume to all active instances
        foreach (var instance in _activeInstances.Values)
        {
            UpdateInstanceVolume(instance);
        }

        _logger.LogInformation("Sound effects unmuted");
    }

    public bool IsPlaying(Guid instanceId)
    {
        ThrowIfDisposed();

        if (_activeInstances.TryGetValue(instanceId, out var instance))
        {
            // TODO: When MonoGame is available:
            // if (instance.RuntimeInstance != null)
            // {
            //     return instance.RuntimeInstance.State == SoundState.Playing;
            // }

            return instance.IsPlaying;
        }

        return false;
    }

    public async Task PreloadAsync(SoundEffect effect, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (effect == null)
        {
            throw new ArgumentNullException(nameof(effect));
        }

        if (effect.IsPreloaded)
        {
            _logger.LogDebug("Sound effect already preloaded: {Name}", effect.Name);
            return;
        }

        _logger.LogInformation("Preloading sound effect: {Name}", effect.Name);

        try
        {
            // TODO: When MonoGame is available:
            // using var stream = File.OpenRead(effect.FilePath);
            // var soundEffect = await Task.Run(() => MonoGame.SoundEffect.FromStream(stream), cancellationToken);
            // _loadedEffects.TryAdd(effect.Name, effect);

            await Task.CompletedTask; // Placeholder for now

            effect.IsPreloaded = true;
            _loadedEffects.TryAdd(effect.Name, effect);

            _logger.LogInformation("Sound effect preloaded: {Name}", effect.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to preload sound effect: {Name}", effect.Name);
            throw new AudioLoadException(effect.FilePath, ex);
        }
    }

    public void Unload(string effectName)
    {
        ThrowIfDisposed();

        if (string.IsNullOrWhiteSpace(effectName))
        {
            throw new ArgumentException("Effect name cannot be null or whitespace", nameof(effectName));
        }

        _logger.LogInformation("Unloading sound effect: {Name}", effectName);

        // Stop all instances of this effect
        StopAllByName(effectName);

        // Remove from loaded effects
        if (_loadedEffects.TryRemove(effectName, out var effect))
        {
            // TODO: When MonoGame is available:
            // effect.RuntimeEffect?.Dispose();

            effect.IsPreloaded = false;
            _logger.LogInformation("Sound effect unloaded: {Name}", effectName);
        }
    }

    /// <summary>
    /// Monitors a sound instance for completion and raises events.
    /// </summary>
    private async Task MonitorInstanceAsync(Guid instanceId)
    {
        try
        {
            // TODO: When MonoGame is available, this will monitor the actual runtime instance
            // For now, simulate a duration-based completion
            if (_activeInstances.TryGetValue(instanceId, out var instance))
            {
                await Task.Delay(instance.Effect.Duration);

                if (_activeInstances.TryRemove(instanceId, out var completedInstance))
                {
                    completedInstance.IsPlaying = false;

                    // Raise completion event
                    SoundCompleted?.Invoke(this, new SoundEffectCompletedEventArgs
                    {
                        InstanceId = instanceId,
                        Effect = completedInstance.Effect
                    });

                    _logger.LogDebug("Sound instance completed: {InstanceId}", instanceId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error monitoring sound instance {InstanceId}", instanceId);
        }
    }

    /// <summary>
    /// Updates the volume of a sound instance.
    /// </summary>
    private void UpdateInstanceVolume(PlayingSoundInstance instance)
    {
        var effectiveVolume = instance.Volume * _masterVolume;

        if (_isMuted)
        {
            effectiveVolume = 0f;
        }

        // TODO: When MonoGame is available:
        // if (instance.RuntimeInstance != null)
        // {
        //     instance.RuntimeInstance.Volume = effectiveVolume;
        // }
    }

    /// <summary>
    /// Cleans up finished sound instances.
    /// </summary>
    private void CleanupFinishedInstances()
    {
        var finishedInstances = _activeInstances
            .Where(kvp => !kvp.Value.IsPlaying)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var instanceId in finishedInstances)
        {
            if (_activeInstances.TryRemove(instanceId, out var instance))
            {
                // TODO: When MonoGame is available:
                // instance.RuntimeInstance?.Dispose();

                _logger.LogDebug("Cleaned up finished sound instance: {InstanceId}", instanceId);
            }
        }
    }

    /// <summary>
    /// Tries to evict the lowest priority sound to make room for a new one.
    /// </summary>
    private bool TryEvictLowestPriority(int newSoundPriority)
    {
        var lowestPriorityInstance = _activeInstances
            .Where(kvp => kvp.Value.Priority < newSoundPriority)
            .OrderBy(kvp => kvp.Value.Priority)
            .ThenBy(kvp => kvp.Value.StartTime)
            .FirstOrDefault();

        if (lowestPriorityInstance.Key != Guid.Empty)
        {
            _logger.LogDebug("Evicting low-priority sound instance: {InstanceId}, Priority: {Priority}",
                lowestPriorityInstance.Key, lowestPriorityInstance.Value.Priority);

            if (_activeInstances.TryRemove(lowestPriorityInstance.Key, out var instance))
            {
                instance.IsPlaying = false;

                // Raise interruption event
                SoundInterrupted?.Invoke(this, new SoundEffectInterruptedEventArgs
                {
                    InstanceId = lowestPriorityInstance.Key,
                    Effect = instance.Effect,
                    Reason = InterruptionReason.ChannelLimitReached
                });

                return true;
            }
        }

        return false;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(SoundEffectPlayer));
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        StopAll();
        _instanceLock.Dispose();

        // Dispose all loaded effects
        foreach (var effect in _loadedEffects.Values)
        {
            // TODO: When MonoGame is available:
            // effect.RuntimeEffect?.Dispose();
        }

        _loadedEffects.Clear();
        _disposed = true;

        _logger.LogInformation("SoundEffectPlayer disposed");
    }
}

/// <summary>
/// Represents a currently playing sound instance.
/// </summary>
internal class PlayingSoundInstance
{
    public Guid Id { get; set; }
    public SoundEffect Effect { get; set; } = null!;
    public float Volume { get; set; }
    public int Priority { get; set; }
    public DateTime StartTime { get; set; }
    public bool IsPlaying { get; set; }
    public object? RuntimeInstance { get; set; } // Will be MonoGame.SoundEffectInstance
}
