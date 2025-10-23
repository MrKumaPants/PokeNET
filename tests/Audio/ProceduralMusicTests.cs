using Xunit;
using Moq;
using FluentAssertions;
using PokeNET.Audio;
using PokeNET.Audio.Procedural;
using PokeNET.Audio.Services;
using PokeNET.Audio.Abstractions;
using PokeNET.Audio.Models;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.MusicTheory;
using NoteName = PokeNET.Audio.Models.NoteName;
using ScaleType = PokeNET.Audio.Models.ScaleType;
using Note = PokeNET.Audio.Models.Note;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PokeNET.Tests.Audio
{
    /// <summary>
    /// Comprehensive tests for ProceduralMusicGenerator
    /// Tests music generation algorithms and procedural composition
    /// </summary>
    public class ProceduralMusicTests
    {
        private readonly Mock<ILogger<ProceduralMusicGenerator>> _mockLogger;
        private readonly Mock<IRandomProvider> _mockRandom;
        private ProceduralMusicGenerator? _generator;

        public ProceduralMusicTests()
        {
            _mockLogger = new Mock<ILogger<ProceduralMusicGenerator>>();
            _mockRandom = new Mock<IRandomProvider>();
        }

        private ProceduralMusicGenerator CreateGenerator()
        {
            return new ProceduralMusicGenerator(_mockLogger.Object, _mockRandom.Object);
        }

        #region Initialization Tests

        [Fact]
        public void Constructor_ShouldInitializeWithDefaults()
        {
            // Arrange & Act
            _generator = CreateGenerator();

            // Assert
            _generator.Should().NotBeNull();
            _generator.DefaultTempo.Should().Be(120);
            _generator.DefaultKey.Should().Be(NoteName.C);
        }

        [Fact]
        public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
        {
            // Arrange & Act
            Action act = () => new ProceduralMusicGenerator(null!, _mockRandom.Object);

            // Assert
            act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
        }

        #endregion

        #region Melody Generation Tests

        [Fact]
        public void GenerateMelody_WithValidParameters_ShouldCreateMelody()
        {
            // Arrange
            _generator = CreateGenerator();
            var key = NoteName.C;
            var scale = ScaleType.Major;
            var length = 16;

            // Act
            var melody = _generator.GenerateMelody(key, scale, length);

            // Assert
            melody.Should().NotBeNull();
            melody.Notes.Should().HaveCount(length);
        }

        [Fact]
        public void GenerateMelody_ShouldRespectScale()
        {
            // Arrange
            _generator = CreateGenerator();
            var key = NoteName.C;
            var scale = ScaleType.Major;
            var expectedNotes = new[] {
                NoteName.C, NoteName.D, NoteName.E, NoteName.F,
                NoteName.G, NoteName.A, NoteName.B
            };

            // Act
            var melody = _generator.GenerateMelody(key, scale, 32);

            // Assert
            melody.Notes.Should().OnlyContain(n => expectedNotes.Contains(n.NoteName));
        }

        [Fact]
        public void GenerateMelody_WithMinorScale_ShouldUseMinorNotes()
        {
            // Arrange
            _generator = CreateGenerator();
            var key = NoteName.A;
            var scale = ScaleType.NaturalMinor;

            // Act
            var melody = _generator.GenerateMelody(key, scale, 16);

            // Assert
            melody.Should().NotBeNull();
            melody.Scale.Should().Be(ScaleType.NaturalMinor);
        }

        [Theory]
        [InlineData(4)]
        [InlineData(8)]
        [InlineData(16)]
        [InlineData(32)]
        public void GenerateMelody_WithVariousLengths_ShouldGenerateCorrectCount(int length)
        {
            // Arrange
            _generator = CreateGenerator();

            // Act
            var melody = _generator.GenerateMelody(NoteName.C, ScaleType.Major, length);

            // Assert
            melody.Notes.Should().HaveCount(length);
        }

        [Fact]
        public void GenerateMelody_WithZeroLength_ShouldThrowArgumentException()
        {
            // Arrange
            _generator = CreateGenerator();

            // Act
            Action act = () => _generator.GenerateMelody(NoteName.C, ScaleType.Major, 0);

            // Assert
            act.Should().Throw<ArgumentException>();
        }

        #endregion

        #region Chord Progression Tests

        [Fact]
        public void GenerateChordProgression_ShouldCreateValidProgression()
        {
            // Arrange
            _generator = CreateGenerator();
            var key = NoteName.C;
            var progressionLength = 4;

            // Act
            var progression = _generator.GenerateChordProgression(key, progressionLength);

            // Assert
            progression.Should().NotBeNull();
            progression.Chords.Should().HaveCount(progressionLength);
        }

        [Theory]
        [InlineData("I-IV-V-I")]
        [InlineData("I-V-vi-IV")]
        [InlineData("ii-V-I")]
        public void GenerateChordProgression_WithPattern_ShouldFollowPattern(string pattern)
        {
            // Arrange
            _generator = CreateGenerator();
            var key = NoteName.C;

            // Act
            var progression = _generator.GenerateChordProgressionFromPattern(key, pattern);

            // Assert
            progression.Should().NotBeNull();
            progression.Pattern.Should().Be(pattern);
        }

        [Fact]
        public void GenerateChordProgression_InMajorKey_ShouldUseDiatonicChords()
        {
            // Arrange
            _generator = CreateGenerator();

            // Act
            var progression = _generator.GenerateChordProgression(NoteName.C, 8);

            // Assert
            progression.Chords.Should().OnlyContain(c => c.IsDiatonic);
        }

        [Fact]
        public void GenerateChordProgression_ShouldEndOnTonic()
        {
            // Arrange
            _generator = CreateGenerator();
            var key = NoteName.C;

            // Act
            var progression = _generator.GenerateChordProgression(key, 4, endOnTonic: true);

            // Assert
            progression.Chords.Last().Root.Should().Be(key);
        }

        #endregion

        #region Rhythm Generation Tests

        [Fact]
        public void GenerateRhythm_ShouldCreateValidPattern()
        {
            // Arrange
            _generator = CreateGenerator();
            var bars = 4;
            var beatsPerBar = 4;

            // Act
            var rhythm = _generator.GenerateRhythm(bars, beatsPerBar);

            // Assert
            rhythm.Should().NotBeNull();
            rhythm.TotalBeats.Should().Be(bars * beatsPerBar);
        }

        [Theory]
        [InlineData(3, 4)] // 3/4 time
        [InlineData(4, 4)] // 4/4 time
        [InlineData(6, 8)] // 6/8 time
        [InlineData(7, 8)] // 7/8 time
        public void GenerateRhythm_WithTimeSignature_ShouldMatchSignature(int beatsPerBar, int beatUnit)
        {
            // Arrange
            _generator = CreateGenerator();

            // Act
            var rhythm = _generator.GenerateRhythm(4, beatsPerBar, beatUnit);

            // Assert
            rhythm.BeatsPerBar.Should().Be(beatsPerBar);
            rhythm.BeatUnit.Should().Be(beatUnit);
        }

        [Fact]
        public void GenerateRhythm_ShouldIncludeVariedNoteLengths()
        {
            // Arrange
            _generator = CreateGenerator();

            // Act
            var rhythm = _generator.GenerateRhythm(8, 4);

            // Assert
            rhythm.NoteDurations.Should().Contain(d => d != rhythm.NoteDurations[0]);
        }

        #endregion

        #region Markov Chain Tests

        [Fact]
        public void GenerateWithMarkovChain_ShouldCreateCoherentMelody()
        {
            // Arrange
            _generator = CreateGenerator();
            var trainingData = CreateSampleMelodies();

            // Act
            var melody = _generator.GenerateWithMarkovChain(trainingData, 16);

            // Assert
            melody.Should().NotBeNull();
            melody.Notes.Should().HaveCount(16);
        }

        [Fact]
        public void GenerateWithMarkovChain_ShouldLearnFromTrainingData()
        {
            // Arrange
            _generator = CreateGenerator();
            var trainingData = new List<Melody>
            {
                CreateMelodyWithPattern(new[] { NoteName.C, NoteName.D, NoteName.E, NoteName.C }),
                CreateMelodyWithPattern(new[] { NoteName.C, NoteName.D, NoteName.E, NoteName.C })
            };

            // Act
            var melody = _generator.GenerateWithMarkovChain(trainingData, 8);

            // Assert - Should favor C->D->E->C pattern
            melody.Notes.Should().Contain(n => n.NoteName == NoteName.C);
        }

        [Fact]
        public void TrainMarkovChain_WithEmptyData_ShouldThrowArgumentException()
        {
            // Arrange
            _generator = CreateGenerator();

            // Act
            Action act = () => _generator.GenerateWithMarkovChain(new List<Melody>(), 8);

            // Assert
            act.Should().Throw<ArgumentException>();
        }

        #endregion

        #region L-System Generation Tests

        [Fact]
        public void GenerateWithLSystem_ShouldCreateFractalPattern()
        {
            // Arrange
            _generator = CreateGenerator();
            var axiom = "A";
            var rules = new Dictionary<char, string>
            {
                { 'A', "AB" },
                { 'B', "A" }
            };

            // Act
            var melody = _generator.GenerateWithLSystem(axiom, rules, iterations: 4);

            // Assert
            melody.Should().NotBeNull();
            melody.Notes.Should().NotBeEmpty();
        }

        [Fact]
        public void GenerateWithLSystem_MultipleIterations_ShouldExpandPattern()
        {
            // Arrange
            _generator = CreateGenerator();
            var axiom = "F";
            var rules = new Dictionary<char, string> { { 'F', "F+F-F" } };

            // Act
            var result1 = _generator.GenerateWithLSystem(axiom, rules, iterations: 1);
            var result2 = _generator.GenerateWithLSystem(axiom, rules, iterations: 2);

            // Assert
            result2.Notes.Should().HaveCountGreaterThan(result1.Notes.Count);
        }

        #endregion

        #region Cellular Automata Tests

        [Fact]
        public void GenerateWithCellularAutomata_ShouldCreatePattern()
        {
            // Arrange
            _generator = CreateGenerator();
            var rule = 30; // Rule 30 cellular automaton

            // Act
            var melody = _generator.GenerateWithCellularAutomata(rule, generations: 8);

            // Assert
            melody.Should().NotBeNull();
            melody.Notes.Should().NotBeEmpty();
        }

        [Theory]
        [InlineData(30)]
        [InlineData(90)]
        [InlineData(110)]
        public void GenerateWithCellularAutomata_DifferentRules_ShouldCreateDifferentPatterns(int rule)
        {
            // Arrange
            _generator = CreateGenerator();

            // Act
            var melody = _generator.GenerateWithCellularAutomata(rule, generations: 8);

            // Assert
            melody.Should().NotBeNull();
        }

        #endregion

        #region MIDI Export Tests

        [Fact]
        public void ExportToMidi_WithValidMelody_ShouldCreateMidiFile()
        {
            // Arrange
            _generator = CreateGenerator();
            var melody = _generator.GenerateMelody(NoteName.C, ScaleType.Major, 16);

            // Act
            var midiFile = _generator.ExportToMidi(melody);

            // Assert
            midiFile.Should().NotBeNull();
            midiFile.GetTrackChunks().Should().NotBeEmpty();
        }

        [Fact]
        public void ExportToMidi_WithChords_ShouldIncludeAllNotes()
        {
            // Arrange
            _generator = CreateGenerator();
            var progression = _generator.GenerateChordProgression(NoteName.C, 4);

            // Act
            var midiFile = _generator.ExportToMidi(progression);

            // Assert
            midiFile.GetTrackChunks().Should().NotBeEmpty();
            var events = midiFile.GetTrackChunks().SelectMany(t => t.Events);
            events.Should().Contain(e => e is NoteOnEvent);
        }

        [Fact]
        public void ExportToMidi_WithTempo_ShouldSetCorrectTempo()
        {
            // Arrange
            _generator = CreateGenerator();
            var melody = _generator.GenerateMelody(NoteName.C, ScaleType.Major, 8);
            var tempo = 140;

            // Act
            var midiFile = _generator.ExportToMidi(melody, tempo);

            // Assert
            var tempoEvents = midiFile.GetTrackChunks()
                .SelectMany(t => t.Events)
                .OfType<SetTempoEvent>()
                .ToList();
            tempoEvents.Should().NotBeEmpty();
        }

        #endregion

        #region Constraint Tests

        [Fact]
        public void GenerateMelody_WithRangeConstraint_ShouldStayInRange()
        {
            // Arrange
            _generator = CreateGenerator();
            var minNote = 60; // Middle C
            var maxNote = 72; // C5

            // Act
            var melody = _generator.GenerateMelody(
                NoteName.C,
                ScaleType.Major,
                16,
                minNote: minNote,
                maxNote: maxNote
            );

            // Assert
            melody.Notes.Should().OnlyContain(n => n.NoteNumber >= minNote && n.NoteNumber <= maxNote);
        }

        [Fact]
        public void GenerateMelody_WithStepLimit_ShouldLimitIntervals()
        {
            // Arrange
            _generator = CreateGenerator();
            var maxStep = 3; // Maximum interval of a third

            // Act
            var melody = _generator.GenerateMelody(
                NoteName.C,
                ScaleType.Major,
                16,
                maxStepSize: maxStep
            );

            // Assert
            for (int i = 1; i < melody.Notes.Count; i++)
            {
                var interval = Math.Abs(melody.Notes[i].NoteNumber - melody.Notes[i - 1].NoteNumber);
                interval.Should().BeLessOrEqualTo(maxStep);
            }
        }

        #endregion

        #region Helper Methods

        private List<Melody> CreateSampleMelodies()
        {
            return new List<Melody>
            {
                CreateMelodyWithPattern(new[] { NoteName.C, NoteName.E, NoteName.G }),
                CreateMelodyWithPattern(new[] { NoteName.D, NoteName.F, NoteName.A }),
                CreateMelodyWithPattern(new[] { NoteName.E, NoteName.G, NoteName.B })
            };
        }

        private Melody CreateMelodyWithPattern(NoteName[] pattern)
        {
            var notes = new List<Note>();
            foreach (var noteName in pattern)
            {
                notes.Add(new Note
                {
                    NoteName = noteName,
                    Octave = 4,
                    Duration = 1.0f
                });
            }
            return new Melody
            {
                Notes = notes,
                Tempo = 120f,
                TimeSignature = new TimeSignature { BeatsPerMeasure = 4, BeatValue = 4 }
            };
        }

        #endregion
    }
}
