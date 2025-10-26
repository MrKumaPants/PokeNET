namespace PokeNET.Audio.Models;

/// <summary>
/// Represents a rhythmic pattern with beats and time signature
/// </summary>
public sealed record Rhythm
{
    /// <summary>
    /// The individual beats in the rhythm pattern
    /// </summary>
    public required IReadOnlyList<Beat> Beats { get; init; }

    /// <summary>
    /// Time signature of the rhythm
    /// </summary>
    public required TimeSignature TimeSignature { get; init; }

    /// <summary>
    /// Total number of beats in the pattern
    /// </summary>
    public float TotalBeats { get; init; }

    /// <summary>
    /// Number of beats per bar
    /// </summary>
    public int BeatsPerBar { get; init; }

    /// <summary>
    /// Beat unit (denominator of time signature)
    /// </summary>
    public int BeatUnit { get; init; }

    /// <summary>
    /// List of note durations for varied patterns
    /// </summary>
    public List<float> NoteDurations { get; init; } = new();

    /// <summary>
    /// Gets the total duration of the rhythm in beats
    /// </summary>
    public float TotalDuration => Beats.Sum(b => b.Duration);

    /// <summary>
    /// Gets the number of measures in the rhythm
    /// </summary>
    public int MeasureCount => (int)Math.Ceiling(TotalDuration / TimeSignature.BeatsPerMeasure);

    /// <summary>
    /// Creates a basic 4/4 rhythm with quarter notes
    /// </summary>
    public static Rhythm CreateBasicRhythm()
    {
        return new Rhythm
        {
            Beats = new[]
            {
                new Beat
                {
                    Duration = 1.0f,
                    Accent = true,
                    Velocity = 1.0f,
                },
                new Beat
                {
                    Duration = 1.0f,
                    Accent = false,
                    Velocity = 0.7f,
                },
                new Beat
                {
                    Duration = 1.0f,
                    Accent = false,
                    Velocity = 0.8f,
                },
                new Beat
                {
                    Duration = 1.0f,
                    Accent = false,
                    Velocity = 0.7f,
                },
            },
            TimeSignature = TimeSignature.CommonTime,
        };
    }

    /// <summary>
    /// Creates a syncopated rhythm pattern
    /// </summary>
    public static Rhythm CreateSyncopatedRhythm()
    {
        return new Rhythm
        {
            Beats = new[]
            {
                new Beat
                {
                    Duration = 0.5f,
                    Accent = true,
                    Velocity = 1.0f,
                },
                new Beat
                {
                    Duration = 0.5f,
                    Accent = false,
                    Velocity = 0.6f,
                },
                new Beat
                {
                    Duration = 1.0f,
                    Accent = false,
                    Velocity = 0.7f,
                },
                new Beat
                {
                    Duration = 0.5f,
                    Accent = true,
                    Velocity = 0.9f,
                },
                new Beat
                {
                    Duration = 0.5f,
                    Accent = false,
                    Velocity = 0.6f,
                },
                new Beat
                {
                    Duration = 1.0f,
                    Accent = false,
                    Velocity = 0.7f,
                },
            },
            TimeSignature = TimeSignature.CommonTime,
        };
    }
}

/// <summary>
/// Represents a single beat in a rhythm pattern
/// </summary>
public sealed record Beat
{
    /// <summary>
    /// Duration of the beat in beats
    /// </summary>
    public required float Duration { get; init; }

    /// <summary>
    /// Whether this beat is accented (emphasized)
    /// </summary>
    public bool Accent { get; init; }

    /// <summary>
    /// Velocity/volume of the beat (0.0 to 1.0)
    /// </summary>
    public float Velocity { get; init; } = 0.8f;

    /// <summary>
    /// Whether this is a rest (no sound)
    /// </summary>
    public bool IsRest { get; init; }
}
