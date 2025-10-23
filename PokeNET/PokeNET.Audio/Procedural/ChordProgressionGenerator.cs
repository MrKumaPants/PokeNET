using Melanchall.DryWetMidi.MusicTheory;
using PokeNET.Audio.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using Note = Melanchall.DryWetMidi.MusicTheory.Note;
using Chord = Melanchall.DryWetMidi.MusicTheory.Chord;

namespace PokeNET.Audio.Procedural
{
    /// <summary>
    /// Generates chord progressions based on music theory rules
    /// </summary>
    public class ChordProgressionGenerator
    {
        private readonly IRandomProvider _randomProvider;
        private static readonly Dictionary<string, int[][]> _progressionPatterns = new()
        {
            // Common progressions in scale degrees
            ["pop"] = new[] { new[] { 1, 5, 6, 4 }, new[] { 1, 4, 5, 1 }, new[] { 6, 4, 1, 5 } },
            ["jazz"] = new[] { new[] { 2, 5, 1 }, new[] { 1, 6, 2, 5 }, new[] { 3, 6, 2, 5, 1 } },
            ["blues"] = new[] { new[] { 1, 1, 1, 1, 4, 4, 1, 1, 5, 4, 1, 5 } },
            ["calm"] = new[] { new[] { 1, 4, 1, 5 }, new[] { 6, 4, 1, 5 }, new[] { 1, 3, 4, 1 } },
            ["tense"] = new[] { new[] { 1, 2, 5, 1 }, new[] { 6, 7, 1 }, new[] { 4, 7, 3, 6 } },
            ["mysterious"] = new[] { new[] { 1, 6, 4, 5 }, new[] { 6, 2, 3, 7 }, new[] { 1, 7, 6, 5 } },
            ["epic"] = new[] { new[] { 1, 5, 6, 3, 4, 1, 4, 5 }, new[] { 6, 4, 1, 5 } }
        };

        public ChordProgressionGenerator(IRandomProvider randomProvider)
        {
            _randomProvider = randomProvider ?? throw new ArgumentNullException(nameof(randomProvider));
        }

        /// <summary>
        /// Generates a chord progression based on style and parameters
        /// </summary>
        public List<ChordInfo> GenerateProgression(
            Scale scale,
            string style,
            int bars,
            int chordsPerBar = 1,
            int octave = 3)
        {
            var progression = new List<ChordInfo>();
            var pattern = GetProgressionPattern(style);
            var totalChords = bars * chordsPerBar;

            for (int i = 0; i < totalChords; i++)
            {
                var degree = pattern[i % pattern.Length];
                var chordType = GetChordTypeForDegree(scale, degree, style);
                var chord = MusicTheoryHelper.GetChordFromDegree(scale, degree, chordType, octave);

                progression.Add(new ChordInfo
                {
                    Chord = chord,
                    Degree = degree,
                    ChordType = chordType,
                    Duration = 1.0 / chordsPerBar  // Duration in bars
                });
            }

            return progression;
        }

        /// <summary>
        /// Generates a progression based on emotional parameters
        /// </summary>
        public List<ChordInfo> GenerateEmotionalProgression(
            Scale scale,
            float tension,      // 0.0 = calm, 1.0 = tense
            float complexity,   // 0.0 = simple, 1.0 = complex
            int bars,
            int octave = 3)
        {
            var progression = new List<ChordInfo>();
            var chordsPerBar = complexity > 0.5f ? 2 : 1;
            var totalChords = bars * chordsPerBar;

            // Start with tonic
            int currentDegree = 1;

            for (int i = 0; i < totalChords; i++)
            {
                // Choose next chord based on tension
                currentDegree = GetNextDegree(currentDegree, tension, complexity);
                var chordType = GetChordTypeForDegree(scale, currentDegree, tension, complexity);
                var chord = MusicTheoryHelper.GetChordFromDegree(scale, currentDegree, chordType, octave);

                // Add extensions for complexity
                if (complexity > 0.7f && _randomProvider.NextDouble() < 0.3)
                {
                    chordType = AddChordExtension(chordType);
                    chord = MusicTheoryHelper.GetChordFromDegree(scale, currentDegree, chordType, octave);
                }

                progression.Add(new ChordInfo
                {
                    Chord = chord,
                    Degree = currentDegree,
                    ChordType = chordType,
                    Duration = 1.0 / chordsPerBar
                });
            }

            // Resolve to tonic if ending with tension
            if (progression.Last().Degree != 1)
            {
                var resolveChord = MusicTheoryHelper.GetChordFromDegree(scale, 1, ChordType.Major, octave);
                progression.Add(new ChordInfo
                {
                    Chord = resolveChord,
                    Degree = 1,
                    ChordType = ChordType.Major,
                    Duration = 1.0
                });
            }

            return progression;
        }

