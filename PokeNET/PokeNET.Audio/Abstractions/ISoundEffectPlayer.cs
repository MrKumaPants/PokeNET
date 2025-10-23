using PokeNET.Audio.Models;
using SoundEffect = PokeNET.Audio.Models.SoundEffect;

namespace PokeNET.Audio.Abstractions;

/// <summary>
/// Specialized interface for short-duration sound effect playback.
/// SOLID PRINCIPLE: Single Responsibility - Handles only sound effect features.
/// SOLID PRINCIPLE: Interface Segregation - Separate from music to keep interfaces focused.
/// </summary>
/// <remarks>
/// Sound effects are characterized by:
/// - Short duration (typically under 5 seconds)
/// - Fire-and-forget playback model
/// - Support for multiple simultaneous sounds
/// - Priority-based playback when channel limits are reached
/// - No looping or crossfading (use IMusicPlayer for that)
/// </remarks>
public interface ISoundEffectPlayer
{
    /// <summary>
    /// Gets the maximum number of simultaneous sound effects.
    /// </summary>
    int MaxSimultaneousSounds { get; }

    /// <summary>
    /// Gets the number of currently playing sound effects.
    /// </summary>
    int ActiveSoundCount { get; }

    /// <summary>
    /// Gets a value indicating whether the sound effect player is muted.
    /// </summary>
    bool IsMuted { get; }

    /// <summary>
    /// Plays a sound effect once.
    /// </summary>
    /// <param name="effect">The sound effect to play.</param>
    /// <param name="volume">Volume level between 0.0 and 1.0 (optional, defaults to effect's volume).</param>
    /// <param name="priority">Playback priority (higher values have precedence).</param>
    /// <returns>A unique identifier for this sound instance, or null if playback failed.</returns>
    /// <remarks>
    /// If the maximum number of simultaneous sounds is reached, the lowest priority
    /// sound will be stopped to make room for the new sound if it has higher priority.
    /// </remarks>
    Guid? Play(SoundEffect effect, float? volume = null, int priority = 0);

    /// <summary>
    /// Plays a sound effect asynchronously and waits for completion.
    /// </summary>
    /// <param name="effect">The sound effect to play.</param>
    /// <param name="volume">Volume level between 0.0 and 1.0 (optional).</param>
    /// <param name="priority">Playback priority.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>A task representing the playback operation.</returns>
    Task PlayAsync(SoundEffect effect, float? volume = null, int priority = 0, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops a specific playing sound effect by its instance ID.
    /// </summary>
    /// <param name="instanceId">The unique identifier returned by Play().</param>
    /// <returns>True if the sound was stopped, false if not found.</returns>
    bool Stop(Guid instanceId);

    /// <summary>
    /// Stops all currently playing sound effects.
    /// </summary>
    void StopAll();

    /// <summary>
    /// Stops all sound effects with the specified name.
    /// </summary>
    /// <param name="effectName">The name of the sound effect to stop.</param>
    /// <returns>The number of sounds stopped.</returns>
    int StopAllByName(string effectName);

    /// <summary>
    /// Sets the master volume for all sound effects.
    /// </summary>
    /// <param name="volume">Volume level between 0.0 (silent) and 1.0 (full volume).</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when volume is not in valid range.</exception>
    void SetMasterVolume(float volume);

    /// <summary>
    /// Gets the master volume for sound effects.
    /// </summary>
    /// <returns>Volume level between 0.0 and 1.0.</returns>
    float GetMasterVolume();

    /// <summary>
    /// Mutes all sound effects without changing volume settings.
    /// </summary>
    void Mute();

    /// <summary>
    /// Unmutes all sound effects.
    /// </summary>
    void Unmute();

    /// <summary>
    /// Checks if a specific sound effect is currently playing.
    /// </summary>
    /// <param name="instanceId">The unique identifier of the sound instance.</param>
    /// <returns>True if the sound is playing, false otherwise.</returns>
    bool IsPlaying(Guid instanceId);

    /// <summary>
    /// Preloads a sound effect into memory for faster playback.
    /// </summary>
    /// <param name="effect">The sound effect to preload.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>A task representing the preload operation.</returns>
    Task PreloadAsync(SoundEffect effect, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unloads a sound effect from memory.
    /// </summary>
    /// <param name="effectName">The name of the sound effect to unload.</param>
    void Unload(string effectName);

    /// <summary>
    /// Event raised when a sound effect completes playback.
    /// </summary>
    event EventHandler<SoundEffectCompletedEventArgs>? SoundCompleted;

    /// <summary>
    /// Event raised when a sound effect is interrupted due to channel limits.
    /// </summary>
    event EventHandler<SoundEffectInterruptedEventArgs>? SoundInterrupted;
}

/// <summary>
/// Event arguments for sound effect completion.
/// </summary>
public class SoundEffectCompletedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the instance ID of the completed sound.
    /// </summary>
    public Guid InstanceId { get; init; }

    /// <summary>
    /// Gets the sound effect that completed.
    /// </summary>
    public SoundEffect Effect { get; init; } = null!;

    /// <summary>
    /// Gets the timestamp when the sound completed.
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Event arguments for sound effect interruption.
/// </summary>
public class SoundEffectInterruptedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the instance ID of the interrupted sound.
    /// </summary>
    public Guid InstanceId { get; init; }

    /// <summary>
    /// Gets the sound effect that was interrupted.
    /// </summary>
    public SoundEffect Effect { get; init; } = null!;

    /// <summary>
    /// Gets the reason for interruption.
    /// </summary>
    public InterruptionReason Reason { get; init; }

    /// <summary>
    /// Gets the timestamp when the interruption occurred.
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Reasons why a sound effect was interrupted.
/// </summary>
public enum InterruptionReason
{
    /// <summary>Channel limit reached and lower priority sound was stopped.</summary>
    ChannelLimitReached,

    /// <summary>Sound was manually stopped by user code.</summary>
    ManualStop,

    /// <summary>All sounds were stopped via StopAll().</summary>
    GlobalStop,

    /// <summary>System resource exhaustion.</summary>
    ResourceExhaustion,

    /// <summary>An error occurred during playback.</summary>
    Error
}
