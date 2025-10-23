# PokeNET.Audio - Architecture Documentation

## Overview

The PokeNET.Audio system provides a comprehensive, extensible audio engine for the PokeNET game framework. It is designed following SOLID principles to ensure maintainability, testability, and extensibility.

## SOLID Principles Implementation

### Single Responsibility Principle (SRP)
Each interface has a single, well-defined responsibility:

- **IAudioService**: Core playback control (play, pause, stop, seek)
- **IAudioManager**: High-level orchestration of audio subsystems
- **IMusicPlayer**: Music-specific features (looping, crossfading, transitions)
- **ISoundEffectPlayer**: Sound effect management (fire-and-forget, priority, channels)
- **IProceduralMusicGenerator**: Procedural music generation via DryWetMidi
- **IAudioMixer**: Volume control and channel mixing
- **IAudioConfiguration**: Configuration management and settings

### Open/Closed Principle (OCP)
The architecture is open for extension but closed for modification:

- Interfaces define contracts without implementation details
- Custom settings via `Dictionary<string, object>` in IAudioConfiguration
- Event-driven architecture allows extending behavior without modifying core code
- Metadata dictionaries in models allow custom data without breaking changes

### Liskov Substitution Principle (LSP)
All implementations can be substituted without breaking functionality:

- IMusicPlayer extends IAudioService, maintaining behavioral compatibility
- All interfaces use consistent exception handling patterns
- Nullable reference types ensure contract clarity

### Interface Segregation Principle (ISP)
Interfaces are small and focused, avoiding fat interfaces:

- Music and sound effects are separate interfaces (IMusicPlayer vs ISoundEffectPlayer)
- Each interface contains only methods relevant to its domain
- Clients depend only on the methods they use

### Dependency Inversion Principle (DIP)
High-level modules depend on abstractions, not concretions:

- IAudioManager depends on IMusicPlayer, ISoundEffectPlayer, IAudioMixer (abstractions)
- All cross-component communication uses interfaces
- No direct dependencies on implementation classes

## Architecture Layers

```
┌─────────────────────────────────────────────────────┐
│           IAudioManager (Facade/Orchestrator)        │
│  - Coordinates all audio subsystems                  │
│  - Enforces business rules (ducking, priority)       │
│  - Provides unified API                              │
└───────────────┬─────────────────────────────────────┘
                │
        ┌───────┴───────┬────────────┬──────────────┐
        │               │            │              │
┌───────▼──────┐  ┌────▼─────┐  ┌──▼──────┐  ┌────▼────────┐
│ IMusicPlayer │  │ ISoundFX │  │ IAudio  │  │ IProc.Music │
│              │  │ Player   │  │ Mixer   │  │ Generator   │
│ - Looping    │  │ - Multi  │  │ - Vols  │  │ - MIDI Gen  │
│ - Crossfade  │  │ - Priority│  │ - Duck  │  │ - Adaptive  │
│ - Fade I/O   │  │ - Channels│  │ - Pan   │  │ - DryWetMidi│
└──────────────┘  └──────────┘  └─────────┘  └─────────────┘
        │               │            │              │
        └───────┬───────┴────────────┴──────────────┘
                │
        ┌───────▼────────────────────┐
        │   IAudioConfiguration      │
        │   - Settings & Defaults    │
        └────────────────────────────┘
```

## Key Interfaces

### IAudioService (Base Playback)
Core playback operations inherited by specialized players:
- Play, Pause, Resume, Stop
- Seek, GetPosition
- PlaybackState management

### IAudioManager (Orchestrator)
High-level facade coordinating all subsystems:
- Unified initialization/shutdown
- Global pause/resume/stop
- Event aggregation
- Business rule enforcement

### IMusicPlayer (Music Specialist)
Extended playback with music-specific features:
- Track loading and queueing
- Crossfade transitions
- Fade in/out effects
- Loop control
- Volume management
- Track events (completed, transitioning)

### ISoundEffectPlayer (SFX Specialist)
Short-duration sound management:
- Fire-and-forget playback
- Priority-based channel allocation
- Simultaneous sound limiting
- Preloading for performance
- Per-sound and master volume
- Sound completion/interruption events

### IProceduralMusicGenerator (Dynamic Music)
DryWetMidi-powered procedural generation:
- Parameter-driven generation (mood, energy, complexity)
- Real-time track adaptation
- MIDI file generation
- Musical variation creation
- Scale, tempo, time signature control
- Adaptive music events

### IAudioMixer (Mixing & Volume)
Multi-channel volume control:
- Master and per-channel volumes
- Audio ducking (reduce music during SFX)
- Stereo panning
- Channel muting
- Fade effects
- Volume change events

### IAudioConfiguration (Settings)
Centralized configuration management:
- Sample rate, bit depth, channels
- Buffer sizes and latency control
- Default volumes and paths
- Ducking and crossfade settings
- Custom settings via dictionary
- Load/save configuration files
- Validation and error reporting

