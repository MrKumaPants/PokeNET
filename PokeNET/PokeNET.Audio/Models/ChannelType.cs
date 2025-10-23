namespace PokeNET.Audio.Models;

/// <summary>
/// Types of audio channels for mixing and routing
/// </summary>
public enum ChannelType
{
    /// <summary>
    /// Background music channel
    /// </summary>
    Music,

    /// <summary>
    /// Sound effects channel (full name)
    /// </summary>
    SoundEffects,

    /// <summary>
    /// Sound effects channel (alias for SoundEffects)
    /// </summary>
    SFX = SoundEffects,

    /// <summary>
    /// Voice/dialogue channel
    /// </summary>
    Voice,

    /// <summary>
    /// Ambient sound channel
    /// </summary>
    Ambient,

    /// <summary>
    /// User interface sounds channel
    /// </summary>
    UI,

    /// <summary>
    /// Master output channel
    /// </summary>
    Master
}

/// <summary>
/// Extension methods for ChannelType
/// </summary>
public static class ChannelTypeExtensions
{
    /// <summary>
    /// Gets the default volume for a channel type
    /// </summary>
    public static float GetDefaultVolume(this ChannelType channelType) => channelType switch
    {
        ChannelType.Music => 0.7f,
        ChannelType.SoundEffects => 0.8f,
        ChannelType.Voice => 0.9f,
        ChannelType.Ambient => 0.5f,
        ChannelType.UI => 0.6f,
        ChannelType.Master => 1.0f,
        _ => 0.8f
    };

    /// <summary>
    /// Gets the priority for a channel type (higher = more important)
    /// </summary>
    public static int GetPriority(this ChannelType channelType) => channelType switch
    {
        ChannelType.Voice => 100,
        ChannelType.UI => 90,
        ChannelType.SoundEffects => 80,
        ChannelType.Music => 50,
        ChannelType.Ambient => 40,
        ChannelType.Master => 0,
        _ => 50
    };
}
