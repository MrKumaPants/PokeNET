using System;
using System.Collections.Generic;
using System.Linq;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.MusicTheory;
using Microsoft.Extensions.Logging;
using PokeNET.Audio.Abstractions;
using PokeNET.Audio.Models;
using ChordProgression = PokeNET.Audio.Models.ChordProgression;
using MidiNote = Melanchall.DryWetMidi.MusicTheory.Note;
using MidiNoteName = Melanchall.DryWetMidi.MusicTheory.NoteName;

namespace PokeNET.Audio.Procedural
{
    /// <summary>
    /// Generates procedural music using advanced algorithms including Markov chains,
    /// L-Systems, and cellular automata. Implements test-driven music theory algorithms.
    /// SOLID PRINCIPLE: Single Responsibility - Focuses solely on procedural music generation.
    /// </summary>
    public class ProceduralMusicGenerator
    {
        private readonly ILogger<ProceduralMusicGenerator> _logger;
        private readonly IRandomProvider _randomProvider;
        private readonly Dictionary<Models.NoteName, Dictionary<Models.NoteName, int>> _markovChain;

        /// <summary>
        /// Default tempo in beats per minute.
        /// </summary>
        public int DefaultTempo { get; set; } = 120;

        /// <summary>
        /// Default musical key for generation.
        /// </summary>
        public Models.NoteName DefaultKey { get; set; } = Models.NoteName.C;

        public ProceduralMusicGenerator(
            ILogger<ProceduralMusicGenerator> logger,
            IRandomProvider randomProvider
        )
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _randomProvider =
                randomProvider ?? throw new ArgumentNullException(nameof(randomProvider));
            _markovChain = new Dictionary<Models.NoteName, Dictionary<Models.NoteName, int>>();

            _logger.LogInformation(
                "ProceduralMusicGenerator initialized with tempo={Tempo}, key={Key}",
                DefaultTempo,
                DefaultKey
            );
        }

        #region Melody Generation

        /// <summary>
        /// Generates a melody in the specified key and scale.
        /// </summary>
        public Melody GenerateMelody(
            Models.NoteName key,
            Models.ScaleType scale,
            int length,
            int minNote = 60,
            int maxNote = 84,
            int maxStepSize = 12
        )
        {
            if (length <= 0)
                throw new ArgumentException("Length must be greater than zero", nameof(length));

            _logger.LogDebug(
                "Generating melody: key={Key}, scale={Scale}, length={Length}",
                key,
                scale,
                length
            );

            var scaleNotes = GetScaleNotes(key, scale, minNote, maxNote);
            var notes = new List<Models.Note>();

            // Start on tonic
            var currentNote =
                scaleNotes.FirstOrDefault(n => n.MidiNoteNumber >= minNote) ?? scaleNotes[0];
            notes.Add(currentNote);

            for (int i = 1; i < length; i++)
            {
                var nextNote = GenerateNextNote(
                    currentNote,
                    scaleNotes,
                    maxStepSize,
                    minNote,
                    maxNote
                );
                notes.Add(nextNote);
                currentNote = nextNote;
            }

            return new Melody
            {
                Notes = notes,
                Tempo = DefaultTempo,
                TimeSignature = Models.TimeSignature.CommonTime,
                Scale = scale,
                Key = key,
            };
        }

        private Models.Note GenerateNextNote(
            Models.Note current,
            List<Models.Note> scaleNotes,
            int maxStep,
            int minNote,
            int maxNote
        )
        {
            var validNotes = scaleNotes
                .Where(n => n.MidiNoteNumber >= minNote && n.MidiNoteNumber <= maxNote)
                .Where(n => Math.Abs(n.MidiNoteNumber - current.MidiNoteNumber) <= maxStep)
                .ToList();

            if (!validNotes.Any())
                validNotes = scaleNotes
                    .Where(n => n.MidiNoteNumber >= minNote && n.MidiNoteNumber <= maxNote)
                    .ToList();

            var index = _randomProvider.Next(validNotes.Count);
            var selected = validNotes[index];

            return new Models.Note
            {
                NoteName = selected.NoteName,
                Octave = selected.Octave,
                Duration = GenerateNoteDuration(),
                Velocity = 0.7f + (float)_randomProvider.NextDouble() * 0.2f,
            };
        }

        private float GenerateNoteDuration()
        {
            var durations = new[] { 0.25f, 0.5f, 1.0f, 2.0f };
            return durations[_randomProvider.Next(durations.Length)];
        }