        private int[] GetProgressionPattern(string style)
        {
            style = style.ToLower();
            if (_progressionPatterns.TryGetValue(style, out var patterns))
            {
                return patterns[_randomProvider.Next(patterns.Length)];
            }

            // Default to pop progression
            return _progressionPatterns["pop"][0];
        }

        private ChordType GetChordTypeForDegree(Scale scale, int degree, string style)
        {
            // For major scale
            if (style.Contains("jazz") || style.Contains("complex"))
            {
                return degree switch
                {
                    1 => ChordType.Major7,
                    2 => ChordType.Minor7,
                    3 => ChordType.Minor7,
                    4 => ChordType.Major7,
                    5 => ChordType.Dominant7,
                    6 => ChordType.Minor7,
                    7 => ChordType.Minor7,
                    _ => ChordType.Major
                };
            }
            else
            {
                return degree switch
                {
                    1 => ChordType.Major,
                    2 => ChordType.Minor,
                    3 => ChordType.Minor,
                    4 => ChordType.Major,
                    5 => ChordType.Major,
                    6 => ChordType.Minor,
                    7 => ChordType.Diminished,
                    _ => ChordType.Major
                };
            }
        }

        private ChordType GetChordTypeForDegree(Scale scale, int degree, float tension, float complexity)
        {
            // Use 7th chords for higher complexity
            if (complexity > 0.5f)
            {
                return degree switch
                {
                    1 => ChordType.Major7,
                    2 => ChordType.Minor7,
                    3 => ChordType.Minor7,
                    4 => ChordType.Major7,
                    5 => tension > 0.5f ? ChordType.Dominant7 : ChordType.Major7,
                    6 => ChordType.Minor7,
                    7 => ChordType.Minor7,
                    _ => ChordType.Major
                };
            }
            else
            {
                return degree switch
                {
                    1 => ChordType.Major,
                    2 => ChordType.Minor,
                    3 => ChordType.Minor,
                    4 => ChordType.Major,
                    5 => ChordType.Major,
                    6 => ChordType.Minor,
                    7 => tension > 0.7f ? ChordType.Diminished : ChordType.Minor,
                    _ => ChordType.Major
                };
            }
        }

        private int GetNextDegree(int currentDegree, float tension, float complexity)
        {
            // Common chord transitions
            var calmTransitions = new Dictionary<int, int[]>
            {
                [1] = new[] { 4, 5, 6 },
                [2] = new[] { 5, 1 },
                [3] = new[] { 6, 4 },
                [4] = new[] { 1, 5, 2 },
                [5] = new[] { 1, 6 },
                [6] = new[] { 2, 4, 5 },
                [7] = new[] { 1, 3 }
            };

            var tenseTransitions = new Dictionary<int, int[]>
            {
                [1] = new[] { 2, 7, 4 },
                [2] = new[] { 5, 7 },
                [3] = new[] { 6, 7 },
                [4] = new[] { 7, 5 },
                [5] = new[] { 1, 6 },
                [6] = new[] { 2, 7 },
                [7] = new[] { 1, 3 }
            };

            var transitions = tension > 0.5f ? tenseTransitions : calmTransitions;
            var possibleDegrees = transitions[currentDegree];

            return possibleDegrees[_randomProvider.Next(possibleDegrees.Length)];
        }

        private ChordType AddChordExtension(ChordType baseType)
        {
            return baseType switch
            {
                ChordType.Major => ChordType.Major7,
                ChordType.Minor => ChordType.Minor7,
                _ => baseType
            };
        }
    }

    /// <summary>
    /// Information about a chord in a progression
    /// </summary>
    public class ChordInfo
    {
        public Chord Chord { get; set; } = null!;
        public int Degree { get; set; }
        public ChordType ChordType { get; set; }
        public double Duration { get; set; }  // Duration in bars
        public Note RootNote { get; set; } = null!;

        public List<Note> Notes
        {
            get
            {
                var notes = new List<Note>();
                foreach (var noteName in Chord.NotesNames)
                {
                    notes.Add(Note.Get(noteName, RootNote.Octave));
                }
                return notes;
            }
        }
    }
}
