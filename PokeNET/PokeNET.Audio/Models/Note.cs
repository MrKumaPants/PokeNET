namespace PokeNET.Audio.Models;

/// <summary>
/// Represents a musical note with pitch, duration, and velocity.
/// SOLID PRINCIPLE: Single Responsibility - Represents only note data.
/// SOLID PRINCIPLE: Immutability - Uses record for value semantics.
/// </summary>
/// <remarks>
/// This immutable record represents a single musical note with all properties
/// needed for MIDI playback and procedural music generation. Includes helper
/// methods for MIDI conversion and frequency calculation using A440 tuning.
/// </remarks>
public sealed record Note
{
    /// <summary>
    /// The name of the note (C, D, E, F, G, A, B with sharps).
    /// </summary>
    public required NoteName NoteName { get; init; }

    /// <summary>
    /// The octave of the note (typically 0-8, middle C is C4).
    /// </summary>
    public required int Octave { get; init; }

    /// <summary>
    /// Duration of the note in beats (quarter note = 1.0).
    /// </summary>
    public required float Duration { get; init; }

    /// <summary>
    /// Velocity/volume of the note (0.0 to 1.0, where 1.0 is maximum volume).
    /// Default: 0.8 for natural, slightly soft playing.
    /// </summary>
    public float Velocity { get; init; } = 0.8f;

    /// <summary>
    /// Gets the MIDI note number for this note (0-127).
    /// Middle C (C4) = 60, A4 (concert pitch) = 69.
    /// </summary>
    public int MidiNoteNumber => (Octave + 1) * 12 + (int)NoteName;

    /// <summary>
    /// Alias for MidiNoteNumber to match test expectations
    /// </summary>
    public int NoteNumber => MidiNoteNumber;

    /// <summary>
    /// Gets the frequency in Hz for this note using A440 tuning standard.
    /// </summary>
    public float Frequency => 440f * MathF.Pow(2f, (MidiNoteNumber - 69) / 12f);

    /// <summary>
    /// Determines if this note is accented (velocity >= 0.9).
    /// </summary>
    public bool IsAccented => Velocity >= 0.9f;

    /// <summary>
    /// Determines if this note is within a valid MIDI range (0-127).
    /// </summary>
    public bool IsValidMidiNote => MidiNoteNumber >= 0 && MidiNoteNumber <= 127;

    /// <summary>
    /// Creates a note transposed by a specified number of semitones.
    /// </summary>
    /// <param name="semitones">Number of semitones to transpose (positive = up, negative = down).</param>
    /// <returns>A new Note instance transposed by the specified interval.</returns>
    public Note Transpose(int semitones)
    {
        var newMidiNote = MidiNoteNumber + semitones;
        var newOctave = (newMidiNote / 12) - 1;
        var newNoteName = (NoteName)(newMidiNote % 12);

        return this with
        {
            NoteName = newNoteName,
            Octave = newOctave,
        };
    }

    /// <summary>
    /// Creates a copy of this note with modified velocity.
    /// </summary>
    /// <param name="velocity">New velocity value (0.0 to 1.0).</param>
    /// <returns>A new Note instance with the specified velocity.</returns>
    public Note WithVelocity(float velocity) =>
        this with
        {
            Velocity = Math.Clamp(velocity, 0f, 1f),
        };

    /// <summary>
    /// Creates a copy of this note with modified duration.
    /// </summary>
    /// <param name="duration">New duration in beats.</param>
    /// <returns>A new Note instance with the specified duration.</returns>
    public Note WithDuration(float duration) => this with { Duration = Math.Max(0f, duration) };

    /// <summary>
    /// String representation of the note (e.g., "C4", "F#5").
    /// </summary>
    public override string ToString() => $"{NoteName.ToDisplayString()}{Octave}";
}

/// <summary>
/// Musical note names with semitone offsets from C.
/// </summary>
public enum NoteName
{
    /// <summary>C natural.</summary>
    C = 0,

    /// <summary>C sharp / D flat.</summary>
    CSharp = 1,

    /// <summary>D natural.</summary>
    D = 2,

    /// <summary>D sharp / E flat.</summary>
    DSharp = 3,

    /// <summary>E natural.</summary>
    E = 4,

    /// <summary>F natural.</summary>
    F = 5,

    /// <summary>F sharp / G flat.</summary>
    FSharp = 6,

    /// <summary>G natural.</summary>
    G = 7,

    /// <summary>G sharp / A flat.</summary>
    GSharp = 8,

    /// <summary>A natural (concert pitch A4 = 440 Hz).</summary>
    A = 9,

    /// <summary>A sharp / B flat.</summary>
    ASharp = 10,

    /// <summary>B natural.</summary>
    B = 11,
}

/// <summary>
/// Extension methods for NoteName enum.
/// </summary>
public static class NoteNameExtensions
{
    /// <summary>
    /// Converts note name to display string with sharp symbol.
    /// </summary>
    /// <param name="noteName">The note name to convert.</param>
    /// <returns>Display string (e.g., "C#", "D", "F#").</returns>
    public static string ToDisplayString(this NoteName noteName) =>
        noteName switch
        {
            NoteName.C => "C",
            NoteName.CSharp => "C#",
            NoteName.D => "D",
            NoteName.DSharp => "D#",
            NoteName.E => "E",
            NoteName.F => "F",
            NoteName.FSharp => "F#",
            NoteName.G => "G",
            NoteName.GSharp => "G#",
            NoteName.A => "A",
            NoteName.ASharp => "A#",
            NoteName.B => "B",
            _ => noteName.ToString(),
        };

    /// <summary>
    /// Determines if the note is a sharp (black key on piano).
    /// </summary>
    /// <param name="noteName">The note name to check.</param>
    /// <returns>True if the note is a sharp/flat.</returns>
    public static bool IsSharp(this NoteName noteName) =>
        noteName switch
        {
            NoteName.CSharp => true,
            NoteName.DSharp => true,
            NoteName.FSharp => true,
            NoteName.GSharp => true,
            NoteName.ASharp => true,
            _ => false,
        };

    /// <summary>
    /// Determines if the note is a natural (white key on piano).
    /// </summary>
    /// <param name="noteName">The note name to check.</param>
    /// <returns>True if the note is a natural.</returns>
    public static bool IsNatural(this NoteName noteName) => !noteName.IsSharp();
}
