namespace PokeNET.Audio.Models;

/// <summary>
/// Represents a music or audio track with metadata.
/// SOLID PRINCIPLE: Single Responsibility - Encapsulates track data and metadata.
/// </summary>
public class AudioTrack
{
    /// <summary>
    /// Gets or sets the unique identifier for this track.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets or sets the display name of the track.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the file path to the audio file.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the duration of the track.
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Gets or sets the artist or composer name.
    /// </summary>
    public string? Artist { get; set; }

    /// <summary>
    /// Gets or sets the album name.
    /// </summary>
    public string? Album { get; set; }

    /// <summary>
    /// Gets or sets the genre.
    /// </summary>
    public string? Genre { get; set; }

    /// <summary>
    /// Gets or sets the default volume for this track (0.0 to 1.0).
    /// </summary>
    public float Volume { get; set; } = 1.0f;

    /// <summary>
    /// Gets or sets a value indicating whether this track should loop by default.
    /// </summary>
    public bool Loop { get; set; }

    /// <summary>
    /// Gets or sets the track type (music, ambient, voice, etc.).
    /// </summary>
    public TrackType Type { get; set; } = TrackType.Music;

    /// <summary>
    /// Gets or sets the mood or emotion associated with this track.
    /// </summary>
    public string? Mood { get; set; }

    /// <summary>
    /// Gets or sets the energy level (0.0 = calm, 1.0 = intense).
    /// </summary>
    public float Energy { get; set; } = 0.5f;

    /// <summary>
    /// Gets or sets custom metadata for this track.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether this track was procedurally generated.
    /// </summary>
    public bool IsProcedurallyGenerated { get; set; }

    /// <summary>
    /// Gets or sets the sample rate of the audio file.
    /// </summary>
    public int SampleRate { get; set; } = 44100;

    /// <summary>
    /// Gets or sets the number of audio channels (1 = mono, 2 = stereo).
    /// </summary>
    public int Channels { get; set; } = 2;

    /// <summary>
    /// Gets or sets the bit depth.
    /// </summary>
    public int BitDepth { get; set; } = 16;

    /// <summary>
    /// Gets or sets the date/time when this track was created or added.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the date/time when this track was last played.
    /// </summary>
    public DateTime? LastPlayedAt { get; set; }

    /// <summary>
    /// Gets or sets the number of times this track has been played.
    /// </summary>
    public int PlayCount { get; set; }

    /// <summary>
    /// Creates a shallow copy of the audio track.
    /// </summary>
    /// <returns>A new AudioTrack instance with copied values.</returns>
    public AudioTrack Clone()
    {
        return new AudioTrack
        {
            Id = Id,
            Name = Name,
            FilePath = FilePath,
            Duration = Duration,
            Artist = Artist,
            Album = Album,
            Genre = Genre,
            Volume = Volume,
            Loop = Loop,
            Type = Type,
            Mood = Mood,
            Energy = Energy,
            Metadata = new Dictionary<string, object>(Metadata),
            IsProcedurallyGenerated = IsProcedurallyGenerated,
            SampleRate = SampleRate,
            Channels = Channels,
            BitDepth = BitDepth,
            CreatedAt = CreatedAt,
            LastPlayedAt = LastPlayedAt,
            PlayCount = PlayCount
        };
    }

    /// <summary>
    /// Returns a string representation of the track.
    /// </summary>
    /// <returns>String containing track name and artist.</returns>
    public override string ToString()
    {
        return string.IsNullOrEmpty(Artist)
            ? Name
            : $"{Name} - {Artist}";
    }
}

/// <summary>
/// Types of audio tracks.
/// </summary>
public enum TrackType
{
    /// <summary>Background music.</summary>
    Music,

    /// <summary>Ambient sound.</summary>
    Ambient,

    /// <summary>Voice or dialogue.</summary>
    Voice,

    /// <summary>Sound effect.</summary>
    SoundEffect,

    /// <summary>UI sound.</summary>
    UI,

    /// <summary>Procedurally generated music.</summary>
    Procedural
}