        private List<Models.Note> GetScaleNotes(
            Models.NoteName key,
            Models.ScaleType scale,
            int minNote,
            int maxNote
        )
        {
            var intervals = scale.GetIntervals();
            var notes = new List<Models.Note>();

            for (int octave = 0; octave <= 8; octave++)
            {
                foreach (var interval in intervals)
                {
                    var noteValue = ((int)key + interval) % 12;
                    var octaveOffset = ((int)key + interval) / 12;
                    var actualOctave = octave + octaveOffset;

                    var note = new Models.Note
                    {
                        NoteName = (Models.NoteName)noteValue,
                        Octave = actualOctave,
                        Duration = 1.0f,
                        Velocity = 0.8f,
                    };

                    if (note.MidiNoteNumber >= minNote && note.MidiNoteNumber <= maxNote)
                        notes.Add(note);
                }
            }

            return notes.OrderBy(n => n.MidiNoteNumber).ToList();
        }

        #endregion

        #region Chord Progression Generation

        /// <summary>
        /// Generates a chord progression in the specified key.
        /// </summary>
        public ChordProgression GenerateChordProgression(
            Models.NoteName key,
            int progressionLength,
            bool endOnTonic = false
        )
        {
            if (progressionLength <= 0)
                throw new ArgumentException(
                    "Progression length must be greater than zero",
                    nameof(progressionLength)
                );

            _logger.LogDebug(
                "Generating chord progression: key={Key}, length={Length}",
                key,
                progressionLength
            );

            var chords = new List<Models.Chord>();
            var degrees = new[] { 1, 4, 5, 6, 2, 3 }; // Common progression degrees

            for (int i = 0; i < progressionLength; i++)
            {
                int degree;
                if (endOnTonic && i == progressionLength - 1)
                {
                    degree = 1; // Tonic
                }
                else
                {
                    degree = degrees[_randomProvider.Next(degrees.Length)];
                }

                var chord = GenerateDiatonicChord(key, degree);
                chords.Add(chord);
            }

            return new ChordProgression
            {
                Chords = chords,
                Tempo = DefaultTempo,
                TimeSignature = Models.TimeSignature.CommonTime,
                Key = key,
                Pattern = string.Join("-", Enumerable.Range(0, progressionLength).Select(_ => "I")),
            };
        }

        /// <summary>
        /// Generates a chord progression from a Roman numeral pattern.
        /// </summary>
        public ChordProgression GenerateChordProgressionFromPattern(
            Models.NoteName key,
            string pattern
        )
        {
            if (string.IsNullOrWhiteSpace(pattern))
                throw new ArgumentException("Pattern cannot be empty", nameof(pattern));

            _logger.LogDebug(
                "Generating chord progression from pattern: key={Key}, pattern={Pattern}",
                key,
                pattern
            );

            var chordSymbols = pattern.Split('-');
            var chords = new List<Models.Chord>();

            foreach (var symbol in chordSymbols)
            {
                var degree = ParseRomanNumeral(symbol.Trim());
                var chord = GenerateDiatonicChord(key, degree);
                chords.Add(chord);
            }

            return new ChordProgression
            {
                Chords = chords,
                Tempo = DefaultTempo,
                TimeSignature = Models.TimeSignature.CommonTime,
                Key = key,
                Pattern = pattern,
            };
        }

        private int ParseRomanNumeral(string roman)
        {
            var normalized = roman.ToUpper().Replace("I", "").Replace("V", "");
            return roman.ToUpper() switch
            {
                "I" => 1,
                "II" => 2,
                "III" => 3,
                "IV" => 4,
                "V" => 5,
                "VI" => 6,
                "VII" => 7,
                _ => 1,
            };
        }

        private Models.Chord GenerateDiatonicChord(Models.NoteName key, int degree)
        {
            var majorScale = new[] { 0, 2, 4, 5, 7, 9, 11 };
            var rootOffset = majorScale[degree - 1];
            var rootNote = (Models.NoteName)(((int)key + rootOffset) % 12);

            // Determine chord quality based on degree in major scale
            var chordType = degree switch
            {
                1 => Models.ChordType.Major, // I
                2 => Models.ChordType.Minor, // ii
                3 => Models.ChordType.Minor, // iii
                4 => Models.ChordType.Major, // IV
                5 => Models.ChordType.Major, // V
                6 => Models.ChordType.Minor, // vi
                7 => Models.ChordType.Diminished, // vii°
                _ => Models.ChordType.Major,
            };

            return new Models.Chord
            {
                RootNote = rootNote,
                ChordType = chordType,
                Octave = 4,
                Degree = degree,
                IsDiatonic = true,
            };
        }