## Domain Models

### AudioTrack
Represents a music track with rich metadata:
- Identity (Id, Name, FilePath)
- Audio properties (Duration, SampleRate, Channels, BitDepth)
- Metadata (Artist, Album, Genre, Mood, Energy)
- Playback settings (Volume, Loop)
- Statistics (CreatedAt, LastPlayedAt, PlayCount)
- Support for procedurally generated tracks

### SoundEffect
Short-duration sound with playback constraints:
- Identity (Id, Name, FilePath)
- Audio properties (Duration, SampleRate, Channels)
- Playback rules (Priority, AllowOverlap, MaxSimultaneousInstances)
- Cooldown management
- Category-based organization
- Statistics and preloading state

### MusicState
Snapshot of music player state:
- Current and next track
- Playback state and position
- Volume and mute state
- Transition state and progress
- Adaptive music properties (Tempo, Energy, Mood)
- Custom state data
- Immutable snapshot support

## Exception Hierarchy

```
AudioException (base)
├── PlaybackException (playback failures)
├── AudioLoadException (file loading errors)
├── AudioInitializationException (setup failures)
└── AudioConfigurationException (invalid settings)
```

All exceptions include:
- Descriptive error messages
- Inner exception support
- Context-specific properties (e.g., FilePath in AudioLoadException)

## Design Patterns Used

### Facade Pattern
`IAudioManager` provides a simplified interface to the complex audio subsystem.

### Strategy Pattern
Different audio backends can be swapped via `IAudioConfiguration.AudioBackend`.

### Observer Pattern
Event-driven architecture using C# events for state changes, errors, and completions.

### Factory Pattern
Track and effect creation can be abstracted behind factory interfaces (future extension).

### Command Pattern
Audio operations can be queued and executed asynchronously (implicit in async methods).

## Extensibility Points

### Custom Audio Backends
Implement interfaces with different audio libraries:
- OpenAL for cross-platform
- XAudio2 for Windows
- WASAPI for low-latency Windows
- Web Audio API for browser

### Custom Music Generation
Extend `IProceduralMusicGenerator` with:
- AI-based composition
- Markov chain generation
- Algorithmic music theories
- Integration with external MIDI libraries

### Custom Effects
Add DSP effects via mixer extensions:
- Reverb, echo, chorus
- Equalization
- Compression, limiting
- 3D spatial audio

### Custom Configuration Sources
Load settings from:
- JSON, XML, YAML files
- Database
- Cloud services
- User preferences UI

## Performance Considerations

### Preloading
- Sound effects can be preloaded to reduce latency
- Music streams are loaded on-demand
- Configuration controls preload behavior

### Channel Limits
- `MaxSimultaneousSounds` prevents resource exhaustion
- Priority-based eviction when limits reached
- Per-sound instance limits

### Async Operations
- All I/O operations are async (loading, generation)
- Cancellation token support throughout
- Non-blocking playback control

### Memory Management
- Unload unused sound effects
- Stream long music tracks instead of loading entirely
- Procedural generation creates tracks on-demand

## Thread Safety

All interfaces should be implemented with thread-safety in mind:
- Async/await patterns for I/O
- Proper locking for shared state
- Thread-safe event raising
- Concurrent playback support

## Testing Strategy

### Unit Testing
- Mock all interfaces for isolated testing
- Test business logic in managers
- Validate configuration rules
- Test exception handling

### Integration Testing
- Test cross-component communication
- Validate event propagation
- Test async operation sequences

### Performance Testing
- Measure playback latency
- Test channel limits under load
- Benchmark procedural generation
- Profile memory usage

## Future Enhancements

### Phase 7+ Considerations
- 3D spatial audio with positional sound
- Audio streaming from network sources
- Voice chat integration
- Advanced DSP effects pipeline
- MIDI controller input support
- Audio recording and capture
- Playlist and queue management
- Audio visualization data export

## Dependencies

### Required NuGet Packages
- **Melanchall.DryWetMidi** (v7.2.0+): MIDI file manipulation and procedural music generation

### Implementation Dependencies (TBD)
- Audio backend library (OpenAL, NAudio, etc.)
- Optional: FFmpeg for format conversion
- Optional: Audio codec libraries

## Integration with PokeNET

The audio system integrates with:
- **PokeNET.Scripting**: Scripted audio events and triggers
- **PokeNET.Engine**: Game state-driven music adaptation
- **PokeNET.UI**: UI sound effects
- **PokeNET.Battle**: Dynamic battle music
- **PokeNET.Configuration**: Shared settings infrastructure

## Conclusion

The PokeNET.Audio architecture provides a robust, extensible foundation for all audio needs in the game framework. By adhering to SOLID principles and providing well-defined interfaces, the system remains maintainable while offering powerful features like procedural music generation, advanced mixing, and adaptive soundscapes.
