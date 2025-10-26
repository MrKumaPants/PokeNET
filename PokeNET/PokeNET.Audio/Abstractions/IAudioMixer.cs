namespace PokeNET.Audio.Abstractions;

/// <summary>
/// Interface for audio mixing, volume control, and channel management.
/// SOLID PRINCIPLE: Single Responsibility - Handles only mixing and volume control.
/// SOLID PRINCIPLE: Interface Segregation - Focused on mixing concerns only.
/// </summary>
/// <remarks>
/// The audio mixer manages:
/// - Master volume and per-channel volumes
/// - Audio ducking (reducing music volume during SFX)
/// - Channel routing and balance
/// - Real-time DSP effects (optional)
/// </remarks>
public interface IAudioMixer
{
    /// <summary>
    /// Gets or sets the master volume for all audio.
    /// </summary>
    /// <value>Volume level between 0.0 (silent) and 1.0 (full volume).</value>
    float MasterVolume { get; set; }

    /// <summary>
    /// Gets or sets the music channel volume.
    /// </summary>
    /// <value>Volume level between 0.0 and 1.0.</value>
    float MusicVolume { get; set; }

    /// <summary>
    /// Gets or sets the sound effects channel volume.
    /// </summary>
    /// <value>Volume level between 0.0 and 1.0.</value>
    float SoundEffectsVolume { get; set; }

    /// <summary>
    /// Gets or sets the voice/dialogue channel volume.
    /// </summary>
    /// <value>Volume level between 0.0 and 1.0.</value>
    float VoiceVolume { get; set; }

    /// <summary>
    /// Gets a value indicating whether audio ducking is enabled.
    /// </summary>
    bool IsDuckingEnabled { get; }

    /// <summary>
    /// Gets the current ducking level (how much music is reduced during SFX).
    /// </summary>
    float DuckingLevel { get; }

    /// <summary>
    /// Sets the volume for a specific audio channel.
    /// </summary>
    /// <param name="channel">The audio channel to adjust.</param>
    /// <param name="volume">Volume level between 0.0 and 1.0.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when volume is out of valid range.</exception>
    void SetChannelVolume(AudioChannel channel, float volume);

    /// <summary>
    /// Gets the volume for a specific audio channel.
    /// </summary>
    /// <param name="channel">The audio channel to query.</param>
    /// <returns>Volume level between 0.0 and 1.0.</returns>
    float GetChannelVolume(AudioChannel channel);

    /// <summary>
    /// Enables audio ducking to reduce music volume when sound effects play.
    /// </summary>
    /// <param name="duckingLevel">How much to reduce music volume (0.0 = no ducking, 1.0 = full silence).</param>
    /// <param name="fadeTime">How quickly to duck/unduck the audio.</param>
    void EnableDucking(float duckingLevel = 0.5f, TimeSpan? fadeTime = null);

    /// <summary>
    /// Disables audio ducking.
    /// </summary>
    void DisableDucking();

    /// <summary>
    /// Mutes a specific audio channel.
    /// </summary>
    /// <param name="channel">The channel to mute.</param>
    void MuteChannel(AudioChannel channel);

    /// <summary>
    /// Unmutes a specific audio channel.
    /// </summary>
    /// <param name="channel">The channel to unmute.</param>
    void UnmuteChannel(AudioChannel channel);

    /// <summary>
    /// Checks if a specific channel is muted.
    /// </summary>
    /// <param name="channel">The channel to check.</param>
    /// <returns>True if the channel is muted, false otherwise.</returns>
    bool IsChannelMuted(AudioChannel channel);

    /// <summary>
    /// Mutes all audio channels.
    /// </summary>
    void MuteAll();

    /// <summary>
    /// Unmutes all audio channels.
    /// </summary>
    void UnmuteAll();

    /// <summary>
    /// Sets the stereo pan for a channel.
    /// </summary>
    /// <param name="channel">The channel to adjust.</param>
    /// <param name="pan">Pan value (-1.0 = full left, 0.0 = center, 1.0 = full right).</param>
    void SetPan(AudioChannel channel, float pan);

    /// <summary>
    /// Gets the current pan setting for a channel.
    /// </summary>
    /// <param name="channel">The channel to query.</param>
    /// <returns>Pan value between -1.0 and 1.0.</returns>
    float GetPan(AudioChannel channel);

    /// <summary>
    /// Applies a fade effect to a channel.
    /// </summary>
    /// <param name="channel">The channel to fade.</param>
    /// <param name="targetVolume">The target volume to fade to.</param>
    /// <param name="duration">Duration of the fade effect.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>A task representing the fade operation.</returns>
    Task FadeChannelAsync(
        AudioChannel channel,
        float targetVolume,
        TimeSpan duration,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Resets all mixer settings to defaults.
    /// </summary>
    void Reset();

    /// <summary>
    /// Event raised when any volume setting changes.
    /// </summary>
    event EventHandler<VolumeChangedEventArgs>? VolumeChanged;
}

/// <summary>
/// Audio channels for mixing.
/// </summary>
public enum AudioChannel
{
    /// <summary>Master channel (affects all audio).</summary>
    Master,

    /// <summary>Music channel.</summary>
    Music,

    /// <summary>Sound effects channel.</summary>
    SoundEffects,

    /// <summary>Voice/dialogue channel.</summary>
    Voice,

    /// <summary>Ambient sound channel.</summary>
    Ambient,

    /// <summary>UI sound channel.</summary>
    UI,
}

/// <summary>
/// Event arguments for volume changes.
/// </summary>
public class VolumeChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the channel that changed.
    /// </summary>
    public AudioChannel Channel { get; init; }

    /// <summary>
    /// Gets the previous volume level.
    /// </summary>
    public float PreviousVolume { get; init; }

    /// <summary>
    /// Gets the new volume level.
    /// </summary>
    public float NewVolume { get; init; }

    /// <summary>
    /// Gets the timestamp of the change.
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
