using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.MusicTheory;
using Melanchall.DryWetMidi.Interaction;
using System;
using System.Collections.Generic;
using System.Linq;
using Note = Melanchall.DryWetMidi.MusicTheory.Note;
using Chord = Melanchall.DryWetMidi.MusicTheory.Chord;

namespace PokeNET.Audio.Procedural
{
    /// <summary>
    /// Generates procedural music based on game state parameters using DryWetMidi
    /// </summary>
    public class ProceduralMusicGenerator
    {
        private readonly ChordProgressionGenerator _chordGenerator;
        private readonly MelodyGenerator _melodyGenerator;
        private readonly RhythmGenerator _rhythmGenerator;
        private readonly Random _random;

        public ProceduralMusicGenerator(int? seed = null)
        {
            var actualSeed = seed ?? Environment.TickCount;
            _random = new Random(actualSeed);
            _chordGenerator = new ChordProgressionGenerator(actualSeed);
            _melodyGenerator = new MelodyGenerator(actualSeed);
            _rhythmGenerator = new RhythmGenerator(actualSeed);
        }

        /// <summary>
        /// Generates music based on game state parameters
        /// </summary>
        public MidiFile GenerateMusic(GameMusicParameters parameters)
        {
            var midiFile = new MidiFile();
            var tempoMap = CreateTempoMap(parameters.BattleIntensity, parameters.TimeOfDay);

            // Create tracks based on style
            switch (parameters.Style)
            {
                case MusicStyle.Chiptune:
                    GenerateChiptuneMusic(midiFile, tempoMap, parameters);
                    break;

                case MusicStyle.Orchestral:
                    GenerateOrchestralMusic(midiFile, tempoMap, parameters);
                    break;

                case MusicStyle.Ambient:
                    GenerateAmbientMusic(midiFile, tempoMap, parameters);
                    break;

                case MusicStyle.Electronic:
                    GenerateElectronicMusic(midiFile, tempoMap, parameters);
                    break;

                default:
                    GenerateChiptuneMusic(midiFile, tempoMap, parameters);
                    break;
            }

            return midiFile;
        }

        /// <summary>
        /// Adapts existing music to new game state in real-time
        /// </summary>
        public MidiFile AdaptMusic(MidiFile currentMusic, GameMusicParameters newParameters)
        {
            // Create new music with smooth transition
            var newMusic = GenerateMusic(newParameters);

            // TODO: Implement smooth transition logic
            // This would involve crossfading, tempo adjustment, etc.

            return newMusic;
        }

        private void GenerateChiptuneMusic(MidiFile midiFile, TempoMap tempoMap, GameMusicParameters parameters)
        {
            var scale = GetScaleForLocation(parameters.LocationType);
            var bars = 16;
            var beatsPerBar = 4;

            // Generate chord progression
            var progression = _chordGenerator.GenerateEmotionalProgression(
                scale,
                parameters.BattleIntensity,
                0.3f,  // Low complexity for chiptune
                bars,
                octave: 3
            );

            // Lead melody track (square wave)
            var leadTrack = CreateMelodyTrack(
                scale,
                progression,
                parameters.BattleIntensity,
                minOctave: 5,
                maxOctave: 6,
                channel: 0
            );
            midiFile.Chunks.Add(leadTrack);

            // Bass track (triangle wave)
            var bassTrack = CreateBassTrack(
                progression,
                channel: 1
            );
            midiFile.Chunks.Add(bassTrack);

            // Arpeggio track (pulse wave)
            var arpeggioTrack = CreateArpeggioTrack(
                progression,
                parameters.BattleIntensity,
                channel: 2
            );
            midiFile.Chunks.Add(arpeggioTrack);

            // Drum track (noise channel)
            var drumTrack = CreateDrumTrack(
                "chiptune",
                bars,
                beatsPerBar,
                parameters.BattleIntensity,
                channel: 9  // Standard MIDI drum channel
            );
            midiFile.Chunks.Add(drumTrack);
        }

