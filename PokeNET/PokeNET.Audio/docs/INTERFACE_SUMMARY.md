# PokeNET.Audio Interface Summary

## Quick Reference Guide

### Core Interfaces (7 Total)

#### 1. IAudioService
**Purpose**: Base playback control interface
**Responsibility**: Core playback operations (SRP)
**Methods**:
- `PlayAsync(AudioTrack, CancellationToken)`
- `Pause()`
- `Resume()`
- `Stop()`
- `Seek(TimeSpan)`
- `GetPosition() → TimeSpan`

**Properties**:
- `PlaybackState State`
- `bool IsPlaying`

---

#### 2. IAudioManager
**Purpose**: Orchestration facade for all audio subsystems
**Responsibility**: High-level coordination (Facade Pattern)
**Methods**:
- `InitializeAsync(CancellationToken)`
- `ShutdownAsync(CancellationToken)`
- `PauseAll()`, `ResumeAll()`, `StopAll()`
- `MuteAll()`, `UnmuteAll()`

**Properties**:
- `IMusicPlayer MusicPlayer`
- `ISoundEffectPlayer SoundEffectPlayer`
- `IAudioMixer Mixer`
- `IAudioConfiguration Configuration`

**Events**:
- `StateChanged`
- `ErrorOccurred`

**SOLID**: Dependency Inversion - depends on abstractions

---

#### 3. IMusicPlayer : IAudioService
**Purpose**: Music-specific playback with transitions
**Responsibility**: Long-form audio with looping/crossfading (ISP)
**Methods**:
- `LoadAsync(AudioTrack, CancellationToken)`
- `TransitionToAsync(AudioTrack, bool useCrossfade, CancellationToken)`
- `FadeOutAsync(TimeSpan, CancellationToken)`
- `FadeInAsync(TimeSpan, CancellationToken)`
- `SetVolume(float)`, `GetVolume() → float`

**Properties**:
- `AudioTrack? CurrentTrack`
- `AudioTrack? NextTrack`
- `MusicState MusicState`
- `bool IsLooping`
- `TimeSpan CrossfadeDuration`

**Events**:
- `TrackCompleted`
- `TrackTransitioning`

**SOLID**: Liskov Substitution - extends IAudioService properly

---

#### 4. ISoundEffectPlayer
**Purpose**: Short-duration sound management
**Responsibility**: Fire-and-forget SFX with channel limits (ISP)
**Methods**:
- `Play(SoundEffect, float? volume, int priority) → Guid?`
- `PlayAsync(SoundEffect, float?, int, CancellationToken)`
- `Stop(Guid) → bool`
- `StopAll()`
- `StopAllByName(string) → int`
- `SetMasterVolume(float)`, `GetMasterVolume() → float`
- `Mute()`, `Unmute()`
- `IsPlaying(Guid) → bool`
- `PreloadAsync(SoundEffect, CancellationToken)`
- `Unload(string)`

**Properties**:
- `int MaxSimultaneousSounds`
- `int ActiveSoundCount`
- `bool IsMuted`

**Events**:
- `SoundCompleted`
- `SoundInterrupted`

**SOLID**: Interface Segregation - separate from music concerns

---

#### 5. IProceduralMusicGenerator
**Purpose**: DryWetMidi-powered procedural music generation
**Responsibility**: Dynamic music creation (OCP)
**Methods**:
- `GenerateAsync(ProceduralMusicParameters, CancellationToken) → AudioTrack`
- `GenerateMidiAsync(ProceduralMusicParameters, CancellationToken) → MidiFile`
- `SetScale(Scale)`, `SetTempo(int)`, `SetTimeSignature(TimeSignature)`
- `AdaptCurrentTrackAsync(MusicAdaptationParameters, CancellationToken)`
- `GenerateVariationAsync(AudioTrack, float variationIntensity, CancellationToken) → AudioTrack`

**Properties**:
- `Scale CurrentScale`
- `int Tempo`
- `TimeSignature TimeSignature`

**Events**:
- `GenerationCompleted`
- `TrackAdapted`

**SOLID**: Open/Closed - extensible for different generation algorithms

---

#### 6. IAudioMixer
**Purpose**: Multi-channel volume and mixing control
**Responsibility**: Volume, ducking, panning (SRP)
**Methods**:
- `SetChannelVolume(AudioChannel, float)`
- `GetChannelVolume(AudioChannel) → float`
- `EnableDucking(float duckingLevel, TimeSpan? fadeTime)`
- `DisableDucking()`
- `MuteChannel(AudioChannel)`, `UnmuteChannel(AudioChannel)`
- `IsChannelMuted(AudioChannel) → bool`
- `MuteAll()`, `UnmuteAll()`
- `SetPan(AudioChannel, float)`, `GetPan(AudioChannel) → float`
- `FadeChannelAsync(AudioChannel, float targetVolume, TimeSpan, CancellationToken)`
- `Reset()`

**Properties**:
- `float MasterVolume`
- `float MusicVolume`
- `float SoundEffectsVolume`
- `float VoiceVolume`
- `bool IsDuckingEnabled`
- `float DuckingLevel`

**Events**:
- `VolumeChanged`

---

