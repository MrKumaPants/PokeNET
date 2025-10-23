namespace PokeNET.Audio.Models;

/// <summary>
/// Represents a musical melody with notes, tempo, and time signature
/// </summary>
public sealed record Melody
{
    /// <summary>
    /// The notes that make up the melody
    /// </summary>
    public required IReadOnlyList<Note> Notes { get; init; }

    /// <summary>
    /// Tempo in beats per minute
    /// </summary>
    public required float Tempo { get; init; }

    /// <summary>
    /// Time signature of the melody
    /// </summary>
    public required TimeSignature TimeSignature { get; init; }

    /// <summary>
    /// Scale type used for the melody
    /// </summary>
    public ScaleType Scale { get; init; }

    /// <summary>
    /// Musical key of the melody
    /// </summary>
    public NoteName Key { get; init; }

    /// <summary>
    /// Gets the total duration of the melody in beats
    /// </summary>
    public float TotalDuration => Notes.Sum(n => n.Duration);

    /// <summary>
    /// Gets the total duration of the melody in seconds
    /// </summary>
    public float TotalDurationSeconds => TotalDuration * (60f / Tempo);

    /// <summary>
    /// Gets the number of measures in the melody
    /// </summary>
    public int MeasureCount => (int)Math.Ceiling(TotalDuration / TimeSignature.BeatsPerMeasure);
}

/// <summary>
/// Represents a musical time signature
/// </summary>
public sealed record TimeSignature
{
    /// <summary>
    /// Number of beats per measure (numerator)
    /// </summary>
    public required int BeatsPerMeasure { get; init; }

    /// <summary>
    /// Note value that gets one beat (denominator)
    /// </summary>
    public required int BeatValue { get; init; }

    /// <summary>
    /// Common 4/4 time signature
    /// </summary>
    public static TimeSignature CommonTime => new() { BeatsPerMeasure = 4, BeatValue = 4 };

    /// <summary>
    /// 3/4 waltz time signature
    /// </summary>
    public static TimeSignature WaltzTime => new() { BeatsPerMeasure = 3, BeatValue = 4 };

    /// <summary>
    /// String representation (e.g., "4/4")
    /// </summary>
    public override string ToString() => $"{BeatsPerMeasure}/{BeatValue}";
}
