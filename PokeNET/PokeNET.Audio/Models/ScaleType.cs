namespace PokeNET.Audio.Models;

/// <summary>
/// Types of musical scales
/// </summary>
public enum ScaleType
{
    /// <summary>
    /// Major scale (Ionian mode) - W-W-H-W-W-W-H
    /// </summary>
    Major,

    /// <summary>
    /// Natural minor scale (Aeolian mode) - W-H-W-W-H-W-W
    /// </summary>
    Minor,

    /// <summary>
    /// Natural minor scale (alias for Minor)
    /// </summary>
    NaturalMinor,

    /// <summary>
    /// Harmonic minor scale - W-H-W-W-H-W+H-H
    /// </summary>
    HarmonicMinor,

    /// <summary>
    /// Melodic minor scale - W-H-W-W-W-W-H (ascending)
    /// </summary>
    MelodicMinor,

    /// <summary>
    /// Dorian mode - W-H-W-W-W-H-W
    /// </summary>
    Dorian,

    /// <summary>
    /// Phrygian mode - H-W-W-W-H-W-W
    /// </summary>
    Phrygian,

    /// <summary>
    /// Lydian mode - W-W-W-H-W-W-H
    /// </summary>
    Lydian,

    /// <summary>
    /// Mixolydian mode - W-W-H-W-W-H-W
    /// </summary>
    Mixolydian,

    /// <summary>
    /// Locrian mode - H-W-W-H-W-W-W
    /// </summary>
    Locrian,

    /// <summary>
    /// Pentatonic major scale - W-W-W+H-W-W+H
    /// </summary>
    PentatonicMajor,

    /// <summary>
    /// Pentatonic minor scale - W+H-W-W-W+H-W
    /// </summary>
    PentatonicMinor,

    /// <summary>
    /// Blues scale - W+H-W-H-H-W+H-W
    /// </summary>
    Blues,

    /// <summary>
    /// Whole tone scale - W-W-W-W-W-W
    /// </summary>
    WholeTone,

    /// <summary>
    /// Chromatic scale - all 12 semitones
    /// </summary>
    Chromatic,
}

/// <summary>
/// Extension methods for working with scales
/// </summary>
public static class ScaleTypeExtensions
{
    /// <summary>
    /// Gets the interval pattern for a scale type (in semitones from root)
    /// </summary>
    public static int[] GetIntervals(this ScaleType scaleType) =>
        scaleType switch
        {
            ScaleType.Major => [0, 2, 4, 5, 7, 9, 11],
            ScaleType.Minor => [0, 2, 3, 5, 7, 8, 10],
            ScaleType.NaturalMinor => [0, 2, 3, 5, 7, 8, 10],
            ScaleType.HarmonicMinor => [0, 2, 3, 5, 7, 8, 11],
            ScaleType.MelodicMinor => [0, 2, 3, 5, 7, 9, 11],
            ScaleType.Dorian => [0, 2, 3, 5, 7, 9, 10],
            ScaleType.Phrygian => [0, 1, 3, 5, 7, 8, 10],
            ScaleType.Lydian => [0, 2, 4, 6, 7, 9, 11],
            ScaleType.Mixolydian => [0, 2, 4, 5, 7, 9, 10],
            ScaleType.Locrian => [0, 1, 3, 5, 6, 8, 10],
            ScaleType.PentatonicMajor => [0, 2, 4, 7, 9],
            ScaleType.PentatonicMinor => [0, 3, 5, 7, 10],
            ScaleType.Blues => [0, 3, 5, 6, 7, 10],
            ScaleType.WholeTone => [0, 2, 4, 6, 8, 10],
            ScaleType.Chromatic => [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11],
            _ => [0],
        };

    /// <summary>
    /// Gets the notes in a scale given a root note and octave
    /// </summary>
    public static List<Note> GetNotes(this ScaleType scaleType, NoteName rootNote, int octave = 4)
    {
        var notes = new List<Note>();
        var intervals = scaleType.GetIntervals();

        foreach (var interval in intervals)
        {
            var noteValue = ((int)rootNote + interval) % 12;
            var octaveOffset = ((int)rootNote + interval) / 12;

            notes.Add(
                new Note
                {
                    NoteName = (NoteName)noteValue,
                    Octave = octave + octaveOffset,
                    Duration = 1.0f,
                    Velocity = 0.8f,
                }
            );
        }

        return notes;
    }
}