        private void GenerateOrchestralMusic(MidiFile midiFile, TempoMap tempoMap, GameMusicParameters parameters)
        {
            var scale = GetScaleForLocation(parameters.LocationType);
            var bars = 32;

            // Generate complex chord progression
            var progression = _chordGenerator.GenerateEmotionalProgression(
                scale,
                parameters.BattleIntensity,
                0.8f,  // High complexity for orchestral
                bars,
                octave: 3
            );

            // Strings section
            var violinTrack = CreateMelodyTrack(scale, progression, parameters.BattleIntensity,
                minOctave: 5, maxOctave: 7, channel: 0);
            midiFile.Chunks.Add(violinTrack);

            // Brass section
            var brassTrack = CreateHarmonyTrack(progression, channel: 1);
            midiFile.Chunks.Add(brassTrack);

            // Woodwinds
            var fluteTrack = CreateMelodyTrack(scale, progression, parameters.BattleIntensity,
                minOctave: 6, maxOctave: 7, channel: 2);
            midiFile.Chunks.Add(fluteTrack);

            // Timpani
            var timpaniTrack = CreateDrumTrack("orchestral", bars, 4, parameters.BattleIntensity, channel: 9);
            midiFile.Chunks.Add(timpaniTrack);
        }

        private void GenerateAmbientMusic(MidiFile midiFile, TempoMap tempoMap, GameMusicParameters parameters)
        {
            var scale = GetScaleForLocation(parameters.LocationType);
            var bars = 64;  // Longer phrases for ambient

            // Generate simple progression
            var progression = _chordGenerator.GenerateProgression(
                scale,
                "calm",
                bars,
                chordsPerBar: 1,
                octave: 3
            );

            // Pad track (sustained chords)
            var padTrack = CreatePadTrack(progression, channel: 0);
            midiFile.Chunks.Add(padTrack);

            // Sparse melody
            var melodyTrack = CreateMelodyTrack(
                scale,
                progression,
                0.2f,  // Low energy
                minOctave: 5,
                maxOctave: 6,
                channel: 1
            );
            midiFile.Chunks.Add(melodyTrack);

            // Texture layer
            var textureTrack = CreateTextureTrack(scale, bars, channel: 2);
            midiFile.Chunks.Add(textureTrack);
        }

        private void GenerateElectronicMusic(MidiFile midiFile, TempoMap tempoMap, GameMusicParameters parameters)
        {
            var scale = GetScaleForLocation(parameters.LocationType);
            var bars = 16;

            var progression = _chordGenerator.GenerateEmotionalProgression(
                scale,
                parameters.BattleIntensity,
                0.6f,
                bars,
                octave: 3
            );

            // Synth lead
            var leadTrack = CreateMelodyTrack(scale, progression, parameters.BattleIntensity,
                minOctave: 5, maxOctave: 6, channel: 0);
            midiFile.Chunks.Add(leadTrack);

            // Bass synth
            var bassTrack = CreateBassTrack(progression, channel: 1);
            midiFile.Chunks.Add(bassTrack);

            // Drums
            var drumTrack = CreateDrumTrack("dance", bars, 4, parameters.BattleIntensity, channel: 9);
            midiFile.Chunks.Add(drumTrack);
        }

        private TrackChunk CreateMelodyTrack(
            Scale scale,
            List<ChordInfo> progression,
            float energy,
            int minOctave,
            int maxOctave,
            int channel)
        {
            var track = new TrackChunk();
            var notesPerChord = (int)Math.Ceiling(2 + energy * 6);

            var melody = _melodyGenerator.GenerateEmotionalMelody(
                scale,
                progression,
                energy,
                movement: energy * 0.5f,
                tension: energy * 0.3f,
                minOctave,
                maxOctave
            );

            long currentTime = 0;
            foreach (var note in melody)
            {
                var velocity = MusicTheoryHelper.GetVelocity(
                    energy > 0.7f ? DynamicsLevel.F : energy > 0.4f ? DynamicsLevel.MF : DynamicsLevel.MP
                );

                var noteOnEvent = new NoteOnEvent(note.Note.NoteNumber, velocity)
                {
                    Channel = (FourBitNumber)channel,
                    DeltaTime = currentTime
                };

                var duration = (long)(note.Duration * 480 * 4);  // Convert to ticks (480 per quarter note)
                var noteOffEvent = new NoteOffEvent(note.Note.NoteNumber, (SevenBitNumber)0)
                {
                    Channel = (FourBitNumber)channel,
                    DeltaTime = duration
                };

                track.Events.Add(noteOnEvent);
                track.Events.Add(noteOffEvent);

                currentTime = 0;  // Reset for next note
            }

            return track;
        }