        #endregion

        #region Rhythm Generation

        /// <summary>
        /// Generates a rhythmic pattern with the specified time signature.
        /// </summary>
        public Rhythm GenerateRhythm(int bars, int beatsPerBar, int beatUnit = 4)
        {
            if (bars <= 0)
                throw new ArgumentException("Bars must be greater than zero", nameof(bars));
            if (beatsPerBar <= 0)
                throw new ArgumentException(
                    "Beats per bar must be greater than zero",
                    nameof(beatsPerBar)
                );

            _logger.LogDebug(
                "Generating rhythm: bars={Bars}, beatsPerBar={BeatsPerBar}, beatUnit={BeatUnit}",
                bars,
                beatsPerBar,
                beatUnit
            );

            var totalBeats = bars * beatsPerBar;
            var beats = new List<Beat>();
            var durations = new[] { 0.25f, 0.5f, 1.0f, 2.0f };

            float currentBeat = 0;
            while (currentBeat < totalBeats)
            {
                var duration = durations[_randomProvider.Next(durations.Length)];
                if (currentBeat + duration > totalBeats)
                    duration = totalBeats - currentBeat;

                var isAccent = (int)currentBeat % beatsPerBar == 0;
                var velocity = isAccent ? 1.0f : 0.6f + (float)_randomProvider.NextDouble() * 0.3f;

                beats.Add(
                    new Beat
                    {
                        Duration = duration,
                        Accent = isAccent,
                        Velocity = velocity,
                        IsRest = _randomProvider.NextDouble() < 0.1, // 10% chance of rest
                    }
                );

                currentBeat += duration;
            }

            return new Rhythm
            {
                Beats = beats,
                TimeSignature = new Models.TimeSignature
                {
                    BeatsPerMeasure = beatsPerBar,
                    BeatValue = beatUnit,
                },
                TotalBeats = totalBeats,
                BeatsPerBar = beatsPerBar,
                BeatUnit = beatUnit,
                NoteDurations = beats.Select(b => b.Duration).ToList(),
            };
        }

        #endregion

        #region Markov Chain Generation

        /// <summary>
        /// Generates a melody using Markov chain trained on sample melodies.
        /// </summary>
        public Melody GenerateWithMarkovChain(List<Melody> trainingData, int length)
        {
            if (trainingData == null || !trainingData.Any())
                throw new ArgumentException("Training data cannot be empty", nameof(trainingData));
            if (length <= 0)
                throw new ArgumentException("Length must be greater than zero", nameof(length));

            _logger.LogDebug("Training Markov chain with {Count} melodies", trainingData.Count);

            // Train the Markov chain
            TrainMarkovChain(trainingData);

            // Generate new melody
            var notes = new List<Models.Note>();
            var firstNote = trainingData[0].Notes[0];
            notes.Add(firstNote);

            for (int i = 1; i < length; i++)
            {
                var nextNote = GetNextNoteFromMarkovChain(notes[i - 1]);
                notes.Add(nextNote);
            }

            return new Melody
            {
                Notes = notes,
                Tempo = trainingData[0].Tempo,
                TimeSignature = trainingData[0].TimeSignature,
                Scale = trainingData[0].Scale,
                Key = trainingData[0].Key,
            };
        }

        private void TrainMarkovChain(List<Melody> trainingData)
        {
            _markovChain.Clear();

            foreach (var melody in trainingData)
            {
                for (int i = 0; i < melody.Notes.Count - 1; i++)
                {
                    var current = melody.Notes[i].NoteName;
                    var next = melody.Notes[i + 1].NoteName;

                    if (!_markovChain.ContainsKey(current))
                        _markovChain[current] = new Dictionary<Models.NoteName, int>();

                    if (!_markovChain[current].ContainsKey(next))
                        _markovChain[current][next] = 0;

                    _markovChain[current][next]++;
                }
            }
        }

