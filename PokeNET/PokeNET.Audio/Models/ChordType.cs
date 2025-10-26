namespace PokeNET.Audio.Models;

/// <summary>
/// Types of musical chords
/// </summary>
public enum ChordType
{
    /// <summary>
    /// Major chord - Root, Major 3rd, Perfect 5th
    /// </summary>
    Major,

    /// <summary>
    /// Minor chord - Root, Minor 3rd, Perfect 5th
    /// </summary>
    Minor,

    /// <summary>
    /// Diminished chord - Root, Minor 3rd, Diminished 5th
    /// </summary>
    Diminished,

    /// <summary>
    /// Augmented chord - Root, Major 3rd, Augmented 5th
    /// </summary>
    Augmented,

    /// <summary>
    /// Major 7th chord - Major triad + Major 7th
    /// </summary>
    Major7,

    /// <summary>
    /// Minor 7th chord - Minor triad + Minor 7th
    /// </summary>
    Minor7,

    /// <summary>
    /// Dominant 7th chord - Major triad + Minor 7th
    /// </summary>
    Dominant7,

    /// <summary>
    /// Diminished 7th chord - Diminished triad + Diminished 7th
    /// </summary>
    Diminished7,

    /// <summary>
    /// Suspended 2nd chord - Root, Major 2nd, Perfect 5th
    /// </summary>
    Sus2,

    /// <summary>
    /// Suspended 4th chord - Root, Perfect 4th, Perfect 5th
    /// </summary>
    Sus4,

    /// <summary>
    /// Major 9th chord - Major 7th + Major 9th
    /// </summary>
    Major9,

    /// <summary>
    /// Minor 9th chord - Minor 7th + Major 9th
    /// </summary>
    Minor9,

    /// <summary>
    /// Dominant 9th chord - Dominant 7th + Major 9th
    /// </summary>
    Dominant9,

    /// <summary>
    /// Added 9th chord - Major triad + Major 9th (no 7th)
    /// </summary>
    Add9,

    /// <summary>
    /// Major 6th chord - Major triad + Major 6th
    /// </summary>
    Major6,

    /// <summary>
    /// Minor 6th chord - Minor triad + Major 6th
    /// </summary>
    Minor6,
}
