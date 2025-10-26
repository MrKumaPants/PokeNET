namespace PokeNET.Audio.Models;

/// <summary>
/// Represents a sequence of chords with timing information
/// </summary>
public sealed record ChordProgression
{
    /// <summary>
    /// The chords in the progression
    /// </summary>
    public required IReadOnlyList<Chord> Chords { get; init; }

    /// <summary>
    /// Tempo in beats per minute
    /// </summary>
    public required float Tempo { get; init; }

    /// <summary>
    /// Time signature of the progression
    /// </summary>
    public required TimeSignature TimeSignature { get; init; }

    /// <summary>
    /// Musical key of the progression
    /// </summary>
    public NoteName Key { get; init; }

    /// <summary>
    /// Roman numeral pattern (e.g., "I-IV-V-I")
    /// </summary>
    public string Pattern { get; init; } = string.Empty;

    /// <summary>
    /// Duration each chord should be held (in beats)
    /// </summary>
    public float ChordDuration { get; init; } = 4.0f;

    /// <summary>
    /// Gets the total duration of the progression in beats
    /// </summary>
    public float TotalDuration => Chords.Count * ChordDuration;

    /// <summary>
    /// Gets the total duration of the progression in seconds
    /// </summary>
    public float TotalDurationSeconds => TotalDuration * (60f / Tempo);

    /// <summary>
    /// Gets the number of measures in the progression
    /// </summary>
    public int MeasureCount => (int)Math.Ceiling(TotalDuration / TimeSignature.BeatsPerMeasure);

    /// <summary>
    /// Creates a common I-IV-V-I progression in the specified key
    /// </summary>
    public static ChordProgression CreateCommonProgression(NoteName key, float tempo = 120f)
    {
        return new ChordProgression
        {
            Chords = new[]
            {
                new Chord { RootNote = key, ChordType = ChordType.Major }, // I
                new Chord
                {
                    RootNote = (NoteName)(((int)key + 5) % 12),
                    ChordType = ChordType.Major,
                }, // IV
                new Chord
                {
                    RootNote = (NoteName)(((int)key + 7) % 12),
                    ChordType = ChordType.Major,
                }, // V
                new Chord { RootNote = key, ChordType = ChordType.Major }, // I
            },
            Tempo = tempo,
            TimeSignature = TimeSignature.CommonTime,
            ChordDuration = 4.0f,
        };
    }

    /// <summary>
    /// Creates a minor progression (i-iv-v-i) in the specified key
    /// </summary>
    public static ChordProgression CreateMinorProgression(NoteName key, float tempo = 120f)
    {
        return new ChordProgression
        {
            Chords = new[]
            {
                new Chord { RootNote = key, ChordType = ChordType.Minor }, // i
                new Chord
                {
                    RootNote = (NoteName)(((int)key + 5) % 12),
                    ChordType = ChordType.Minor,
                }, // iv
                new Chord
                {
                    RootNote = (NoteName)(((int)key + 7) % 12),
                    ChordType = ChordType.Minor,
                }, // v
                new Chord { RootNote = key, ChordType = ChordType.Minor }, // i
            },
            Tempo = tempo,
            TimeSignature = TimeSignature.CommonTime,
            ChordDuration = 4.0f,
        };
    }

    /// <summary>
    /// String representation of the progression
    /// </summary>
    public override string ToString() => string.Join(" - ", Chords.Select(c => c.ToString()));
}
