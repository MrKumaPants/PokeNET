using System;
using System.Collections.Generic;
using System.Linq;
using Melanchall.DryWetMidi.MusicTheory;
using PokeNET.Audio.Abstractions;
using Note = Melanchall.DryWetMidi.MusicTheory.Note;

namespace PokeNET.Audio.Procedural
{
    /// <summary>
    /// Generates melodies using music theory rules
    /// </summary>
    public class MelodyGenerator
    {
        private readonly IRandomProvider _randomProvider;
        private const int MaxMelodicLeap = 7; // Octave
        private const int PreferredMaxLeap = 4; // Major third

        public MelodyGenerator(IRandomProvider randomProvider)
        {
            _randomProvider =
                randomProvider ?? throw new ArgumentNullException(nameof(randomProvider));
        }

        /// <summary>
        /// Generates a melody over a chord progression
        /// </summary>
        public List<MelodicNote> GenerateMelody(
            Scale scale,
            List<ChordInfo> progression,
            int notesPerChord,
            int minOctave = 4,
            int maxOctave = 6,
            MelodicStyle style = MelodicStyle.Balanced
        )
        {
            var melody = new List<MelodicNote>();
            var scaleNotes = MusicTheoryHelper.GetScaleNotes(scale, minOctave, maxOctave);
            Note? previousNote = null;

            foreach (var chordInfo in progression)
            {
                var chordNotes = chordInfo.Notes;
                var noteDuration = chordInfo.Duration / notesPerChord;

                for (int i = 0; i < notesPerChord; i++)
                {
                    Note selectedNote;

                    // Use different strategies based on style
                    if (style == MelodicStyle.Chordal)
                    {
                        // Prefer chord tones
                        selectedNote = SelectChordalNote(chordNotes, scaleNotes, previousNote);
                    }
                    else if (style == MelodicStyle.Stepwise)
                    {
                        // Prefer stepwise motion
                        selectedNote = SelectStepwiseNote(scaleNotes, previousNote);
                    }
                    else if (style == MelodicStyle.Arpeggiated)
                    {
                        // Arpeggiate through chord
                        selectedNote = chordNotes[i % chordNotes.Count];
                    }
                    else
                    {
                        // Balanced approach
                        selectedNote = SelectBalancedNote(chordNotes, scaleNotes, previousNote, i);
                    }

                    melody.Add(
                        new MelodicNote
                        {
                            Note = selectedNote,
                            Duration = noteDuration,
                            IsChordTone = chordNotes.Any(n => n.NoteName == selectedNote.NoteName),
                        }
                    );

                    previousNote = selectedNote;
                }
            }

            return ApplyMelodicContour(melody, style);
        }

        /// <summary>
        /// Generates a melody based on emotional parameters
        /// </summary>
        public List<MelodicNote> GenerateEmotionalMelody(
            Scale scale,
            List<ChordInfo> progression,
            float energy, // 0.0 = sparse, 1.0 = dense
            float movement, // 0.0 = stepwise, 1.0 = leaps
            float tension, // 0.0 = consonant, 1.0 = dissonant
            int minOctave = 4,
            int maxOctave = 6
        )
        {
            var melody = new List<MelodicNote>();
            var scaleNotes = MusicTheoryHelper.GetScaleNotes(scale, minOctave, maxOctave);
            Note? previousNote = null;

            // Energy determines note density
            int notesPerChord = (int)Math.Ceiling(1 + energy * 7);

            foreach (var chordInfo in progression)
            {
                var chordNotes = chordInfo.Notes;
                var noteDuration = chordInfo.Duration / notesPerChord;

                for (int i = 0; i < notesPerChord; i++)
                {
                    Note selectedNote;

                    if (previousNote == null)
                    {
                        // Start on chord tone
                        selectedNote = chordNotes[_randomProvider.Next(chordNotes.Count)];
                    }
                    else
                    {
                        // Select based on movement parameter
                        if (_randomProvider.NextDouble() < movement)
                        {
                            // Melodic leap
                            selectedNote = SelectLeapNote(
                                scaleNotes,
                                previousNote,
                                (int)(movement * MaxMelodicLeap)
                            );
                        }
                        else
                        {
                            // Stepwise motion
                            selectedNote = SelectStepwiseNote(scaleNotes, previousNote);
                        }

                        // Add tension through non-chord tones
                        if (
                            _randomProvider.NextDouble() < tension
                            && !chordNotes.Any(n => n.NoteName == selectedNote.NoteName)
                        )
                        {
                            // Use passing tone or neighbor tone
                            selectedNote = SelectTensionNote(scaleNotes, chordNotes, previousNote);
                        }
                    }

                    melody.Add(
                        new MelodicNote
                        {
                            Note = selectedNote,
                            Duration = noteDuration,
                            IsChordTone = chordNotes.Any(n => n.NoteName == selectedNote.NoteName),
                        }
                    );

                    previousNote = selectedNote;
                }
            }

            return melody;
        }

