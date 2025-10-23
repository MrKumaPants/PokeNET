using Melanchall.DryWetMidi.MusicTheory;
using Melanchall.DryWetMidi.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using Note = Melanchall.DryWetMidi.MusicTheory.Note;
using Chord = Melanchall.DryWetMidi.MusicTheory.Chord;

namespace PokeNET.Audio.Procedural
{
    /// <summary>
    /// Helper class for music theory operations using DryWetMidi
    /// </summary>
    public static partial class MusicTheoryHelper
    {
        private static readonly Random _random = new Random();

        /// <summary>
        /// Gets a scale based on key and mode
        /// </summary>
        public static Scale GetScale(NoteName rootNote, ScaleType scaleType)
        {
            var intervals = scaleType switch
            {
                ScaleType.Major => ScaleIntervals.Major,
                ScaleType.Minor => ScaleIntervals.NaturalMinor,
                ScaleType.Dorian => ScaleIntervals.Dorian,
                ScaleType.Phrygian => ScaleIntervals.Phrygian,
                ScaleType.Lydian => ScaleIntervals.Lydian,
                ScaleType.Mixolydian => ScaleIntervals.Mixolydian,
                ScaleType.HarmonicMinor => ScaleIntervals.HarmonicMinor,
                ScaleType.MelodicMinor => ScaleIntervals.MelodicMinor,
                ScaleType.Pentatonic => ScaleIntervals.MajorPentatonic,
                ScaleType.MinorPentatonic => ScaleIntervals.MinorPentatonic,
                _ => ScaleIntervals.Major
            };
            return new Scale(intervals, rootNote);
        }

        /// <summary>
        /// Gets notes from a scale within an octave range
        /// </summary>
        public static List<Note> GetScaleNotes(Scale scale, int minOctave, int maxOctave)
        {
            var notes = new List<Note>();
            for (int octave = minOctave; octave <= maxOctave; octave++)
            {
                foreach (var note in scale.GetNotes())
                {
                    notes.Add(Note.Get(note.NoteName, octave));
                }
            }
            return notes;
        }

        /// <summary>
        /// Gets a chord from a scale degree
        /// </summary>
        public static Chord GetChordFromDegree(Scale scale, int degree, ChordType chordType, int octave)
        {
            var scaleNotes = scale.GetNotes().ToArray();
            var rootNote = scaleNotes[(degree - 1) % scaleNotes.Length];

            // Create chord using root note name
            var rootNoteName = rootNote.NoteName;

            // Build chord notes based on type
            var noteNames = chordType switch
            {
                ChordType.Major => new[] { rootNoteName, GetTransposedNoteName(rootNoteName, 4), GetTransposedNoteName(rootNoteName, 7) },
                ChordType.Minor => new[] { rootNoteName, GetTransposedNoteName(rootNoteName, 3), GetTransposedNoteName(rootNoteName, 7) },
                ChordType.Diminished => new[] { rootNoteName, GetTransposedNoteName(rootNoteName, 3), GetTransposedNoteName(rootNoteName, 6) },
                ChordType.Augmented => new[] { rootNoteName, GetTransposedNoteName(rootNoteName, 4), GetTransposedNoteName(rootNoteName, 8) },
                ChordType.Major7 => new[] { rootNoteName, GetTransposedNoteName(rootNoteName, 4), GetTransposedNoteName(rootNoteName, 7), GetTransposedNoteName(rootNoteName, 11) },
                ChordType.Minor7 => new[] { rootNoteName, GetTransposedNoteName(rootNoteName, 3), GetTransposedNoteName(rootNoteName, 7), GetTransposedNoteName(rootNoteName, 10) },
                ChordType.Dominant7 => new[] { rootNoteName, GetTransposedNoteName(rootNoteName, 4), GetTransposedNoteName(rootNoteName, 7), GetTransposedNoteName(rootNoteName, 10) },
                _ => new[] { rootNoteName, GetTransposedNoteName(rootNoteName, 4), GetTransposedNoteName(rootNoteName, 7) }
            };

            return new Chord(noteNames);
        }

        /// <summary>
        /// Gets a random note from a scale
        /// </summary>
        public static Note GetRandomNoteFromScale(List<Note> scaleNotes)
        {
            return scaleNotes[_random.Next(scaleNotes.Count)];
        }

        /// <summary>
        /// Gets the nearest scale note to a given note
        /// </summary>
        public static Note GetNearestScaleNote(Note note, List<Note> scaleNotes)
        {
            return scaleNotes.OrderBy(n => Math.Abs(n.NoteNumber - note.NoteNumber)).First();
        }

        /// <summary>
        /// Transposes a note by semitones
        /// </summary>
        public static Note Transpose(Note note, int semitones)
        {
            var newNoteNumber = note.NoteNumber + semitones;
            newNoteNumber = Math.Clamp(newNoteNumber, (int)SevenBitNumber.MinValue, (int)SevenBitNumber.MaxValue);
            return Note.Get((SevenBitNumber)newNoteNumber);
        }

        /// <summary>
        /// Gets interval between two notes in semitones
        /// </summary>
        public static int GetInterval(Note note1, Note note2)
        {
            return note2.NoteNumber - note1.NoteNumber;
        }

