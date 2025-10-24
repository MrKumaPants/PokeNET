# AudioManager Refactoring - ADR-002

## Status
**ACCEPTED** - Implementation in progress

## Context
AudioManager.cs has grown to 749 lines, violating the Single Responsibility Principle (SRP) with multiple distinct concerns:
- Music playback management
- Sound effect coordination
- Ambient audio management
- Volume control and mixing
- Audio caching coordination
- Lifecycle and state management
- Event coordination

## Decision
Refactor AudioManager into focused, single-responsibility classes:

### New Architecture

```
AudioManager (Orchestrator - ~300 lines)
├── AudioVolumeManager (~120 lines)
│   ├── Master/Music/SFX/Ambient volume control
│   ├── Ducking logic
│   └── Player volume synchronization
│
├── AudioStateManager (~100 lines)
│   ├── State tracking (tracks, playback states)
│   ├── Event coordination (StateChanged, ErrorOccurred)
│   └── Global controls (PauseAll, ResumeAll, StopAll, Mute/Unmute)
│
├── AudioCacheCoordinator (~100 lines)
│   ├── Preload operations
│   ├── Cache management
│   └── Size tracking
│
└── AmbientAudioManager (~120 lines)
    ├── Ambient playback
    ├── Ambient state tracking
    └── ISoundEffectPlayer integration
```

### Class Responsibilities

#### AudioManager (Facade/Orchestrator)
**Responsibility:** High-level audio system coordination
- Implements IAudioManager interface
- Delegates to specialized managers
- Lifecycle management (Initialize, Shutdown, Dispose)
- Music playback operations (delegates to IMusicPlayer + managers)
- Sound effect operations (delegates to ISoundEffectPlayer + managers)
- **Size:** ~300 lines

#### AudioVolumeManager
**Responsibility:** Volume control across all audio channels
- Master, Music, SFX, Ambient volume management
- Volume clamping and validation
- Ducking implementation
- Player volume synchronization
- **Interface:** IAudioVolumeManager
- **Size:** ~120 lines

#### AudioStateManager
**Responsibility:** Audio system state and event coordination
- Current track tracking
- Playback state management
- Event raising (StateChanged, ErrorOccurred)
- Global playback controls (PauseAll, ResumeAll, StopAll)
- Mute/Unmute coordination
- **Interface:** IAudioStateManager
- **Size:** ~100 lines

#### AudioCacheCoordinator
**Responsibility:** High-level audio caching operations
- Preload operations (single and batch)
- Cache clearing
- Cache size reporting
- Wraps IAudioCache with coordinator logic
- **Interface:** IAudioCacheCoordinator
- **Size:** ~100 lines

#### AmbientAudioManager
**Responsibility:** Ambient audio lifecycle management
- Ambient playback (Play, Pause, Resume, Stop)
- Ambient track state tracking
- Volume management for ambient audio
- Integration with ISoundEffectPlayer
- **Interface:** IAmbientAudioManager
- **Size:** ~120 lines

## Consequences

### Positive
- **SRP Compliance:** Each class has one clear responsibility
- **Maintainability:** Smaller, focused classes easier to understand and modify
- **Testability:** Isolated concerns simplify unit testing
- **Extensibility:** New features added to appropriate manager
- **Code Size:** AudioManager reduced from 749 to ~300 lines

### Negative
- **Increased Files:** 5 new classes (8 files with interfaces)
- **Indirection:** Extra layer of delegation
- **Migration Effort:** DI configuration updates required

### Neutral
- **Interface Stability:** IAudioManager remains unchanged
- **Test Coverage:** Existing tests will be updated, new tests added
- **Performance:** Negligible impact (delegation is fast)

## Implementation Plan

### Phase 1: Interface Design
1. Define IAudioVolumeManager
2. Define IAudioStateManager
3. Define IAudioCacheCoordinator
4. Define IAmbientAudioManager

### Phase 2: Implementation
1. Implement AudioVolumeManager
2. Implement AudioStateManager
3. Implement AudioCacheCoordinator
4. Implement AmbientAudioManager

### Phase 3: AudioManager Refactoring
1. Add new manager dependencies to constructor
2. Replace inline logic with manager delegation
3. Remove extracted responsibilities
4. Verify line count <350

### Phase 4: Testing
1. Create tests for each new manager
2. Update existing AudioManager tests
3. Verify 100% feature parity
4. Run full test suite

### Phase 5: Integration
1. Update DI registration
2. Update documentation
3. Verify build success

## Verification Criteria

- AudioManager.cs < 350 lines
- Each new manager < 200 lines
- All existing tests pass
- No breaking changes to IAudioManager
- 100% feature parity maintained
- Build succeeds without warnings

## References
- SOLID Principles: Single Responsibility Principle
- Design Patterns: Facade, Delegation
- AudioManager.cs (original): 749 lines
