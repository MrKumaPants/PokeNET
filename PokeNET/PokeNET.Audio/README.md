# PokeNET.Audio

**SOLID-Compliant Audio System for PokeNET Game Framework**

## Overview

PokeNET.Audio is a comprehensive, extensible audio engine designed following SOLID principles. It provides background music, sound effects, procedural music generation, and advanced mixing capabilities.

## Features

- **Background Music**: Seamless looping, crossfading, and track transitions
- **Sound Effects**: Multi-channel playback with priority-based channel management
- **Procedural Music**: DryWetMidi-powered dynamic music generation
- **Audio Mixing**: Per-channel volume control, ducking, and stereo panning
- **Configuration**: Flexible settings with JSON support and validation
- **Event-Driven**: Reactive architecture for state changes and errors
- **Async/Await**: Non-blocking I/O operations throughout
- **Platform-Agnostic**: Interface-based design supports multiple audio backends

## Architecture

The system is organized into three layers:

### 1. Orchestration Layer
- **IAudioManager**: Facade coordinating all audio subsystems

### 2. Specialized Player Layer
- **IMusicPlayer**: Long-form audio with looping and crossfading
- **ISoundEffectPlayer**: Short-duration multi-channel audio
- **IProceduralMusicGenerator**: DryWetMidi-based procedural generation

### 3. Foundation Layer
- **IAudioService**: Core playback control
- **IAudioMixer**: Volume and channel management
- **IAudioConfiguration**: Settings and validation

## SOLID Principles

| Principle | Implementation |
|-----------|----------------|
| **Single Responsibility** | Each interface has one clear purpose |
| **Open/Closed** | Extensible via events, custom settings, and polymorphism |
| **Liskov Substitution** | IMusicPlayer extends IAudioService correctly |
| **Interface Segregation** | Small, focused interfaces (Music vs SFX separate) |
| **Dependency Inversion** | Manager depends on abstractions, not implementations |

## Project Structure

```
PokeNET.Audio/
├── Abstractions/          # Core interfaces (7 total)
│   ├── IAudioService.cs
│   ├── IAudioManager.cs
│   ├── IMusicPlayer.cs
│   ├── ISoundEffectPlayer.cs
│   ├── IProceduralMusicGenerator.cs
│   ├── IAudioMixer.cs
│   └── IAudioConfiguration.cs
├── Models/                # Domain models (3 total)
│   ├── AudioTrack.cs
│   ├── SoundEffect.cs
│   └── MusicState.cs
├── Exceptions/            # Exception hierarchy
│   └── AudioException.cs  # + 4 derived types
└── docs/                  # Documentation
    ├── ARCHITECTURE.md
    ├── INTERFACE_SUMMARY.md
    ├── ADR-001-AUDIO-ARCHITECTURE.md
    └── CLASS_DIAGRAM.txt
```

## Quick Start

### Basic Usage (Future Implementation)

```csharp
// Initialize the audio system
var audioManager = new AudioManager(configuration);
await audioManager.InitializeAsync();

// Play background music with crossfade
var track = new AudioTrack { Name = "Battle Theme", FilePath = "music/battle.mp3" };
await audioManager.MusicPlayer.TransitionToAsync(track, useCrossfade: true);

// Play a sound effect
var sfx = new SoundEffect { Name = "Attack", FilePath = "sfx/attack.wav", Priority = 10 };
audioManager.SoundEffectPlayer.Play(sfx);

// Adjust volumes
audioManager.Mixer.MusicVolume = 0.7f;
audioManager.Mixer.EnableDucking(duckingLevel: 0.5f);
```

### Procedural Music Generation (Future Implementation)

```csharp
var generator = audioManager.ProceduralMusicGenerator;
generator.SetScale(Scale.GetScale(ScaleIntervals.Major, NoteName.C));
generator.SetTempo(120);

var parameters = new ProceduralMusicParameters
{
    Mood = "tense",
    Energy = 0.8f,
    Complexity = 0.6f,
    Duration = TimeSpan.FromMinutes(3),
    Style = "orchestral"
};

var generatedTrack = await generator.GenerateAsync(parameters);
await audioManager.MusicPlayer.PlayAsync(generatedTrack);
```

## Interfaces

### IAudioService (Base)
Core playback operations: Play, Pause, Resume, Stop, Seek