        private TrackChunk CreateBassTrack(List<ChordInfo> progression, int channel)
        {
            var track = new TrackChunk();
            long currentTime = 0;

            foreach (var chord in progression)
            {
                var rootNote = chord.Notes[0];
                var bassNote = Note.Get(rootNote.NoteName, 2);  // Bass octave

                var noteOnEvent = new NoteOnEvent(bassNote.NoteNumber, (SevenBitNumber)100)
                {
                    Channel = (FourBitNumber)channel,
                    DeltaTime = currentTime
                };

                var duration = (long)(chord.Duration * 480 * 4);
                var noteOffEvent = new NoteOffEvent(bassNote.NoteNumber, (SevenBitNumber)0)
                {
                    Channel = (FourBitNumber)channel,
                    DeltaTime = duration
                };

                track.Events.Add(noteOnEvent);
                track.Events.Add(noteOffEvent);

                currentTime = 0;
            }

            return track;
        }

        private TrackChunk CreateArpeggioTrack(List<ChordInfo> progression, float energy, int channel)
        {
            var track = new TrackChunk();
            long currentTime = 0;
            var notesPerChord = (int)(4 + energy * 4);  // 4-8 notes per chord

            foreach (var chord in progression)
            {
                var chordNotes = chord.Notes;
                var noteDuration = (long)(chord.Duration * 480 * 4 / notesPerChord);

                for (int i = 0; i < notesPerChord; i++)
                {
                    var note = chordNotes[i % chordNotes.Count];

                    var noteOnEvent = new NoteOnEvent(note.NoteNumber, (SevenBitNumber)80)
                    {
                        Channel = (FourBitNumber)channel,
                        DeltaTime = currentTime
                    };

                    var noteOffEvent = new NoteOffEvent(note.NoteNumber, (SevenBitNumber)0)
                    {
                        Channel = (FourBitNumber)channel,
                        DeltaTime = noteDuration
                    };

                    track.Events.Add(noteOnEvent);
                    track.Events.Add(noteOffEvent);

                    currentTime = 0;
                }
            }

            return track;
        }

        private TrackChunk CreatePadTrack(List<ChordInfo> progression, int channel)
        {
            var track = new TrackChunk();
            long currentTime = 0;

            foreach (var chord in progression)
            {
                var duration = (long)(chord.Duration * 480 * 4);

                // Play all chord notes together
                foreach (var note in chord.Notes)
                {
                    var noteOnEvent = new NoteOnEvent(note.NoteNumber, (SevenBitNumber)60)
                    {
                        Channel = (FourBitNumber)channel,
                        DeltaTime = currentTime
                    };
                    track.Events.Add(noteOnEvent);
                    currentTime = 0;
                }

                // Note offs
                foreach (var note in chord.Notes)
                {
                    var noteOffEvent = new NoteOffEvent(note.NoteNumber, (SevenBitNumber)0)
                    {
                        Channel = (FourBitNumber)channel,
                        DeltaTime = duration
                    };
                    track.Events.Add(noteOffEvent);
                    duration = 0;
                }

                currentTime = 0;
            }

            return track;
        }

        private TrackChunk CreateHarmonyTrack(List<ChordInfo> progression, int channel)
        {
            // Similar to pad but with rhythm
            return CreatePadTrack(progression, channel);
        }

        private TrackChunk CreateTextureTrack(Scale scale, int bars, int channel)
        {
            var track = new TrackChunk();
            var scaleNotes = MusicTheoryHelper.GetScaleNotes(scale, 4, 6);

            // Sparse random notes for texture
            for (int i = 0; i < bars * 4; i++)
            {
                if (_random.NextDouble() < 0.2)  // 20% chance
                {
                    var note = MusicTheoryHelper.GetRandomNoteFromScale(scaleNotes);
                    var velocity = MusicTheoryHelper.GetVelocity(DynamicsLevel.PP);
                    var duration = 480L * 2;  // Half note

                    track.Events.Add(new NoteOnEvent(note.NoteNumber, velocity)
                    {
                        Channel = (FourBitNumber)channel,
                        DeltaTime = i * 480L
                    });

                    track.Events.Add(new NoteOffEvent(note.NoteNumber, (SevenBitNumber)0)
                    {
                        Channel = (FourBitNumber)channel,
                        DeltaTime = duration
                    });
                }
            }

            return track;
        }

