using System.Collections.Concurrent;
using PokeNET.Audio.Abstractions;

namespace PokeNET.Audio.Services;

/// <summary>
/// Mock implementation of IAudioEngine for testing purposes.
/// Simulates audio playback without actual sound output.
/// </summary>
public sealed class MockAudioEngine : IAudioEngine
{
    private readonly ConcurrentDictionary<int, SoundState> _activeSounds = new();
    private int _nextSoundId;
    private bool _initialized;
    private bool _disposed;

    /// <summary>
    /// Gets the collection of currently active sounds.
    /// </summary>
    public IReadOnlyDictionary<int, SoundState> ActiveSounds => _activeSounds;

    /// <inheritdoc />
    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _initialized = true;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<int> PlaySoundAsync(byte[] audioData, float volume, float pitch, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(audioData);
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!_initialized)
        {
            throw new InvalidOperationException("Audio engine must be initialized before playing sounds.");
        }

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(audioData.Length);
        ArgumentOutOfRangeException.ThrowIfLessThan(volume, 0f);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(volume, 1f);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(pitch, 0f);

        var soundId = Interlocked.Increment(ref _nextSoundId);
        var state = new SoundState(audioData.Length, volume, pitch);

        _activeSounds[soundId] = state;

        return Task.FromResult(soundId);
    }

    /// <inheritdoc />
    public Task StopSoundAsync(int soundId)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _activeSounds.TryRemove(soundId, out _);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAllSoundsAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _activeSounds.Clear();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SetSoundVolumeAsync(int soundId, float volume)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(volume, 0f);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(volume, 1f);
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_activeSounds.TryGetValue(soundId, out var state))
        {
            _activeSounds[soundId] = state with { Volume = volume };
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _activeSounds.Clear();
    }

    /// <summary>
    /// Represents the state of a playing sound.
    /// </summary>
    public sealed record SoundState(int DataLength, float Volume, float Pitch);
}