        private Models.Note GetNextNoteFromMarkovChain(Models.Note currentNote)
        {
            if (
                !_markovChain.ContainsKey(currentNote.NoteName)
                || !_markovChain[currentNote.NoteName].Any()
            )
            {
                // Fallback to random note if no data
                return new Models.Note
                {
                    NoteName = (Models.NoteName)_randomProvider.Next(12),
                    Octave = currentNote.Octave,
                    Duration = GenerateNoteDuration(),
                    Velocity = 0.8f,
                };
            }

            var transitions = _markovChain[currentNote.NoteName];
            var totalWeight = transitions.Values.Sum();
            var random = _randomProvider.Next(totalWeight);

            var cumulative = 0;
            foreach (var (noteName, weight) in transitions)
            {
                cumulative += weight;
                if (random < cumulative)
                {
                    return new Models.Note
                    {
                        NoteName = noteName,
                        Octave = currentNote.Octave,
                        Duration = GenerateNoteDuration(),
                        Velocity = 0.7f + (float)_randomProvider.NextDouble() * 0.2f,
                    };
                }
            }

            return new Models.Note
            {
                NoteName = transitions.Keys.First(),
                Octave = currentNote.Octave,
                Duration = 1.0f,
                Velocity = 0.8f,
            };
        }

        #endregion

        #region L-System Generation

        /// <summary>
        /// Generates a melody using L-System (Lindenmayer system) fractal patterns.
        /// </summary>
        public Melody GenerateWithLSystem(
            string axiom,
            Dictionary<char, string> rules,
            int iterations
        )
        {
            if (string.IsNullOrWhiteSpace(axiom))
                throw new ArgumentException("Axiom cannot be empty", nameof(axiom));
            if (rules == null)
                throw new ArgumentNullException(nameof(rules));

            _logger.LogDebug(
                "Generating L-System melody: axiom={Axiom}, iterations={Iterations}",
                axiom,
                iterations
            );

            var current = axiom;
            for (int i = 0; i < iterations; i++)
            {
                current = ExpandLSystem(current, rules);
            }

            // Convert L-System string to musical notes
            var notes = ConvertLSystemToNotes(current);

            return new Melody
            {
                Notes = notes,
                Tempo = DefaultTempo,
                TimeSignature = Models.TimeSignature.CommonTime,
                Scale = Models.ScaleType.Major,
                Key = DefaultKey,
            };
        }

        private string ExpandLSystem(string current, Dictionary<char, string> rules)
        {
            var result = "";
            foreach (var c in current)
            {
                result += rules.ContainsKey(c) ? rules[c] : c.ToString();
            }
            return result;
        }

        private List<Models.Note> ConvertLSystemToNotes(string lsystem)
        {
            var notes = new List<Models.Note>();
            var scale = new[]
            {
                Models.NoteName.C,
                Models.NoteName.D,
                Models.NoteName.E,
                Models.NoteName.F,
                Models.NoteName.G,
                Models.NoteName.A,
                Models.NoteName.B,
            };
            var currentOctave = 4;
            var currentIndex = 0;

            foreach (var c in lsystem)
            {
                switch (c)
                {
                    case 'A':
                    case 'B':
                    case 'F':
                        notes.Add(
                            new Models.Note
                            {
                                NoteName = scale[currentIndex % scale.Length],
                                Octave = currentOctave,
                                Duration = 0.5f,
                                Velocity = 0.7f,
                            }
                        );
                        currentIndex++;
                        break;
                    case '+':
                        currentIndex++;
                        break;
                    case '-':
                        currentIndex = Math.Max(0, currentIndex - 1);
                        break;
                }
            }

            return notes;
        }

        #endregion

        #region Cellular Automata Generation

        /// <summary>
        /// Generates a melody using cellular automata rules (e.g., Rule 30, 90, 110).
        /// </summary>
        public Melody GenerateWithCellularAutomata(int rule, int generations)
        {
            if (generations <= 0)
                throw new ArgumentException(
                    "Generations must be greater than zero",
                    nameof(generations)
                );

            _logger.LogDebug(
                "Generating cellular automata melody: rule={Rule}, generations={Generations}",
                rule,
                generations
            );

            var width = 32;
            var cells = InitializeCells(width);
            var allGenerations = new List<bool[]> { cells };

            for (int gen = 0; gen < generations; gen++)
            {
                cells = ApplyCellularRule(cells, rule);
                allGenerations.Add(cells);
            }

            // Convert cellular automata to notes
            var notes = ConvertCellularAutomataToNotes(allGenerations);

            return new Melody
            {
                Notes = notes,
                Tempo = DefaultTempo,
                TimeSignature = Models.TimeSignature.CommonTime,
                Scale = Models.ScaleType.Major,
                Key = DefaultKey,
            };
        }

