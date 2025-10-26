namespace PokeNET.Audio.Models;

/// <summary>
/// Represents a short-duration sound effect.
/// SOLID PRINCIPLE: Single Responsibility - Encapsulates sound effect data.
/// </summary>
public class SoundEffect
{
    /// <summary>
    /// Gets or sets the unique identifier for this sound effect.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets or sets the name of the sound effect.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the file path to the sound effect file.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the duration of the sound effect.
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Gets or sets the default volume (0.0 to 1.0).
    /// </summary>
    public float Volume { get; set; } = 1.0f;

    /// <summary>
    /// Gets or sets the default playback priority (higher = more important).
    /// </summary>
    public int Priority { get; set; } = 0;

    /// <summary>
    /// Gets or sets the sound category for grouping.
    /// </summary>
    public SoundCategory Category { get; set; } = SoundCategory.General;

    /// <summary>
    /// Gets or sets a value indicating whether this sound can overlap with itself.
    /// </summary>
    /// <remarks>
    /// If false, playing the sound while it's already playing will stop the previous instance.
    /// </remarks>
    public bool AllowOverlap { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of simultaneous instances allowed.
    /// </summary>
    public int MaxSimultaneousInstances { get; set; } = 3;

    /// <summary>
    /// Gets or sets the cooldown period before this sound can be played again.
    /// </summary>
    public TimeSpan? Cooldown { get; set; }

    /// <summary>
    /// Gets or sets the sample rate of the audio file.
    /// </summary>
    public int SampleRate { get; set; } = 44100;

    /// <summary>
    /// Gets or sets the number of audio channels.
    /// </summary>
    public int Channels { get; set; } = 2;

    /// <summary>
    /// Gets or sets custom metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether this sound is preloaded in memory.
    /// </summary>
    public bool IsPreloaded { get; set; }

    /// <summary>
    /// Gets or sets the date/time when this sound was created or added.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the date/time when this sound was last played.
    /// </summary>
    public DateTime? LastPlayedAt { get; set; }

    /// <summary>
    /// Gets or sets the number of times this sound has been played.
    /// </summary>
    public int PlayCount { get; set; }

    /// <summary>
    /// Creates a shallow copy of the sound effect.
    /// </summary>
    /// <returns>A new SoundEffect instance with copied values.</returns>
    public SoundEffect Clone()
    {
        return new SoundEffect
        {
            Id = Id,
            Name = Name,
            FilePath = FilePath,
            Duration = Duration,
            Volume = Volume,
            Priority = Priority,
            Category = Category,
            AllowOverlap = AllowOverlap,
            MaxSimultaneousInstances = MaxSimultaneousInstances,
            Cooldown = Cooldown,
            SampleRate = SampleRate,
            Channels = Channels,
            Metadata = new Dictionary<string, object>(Metadata),
            IsPreloaded = IsPreloaded,
            CreatedAt = CreatedAt,
            LastPlayedAt = LastPlayedAt,
            PlayCount = PlayCount,
        };
    }

    /// <summary>
    /// Returns a string representation of the sound effect.
    /// </summary>
    /// <returns>String containing the sound effect name.</returns>
    public override string ToString() => Name;
}

/// <summary>
/// Categories for organizing sound effects.
/// </summary>
public enum SoundCategory
{
    /// <summary>General uncategorized sounds.</summary>
    General,

    /// <summary>UI interaction sounds (clicks, hovers).</summary>
    UI,

    /// <summary>Combat-related sounds (attacks, hits).</summary>
    Combat,

    /// <summary>Movement sounds (footsteps, jumps).</summary>
    Movement,

    /// <summary>Environmental sounds (doors, switches).</summary>
    Environment,

    /// <summary>Item-related sounds (pickup, use).</summary>
    Item,

    /// <summary>Character vocalizations (grunts, exclamations).</summary>
    Voice,

    /// <summary>Special effects (explosions, magic).</summary>
    SpecialEffect,

    /// <summary>Pokemon-specific sounds (cries, abilities).</summary>
    Pokemon,

    /// <summary>Battle system sounds.</summary>
    Battle,

    /// <summary>Menu navigation sounds.</summary>
    Menu,
}