### IAudioManager (Orchestrator)
High-level facade: Initialize, Shutdown, PauseAll, ResumeAll, StopAll

### IMusicPlayer : IAudioService
Music-specific: LoadAsync, TransitionToAsync, FadeIn/Out, Looping, Crossfading

### ISoundEffectPlayer
SFX-specific: Play, Stop, Priority management, Channel limits, Preloading

### IProceduralMusicGenerator
Procedural: GenerateAsync, SetScale, SetTempo, AdaptCurrentTrackAsync

### IAudioMixer
Mixing: Channel volumes, Ducking, Panning, Muting, Fade effects

### IAudioConfiguration
Settings: SampleRate, BufferSize, Paths, Volumes, Load/Save, Validation

## Domain Models

### AudioTrack
Music track with metadata (Artist, Album, Mood, Energy, Duration, etc.)

### SoundEffect
Short audio with constraints (Priority, Cooldown, MaxSimultaneous, Category)

### MusicState
Immutable snapshot of music player state (Track, Position, Volume, Transition state)

## Exception Hierarchy

```
AudioException (base)
├── PlaybackException
├── AudioLoadException
├── AudioInitializationException
└── AudioConfigurationException
```

## Events

### IAudioManager Events
- `StateChanged`: Audio system state transitions
- `ErrorOccurred`: Error notifications

### IMusicPlayer Events
- `TrackCompleted`: Track finished playing
- `TrackTransitioning`: Track transition started

### ISoundEffectPlayer Events
- `SoundCompleted`: Sound effect finished
- `SoundInterrupted`: Sound interrupted by channel limit

### IProceduralMusicGenerator Events
- `GenerationCompleted`: Music generation finished
- `TrackAdapted`: Real-time adaptation occurred

### IAudioMixer Events
- `VolumeChanged`: Volume setting changed

### IAudioConfiguration Events
- `ConfigurationChanged`: Setting modified

## Dependencies

### Required
- **Melanchall.DryWetMidi** (v7.2.0): MIDI file manipulation and procedural music generation
- **MonoGame.Framework.DesktopGL** (v3.8.x): Audio playback backend
- **Microsoft.Extensions.Logging.Abstractions** (v9.0.0): Logging support
- **Microsoft.Extensions.Options** (v9.0.0): Configuration binding
- **Microsoft.Extensions.Configuration** (v9.0.0): Settings management

## Documentation

- [**ARCHITECTURE.md**](docs/ARCHITECTURE.md): Comprehensive architecture overview
- [**INTERFACE_SUMMARY.md**](docs/INTERFACE_SUMMARY.md): Quick reference for all interfaces
- [**ADR-001-AUDIO-ARCHITECTURE.md**](docs/ADR-001-AUDIO-ARCHITECTURE.md): Architecture Decision Record
- [**CLASS_DIAGRAM.txt**](docs/CLASS_DIAGRAM.txt): Visual class diagram (C4 model)

## Design Patterns

1. **Facade**: IAudioManager simplifies complex subsystem
2. **Strategy**: Swappable audio backends via configuration
3. **Observer**: Event-driven state changes
4. **Factory**: Future extension for track/effect creation
5. **Command**: Async operations enable command queuing

## Testing Strategy

### Unit Tests
- Mock all interfaces for isolated testing
- Test business logic in managers
- Validate configuration rules
- Test exception handling

### Integration Tests
- Cross-component communication
- Event propagation
- Async operation sequences

### Performance Tests
- Playback latency measurement
- Channel limit stress testing
- Procedural generation benchmarks
- Memory profiling

## Future Enhancements

- 3D spatial audio with positional sound
- Network audio streaming
- Voice chat integration
- Advanced DSP effects pipeline
- MIDI controller input
- Audio recording/capture
- Playlist management
- Audio visualization

## Contributing

When contributing to this project:
1. Follow SOLID principles
2. Add comprehensive XML documentation
3. Write unit tests for new features
4. Update architecture documentation
5. Maintain backward compatibility

## License

Part of the PokeNET framework. See main project license.

## Version

**Phase 6 - Architecture Design (v0.1.0)**
- ✅ Interface definitions complete
- ✅ Domain models complete
- ✅ Exception hierarchy complete
- ✅ Comprehensive documentation
- ⏳ Implementation in progress

---

**Built with SOLID principles for maintainability, testability, and extensibility.**