        private bool[] InitializeCells(int width)
        {
            var cells = new bool[width];
            cells[width / 2] = true; // Single cell in center
            return cells;
        }

        private bool[] ApplyCellularRule(bool[] cells, int rule)
        {
            var newCells = new bool[cells.Length];

            for (int i = 0; i < cells.Length; i++)
            {
                var left = i > 0 ? cells[i - 1] : false;
                var center = cells[i];
                var right = i < cells.Length - 1 ? cells[i + 1] : false;

                var neighborhood = (left ? 4 : 0) + (center ? 2 : 0) + (right ? 1 : 0);
                newCells[i] = ((rule >> neighborhood) & 1) == 1;
            }

            return newCells;
        }

        private List<Models.Note> ConvertCellularAutomataToNotes(List<bool[]> generations)
        {
            var notes = new List<Models.Note>();
            var scale = GetScaleNotes(DefaultKey, Models.ScaleType.Major, 60, 84);

            foreach (var generation in generations)
            {
                var activeCount = generation.Count(c => c);
                if (activeCount > 0)
                {
                    var noteIndex = activeCount % scale.Count;
                    notes.Add(scale[noteIndex]);
                }
            }

            return notes;
        }

        #endregion

        #region MIDI Export

        /// <summary>
        /// Exports a melody to a MIDI file.
        /// </summary>
        public MidiFile ExportToMidi(Melody melody, int tempo = 120)
        {
            if (melody == null)
                throw new ArgumentNullException(nameof(melody));

            _logger.LogDebug(
                "Exporting melody to MIDI: notes={Count}, tempo={Tempo}",
                melody.Notes.Count,
                tempo
            );

            var midiFile = new MidiFile();
            var trackChunk = new TrackChunk();

            // Set tempo
            trackChunk.Events.Add(new SetTempoEvent(60000000 / tempo));

            long currentTime = 0;
            foreach (var note in melody.Notes)
            {
                var midiNote = ConvertToMidiNote(note);
                var velocity = (SevenBitNumber)Math.Clamp((int)(note.Velocity * 127), 1, 127);
                var duration = (long)(note.Duration * 480); // 480 ticks per quarter note

                trackChunk.Events.Add(
                    new NoteOnEvent(midiNote.NoteNumber, velocity) { DeltaTime = currentTime }
                );

                trackChunk.Events.Add(
                    new NoteOffEvent(midiNote.NoteNumber, (SevenBitNumber)0)
                    {
                        DeltaTime = duration,
                    }
                );

                currentTime = 0;
            }

            midiFile.Chunks.Add(trackChunk);
            return midiFile;
        }

        /// <summary>
        /// Exports a chord progression to a MIDI file.
        /// </summary>
        public MidiFile ExportToMidi(ChordProgression progression)
        {
            if (progression == null)
                throw new ArgumentNullException(nameof(progression));

            _logger.LogDebug(
                "Exporting chord progression to MIDI: chords={Count}",
                progression.Chords.Count
            );

            var midiFile = new MidiFile();
            var trackChunk = new TrackChunk();

            // Set tempo
            trackChunk.Events.Add(new SetTempoEvent(60000000 / (int)progression.Tempo));

            long currentTime = 0;
            foreach (var chord in progression.Chords)
            {
                var chordNotes = chord.Notes;
                var duration = (long)(progression.ChordDuration * 480);

                // Note-on events for all chord notes
                foreach (var note in chordNotes)
                {
                    var midiNote = ConvertToMidiNote(note);
                    trackChunk.Events.Add(
                        new NoteOnEvent(midiNote.NoteNumber, (SevenBitNumber)80)
                        {
                            DeltaTime = currentTime,
                        }
                    );
                    currentTime = 0;
                }

                // Note-off events for all chord notes
                long noteOffDelta = duration;
                foreach (var note in chordNotes)
                {
                    var midiNote = ConvertToMidiNote(note);
                    trackChunk.Events.Add(
                        new NoteOffEvent(midiNote.NoteNumber, (SevenBitNumber)0)
                        {
                            DeltaTime = noteOffDelta,
                        }
                    );
                    noteOffDelta = 0;
                }

                currentTime = 0;
            }

            midiFile.Chunks.Add(trackChunk);
            return midiFile;
        }

        private MidiNote ConvertToMidiNote(Models.Note note)
        {
            var midiNoteName = (MidiNoteName)(int)note.NoteName;
            return MidiNote.Get(midiNoteName, note.Octave);
        }

        #endregion
    }
}