#### 7. IAudioConfiguration
**Purpose**: Centralized settings management
**Responsibility**: Configuration and validation (OCP)
**Methods**:
- `SetCustomSetting<T>(string key, T value)`
- `GetCustomSetting<T>(string key, T defaultValue) → T`
- `LoadAsync(string filePath, CancellationToken)`
- `SaveAsync(string filePath, CancellationToken)`
- `ResetToDefaults()`
- `Validate() → bool`
- `GetValidationErrors() → IReadOnlyCollection<string>`

**Properties** (Sample):
- `int SampleRate`, `int BitDepth`, `int Channels`
- `int BufferSize`, `int MaxSimultaneousSounds`
- `float DefaultMusicVolume`, `float DefaultSoundEffectsVolume`
- `bool EnableDucking`, `float DuckingLevel`
- `TimeSpan DefaultCrossfadeDuration`
- `string AudioBasePath`, `string MusicPath`, `string SoundEffectsPath`
- `bool PreloadAudio`, `bool EnableProceduralMusic`
- `string AudioBackend`
- `IReadOnlyDictionary<string, object> CustomSettings`

**Events**:
- `ConfigurationChanged`

**SOLID**: Open/Closed - extensible via CustomSettings dictionary

---

## Domain Models (3 Total)

### AudioTrack
Long-form music with rich metadata
- Identity: Id, Name, FilePath
- Properties: Duration, SampleRate, Channels, BitDepth, Volume
- Metadata: Artist, Album, Genre, Mood, Energy
- Behavior: Loop, TrackType
- Statistics: CreatedAt, LastPlayedAt, PlayCount
- Support: Procedurally generated flag

### SoundEffect
Short-duration audio with constraints
- Identity: Id, Name, FilePath
- Properties: Duration, Volume, Priority, SampleRate
- Constraints: AllowOverlap, MaxSimultaneousInstances, Cooldown
- Organization: Category (UI, Combat, Movement, etc.)
- State: IsPreloaded, PlayCount

### MusicState
Immutable snapshot of music player state
- Tracks: CurrentTrack, NextTrack
- State: PlaybackState, Position, Volume
- Settings: IsLooping, IsMuted, PlaybackSpeed
- Transition: IsTransitioning, TransitionProgress
- Adaptive: Tempo, Energy, Mood
- Method: `Snapshot() → MusicState`

---

## Exception Hierarchy

```
AudioException (base)
├── PlaybackException
├── AudioLoadException (+ FilePath property)
├── AudioInitializationException
└── AudioConfigurationException (+ ValidationErrors property)
```

---

## Enumerations

### PlaybackState
`Stopped`, `Playing`, `Paused`, `Buffering`, `Error`

### TrackType
`Music`, `Ambient`, `Voice`, `SoundEffect`, `UI`, `Procedural`

### AudioChannel
`Master`, `Music`, `SoundEffects`, `Voice`, `Ambient`, `UI`

### SoundCategory
`General`, `UI`, `Combat`, `Movement`, `Environment`, `Item`, `Voice`, `SpecialEffect`, `Pokemon`, `Battle`, `Menu`

### InterruptionReason
`ChannelLimitReached`, `ManualStop`, `GlobalStop`, `ResourceExhaustion`, `Error`

### AdaptationStyle
`Smooth`, `Abrupt`, `NextPhrase`, `NextMeasure`

---

## SOLID Compliance Matrix

| Principle | Implementation |
|-----------|----------------|
| **Single Responsibility** | Each interface handles ONE concern (playback, mixing, config, etc.) |
| **Open/Closed** | Extensible via events, custom settings, and polymorphism |
| **Liskov Substitution** | IMusicPlayer properly extends IAudioService |
| **Interface Segregation** | Small, focused interfaces (Music vs SFX separation) |
| **Dependency Inversion** | Manager depends on abstractions, not implementations |

---

## Design Patterns Applied

1. **Facade**: IAudioManager simplifies complex subsystem interactions
2. **Strategy**: Different audio backends via AudioBackend setting
3. **Observer**: Event-driven state changes and notifications
4. **Factory**: Future extension point for track/effect creation
5. **Command**: Async operations enable command pattern for playback queue

---

## File Structure

```
PokeNET.Audio/
├── PokeNET.Audio.csproj
├── Abstractions/
│   ├── IAudioService.cs
│   ├── IAudioManager.cs
│   ├── IMusicPlayer.cs
│   ├── ISoundEffectPlayer.cs
│   ├── IProceduralMusicGenerator.cs
│   ├── IAudioMixer.cs
│   └── IAudioConfiguration.cs
├── Models/
│   ├── AudioTrack.cs
│   ├── SoundEffect.cs
│   └── MusicState.cs
├── Exceptions/
│   └── AudioException.cs
└── docs/
    ├── ARCHITECTURE.md
    └── INTERFACE_SUMMARY.md
```

---

## Next Steps (Implementation Phase)

1. Implement concrete audio service using MonoGame/OpenAL
2. Create AudioManager orchestration logic
3. Implement MusicPlayer with crossfading support
4. Implement SoundEffectPlayer with channel management
5. Implement ProceduralMusicGenerator using DryWetMidi
6. Create AudioMixer with ducking support
7. Implement configuration system with JSON support
8. Write comprehensive unit tests
9. Create integration tests
10. Performance optimization and profiling