        private TrackChunk CreateDrumTrack(string style, int bars, int beatsPerBar, float energy, int channel)
        {
            var track = new TrackChunk();
            var drumPatterns = _rhythmGenerator.GenerateDrumPattern(style, bars, beatsPerBar);

            foreach (var (drumType, pattern) in drumPatterns)
            {
                var midiNote = GetDrumMidiNote(drumType);

                foreach (var hit in pattern)
                {
                    var startTicks = (long)(hit.StartTime * 480);
                    var velocity = (SevenBitNumber)(hit.Velocity * energy * 127);

                    track.Events.Add(new NoteOnEvent((SevenBitNumber)midiNote, velocity)
                    {
                        Channel = (FourBitNumber)channel,
                        DeltaTime = startTicks
                    });

                    track.Events.Add(new NoteOffEvent((SevenBitNumber)midiNote, (SevenBitNumber)0)
                    {
                        Channel = (FourBitNumber)channel,
                        DeltaTime = (long)(hit.Duration * 480)
                    });
                }
            }

            return track;
        }

        private TempoMap CreateTempoMap(float battleIntensity, TimeOfDay timeOfDay)
        {
            // Base tempo influenced by intensity
            var baseTempo = 90 + (int)(battleIntensity * 60);  // 90-150 BPM

            // Adjust for time of day
            baseTempo = timeOfDay switch
            {
                TimeOfDay.Morning => (int)(baseTempo * 0.9f),
                TimeOfDay.Night => (int)(baseTempo * 0.85f),
                _ => baseTempo
            };

            var tempo = new Tempo(baseTempo);
            return TempoMap.Create(tempo);
        }

        private Scale GetScaleForLocation(LocationType locationType)
        {
            return locationType switch
            {
                LocationType.Calm => MusicTheoryHelper.GetScale(NoteName.C, ScaleType.Major),
                LocationType.Tense => MusicTheoryHelper.GetScale(NoteName.D, ScaleType.Minor),
                LocationType.Mysterious => MusicTheoryHelper.GetScale(NoteName.E, ScaleType.Phrygian),
                LocationType.Epic => MusicTheoryHelper.GetScale(NoteName.A, ScaleType.Minor),
                _ => MusicTheoryHelper.GetScale(NoteName.C, ScaleType.Major)
            };
        }

        private int GetDrumMidiNote(DrumType drumType)
        {
            return drumType switch
            {
                DrumType.Kick => 36,      // Bass Drum 1
                DrumType.Snare => 38,     // Acoustic Snare
                DrumType.HiHat => 42,     // Closed Hi-Hat
                DrumType.Tom => 45,       // Low Tom
                DrumType.Crash => 49,     // Crash Cymbal 1
                DrumType.Ride => 51,      // Ride Cymbal 1
                DrumType.Clap => 39,      // Hand Clap
                DrumType.Rim => 37,       // Side Stick
                _ => 36
            };
        }
    }

    /// <summary>
    /// Parameters for procedural music generation based on game state
    /// </summary>
    public class GameMusicParameters
    {
        public float BattleIntensity { get; set; }  // 0.0 = calm, 1.0 = intense
        public LocationType LocationType { get; set; }
        public TimeOfDay TimeOfDay { get; set; }
        public MusicStyle Style { get; set; }
        public int DurationBars { get; set; } = 16;
    }

    public enum LocationType
    {
        Calm,
        Tense,
        Mysterious,
        Epic,
        Safe,
        Dangerous
    }

    public enum TimeOfDay
    {
        Morning,
        Day,
        Evening,
        Night
    }

    public enum MusicStyle
    {
        Chiptune,
        Orchestral,
        Ambient,
        Electronic,
        Rock,
        Jazz
    }
}
