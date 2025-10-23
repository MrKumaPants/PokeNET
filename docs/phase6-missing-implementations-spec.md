# Phase 6: Missing Audio System Implementations - Comprehensive Requirements Specification

**Document Version:** 1.0
**Date:** 2025-10-23
**Status:** Research Complete - Ready for Implementation
**Total Compilation Errors:** 201
**Test Files Analyzed:** 6

---

## Executive Summary

This document catalogs all missing implementations required to resolve 201 compilation errors in the PokeNET.Tests audio test suite. The analysis covers **ProceduralMusicTests.cs**, **ReactiveAudioTests.cs**, **AudioManagerTests.cs**, **MusicPlayerTests.cs**, and related test files.

### Key Findings:
- **Missing Interfaces:** 1 (IRandomProvider)
- **Missing Enums:** 2 (ScaleType variations, AudioReactionType)
- **Missing Domain Models:** 5 major classes (Melody, Note, Chord, ChordProgression, Rhythm)
- **Missing Service Methods:** 40+ across multiple services
- **Missing Event Types:** 12+ event classes
- **Missing Extensions:** Multiple IAudioManager extension methods

---

## Table of Contents

1. [Missing Interfaces](#1-missing-interfaces)
2. [Missing Enums](#2-missing-enums)
3. [Missing Domain Models](#3-missing-domain-models)
4. [Missing Services](#4-missing-services)
5. [Missing Event Types](#5-missing-event-types)
6. [Missing Extensions](#6-missing-extensions)
7. [Implementation Dependencies](#7-implementation-dependencies)
8. [Test Coverage Requirements](#8-test-coverage-requirements)

---

## 1. Missing Interfaces

### 1.1 IRandomProvider

**Namespace:** `PokeNET.Audio.Abstractions`
**Purpose:** Provides testable random number generation for procedural music algorithms
**Used In:** ProceduralMusicGenerator constructor

```csharp
namespace PokeNET.Audio.Abstractions;

/// <summary>
/// Abstraction for random number generation to enable deterministic testing
/// </summary>
public interface IRandomProvider
{
    /// <summary>
    /// Returns a non-negative random integer
    /// </summary>
    int Next();

    /// <summary>
    /// Returns a random integer within [0, maxValue)
    /// </summary>
    int Next(int maxValue);

    /// <summary>
    /// Returns a random integer within [minValue, maxValue)
    /// </summary>
    int Next(int minValue, int maxValue);

    /// <summary>
    /// Returns a random double in the range [0.0, 1.0)
    /// </summary>
    double NextDouble();

    /// <summary>
    /// Fills the elements of a specified array with random numbers
    /// </summary>
    void NextBytes(byte[] buffer);
}
```

**Implementation Notes:**
- Default implementation should wrap `System.Random`
- Support seeding for reproducible music generation
- Thread-safe implementation recommended

---

## 2. Missing Enums

### 2.1 ScaleType

**Namespace:** `PokeNET.Audio.Models`
**Purpose:** Defines musical scale types for melody generation
**Status:** **PARTIALLY EXISTS** - needs additional values

**Required Values:**
```csharp
public enum ScaleType
{
    // Existing (from Models/ScaleType.cs)
    Major,
    Minor,

    // MISSING - Required by tests:
    NaturalMinor,
    HarmonicMinor,
    MelodicMinor,
    Dorian,
    Phrygian,
    Lydian,
    Mixolydian,
    Locrian,
    Pentatonic,
    Blues,
    Chromatic,
    WholeTone,
    Diminished
}
```

**Test References:**
- `ProceduralMusicTests.cs:111` - Uses `ScaleType.NaturalMinor`
- `ProceduralMusicTests.cs:75-92` - Uses `ScaleType.Major`

---

### 2.2 AudioReactionType

**Namespace:** `PokeNET.Audio.Models` or `PokeNET.Audio.Reactive`
**Purpose:** Defines types of audio reactions for enable/disable control

```csharp
namespace PokeNET.Audio.Models;

/// <summary>
/// Types of reactive audio behaviors that can be enabled or disabled
/// </summary>
[Flags]
public enum AudioReactionType
{
    None = 0,
    BattleMusic = 1 << 0,
    OverworldMusic = 1 << 1,
    MenuDucking = 1 << 2,
    HealthBasedMusic = 1 << 3,
    WeatherAmbient = 1 << 4,
    BattleSoundEffects = 1 << 5,
    ItemSoundEffects = 1 << 6,
    LevelUpSoundEffects = 1 << 7,
    All = ~0
}
```

**Test References:**
- `ReactiveAudioTests.cs:526` - `SetReactionEnabled(AudioReactionType.BattleMusic, false)`
- `ReactiveAudioTests.cs:529` - `IsReactionEnabled(AudioReactionType.BattleMusic)`

---

## 3. Missing Domain Models

### 3.1 Note

**Namespace:** `PokeNET.Audio.Models`
**Purpose:** Represents a musical note with pitch, duration, and velocity
**Status:** **PARTIALLY EXISTS** - needs extensions

**Required Properties & Methods:**

```csharp
namespace PokeNET.Audio.Models;

public class Note
{
    // Constructor from tests
    public Note(NoteName noteName, int octave, float duration)
    {
        NoteName = noteName;
        Octave = octave;
        Duration = duration;
        NoteNumber = CalculateNoteNumber(noteName, octave);
    }

    /// <summary>
    /// The note name (C, D, E, etc.)
    /// </summary>
    public NoteName NoteName { get; set; }

    /// <summary>
    /// The octave (0-10, middle C is octave 4)
    /// </summary>
    public int Octave { get; set; }

    /// <summary>
    /// Duration in beats (1.0 = quarter note)
    /// </summary>
    public float Duration { get; set; }

    /// <summary>
    /// MIDI note number (0-127)
    /// </summary>
    public int NoteNumber { get; set; }

    /// <summary>
    /// Velocity (volume) 0-127
    /// </summary>
    public byte Velocity { get; set; } = 100;

    /// <summary>
    /// Whether this is a rest (silence)
    /// </summary>
    public bool IsRest { get; set; }

    private static int CalculateNoteNumber(NoteName noteName, int octave)
    {
        // C0 = 12, C4 (middle C) = 60
        return ((octave + 1) * 12) + (int)noteName;
    }
}
```

**Test References:**
- `ProceduralMusicTests.cs:512` - `new Note(note, octave: 4, duration: 1.0f)`
- `ProceduralMusicTests.cs:467` - `n.NoteNumber >= minNote && n.NoteNumber <= maxNote`
- `ProceduralMusicTests.cs:102` - `n.NoteName` property access

---

### 3.2 Melody

**Namespace:** `PokeNET.Audio.Models`
**Purpose:** Container for a sequence of musical notes
**Status:** **PARTIALLY EXISTS** - needs extensions

**Required Properties & Methods:**

```csharp
namespace PokeNET.Audio.Models;

public class Melody
{
    /// <summary>
    /// Collection of notes in this melody
    /// </summary>
    public List<Note> Notes { get; set; } = new();

    /// <summary>
    /// The key of this melody
    /// </summary>
    public NoteName Key { get; set; }

    /// <summary>
    /// The scale type of this melody
    /// </summary>
    public ScaleType Scale { get; set; }

    /// <summary>
    /// Tempo in BPM (beats per minute)
    /// </summary>
    public int Tempo { get; set; } = 120;

    /// <summary>
    /// Time signature numerator (beats per bar)
    /// </summary>
    public int BeatsPerBar { get; set; } = 4;

    /// <summary>
    /// Time signature denominator (note value)
    /// </summary>
    public int BeatUnit { get; set; } = 4;

    /// <summary>
    /// Adds a note to the melody
    /// </summary>
    public void AddNote(Note note)
    {
        Notes.Add(note);
    }

    /// <summary>
    /// Total duration of the melody in beats
    /// </summary>
    public float TotalDuration => Notes.Sum(n => n.Duration);

    /// <summary>
    /// Number of bars in the melody
    /// </summary>
    public int BarCount => (int)Math.Ceiling(TotalDuration / BeatsPerBar);
}
```

**Test References:**
- `ProceduralMusicTests.cs:509` - `new Melody()` constructor
- `ProceduralMusicTests.cs:511` - `melody.AddNote(note)` method
- `ProceduralMusicTests.cs:83` - `melody.Notes.Should().HaveCount(length)`
- `ProceduralMusicTests.cs:118` - `melody.Scale.Should().Be(ScaleType.NaturalMinor)`

---

### 3.3 Chord

**Namespace:** `PokeNET.Audio.Models`
**Purpose:** Represents a musical chord (multiple notes played together)
**Status:** **PARTIALLY EXISTS** - needs extensions

**Required Properties & Methods:**

```csharp
namespace PokeNET.Audio.Models;

public class Chord
{
    /// <summary>
    /// Root note of the chord
    /// </summary>
    public NoteName Root { get; set; }

    /// <summary>
    /// Type of chord (Major, Minor, Diminished, etc.)
    /// </summary>
    public ChordType Type { get; set; }

    /// <summary>
    /// Notes that make up this chord
    /// </summary>
    public List<Note> Notes { get; set; } = new();

    /// <summary>
    /// Whether this chord is diatonic to its key
    /// </summary>
    public bool IsDiatonic { get; set; }

    /// <summary>
    /// Duration of the chord in beats
    /// </summary>
    public float Duration { get; set; } = 4.0f;

    /// <summary>
    /// Roman numeral notation (I, ii, iii, IV, V, vi, vii°)
    /// </summary>
    public string? RomanNumeral { get; set; }
}
```

**Test References:**
- `ProceduralMusicTests.cs:199` - `c.IsDiatonic` property
- `ProceduralMusicTests.cs:213` - `progression.Chords.Last().Root`

---

### 3.4 ChordProgression

**Namespace:** `PokeNET.Audio.Models`
**Purpose:** Sequence of chords forming a harmonic progression
**Status:** **PARTIALLY EXISTS** - needs extensions

**Required Properties & Methods:**

```csharp
namespace PokeNET.Audio.Models;

public class ChordProgression
{
    /// <summary>
    /// Collection of chords in this progression
    /// </summary>
    public List<Chord> Chords { get; set; } = new();

    /// <summary>
    /// The key of this progression
    /// </summary>
    public NoteName Key { get; set; }

    /// <summary>
    /// Pattern string (e.g., "I-IV-V-I", "I-V-vi-IV")
    /// </summary>
    public string Pattern { get; set; } = string.Empty;

    /// <summary>
    /// Tempo in BPM
    /// </summary>
    public int Tempo { get; set; } = 120;

    /// <summary>
    /// Total duration in beats
    /// </summary>
    public float TotalDuration => Chords.Sum(c => c.Duration);
}
```

**Test References:**
- `ProceduralMusicTests.cs:168` - `progression.Chords.Should().HaveCount(progressionLength)`
- `ProceduralMusicTests.cs:186` - `progression.Pattern.Should().Be(pattern)`

---

### 3.5 Rhythm

**Namespace:** `PokeNET.Audio.Models`
**Purpose:** Defines rhythmic patterns independent of pitch

```csharp
namespace PokeNET.Audio.Models;

public class Rhythm
{
    /// <summary>
    /// Number of beats per bar
    /// </summary>
    public int BeatsPerBar { get; set; } = 4;

    /// <summary>
    /// Beat unit (denominator of time signature)
    /// </summary>
    public int BeatUnit { get; set; } = 4;

    /// <summary>
    /// Number of bars in this rhythm pattern
    /// </summary>
    public int Bars { get; set; }

    /// <summary>
    /// Total beats in the pattern
    /// </summary>
    public int TotalBeats => Bars * BeatsPerBar;

    /// <summary>
    /// Array of note durations in the pattern
    /// </summary>
    public List<float> NoteDurations { get; set; } = new();

    /// <summary>
    /// Accent pattern (true = accented beat)
    /// </summary>
    public List<bool> Accents { get; set; } = new();
}
```

**Test References:**
- `ProceduralMusicTests.cs:233` - `rhythm.TotalBeats.Should().Be(bars * beatsPerBar)`
- `ProceduralMusicTests.cs:250` - `rhythm.BeatsPerBar.Should().Be(beatsPerBar)`
- `ProceduralMusicTests.cs:264` - `rhythm.NoteDurations` property access

---

## 4. Missing Services

### 4.1 ProceduralMusicGenerator

**Namespace:** `PokeNET.Audio.Procedural`
**Purpose:** Generates music algorithmically using various techniques

**Required Properties:**

```csharp
public class ProceduralMusicGenerator
{
    /// <summary>
    /// Default tempo for generated music (BPM)
    /// </summary>
    public int DefaultTempo { get; set; } = 120;

    /// <summary>
    /// Default key for generated music
    /// </summary>
    public NoteName DefaultKey { get; set; } = NoteName.C;
}
```

**Required Methods:**

#### 4.1.1 Melody Generation

```csharp
/// <summary>
/// Generates a melody in the specified key and scale
/// </summary>
/// <param name="key">Root note of the key</param>
/// <param name="scale">Type of scale to use</param>
/// <param name="length">Number of notes to generate</param>
/// <param name="minNote">Minimum MIDI note number (optional)</param>
/// <param name="maxNote">Maximum MIDI note number (optional)</param>
/// <param name="maxStepSize">Maximum interval between consecutive notes (optional)</param>
/// <returns>Generated melody</returns>
/// <exception cref="ArgumentException">Thrown when length is zero or negative</exception>
Melody GenerateMelody(
    NoteName key,
    ScaleType scale,
    int length,
    int? minNote = null,
    int? maxNote = null,
    int? maxStepSize = null);
```

**Test References:**
- `ProceduralMusicTests.cs:79` - Basic melody generation
- `ProceduralMusicTests.cs:458-464` - With range constraint
- `ProceduralMusicTests.cs:478-483` - With step limit
- `ProceduralMusicTests.cs:145` - Throws ArgumentException for zero length

---

#### 4.1.2 Chord Progression Generation

```csharp
/// <summary>
/// Generates a chord progression
/// </summary>
/// <param name="key">Root note of the key</param>
/// <param name="length">Number of chords to generate</param>
/// <param name="endOnTonic">Whether to end on the tonic chord</param>
/// <returns>Generated chord progression</returns>
ChordProgression GenerateChordProgression(
    NoteName key,
    int length,
    bool endOnTonic = false);

/// <summary>
/// Generates a chord progression from a pattern string
/// </summary>
/// <param name="key">Root note of the key</param>
/// <param name="pattern">Pattern string like "I-IV-V-I"</param>
/// <returns>Chord progression matching the pattern</returns>
ChordProgression GenerateChordProgressionFromPattern(
    NoteName key,
    string pattern);
```

**Test References:**
- `ProceduralMusicTests.cs:164` - Basic progression generation
- `ProceduralMusicTests.cs:182` - From pattern
- `ProceduralMusicTests.cs:210` - With endOnTonic parameter

---

#### 4.1.3 Rhythm Generation

```csharp
/// <summary>
/// Generates a rhythmic pattern
/// </summary>
/// <param name="bars">Number of bars</param>
/// <param name="beatsPerBar">Beats per bar (time signature numerator)</param>
/// <param name="beatUnit">Beat unit (time signature denominator)</param>
/// <returns>Generated rhythm pattern</returns>
Rhythm GenerateRhythm(
    int bars,
    int beatsPerBar,
    int beatUnit = 4);
```

**Test References:**
- `ProceduralMusicTests.cs:229` - Basic rhythm generation
- `ProceduralMusicTests.cs:247` - With time signature

---

#### 4.1.4 Markov Chain Generation

```csharp
/// <summary>
/// Generates a melody using Markov chain analysis of training data
/// </summary>
/// <param name="trainingData">Collection of melodies to learn from</param>
/// <param name="length">Number of notes to generate</param>
/// <returns>Generated melody based on training data patterns</returns>
/// <exception cref="ArgumentException">Thrown when trainingData is empty</exception>
Melody GenerateWithMarkovChain(
    List<Melody> trainingData,
    int length);
```

**Test References:**
- `ProceduralMusicTests.cs:279` - Basic Markov generation
- `ProceduralMusicTests.cs:298` - Learning from training data
- `ProceduralMusicTests.cs:311` - Throws on empty training data

---

#### 4.1.5 L-System Generation

```csharp
/// <summary>
/// Generates a melody using L-System (Lindenmayer System) rules
/// </summary>
/// <param name="axiom">Starting string/symbol</param>
/// <param name="rules">Production rules for symbol replacement</param>
/// <param name="iterations">Number of iterations to apply rules</param>
/// <returns>Generated melody with fractal-like patterns</returns>
Melody GenerateWithLSystem(
    string axiom,
    Dictionary<char, string> rules,
    int iterations);
```

**Test References:**
- `ProceduralMusicTests.cs:334` - Basic L-System generation
- `ProceduralMusicTests.cs:350` - Multiple iterations expansion

---

#### 4.1.6 Cellular Automata Generation

```csharp
/// <summary>
/// Generates a melody using cellular automata rules
/// </summary>
/// <param name="rule">Rule number (0-255, e.g., Rule 30, Rule 110)</param>
/// <param name="generations">Number of generations to simulate</param>
/// <returns>Generated melody based on cellular automata evolution</returns>
Melody GenerateWithCellularAutomata(
    int rule,
    int generations);
```

**Test References:**
- `ProceduralMusicTests.cs:369` - Basic CA generation
- `ProceduralMusicTests.cs:386` - Different rules produce different patterns

---

#### 4.1.7 MIDI Export

```csharp
/// <summary>
/// Exports a melody to a MIDI file
/// </summary>
/// <param name="melody">Melody to export</param>
/// <param name="tempo">Tempo in BPM (optional, uses melody's tempo if not specified)</param>
/// <returns>MIDI file representation</returns>
MidiFile ExportToMidi(Melody melody, int? tempo = null);

/// <summary>
/// Exports a chord progression to a MIDI file
/// </summary>
/// <param name="progression">Chord progression to export</param>
/// <param name="tempo">Tempo in BPM (optional)</param>
/// <returns>MIDI file representation</returns>
MidiFile ExportToMidi(ChordProgression progression, int? tempo = null);
```

**Test References:**
- `ProceduralMusicTests.cs:404` - Export melody to MIDI
- `ProceduralMusicTests.cs:419` - Export progression with chords
- `ProceduralMusicTests.cs:436` - Export with custom tempo

---

### 4.2 ReactiveAudioEngine

**Namespace:** `PokeNET.Audio.Reactive`
**Purpose:** Handles reactive audio responses to game state changes

**Required Properties:**

```csharp
/// <summary>
/// Whether the reactive audio engine is initialized
/// </summary>
public bool IsInitialized { get; private set; }
```

**Required Methods:**

#### 4.2.1 Initialization

```csharp
/// <summary>
/// Initializes the reactive audio engine and subscribes to events
/// </summary>
Task InitializeAsync(CancellationToken cancellationToken = default);
```

**Test References:**
- `ReactiveAudioTests.cs:66` - InitializeAsync subscribes to events
- `ReactiveAudioTests.cs:70-71` - Subscribes to GameStateChangedEvent and BattleEvent

---

#### 4.2.2 Game State Reactions

```csharp
/// <summary>
/// Handles game state transitions and plays appropriate music
/// </summary>
Task OnGameStateChangedAsync(
    GameState previousState,
    GameState newState);
```

**Test References:**
- `ReactiveAudioTests.cs:86` - Battle music on state change to Battle
- `ReactiveAudioTests.cs:103` - Overworld music on state change to Overworld
- `ReactiveAudioTests.cs:120` - Duck music on state change to Menu
- `ReactiveAudioTests.cs:138` - Unduck music when leaving Menu

---

#### 4.2.3 Battle Event Reactions

```csharp
/// <summary>
/// Handles battle start events
/// </summary>
Task OnBattleStartAsync(BattleStartEvent battleEvent);

/// <summary>
/// Handles battle end events
/// </summary>
Task OnBattleEndAsync(BattleEndEvent battleEvent);

/// <summary>
/// Handles Pokemon faint events
/// </summary>
Task OnPokemonFaintAsync(PokemonFaintEvent faintEvent);

/// <summary>
/// Handles attack use events
/// </summary>
Task OnAttackUseAsync(AttackEvent attackEvent);

/// <summary>
/// Handles critical hit events
/// </summary>
Task OnCriticalHitAsync(CriticalHitEvent criticalEvent);
```

**Test References:**
- `ReactiveAudioTests.cs:177` - Battle start plays intro sound
- `ReactiveAudioTests.cs:194` - Wild battle plays wild music
- `ReactiveAudioTests.cs:211` - Trainer battle plays trainer music
- `ReactiveAudioTests.cs:228` - Gym leader battle plays gym music
- `ReactiveAudioTests.cs:245` - Battle win plays victory music
- `ReactiveAudioTests.cs:262` - Pokemon faint plays faint sound
- `ReactiveAudioTests.cs:279` - Attack use plays attack sound
- `ReactiveAudioTests.cs:296` - Critical hit plays crit sound

---

#### 4.2.4 Health-Based Reactions

```csharp
/// <summary>
/// Handles health changes and plays low health music if needed
/// </summary>
Task OnHealthChangedAsync(HealthChangedEvent healthEvent);
```

**Test References:**
- `ReactiveAudioTests.cs:317-322` - Low health threshold triggers low health music
- `ReactiveAudioTests.cs:340` - Above threshold stops low health music

---

#### 4.2.5 Weather and Environment Reactions

```csharp
/// <summary>
/// Handles weather changes and plays appropriate ambient sounds
/// </summary>
Task OnWeatherChangedAsync(WeatherChangedEvent weatherEvent);
```

**Test References:**
- `ReactiveAudioTests.cs:361` - Rain weather plays rain ambient
- `ReactiveAudioTests.cs:379` - Clear weather stops ambient
- `ReactiveAudioTests.cs:400` - Different weather types play corresponding ambients

---

#### 4.2.6 Item and Interaction Reactions

```csharp
/// <summary>
/// Handles item use events
/// </summary>
Task OnItemUseAsync(ItemUseEvent itemEvent);

/// <summary>
/// Handles Pokemon caught events
/// </summary>
Task OnPokemonCaughtAsync(PokemonCaughtEvent caughtEvent);

/// <summary>
/// Handles level up events
/// </summary>
Task OnLevelUpAsync(LevelUpEvent levelUpEvent);
```

**Test References:**
- `ReactiveAudioTests.cs:421` - Item use plays sound
- `ReactiveAudioTests.cs:438` - Pokemon caught plays catch sound
- `ReactiveAudioTests.cs:455` - Level up plays level up sound

---

#### 4.2.7 Audio State Management

```csharp
/// <summary>
/// Gets the current music state
/// </summary>
MusicState GetCurrentMusicState();

/// <summary>
/// Pauses all audio (music and ambient)
/// </summary>
Task PauseAllAsync();

/// <summary>
/// Resumes all audio (music and ambient)
/// </summary>
Task ResumeAllAsync();
```

**Test References:**
- `ReactiveAudioTests.cs:477` - GetCurrentMusicState returns correct state
- `ReactiveAudioTests.cs:492` - PauseAllAsync pauses music and ambient
- `ReactiveAudioTests.cs:508` - ResumeAllAsync resumes music and ambient

---

#### 4.2.8 Configuration

```csharp
/// <summary>
/// Enables or disables a specific audio reaction type
/// </summary>
void SetReactionEnabled(AudioReactionType reactionType, bool enabled);

/// <summary>
/// Checks if a specific audio reaction type is enabled
/// </summary>
bool IsReactionEnabled(AudioReactionType reactionType);
```

**Test References:**
- `ReactiveAudioTests.cs:526` - SetReactionEnabled disables reactions
- `ReactiveAudioTests.cs:529` - IsReactionEnabled checks status
- `ReactiveAudioTests.cs:541` - Disabled reactions don't trigger

---

### 4.3 IAudioManager Extensions

**Namespace:** `PokeNET.Audio.Services` or `PokeNET.Audio.Abstractions`
**Purpose:** Extension methods for IAudioManager

**Missing Methods (Used in Tests):**

```csharp
public static class AudioManagerExtensions
{
    /// <summary>
    /// Plays music with optional volume control
    /// </summary>
    Task PlayMusicAsync(this IAudioManager manager, string musicFile, float volume = 1.0f);

    /// <summary>
    /// Plays a sound effect with optional volume control
    /// </summary>
    Task PlaySoundEffectAsync(this IAudioManager manager, string soundFile, float volume = 1.0f);

    /// <summary>
    /// Plays ambient audio loop
    /// </summary>
    Task PlayAmbientAsync(this IAudioManager manager, string ambientFile, float volume = 1.0f);

    /// <summary>
    /// Stops ambient audio
    /// </summary>
    Task StopAmbientAsync(this IAudioManager manager);

    /// <summary>
    /// Pauses ambient audio
    /// </summary>
    Task PauseAmbientAsync(this IAudioManager manager);

    /// <summary>
    /// Resumes ambient audio
    /// </summary>
    Task ResumeAmbientAsync(this IAudioManager manager);

    /// <summary>
    /// Ducks (lowers) music volume temporarily
    /// </summary>
    Task DuckMusicAsync(this IAudioManager manager, float duckLevel = 0.3f);

    /// <summary>
    /// Stops ducking and restores music volume
    /// </summary>
    Task StopDuckingAsync(this IAudioManager manager);

    /// <summary>
    /// Stops music playback
    /// </summary>
    Task StopMusicAsync(this IAudioManager manager);

    /// <summary>
    /// Pauses music playback
    /// </summary>
    Task PauseMusicAsync(this IAudioManager manager);

    /// <summary>
    /// Resumes music playback
    /// </summary>
    Task ResumeMusicAsync(this IAudioManager manager);
}
```

**Properties Needed on IAudioManager:**

```csharp
public interface IAudioManager
{
    // Existing properties...

    /// <summary>
    /// Whether music is currently playing
    /// </summary>
    bool IsMusicPlaying { get; }

    /// <summary>
    /// Currently playing music track
    /// </summary>
    string? CurrentMusicTrack { get; }
}
```

**Test References:**
- `ReactiveAudioTests.cs:90` - PlayMusicAsync with file path
- `ReactiveAudioTests.cs:124` - DuckMusicAsync
- `ReactiveAudioTests.cs:142` - StopDuckingAsync
- `ReactiveAudioTests.cs:365` - PlayAmbientAsync
- `ReactiveAudioTests.cs:383` - StopAmbientAsync

---

### 4.4 AudioManager Service

**Required Extensions to Existing Service:**

```csharp
public class AudioManager : IAudioManager
{
    // Existing implementation...

    /// <summary>
    /// Initializes the audio system
    /// </summary>
    public bool IsInitialized { get; private set; }

    public async Task InitializeAsync()
    {
        // Initialize music player and SFX player
        await MusicPlayer.InitializeAsync();
        await SoundEffectPlayer.InitializeAsync();
        IsInitialized = true;
    }

    /// <summary>
    /// Master volume (0.0 - 1.0)
    /// </summary>
    public float MasterVolume { get; private set; } = 1.0f;

    public void SetMasterVolume(float volume)
    {
        MasterVolume = Math.Clamp(volume, 0.0f, 1.0f);
        // Apply to all channels
    }

    /// <summary>
    /// Music channel volume (0.0 - 1.0)
    /// </summary>
    public float MusicVolume { get; private set; } = 1.0f;

    public void SetMusicVolume(float volume)
    {
        MusicVolume = Math.Clamp(volume, 0.0f, 1.0f);
        MusicPlayer.SetVolume(volume);
    }

    /// <summary>
    /// Sound effects channel volume (0.0 - 1.0)
    /// </summary>
    public float SfxVolume { get; private set; } = 1.0f;

    public void SetSfxVolume(float volume)
    {
        SfxVolume = Math.Clamp(volume, 0.0f, 1.0f);
        SoundEffectPlayer.SetVolume(volume);
    }

    /// <summary>
    /// Whether music is currently playing
    /// </summary>
    public bool IsMusicPlaying => MusicPlayer.IsPlaying;

    /// <summary>
    /// Whether music is paused
    /// </summary>
    public bool IsMusicPaused => MusicPlayer.IsPaused;

    /// <summary>
    /// Gets current music playback position
    /// </summary>
    public TimeSpan GetMusicPosition() => MusicPlayer.GetPosition();

    /// <summary>
    /// Gets cache size in bytes
    /// </summary>
    public long GetCacheSize() => _cache.GetSize();

    /// <summary>
    /// Preloads an audio file into cache
    /// </summary>
    public async Task PreloadAudioAsync(string audioFile)
    {
        await _cache.PreloadAsync(audioFile);
    }

    /// <summary>
    /// Preloads multiple audio files
    /// </summary>
    public async Task PreloadMultipleAsync(string[] audioFiles)
    {
        var tasks = audioFiles.Select(f => _cache.PreloadAsync(f));
        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Clears the audio cache
    /// </summary>
    public async Task ClearCacheAsync()
    {
        await _cache.ClearAsync();
    }
}
```

**Test References:**
- `AudioManagerTests.cs:55` - IsInitialized property
- `AudioManagerTests.cs:81` - InitializeAsync method
- `AudioManagerTests.cs:297` - MasterVolume property
- `AudioManagerTests.cs:336` - MusicVolume property
- `AudioManagerTests.cs:350` - SfxVolume property

---

### 4.5 MusicPlayer Service

**Required Extensions:**

```csharp
public class MusicPlayer : IMusicPlayer
{
    /// <summary>
    /// Whether the player is initialized
    /// </summary>
    public bool IsInitialized { get; private set; }

    /// <summary>
    /// Whether music is currently playing
    /// </summary>
    public bool IsPlaying { get; private set; }

    /// <summary>
    /// Whether music is paused
    /// </summary>
    public bool IsPaused { get; private set; }

    /// <summary>
    /// Current volume (0.0 - 1.0)
    /// </summary>
    public float Volume { get; private set; } = 1.0f;

    /// <summary>
    /// Whether looping is enabled
    /// </summary>
    public bool IsLooping { get; private set; }

    /// <summary>
    /// Playback tempo multiplier
    /// </summary>
    public float Tempo { get; private set; } = 1.0f;

    /// <summary>
    /// Currently loaded MIDI file
    /// </summary>
    public MidiFile? CurrentMidi { get; private set; }

    /// <summary>
    /// Whether a MIDI file is loaded
    /// </summary>
    public bool IsLoaded => CurrentMidi != null;

    public async Task InitializeAsync()
    {
        // Initialize MIDI output device
        IsInitialized = true;
    }

    public async Task LoadMidiAsync(byte[] midiData)
    {
        if (midiData == null) throw new ArgumentNullException(nameof(midiData));

        using var stream = new MemoryStream(midiData);
        CurrentMidi = MidiFile.Read(stream);
    }

    public async Task LoadMidiFromFileAsync(string filePath)
    {
        if (!File.Exists(filePath)) throw new FileNotFoundException(filePath);

        CurrentMidi = MidiFile.Read(filePath);
    }

    public async Task PlayAsync(byte[] midiData)
    {
        await LoadMidiAsync(midiData);
        await PlayAsync();
    }

    public async Task PlayAsync()
    {
        if (!IsLoaded) throw new InvalidOperationException("No MIDI file loaded");

        IsPlaying = true;
        IsPaused = false;
    }

    public async Task PauseAsync()
    {
        IsPaused = true;
        IsPlaying = false;
    }

    public async Task ResumeAsync()
    {
        IsPlaying = true;
        IsPaused = false;
    }

    public async Task StopAsync()
    {
        IsPlaying = false;
        IsPaused = false;
        // Reset position to zero
    }

    public async Task SeekAsync(TimeSpan position)
    {
        var duration = GetDuration();
        // Clamp position between 0 and duration
        var clampedPosition = TimeSpan.FromTicks(
            Math.Clamp(position.Ticks, 0, duration.Ticks)
        );
        // Set playback position
    }

    public void SetVolume(float volume)
    {
        Volume = Math.Clamp(volume, 0.0f, 1.0f);
    }

    public void SetLoop(bool loop)
    {
        IsLooping = loop;
    }

    public void SetTempo(float tempo)
    {
        if (tempo <= 0) throw new ArgumentException("Tempo must be positive", nameof(tempo));
        Tempo = tempo;
    }

    public TimeSpan GetPosition()
    {
        // Return current playback position
        return TimeSpan.Zero; // Placeholder
    }

    public TimeSpan GetDuration()
    {
        if (!IsLoaded) return TimeSpan.Zero;

        return CurrentMidi.GetDuration<MetricTimeSpan>().TimeSpan;
    }

    public int GetTrackCount()
    {
        return CurrentMidi?.GetTrackChunks().Count() ?? 0;
    }
}
```

**Test References:**
- `MusicPlayerTests.cs:51-53` - IsPlaying, IsPaused, Volume properties
- `MusicPlayerTests.cs:63` - InitializeAsync
- `MusicPlayerTests.cs:94` - CurrentMidi, IsLoaded properties
- `MusicPlayerTests.cs:169` - PlayAsync with data
- `MusicPlayerTests.cs:239` - GetPosition returns TimeSpan.Zero after stop

---

## 5. Missing Event Types

All events should be in namespace `PokeNET.Domain.ECS.Events` or `PokeNET.Domain.Models`.

### 5.1 GameStateChangedEvent

```csharp
public class GameStateChangedEvent : IGameEvent
{
    public GameState PreviousState { get; set; }
    public GameState NewState { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
```

---

### 5.2 BattleStartEvent

```csharp
public class BattleStartEvent : IGameEvent
{
    public bool IsWildBattle { get; set; }
    public bool IsGymLeader { get; set; }
    public string? TrainerName { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
```

---

### 5.3 BattleEndEvent

```csharp
public class BattleEndEvent : IGameEvent
{
    public bool PlayerWon { get; set; }
    public int ExperienceGained { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
```

---

### 5.4 BattleEvent (Base Class)

```csharp
public abstract class BattleEvent : IGameEvent
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
```

---

### 5.5 PokemonFaintEvent

```csharp
public class PokemonFaintEvent : IGameEvent
{
    public string PokemonName { get; set; } = string.Empty;
    public bool IsPlayerPokemon { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
```

---

### 5.6 AttackEvent

```csharp
public class AttackEvent : IGameEvent
{
    public string AttackName { get; set; } = string.Empty;
    public string AttackType { get; set; } = string.Empty;
    public int Damage { get; set; }
    public bool IsPlayerAttack { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
```

---

### 5.7 CriticalHitEvent

```csharp
public class CriticalHitEvent : IGameEvent
{
    public string AttackName { get; set; } = string.Empty;
    public int Damage { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
```

---

### 5.8 HealthChangedEvent

```csharp
public class HealthChangedEvent : IGameEvent
{
    public int CurrentHealth { get; set; }
    public int MaxHealth { get; set; }
    public float HealthPercentage { get; set; }
    public int HealthDelta { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
```

---

### 5.9 WeatherChangedEvent

```csharp
public class WeatherChangedEvent : IGameEvent
{
    public Weather PreviousWeather { get; set; }
    public Weather NewWeather { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
```

---

### 5.10 ItemUseEvent

```csharp
public class ItemUseEvent : IGameEvent
{
    public string ItemName { get; set; } = string.Empty;
    public string ItemType { get; set; } = string.Empty;
    public string? TargetPokemon { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
```

---

### 5.11 PokemonCaughtEvent

```csharp
public class PokemonCaughtEvent : IGameEvent
{
    public string PokemonName { get; set; } = string.Empty;
    public int Level { get; set; }
    public bool IsShiny { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
```

---

### 5.12 LevelUpEvent

```csharp
public class LevelUpEvent : IGameEvent
{
    public string PokemonName { get; set; } = string.Empty;
    public int PreviousLevel { get; set; }
    public int NewLevel { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
```

---

## 6. Missing Extensions

### 6.1 GameState Enum

**Namespace:** `PokeNET.Domain.Models`
**Status:** Needs additional values

```csharp
public enum GameState
{
    Overworld,
    Battle,
    Menu,
    Cutscene,
    Dialogue,
    Inventory,
    PokemonMenu,
    Settings
}
```

---

### 6.2 Weather Enum

**Namespace:** `PokeNET.Domain.Models`

```csharp
public enum Weather
{
    Clear,
    Rain,
    Snow,
    Sandstorm,
    Fog,
    Hail,
    HarshSunlight,
    StrongWinds
}
```

---

### 6.3 MusicState Class

**Namespace:** `PokeNET.Audio.Models`

```csharp
public class MusicState
{
    /// <summary>
    /// Whether music is currently playing
    /// </summary>
    public bool IsPlaying { get; set; }

    /// <summary>
    /// Current track identifier
    /// </summary>
    public string? CurrentTrack { get; set; }

    /// <summary>
    /// Current volume level
    /// </summary>
    public float Volume { get; set; }

    /// <summary>
    /// Whether music is paused
    /// </summary>
    public bool IsPaused { get; set; }

    /// <summary>
    /// Current playback position
    /// </summary>
    public TimeSpan Position { get; set; }
}
```

**Test References:**
- `ReactiveAudioTests.cs:480` - `state.IsPlaying`
- `ReactiveAudioTests.cs:481` - `state.CurrentTrack`

---

### 6.4 NoteName Enum

**Namespace:** `PokeNET.Audio.Models`
**Status:** Should already exist, verify completeness

```csharp
public enum NoteName
{
    C = 0,
    CSharp = 1,
    D = 2,
    DSharp = 3,
    E = 4,
    F = 5,
    FSharp = 6,
    G = 7,
    GSharp = 8,
    A = 9,
    ASharp = 10,
    B = 11
}
```

---

## 7. Implementation Dependencies

### Dependency Graph

```
ProceduralMusicGenerator
├── IRandomProvider (interface)
├── Note (model)
├── Melody (model)
├── Chord (model)
├── ChordProgression (model)
├── Rhythm (model)
├── ScaleType (enum - extend existing)
└── NoteName (enum - verify existing)

ReactiveAudioEngine
├── IAudioManager (interface - extend with missing methods)
├── IEventBus (existing)
├── AudioReactionType (enum)
├── MusicState (model)
├── GameState (enum - extend existing)
├── Weather (enum)
└── Event Types (12 classes):
    ├── GameStateChangedEvent
    ├── BattleStartEvent
    ├── BattleEndEvent
    ├── BattleEvent
    ├── PokemonFaintEvent
    ├── AttackEvent
    ├── CriticalHitEvent
    ├── HealthChangedEvent
    ├── WeatherChangedEvent
    ├── ItemUseEvent
    ├── PokemonCaughtEvent
    └── LevelUpEvent

AudioManager (extend existing)
├── IAudioCache (existing)
├── IMusicPlayer (extend)
├── ISoundEffectPlayer (existing)
└── Extension Methods

MusicPlayer (extend existing)
└── Melanchall.DryWetMidi (external dependency - already referenced)
```

### Implementation Order (Recommended)

#### Phase 1: Foundation (No Dependencies)
1. `IRandomProvider` interface + default implementation
2. `NoteName` enum (verify/extend)
3. `ScaleType` enum (extend existing)
4. `AudioReactionType` enum
5. `Weather` enum
6. `GameState` enum (extend existing)

#### Phase 2: Domain Models
7. `Note` class
8. `Melody` class
9. `Chord` class
10. `ChordProgression` class
11. `Rhythm` class
12. `MusicState` class

#### Phase 3: Event Types
13. All 12 event classes (can be done in parallel)

#### Phase 4: Service Extensions
14. `MusicPlayer` extensions
15. `AudioManager` extensions
16. `IAudioManager` extension methods
17. `ProceduralMusicGenerator` implementation
18. `ReactiveAudioEngine` implementation

---

## 8. Test Coverage Requirements

### Expected Test Results After Implementation

#### ProceduralMusicTests
- **Total Tests:** 30+
- **Test Categories:**
  - Initialization: 2 tests
  - Melody Generation: 6 tests
  - Chord Progression: 5 tests
  - Rhythm Generation: 3 tests
  - Markov Chain: 3 tests
  - L-System: 2 tests
  - Cellular Automata: 2 tests
  - MIDI Export: 3 tests
  - Constraints: 2 tests

#### ReactiveAudioTests
- **Total Tests:** 35+
- **Test Categories:**
  - Initialization: 2 tests
  - Game State Reactions: 5 tests
  - Battle Event Reactions: 8 tests
  - Health-Based Reactions: 2 tests
  - Weather/Environment: 4 tests
  - Item/Interaction: 3 tests
  - Audio State Management: 3 tests
  - Configuration: 2 tests
  - Disposal: 2 tests

#### AudioManagerTests
- **Total Tests:** 25+
- **Test Categories:**
  - Initialization: 3 tests
  - Playback: 7 tests
  - Caching: 6 tests
  - Volume/Mixing: 4 tests
  - Disposal: 3 tests
  - State Management: 3 tests

#### MusicPlayerTests
- **Total Tests:** 35+
- **Test Categories:**
  - Initialization: 3 tests
  - MIDI Loading: 6 tests
  - Playback Control: 9 tests
  - Volume Control: 4 tests
  - Loop Control: 3 tests
  - Tempo Control: 3 tests
  - Metadata: 3 tests
  - Disposal: 3 tests

### Code Coverage Goals
- **Line Coverage:** > 80%
- **Branch Coverage:** > 75%
- **Method Coverage:** > 90%

---

## 9. Additional Notes

### External Dependencies
- **Melanchall.DryWetMidi:** Already referenced in project
  - Used for MIDI file handling
  - Used for music playback
  - Already in use in existing code

### Testing Dependencies
- **Xunit:** Test framework
- **Moq:** Mocking library
- **FluentAssertions:** Assertion library
- All already configured in test project

### SOLID Principles Applied
- **Single Responsibility:** Each class has one clear purpose
- **Open/Closed:** Extensible through composition and inheritance
- **Liskov Substitution:** Interfaces can be replaced with implementations
- **Interface Segregation:** Focused, specific interfaces
- **Dependency Inversion:** Depends on abstractions (IRandomProvider, IAudioManager)

### Performance Considerations
- Procedural generation should be optimized for real-time use
- Consider caching generated melodies/progressions
- MIDI export should be async to avoid blocking
- Reactive audio should have minimal latency (<50ms)

### Thread Safety
- `IRandomProvider` should be thread-safe or use thread-local instances
- Audio playback methods should be thread-safe
- Event handlers should not block the event bus

---

## 10. Summary Checklist

### Interfaces
- [ ] `IRandomProvider` - New interface

### Enums
- [ ] `ScaleType` - Extend with 12 additional values
- [ ] `AudioReactionType` - New enum
- [ ] `Weather` - New enum
- [ ] `GameState` - Verify/extend

### Models
- [ ] `Note` - Extend existing
- [ ] `Melody` - Extend existing
- [ ] `Chord` - Extend existing
- [ ] `ChordProgression` - Extend existing
- [ ] `Rhythm` - New class
- [ ] `MusicState` - New class

### Services
- [ ] `ProceduralMusicGenerator` - New service (15+ methods)
- [ ] `ReactiveAudioEngine` - New service (15+ methods)
- [ ] `AudioManager` - Extend existing (10+ methods)
- [ ] `MusicPlayer` - Extend existing (10+ methods)
- [ ] `IAudioManager` extensions - New extension methods (10+)

### Events
- [ ] `GameStateChangedEvent`
- [ ] `BattleStartEvent`
- [ ] `BattleEndEvent`
- [ ] `BattleEvent`
- [ ] `PokemonFaintEvent`
- [ ] `AttackEvent`
- [ ] `CriticalHitEvent`
- [ ] `HealthChangedEvent`
- [ ] `WeatherChangedEvent`
- [ ] `ItemUseEvent`
- [ ] `PokemonCaughtEvent`
- [ ] `LevelUpEvent`

---

## End of Specification

**Next Steps:**
1. Review and approve this specification
2. Create implementation tasks based on dependency order
3. Implement Phase 1 (Foundation) components
4. Implement Phase 2 (Domain Models)
5. Implement Phase 3 (Events)
6. Implement Phase 4 (Services)
7. Run tests and verify all 201 errors are resolved
8. Achieve >80% code coverage

**Estimated Implementation Time:** 16-24 hours
**Estimated Lines of Code:** ~3,500-4,500 LOC
