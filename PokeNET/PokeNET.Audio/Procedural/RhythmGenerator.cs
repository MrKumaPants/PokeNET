using PokeNET.Audio.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PokeNET.Audio.Procedural
{
    /// <summary>
    /// Generates rhythmic patterns
    /// </summary>
    public class RhythmGenerator
    {
        private readonly IRandomProvider _randomProvider;

        // Common rhythm patterns (in 16th note subdivisions per beat)
        private static readonly Dictionary<string, int[][]> _rhythmPatterns = new()
        {
            ["straight"] = new[] { new[] { 4, 4, 4, 4 } },  // Quarter notes
            ["swing"] = new[] { new[] { 3, 1, 3, 1, 3, 1, 3, 1 } },
            ["syncopated"] = new[] { new[] { 3, 1, 2, 2, 3, 1, 2, 2 } },
            ["triplet"] = new[] { new[] { 2, 2, 2, 2, 2, 2, 2, 2 } },  // 8th note triplets
            ["dotted"] = new[] { new[] { 3, 1, 3, 1, 4, 4 } },
            ["chiptune"] = new[] { new[] { 2, 2, 2, 2, 2, 2, 2, 2 }, new[] { 4, 2, 2, 4, 2, 2 } },
            ["dance"] = new[] { new[] { 4, 4, 4, 4 }, new[] { 2, 2, 2, 2, 2, 2, 2, 2 } },
            ["ambient"] = new[] { new[] { 8, 8 }, new[] { 6, 2, 6, 2 } }
        };

        public RhythmGenerator(IRandomProvider randomProvider)
        {
            _randomProvider = randomProvider ?? throw new ArgumentNullException(nameof(randomProvider));
        }

        /// <summary>
        /// Generates a rhythmic pattern
        /// </summary>
        public List<RhythmicEvent> GeneratePattern(
            string style,
            int bars,
            int beatsPerBar = 4,
            int subdivisionsPerBeat = 4)  // 16th notes
        {
            var pattern = new List<RhythmicEvent>();
            var basePattern = GetRhythmPattern(style);
            var totalSubdivisions = bars * beatsPerBar * subdivisionsPerBeat;

            double currentTime = 0.0;
            int patternIndex = 0;

            while (currentTime < totalSubdivisions)
            {
                var duration = basePattern[patternIndex % basePattern.Length];
                var beat = (int)(currentTime / subdivisionsPerBeat);
                var subdivision = (int)(currentTime % subdivisionsPerBeat);

                pattern.Add(new RhythmicEvent
                {
                    StartTime = currentTime / subdivisionsPerBeat,  // Convert to beats
                    Duration = duration / (double)subdivisionsPerBeat,
                    Velocity = CalculateVelocity(beat, subdivision, beatsPerBar),
                    IsOnBeat = subdivision == 0,
                    IsOnDownbeat = (beat % beatsPerBar) == 0 && subdivision == 0
                });

                currentTime += duration;
                patternIndex++;
            }

            return pattern;
        }

        /// <summary>
        /// Generates rhythm based on energy and complexity
        /// </summary>
        public List<RhythmicEvent> GenerateEnergeticPattern(
            float energy,        // 0.0 = sparse, 1.0 = dense
            float complexity,    // 0.0 = simple, 1.0 = complex
            int bars,
            int beatsPerBar = 4)
        {
            var pattern = new List<RhythmicEvent>();
            var subdivisionsPerBeat = complexity > 0.5f ? 4 : 2;
            var totalSubdivisions = bars * beatsPerBar * subdivisionsPerBeat;

            double currentTime = 0.0;

            while (currentTime < totalSubdivisions)
            {
                // Energy affects note density
                var noteProbability = 0.3 + (energy * 0.7);

                if (_randomProvider.NextDouble() < noteProbability)
                {
                    // Complexity affects duration variation
                    var duration = SelectDuration(complexity, subdivisionsPerBeat);
                    var beat = (int)(currentTime / subdivisionsPerBeat);
                    var subdivision = (int)(currentTime % subdivisionsPerBeat);

                    pattern.Add(new RhythmicEvent
                    {
                        StartTime = currentTime / subdivisionsPerBeat,
                        Duration = duration / (double)subdivisionsPerBeat,
                        Velocity = CalculateEnergeticVelocity(energy, beat, subdivision, beatsPerBar),
                        IsOnBeat = subdivision == 0,
                        IsOnDownbeat = (beat % beatsPerBar) == 0 && subdivision == 0
                    });

                    currentTime += duration;
                }
                else
                {
                    // Rest
                    currentTime += subdivisionsPerBeat;
                }
            }

            return pattern;
        }

        /// <summary>
        /// Generates a drum pattern
        /// </summary>
        public Dictionary<DrumType, List<RhythmicEvent>> GenerateDrumPattern(
            string style,
            int bars,
            int beatsPerBar = 4)
        {
            var patterns = new Dictionary<DrumType, List<RhythmicEvent>>();

            switch (style.ToLower())
            {
                case "chiptune":
                    patterns[DrumType.Kick] = GenerateKickPattern(bars, beatsPerBar, new[] { 0, 2 });
                    patterns[DrumType.Snare] = GenerateSnarePattern(bars, beatsPerBar, new[] { 1, 3 });
                    patterns[DrumType.HiHat] = GenerateHiHatPattern(bars, beatsPerBar, 8);  // 8th notes
                    break;

                case "dance":
                    patterns[DrumType.Kick] = GenerateKickPattern(bars, beatsPerBar, new[] { 0, 1, 2, 3 });
                    patterns[DrumType.Snare] = GenerateSnarePattern(bars, beatsPerBar, new[] { 1, 3 });
                    patterns[DrumType.HiHat] = GenerateHiHatPattern(bars, beatsPerBar, 16);  // 16th notes
                    patterns[DrumType.Clap] = GenerateSnarePattern(bars, beatsPerBar, new[] { 1, 3 });
                    break;

                case "ambient":
                    patterns[DrumType.Kick] = GenerateKickPattern(bars, beatsPerBar, new[] { 0 });
                    patterns[DrumType.HiHat] = GenerateHiHatPattern(bars, beatsPerBar, 4, 0.3f);  // Sparse
                    break;

                default:  // Rock/Pop
                    patterns[DrumType.Kick] = GenerateKickPattern(bars, beatsPerBar, new[] { 0, 2 });
                    patterns[DrumType.Snare] = GenerateSnarePattern(bars, beatsPerBar, new[] { 1, 3 });
                    patterns[DrumType.HiHat] = GenerateHiHatPattern(bars, beatsPerBar, 8);
                    break;
            }

            return patterns;
        }

        private int[] GetRhythmPattern(string style)
        {
            style = style.ToLower();
            if (_rhythmPatterns.TryGetValue(style, out var patterns))
            {
                return patterns[_randomProvider.Next(patterns.Length)];
            }

            return _rhythmPatterns["straight"][0];
        }

        private int SelectDuration(float complexity, int subdivisionsPerBeat)
        {
            if (complexity < 0.3f)
            {
                // Simple: quarter and eighth notes
                return _randomProvider.Next(2) == 0 ? subdivisionsPerBeat : subdivisionsPerBeat / 2;
            }
            else if (complexity < 0.7f)
            {
                // Medium: add 16th notes
                var durations = new[] { subdivisionsPerBeat, subdivisionsPerBeat / 2, subdivisionsPerBeat / 4 };
                return durations[_randomProvider.Next(durations.Length)];
            }
            else
            {
                // Complex: include triplets and syncopation
                var durations = new[] {
                    subdivisionsPerBeat,
                    subdivisionsPerBeat / 2,
                    subdivisionsPerBeat / 4,
                    subdivisionsPerBeat / 3,  // Triplets
                    subdivisionsPerBeat * 3 / 4  // Dotted
                };
                return durations[_randomProvider.Next(durations.Length)];
            }
        }

        private float CalculateVelocity(int beat, int subdivision, int beatsPerBar)
        {
            // Downbeat is strongest
            if ((beat % beatsPerBar) == 0 && subdivision == 0)
                return 1.0f;

            // On-beats are medium
            if (subdivision == 0)
                return 0.8f;

            // Off-beats are softer
            return 0.6f;
        }

        private float CalculateEnergeticVelocity(float energy, int beat, int subdivision, int beatsPerBar)
        {
            var baseVelocity = CalculateVelocity(beat, subdivision, beatsPerBar);

            // Energy increases overall velocity
            var velocityRange = 0.3f + (energy * 0.7f);

            return baseVelocity * velocityRange;
        }

        private List<RhythmicEvent> GenerateKickPattern(int bars, int beatsPerBar, int[] beatPositions)
        {
            var pattern = new List<RhythmicEvent>();

            for (int bar = 0; bar < bars; bar++)
            {
                foreach (var beatPos in beatPositions)
                {
                    if (beatPos < beatsPerBar)
                    {
                        pattern.Add(new RhythmicEvent
                        {
                            StartTime = bar * beatsPerBar + beatPos,
                            Duration = 0.25,
                            Velocity = 1.0f,
                            IsOnBeat = true,
                            IsOnDownbeat = beatPos == 0
                        });
                    }
                }
            }

            return pattern;
        }

        private List<RhythmicEvent> GenerateSnarePattern(int bars, int beatsPerBar, int[] beatPositions)
        {
            var pattern = new List<RhythmicEvent>();

            for (int bar = 0; bar < bars; bar++)
            {
                foreach (var beatPos in beatPositions)
                {
                    if (beatPos < beatsPerBar)
                    {
                        pattern.Add(new RhythmicEvent
                        {
                            StartTime = bar * beatsPerBar + beatPos,
                            Duration = 0.25,
                            Velocity = 0.9f,
                            IsOnBeat = true,
                            IsOnDownbeat = false
                        });
                    }
                }
            }

            return pattern;
        }

        private List<RhythmicEvent> GenerateHiHatPattern(int bars, int beatsPerBar, int notesPerBar, float density = 1.0f)
        {
            var pattern = new List<RhythmicEvent>();
            var subdivisions = notesPerBar / beatsPerBar;

            for (int bar = 0; bar < bars; bar++)
            {
                for (int i = 0; i < notesPerBar; i++)
                {
                    if (_randomProvider.NextDouble() < density)
                    {
                        var isOnBeat = (i % subdivisions) == 0;
                        pattern.Add(new RhythmicEvent
                        {
                            StartTime = bar * beatsPerBar + (i / (double)subdivisions),
                            Duration = 1.0 / subdivisions,
                            Velocity = isOnBeat ? 0.7f : 0.5f,
                            IsOnBeat = isOnBeat,
                            IsOnDownbeat = i == 0
                        });
                    }
                }
            }

            return pattern;
        }
    }

    /// <summary>
    /// Represents a rhythmic event (note or hit)
    /// </summary>
    public class RhythmicEvent
    {
        public double StartTime { get; set; }      // In beats
        public double Duration { get; set; }       // In beats
        public float Velocity { get; set; }        // 0.0 - 1.0
        public bool IsOnBeat { get; set; }
        public bool IsOnDownbeat { get; set; }
    }

    public enum DrumType
    {
        Kick,
        Snare,
        HiHat,
        Tom,
        Crash,
        Ride,
        Clap,
        Rim
    }
}
