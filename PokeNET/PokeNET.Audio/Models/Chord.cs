namespace PokeNET.Audio.Models;

/// <summary>
/// Represents a musical chord with root note and type
/// </summary>
public sealed record Chord
{
    /// <summary>
    /// The root note of the chord
    /// </summary>
    public required NoteName RootNote { get; init; }

    /// <summary>
    /// Alias for RootNote to match test expectations
    /// </summary>
    public NoteName Root => RootNote;

    /// <summary>
    /// The type/quality of the chord
    /// </summary>
    public required ChordType ChordType { get; init; }

    /// <summary>
    /// The octave for the root note
    /// </summary>
    public int Octave { get; init; } = 4;

    /// <summary>
    /// Scale degree of the chord (1-7)
    /// </summary>
    public int Degree { get; init; }

    /// <summary>
    /// Whether this is a diatonic chord in the key
    /// </summary>
    public bool IsDiatonic { get; init; }

    /// <summary>
    /// Gets the notes that make up this chord
    /// </summary>
    public IReadOnlyList<Note> Notes => GenerateNotes();

    private List<Note> GenerateNotes()
    {
        var notes = new List<Note>();
        var intervals = GetIntervalsForChordType();

        foreach (var interval in intervals)
        {
            var noteValue = ((int)RootNote + interval) % 12;
            var octaveOffset = ((int)RootNote + interval) / 12;

            notes.Add(
                new Note
                {
                    NoteName = (NoteName)noteValue,
                    Octave = Octave + octaveOffset,
                    Duration = 1.0f,
                    Velocity = 0.7f,
                }
            );
        }

        return notes;
    }

    private int[] GetIntervalsForChordType() =>
        ChordType switch
        {
            ChordType.Major => [0, 4, 7], // Root, Major 3rd, Perfect 5th
            ChordType.Minor => [0, 3, 7], // Root, Minor 3rd, Perfect 5th
            ChordType.Diminished => [0, 3, 6], // Root, Minor 3rd, Diminished 5th
            ChordType.Augmented => [0, 4, 8], // Root, Major 3rd, Augmented 5th
            ChordType.Major7 => [0, 4, 7, 11], // Major + Major 7th
            ChordType.Minor7 => [0, 3, 7, 10], // Minor + Minor 7th
            ChordType.Dominant7 => [0, 4, 7, 10], // Major + Minor 7th
            ChordType.Diminished7 => [0, 3, 6, 9], // Diminished + Diminished 7th
            ChordType.Sus2 => [0, 2, 7], // Root, Major 2nd, Perfect 5th
            ChordType.Sus4 => [0, 5, 7], // Root, Perfect 4th, Perfect 5th
            ChordType.Major9 => [0, 4, 7, 11, 14], // Major7 + Major 9th
            ChordType.Minor9 => [0, 3, 7, 10, 14], // Minor7 + Major 9th
            ChordType.Dominant9 => [0, 4, 7, 10, 14], // Dominant7 + Major 9th
            ChordType.Add9 => [0, 4, 7, 14], // Major + Major 9th (no 7th)
            ChordType.Major6 => [0, 4, 7, 9], // Major + Major 6th
            ChordType.Minor6 => [0, 3, 7, 9], // Minor + Major 6th
            _ => [0],
        };

    /// <summary>
    /// String representation of the chord (e.g., "Cmaj", "Dmin")
    /// </summary>
    public override string ToString() => $"{RootNote}{GetChordSuffix()}";

    private string GetChordSuffix() =>
        ChordType switch
        {
            ChordType.Major => "maj",
            ChordType.Minor => "min",
            ChordType.Diminished => "dim",
            ChordType.Augmented => "aug",
            ChordType.Major7 => "maj7",
            ChordType.Minor7 => "min7",
            ChordType.Dominant7 => "7",
            ChordType.Diminished7 => "dim7",
            ChordType.Sus2 => "sus2",
            ChordType.Sus4 => "sus4",
            ChordType.Major9 => "maj9",
            ChordType.Minor9 => "min9",
            ChordType.Dominant9 => "9",
            ChordType.Add9 => "add9",
            ChordType.Major6 => "6",
            ChordType.Minor6 => "min6",
            _ => "",
        };

    /// <summary>
    /// Transposes the chord by a specified number of semitones.
    /// </summary>
    /// <param name="semitones">Number of semitones to transpose.</param>
    /// <returns>A new Chord transposed by the specified interval.</returns>
    public Chord Transpose(int semitones)
    {
        var newRoot = (NoteName)(((int)RootNote + semitones + 12) % 12);
        return this with { RootNote = newRoot };
    }

    /// <summary>
    /// Creates an inversion of this chord (shifts lowest note up an octave).
    /// </summary>
    /// <param name="inversion">Inversion number (1 = first inversion, 2 = second, etc.).</param>
    /// <returns>A new Chord in the specified inversion.</returns>
    public Chord GetInversion(int inversion)
    {
        // For now, this returns the same chord. Full implementation would modify note order.
        // This is a placeholder for future enhancement.
        return this;
    }
}