        private Note SelectChordalNote(
            List<Note> chordNotes,
            List<Note> scaleNotes,
            Note? previousNote
        )
        {
            if (previousNote == null || _randomProvider.NextDouble() < 0.7)
            {
                return chordNotes[_randomProvider.Next(chordNotes.Count)];
            }

            // Approach chord tone by step
            var nearbyChordTones = chordNotes
                .Where(n => Math.Abs(n.NoteNumber - previousNote.NoteNumber) <= 2)
                .ToList();

            return nearbyChordTones.Count > 0
                ? nearbyChordTones[_randomProvider.Next(nearbyChordTones.Count)]
                : chordNotes[_randomProvider.Next(chordNotes.Count)];
        }

        private Note SelectStepwiseNote(List<Note> scaleNotes, Note? previousNote)
        {
            if (previousNote == null)
            {
                return scaleNotes[_randomProvider.Next(scaleNotes.Count)];
            }

            var currentIndex = scaleNotes.FindIndex(n => n.NoteNumber == previousNote.NoteNumber);
            if (currentIndex < 0)
                currentIndex = scaleNotes.Count / 2;

            // Move up or down by step
            var direction = _randomProvider.Next(2) == 0 ? 1 : -1;
            var newIndex = Math.Clamp(currentIndex + direction, 0, scaleNotes.Count - 1);

            return scaleNotes[newIndex];
        }

        private Note SelectLeapNote(List<Note> scaleNotes, Note previousNote, int maxLeap)
        {
            var validNotes = scaleNotes
                .Where(n =>
                    Math.Abs(n.NoteNumber - previousNote.NoteNumber) >= 3
                    && Math.Abs(n.NoteNumber - previousNote.NoteNumber) <= maxLeap
                )
                .ToList();

            return validNotes.Count > 0
                ? validNotes[_randomProvider.Next(validNotes.Count)]
                : scaleNotes[_randomProvider.Next(scaleNotes.Count)];
        }

        private Note SelectBalancedNote(
            List<Note> chordNotes,
            List<Note> scaleNotes,
            Note? previousNote,
            int position
        )
        {
            // Prefer chord tones on strong beats (position 0, 2, 4...)
            if (position % 2 == 0 || _randomProvider.NextDouble() < 0.6)
            {
                return SelectChordalNote(chordNotes, scaleNotes, previousNote);
            }
            else
            {
                return SelectStepwiseNote(scaleNotes, previousNote);
            }
        }

        private Note SelectTensionNote(
            List<Note> scaleNotes,
            List<Note> chordNotes,
            Note previousNote
        )
        {
            // Select non-chord tones near the previous note
            var tensionNotes = scaleNotes
                .Where(n =>
                    !chordNotes.Any(c => c.NoteName == n.NoteName)
                    && Math.Abs(n.NoteNumber - previousNote.NoteNumber) <= 2
                )
                .ToList();

            return tensionNotes.Count > 0
                ? tensionNotes[_randomProvider.Next(tensionNotes.Count)]
                : scaleNotes[_randomProvider.Next(scaleNotes.Count)];
        }

        private List<MelodicNote> ApplyMelodicContour(List<MelodicNote> melody, MelodicStyle style)
        {
            // Apply contour shaping based on style
            if (style == MelodicStyle.Ascending)
            {
                // Gradually move upward
                for (int i = 1; i < melody.Count; i++)
                {
                    if (melody[i].Note.NoteNumber < melody[i - 1].Note.NoteNumber)
                    {
                        // Transpose up if descending
                        melody[i] = new MelodicNote
                        {
                            Note = MusicTheoryHelper.Transpose(melody[i].Note, 2),
                            Duration = melody[i].Duration,
                            IsChordTone = melody[i].IsChordTone,
                        };
                    }
                }
            }
            else if (style == MelodicStyle.Descending)
            {
                // Gradually move downward
                for (int i = 1; i < melody.Count; i++)
                {
                    if (melody[i].Note.NoteNumber > melody[i - 1].Note.NoteNumber)
                    {
                        melody[i] = new MelodicNote
                        {
                            Note = MusicTheoryHelper.Transpose(melody[i].Note, -2),
                            Duration = melody[i].Duration,
                            IsChordTone = melody[i].IsChordTone,
                        };
                    }
                }
            }

            return melody;
        }
    }

    /// <summary>
    /// Represents a note in a melody
    /// </summary>
    public class MelodicNote
    {
        public Note Note { get; set; } = null!;
        public double Duration { get; set; } // Duration in bars
        public bool IsChordTone { get; set; }
    }

    public enum MelodicStyle
    {
        Balanced, // Mix of steps and chord tones
        Stepwise, // Prefer stepwise motion
        Chordal, // Prefer chord tones
        Arpeggiated, // Arpeggiate through chords
        Ascending, // Upward contour
        Descending, // Downward contour
    }
}
