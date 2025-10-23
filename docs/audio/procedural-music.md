# Procedural Music Generation Guide

## Introduction

PokeNET's procedural music system uses **DryWetMidi** to generate dynamic, context-aware music in real-time. This guide covers everything from basic concepts to advanced techniques.

## Table of Contents

1. [Music Theory Basics](#music-theory-basics)
2. [Music Settings](#music-settings)
3. [Track Configuration](#track-configuration)
4. [Pattern Types](#pattern-types)
5. [Advanced Techniques](#advanced-techniques)
6. [Complete Examples](#complete-examples)
7. [Performance Tips](#performance-tips)

## Music Theory Basics

### Musical Keys

A **key** defines the tonal center of your music:

```csharp
// Common keys for different moods
var happyKey = NoteName.C;      // C Major - bright, happy
var sadKey = NoteName.A;        // A Minor - melancholic
var epicKey = NoteName.D;       // D Major - triumphant
var mysteriousKey = NoteName.F; // F Minor - mysterious
```

### Scales

**Scales** determine which notes are available:

```csharp
public enum ScaleType
{
    Major,        // Happy, bright (Do-Re-Mi pattern)
    Minor,        // Sad, emotional (La-Ti-Do pattern)
    Dorian,       // Jazzy, sophisticated
    Phrygian,     // Spanish, exotic
    Lydian,       // Dreamy, floating
    Mixolydian,   // Bluesy, rock
    Aeolian,      // Natural minor
    Locrian,      // Dissonant, unstable
    Pentatonic,   // Simple, universal (5 notes)
    Blues         // Blues scale with blue notes
}
```

**Scale Examples:**

```csharp
// Major scale: C D E F G A B
var major = new MusicSettings
{
    Key = NoteName.C,
    Scale = ScaleType.Major,
    Mood = MusicMood.Happy
};

// Minor scale: A B C D E F G
var minor = new MusicSettings
{
    Key = NoteName.A,
    Scale = ScaleType.Minor,
    Mood = MusicMood.Sad
};

// Pentatonic: C D E G A (no half steps)
var pentatonic = new MusicSettings
{
    Key = NoteName.C,
    Scale = ScaleType.Pentatonic,
    Mood = MusicMood.Calm
};
```

### Time Signatures

**Time signatures** define the rhythmic structure:

```csharp
// 4/4 - Standard time (4 beats per measure)
var standardTime = new TimeSignature(4, 4);

// 3/4 - Waltz time (3 beats per measure)
var waltzTime = new TimeSignature(3, 4);

// 6/8 - Compound time (feels like 2 beats)
var compoundTime = new TimeSignature(6, 8);

// 5/4 - Unusual time (progressive rock)
var progressiveTime = new TimeSignature(5, 4);
```

### Tempo

**Tempo** (BPM - Beats Per Minute) controls the speed:

```csharp
// Very slow (40-60 BPM)
var largo = 50;

// Slow (60-80 BPM)
var adagio = 70;

// Moderate (80-120 BPM)
var moderato = 100;

// Fast (120-160 BPM)
var allegro = 140;

// Very fast (160-200 BPM)
var presto = 180;
```

## Music Settings

### Basic Settings Structure

```csharp
var settings = new MusicSettings
{
    // Timing
    Tempo = 120,                            // BPM
    TimeSignature = new TimeSignature(4, 4),// 4/4 time
    Measures = 16,                          // Length in measures

    // Tonality
    Key = NoteName.C,                       // Root note
    Scale = ScaleType.Major,                // Scale type

    // Character
    Mood = MusicMood.Neutral,               // Overall mood
    Complexity = 50                         // 0-100
};
```

### Mood System

```csharp
public enum MusicMood
{
    Calm,        // Peaceful, relaxed
    Happy,       // Upbeat, joyful
    Sad,         // Melancholic, emotional
    Tense,       // Suspenseful, anxious
    Mysterious,  // Enigmatic, unknown
    Epic,        // Grandiose, heroic
    Neutral      // Balanced, standard
}
```

**Mood affects:**
- Instrument selection
- Pattern density
- Harmonic choices
- Rhythm complexity

### Complexity Levels

```csharp
// Low complexity (0-30)
// - Simple melodies
// - Basic rhythms
// - Fewer notes
var simple = new MusicSettings { Complexity = 20 };

// Medium complexity (30-70)
// - Moderate melodies
// - Varied rhythms
// - Balanced density
var moderate = new MusicSettings { Complexity = 50 };

// High complexity (70-100)
// - Complex melodies
// - Intricate rhythms
// - Dense arrangements
var complex = new MusicSettings { Complexity = 85 };
```

## Track Configuration

### Track Settings Structure

```csharp
var track = new TrackSettings
{
    // Instrument
    Instrument = MidiProgram.Violin,        // MIDI instrument

    // Mix
    Volume = 0.8f,                          // Track volume (0-1)

    // Pattern
    Pattern = PatternType.Melodic,          // Generation pattern

    // Notes
    Notes = new[] {                         // Available notes
        NoteName.C, NoteName.E, NoteName.G
    },

    // Rhythm
    Density = 8,                            // Notes per measure
    NoteDuration = (                        // Note length range
        MusicalTimeSpan.Eighth,
        MusicalTimeSpan.Quarter
    ),

    // Pitch
    OctaveRange = (4, 6),                   // Octave min/max

    // Feel
    Swing = 0.0f                            // Swing amount (0-1)
};
```

### MIDI Instruments

```csharp
// Piano Family
MidiProgram.AcousticGrandPiano
MidiProgram.BrightAcousticPiano
MidiProgram.ElectricGrandPiano
MidiProgram.HonkyTonkPiano

// Chromatic Percussion
MidiProgram.Glockenspiel
MidiProgram.MusicBox
MidiProgram.Vibraphone
MidiProgram.Marimba
MidiProgram.Xylophone

// Organ
MidiProgram.DrawbarOrgan
MidiProgram.PercussiveOrgan
MidiProgram.RockOrgan
MidiProgram.ChurchOrgan

// Guitar
MidiProgram.AcousticGuitarNylon
MidiProgram.AcousticGuitarSteel
MidiProgram.ElectricGuitarJazz
MidiProgram.ElectricGuitarClean
MidiProgram.DistortionGuitar

// Bass
MidiProgram.AcousticBass
MidiProgram.ElectricBassFinger
MidiProgram.ElectricBassPick
MidiProgram.FretlessBass
MidiProgram.SlapBass1
MidiProgram.SlapBass2
MidiProgram.SynthBass1
MidiProgram.SynthBass2

// Strings
MidiProgram.Violin
MidiProgram.Viola
MidiProgram.Cello
MidiProgram.Contrabass
MidiProgram.StringEnsemble1
MidiProgram.StringEnsemble2

// Ensemble
MidiProgram.ChoirAahs
MidiProgram.VoiceOohs
MidiProgram.SynthVoice
MidiProgram.OrchestraHit

// Brass
MidiProgram.Trumpet
MidiProgram.Trombone
MidiProgram.Tuba
MidiProgram.FrenchHorn
MidiProgram.BrassSection

// Reed
MidiProgram.SopranoSax
MidiProgram.AltoSax
MidiProgram.TenorSax
MidiProgram.BaritoneSax
MidiProgram.Oboe
MidiProgram.Clarinet
MidiProgram.Bassoon

// Synth Lead
MidiProgram.Lead1Square
MidiProgram.Lead2Sawtooth
MidiProgram.Lead5Charang
MidiProgram.Lead8BassLead

// Synth Pad
MidiProgram.Pad1NewAge
MidiProgram.Pad2Warm
MidiProgram.Pad3Polysynth
MidiProgram.Pad4Choir
MidiProgram.Pad8Sweep

// Drums
MidiProgram.DrumSet        // Channel 10 for drums
```

## Pattern Types

### Melodic Pattern

Flowing melodies with stepwise motion:

```csharp
music.AddTrack("melody", new TrackSettings
{
    Pattern = PatternType.Melodic,
    Instrument = MidiProgram.Flute,
    Notes = GetScaleNotes(NoteName.C, ScaleType.Major),
    Density = 8,              // 8 notes per measure
    OctaveRange = (5, 6),     // Higher register
    Volume = 0.8f
});
```

### Rhythmic Pattern

Repeated rhythmic patterns:

```csharp
music.AddTrack("bass", new TrackSettings
{
    Pattern = PatternType.Rhythmic,
    Instrument = MidiProgram.ElectricBassFinger,
    Notes = new[] { NoteName.C, NoteName.G },
    Density = 4,              // 4 notes per measure
    OctaveRange = (2, 3),     // Lower register
    Volume = 0.9f
});
```

### Harmonic Pattern

Chord progressions:

```csharp
music.AddTrack("chords", new TrackSettings
{
    Pattern = PatternType.Harmonic,
    Instrument = MidiProgram.ElectricGuitarClean,
    Notes = new[] {
        NoteName.C, NoteName.E,
        NoteName.G, NoteName.B
    },
    Density = 2,              // 2 chords per measure
    OctaveRange = (3, 4),
    Volume = 0.6f
});
```

### Arpeggio Pattern

Broken chords:

```csharp
music.AddTrack("arp", new TrackSettings
{
    Pattern = PatternType.Arpeggio,
    Instrument = MidiProgram.Harp,
    Notes = new[] {
        NoteName.C, NoteName.E,
        NoteName.G, NoteName.C
    },
    Density = 16,             // 16 notes per measure
    OctaveRange = (4, 6),
    Volume = 0.5f
});
```

### Percussive Pattern

Drum patterns:

```csharp
music.AddTrack("drums", new TrackSettings
{
    Pattern = PatternType.Percussive,
    Instrument = MidiProgram.DrumSet,
    Density = 16,             // 16 hits per measure
    Volume = 0.7f
    // No notes needed for drums
});
```

### Ambient Pattern

Atmospheric pads:

```csharp
music.AddTrack("ambiance", new TrackSettings
{
    Pattern = PatternType.Ambient,
    Instrument = MidiProgram.Pad2Warm,
    Notes = GetScaleNotes(NoteName.C, ScaleType.Major),
    Density = 2,              // Very sparse
    OctaveRange = (3, 4),
    Volume = 0.4f
});
```

## Advanced Techniques

### Dynamic Intensity System

```csharp
public class AdaptiveMusicSystem
{
    private IProceduralMusic _currentMusic;
    private float _intensity = 0.5f;

    public void UpdateIntensity(float newIntensity)
    {
        _intensity = Math.Clamp(newIntensity, 0f, 1f);

        // Regenerate if intensity changed significantly
        if (Math.Abs(newIntensity - _intensity) > 0.2f)
        {
            RegenerateMusic();
        }
    }

    private void RegenerateMusic()
    {
        var tempo = (int)(80 + (_intensity * 80));  // 80-160 BPM
        var complexity = (int)(30 + (_intensity * 60)); // 30-90

        var settings = new MusicSettings
        {
            Tempo = tempo,
            Complexity = complexity,
            Mood = _intensity > 0.7f ? MusicMood.Tense : MusicMood.Neutral
        };

        _currentMusic = Api.Audio.CreateProceduralMusic(settings);

        // Layer tracks based on intensity
        AddBaseLayer();
        if (_intensity > 0.3f) AddMidLayer();
        if (_intensity > 0.6f) AddHighLayer();

        Api.Audio.PlayProceduralMusic(_currentMusic, loop: true);
    }

    private void AddBaseLayer()
    {
        _currentMusic.AddTrack("ambiance", new TrackSettings
        {
            Instrument = MidiProgram.Pad2Warm,
            Pattern = PatternType.Ambient,
            Volume = 0.5f,
            Density = 2
        });
    }

    private void AddMidLayer()
    {
        _currentMusic.AddTrack("melody", new TrackSettings
        {
            Instrument = MidiProgram.Violin,
            Pattern = PatternType.Melodic,
            Volume = 0.7f,
            Density = 8
        });

        _currentMusic.AddTrack("bass", new TrackSettings
        {
            Instrument = MidiProgram.AcousticBass,
            Pattern = PatternType.Rhythmic,
            Volume = 0.8f,
            Density = 4
        });
    }

    private void AddHighLayer()
    {
        _currentMusic.AddTrack("drums", new TrackSettings
        {
            Instrument = MidiProgram.DrumSet,
            Pattern = PatternType.Percussive,
            Volume = 0.8f,
            Density = 16
        });

        _currentMusic.AddTrack("lead", new TrackSettings
        {
            Instrument = MidiProgram.Distortion,
            Pattern = PatternType.Melodic,
            Volume = 0.8f,
            Density = 12,
            OctaveRange = (5, 7)
        });
    }
}
```

### Biome-Specific Music

```csharp
public class BiomeMusicGenerator
{
    public void GenerateBiomeMusic(string biome)
    {
        var settings = GetBiomeSettings(biome);
        var music = Api.Audio.CreateProceduralMusic(settings);

        // Add biome-specific instrumentation
        switch (biome)
        {
            case "forest":
                AddForestInstrumentation(music);
                break;
            case "cave":
                AddCaveInstrumentation(music);
                break;
            case "ocean":
                AddOceanInstrumentation(music);
                break;
            case "mountain":
                AddMountainInstrumentation(music);
                break;
        }

        Api.Audio.PlayProceduralMusic(music, loop: true);
    }

    private MusicSettings GetBiomeSettings(string biome)
    {
        return biome switch
        {
            "forest" => new MusicSettings
            {
                Tempo = 100,
                Key = NoteName.G,
                Scale = ScaleType.Major,
                Mood = MusicMood.Calm,
                Complexity = 40
            },
            "cave" => new MusicSettings
            {
                Tempo = 70,
                Key = NoteName.D,
                Scale = ScaleType.Minor,
                Mood = MusicMood.Mysterious,
                Complexity = 30
            },
            "ocean" => new MusicSettings
            {
                Tempo = 90,
                Key = NoteName.A,
                Scale = ScaleType.Dorian,
                Mood = MusicMood.Calm,
                Complexity = 50
            },
            "mountain" => new MusicSettings
            {
                Tempo = 110,
                Key = NoteName.E,
                Scale = ScaleType.Lydian,
                Mood = MusicMood.Epic,
                Complexity = 60
            },
            _ => new MusicSettings()
        };
    }

    private void AddForestInstrumentation(IProceduralMusic music)
    {
        music.AddTrack("ambiance", new TrackSettings
        {
            Instrument = MidiProgram.Pad2Warm,
            Pattern = PatternType.Ambient,
            Volume = 0.5f,
            Density = 2
        });

        music.AddTrack("melody", new TrackSettings
        {
            Instrument = MidiProgram.Flute,
            Pattern = PatternType.Melodic,
            Volume = 0.6f,
            Density = 6,
            OctaveRange = (5, 6)
        });

        music.AddTrack("texture", new TrackSettings
        {
            Instrument = MidiProgram.Harp,
            Pattern = PatternType.Arpeggio,
            Volume = 0.4f,
            Density = 8
        });
    }

    private void AddCaveInstrumentation(IProceduralMusic music)
    {
        music.AddTrack("ambiance", new TrackSettings
        {
            Instrument = MidiProgram.Pad8Sweep,
            Pattern = PatternType.Ambient,
            Volume = 0.6f,
            Density = 1,
            OctaveRange = (2, 3)
        });

        music.AddTrack("drops", new TrackSettings
        {
            Instrument = MidiProgram.Glockenspiel,
            Pattern = PatternType.Melodic,
            Volume = 0.3f,
            Density = 3,
            OctaveRange = (6, 7)
        });
    }

    private void AddOceanInstrumentation(IProceduralMusic music)
    {
        music.AddTrack("waves", new TrackSettings
        {
            Instrument = MidiProgram.Pad1NewAge,
            Pattern = PatternType.Ambient,
            Volume = 0.5f,
            Density = 2
        });

        music.AddTrack("melody", new TrackSettings
        {
            Instrument = MidiProgram.Vibraphone,
            Pattern = PatternType.Melodic,
            Volume = 0.5f,
            Density = 4
        });

        music.AddTrack("bass", new TrackSettings
        {
            Instrument = MidiProgram.SynthBass2,
            Pattern = PatternType.Rhythmic,
            Volume = 0.6f,
            Density = 2,
            OctaveRange = (2, 3)
        });
    }

    private void AddMountainInstrumentation(IProceduralMusic music)
    {
        music.AddTrack("strings", new TrackSettings
        {
            Instrument = MidiProgram.StringEnsemble1,
            Pattern = PatternType.Harmonic,
            Volume = 0.7f,
            Density = 4
        });

        music.AddTrack("brass", new TrackSettings
        {
            Instrument = MidiProgram.FrenchHorn,
            Pattern = PatternType.Melodic,
            Volume = 0.8f,
            Density = 6,
            OctaveRange = (4, 5)
        });

        music.AddTrack("percussion", new TrackSettings
        {
            Instrument = MidiProgram.TaikoDrum,
            Pattern = PatternType.Percussive,
            Volume = 0.6f,
            Density = 8
        });
    }
}
```

### Time-of-Day Music

```csharp
public class TimeBasedMusicSystem
{
    public void UpdateMusicForTimeOfDay(int hour)
    {
        var settings = hour switch
        {
            >= 5 and < 8 => GetDawnSettings(),     // Dawn
            >= 8 and < 17 => GetDaySettings(),     // Day
            >= 17 and < 20 => GetDuskSettings(),   // Dusk
            _ => GetNightSettings()                 // Night
        };

        var music = Api.Audio.CreateProceduralMusic(settings);
        ConfigureTracksForTime(music, hour);
        Api.Audio.PlayProceduralMusic(music, loop: true);
    }

    private MusicSettings GetDawnSettings()
    {
        return new MusicSettings
        {
            Tempo = 90,
            Key = NoteName.E,
            Scale = ScaleType.Major,
            Mood = MusicMood.Calm,
            Complexity = 40
        };
    }

    private MusicSettings GetDaySettings()
    {
        return new MusicSettings
        {
            Tempo = 110,
            Key = NoteName.C,
            Scale = ScaleType.Major,
            Mood = MusicMood.Happy,
            Complexity = 60
        };
    }

    private MusicSettings GetDuskSettings()
    {
        return new MusicSettings
        {
            Tempo = 85,
            Key = NoteName.G,
            Scale = ScaleType.Dorian,
            Mood = MusicMood.Calm,
            Complexity = 45
        };
    }

    private MusicSettings GetNightSettings()
    {
        return new MusicSettings
        {
            Tempo = 70,
            Key = NoteName.A,
            Scale = ScaleType.Minor,
            Mood = MusicMood.Mysterious,
            Complexity = 35
        };
    }
}
```

## Complete Examples

See the [API Reference](../api/audio.md) for complete battle music, exploration music, and dynamic intensity examples.

## Performance Tips

### 1. Cache Generated Music

```csharp
private Dictionary<string, IProceduralMusic> _musicCache = new();

public IProceduralMusic GetOrCreateMusic(string key, Func<IProceduralMusic> factory)
{
    if (!_musicCache.ContainsKey(key))
    {
        _musicCache[key] = factory();
    }
    return _musicCache[key];
}
```

### 2. Limit Track Count

```csharp
// ❌ BAD: Too many tracks
for (int i = 0; i < 20; i++)
{
    music.AddTrack($"track{i}", settings);
}

// ✅ GOOD: 4-6 tracks maximum
music.AddTrack("ambiance", ambientSettings);
music.AddTrack("melody", melodySettings);
music.AddTrack("bass", bassSettings);
music.AddTrack("drums", drumSettings);
```

### 3. Optimize Note Density

```csharp
// ❌ BAD: Too dense
var tooManyNotes = new TrackSettings { Density = 64 };

// ✅ GOOD: Reasonable density
var goodDensity = new TrackSettings { Density = 8 };
```

### 4. Dispose Resources

```csharp
using (var music = Api.Audio.CreateProceduralMusic(settings))
{
    // Use music
    Api.Audio.PlayProceduralMusic(music);
} // Automatically disposed
```

## Next Steps

- **[Configuration Reference](configuration.md)** - Detailed configuration options
- **[Best Practices](best-practices.md)** - Performance and quality guidelines
- **[API Reference](../api/audio.md)** - Complete API documentation

---

*Last Updated: 2025-10-22*