        /// <summary>
        /// Checks if a note is in a scale
        /// </summary>
        public static bool IsNoteInScale(Note note, Scale scale)
        {
            var scaleNotes = scale.GetNotes();
            return scaleNotes.Any(n => n.NoteName == note.NoteName);
        }

        /// <summary>
        /// Gets velocity based on dynamics
        /// </summary>
        public static SevenBitNumber GetVelocity(DynamicsLevel dynamics, float variation = 0.1f)
        {
            int baseVelocity = dynamics switch
            {
                DynamicsLevel.PPP => 16,
                DynamicsLevel.PP => 32,
                DynamicsLevel.P => 48,
                DynamicsLevel.MP => 64,
                DynamicsLevel.MF => 80,
                DynamicsLevel.F => 96,
                DynamicsLevel.FF => 112,
                DynamicsLevel.FFF => 127,
                _ => 64
            };

            // Add variation
            int variationAmount = (int)(baseVelocity * variation);
            int velocity = baseVelocity + _random.Next(-variationAmount, variationAmount);
            return (SevenBitNumber)Math.Clamp(velocity, 1, 127);
        }

        /// <summary>
        /// Converts tempo to microseconds per quarter note
        /// </summary>
        public static int TempoToMicroseconds(int bpm)
        {
            return 60000000 / bpm;
        }
    }

    public enum ScaleType
    {
        Major,
        Minor,
        Dorian,
        Phrygian,
        Lydian,
        Mixolydian,
        HarmonicMinor,
        MelodicMinor,
        Pentatonic,
        MinorPentatonic
    }

    public enum ChordType
    {
        Major,
        Minor,
        Diminished,
        Augmented,
        Major7,
        Minor7,
        Dominant7,
        Suspended2,
        Suspended4
    }

    public enum DynamicsLevel
    {
        PPP,  // Pianississimo
        PP,   // Pianissimo
        P,    // Piano
        MP,   // Mezzo-piano
        MF,   // Mezzo-forte
        F,    // Forte
        FF,   // Fortissimo
        FFF   // Fortississimo
    }

    /// <summary>
    /// Music theory intervals for scales and chords
    /// </summary>
    public static class ScaleIntervals
    {
        public static readonly Interval[] Major = { Interval.Zero, Interval.Two, Interval.Four, Interval.Five, Interval.Seven, Interval.Nine, Interval.Eleven };
        public static readonly Interval[] NaturalMinor = { Interval.Zero, Interval.Two, Interval.Three, Interval.Five, Interval.Seven, Interval.Eight, Interval.Ten };
        public static readonly Interval[] HarmonicMinor = { Interval.Zero, Interval.Two, Interval.Three, Interval.Five, Interval.Seven, Interval.Eight, Interval.Eleven };
        public static readonly Interval[] MelodicMinor = { Interval.Zero, Interval.Two, Interval.Three, Interval.Five, Interval.Seven, Interval.Nine, Interval.Eleven };
        public static readonly Interval[] Dorian = { Interval.Zero, Interval.Two, Interval.Three, Interval.Five, Interval.Seven, Interval.Nine, Interval.Ten };
        public static readonly Interval[] Phrygian = { Interval.Zero, Interval.One, Interval.Three, Interval.Five, Interval.Seven, Interval.Eight, Interval.Ten };
        public static readonly Interval[] Lydian = { Interval.Zero, Interval.Two, Interval.Four, Interval.Six, Interval.Seven, Interval.Nine, Interval.Eleven };
        public static readonly Interval[] Mixolydian = { Interval.Zero, Interval.Two, Interval.Four, Interval.Five, Interval.Seven, Interval.Nine, Interval.Ten };
        public static readonly Interval[] MajorPentatonic = { Interval.Zero, Interval.Two, Interval.Four, Interval.Seven, Interval.Nine };
        public static readonly Interval[] MinorPentatonic = { Interval.Zero, Interval.Three, Interval.Five, Interval.Seven, Interval.Ten };
    }

    public static class ChordIntervals
    {
        public static readonly Interval[] Major = { Interval.Zero, Interval.Four, Interval.Seven };
        public static readonly Interval[] Minor = { Interval.Zero, Interval.Three, Interval.Seven };
        public static readonly Interval[] Diminished = { Interval.Zero, Interval.Three, Interval.Six };
        public static readonly Interval[] Augmented = { Interval.Zero, Interval.Four, Interval.Eight };
        public static readonly Interval[] MajorSeventh = { Interval.Zero, Interval.Four, Interval.Seven, Interval.Eleven };
        public static readonly Interval[] MinorSeventh = { Interval.Zero, Interval.Three, Interval.Seven, Interval.Ten };
        public static readonly Interval[] DominantSeventh = { Interval.Zero, Interval.Four, Interval.Seven, Interval.Ten };
        public static readonly Interval[] Suspended2 = { Interval.Zero, Interval.Two, Interval.Seven };
        public static readonly Interval[] Suspended4 = { Interval.Zero, Interval.Five, Interval.Seven };
    }
}
