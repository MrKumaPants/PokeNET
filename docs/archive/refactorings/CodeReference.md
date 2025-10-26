# MusicPlayer Refactoring - Code Reference

## File Structure

```
PokeNET/
├── PokeNET.Audio/
│   └── Services/
│       ├── MusicPlayer.cs (611 lines) ← REFACTORED FACADE
│       └── Music/
│           ├── IMusicFileManager.cs (45 lines)
│           ├── MusicFileManager.cs (125 lines)
│           ├── IMusicStateManager.cs (94 lines)
│           ├── MusicStateManager.cs (165 lines)
│           ├── IMusicVolumeController.cs (53 lines)
│           ├── MusicVolumeController.cs (101 lines)
│           ├── IMusicPlaybackEngine.cs (73 lines)
│           ├── MusicPlaybackEngine.cs (281 lines)
│           ├── IMusicTransitionHandler.cs (56 lines)
│           └── MusicTransitionHandler.cs (102 lines)
└── docs/
    └── architecture/
        └── refactoring/
            ├── MusicPlayerRefactoring.md ← DETAILED GUIDE
            ├── Summary.md ← EXECUTIVE SUMMARY
            └── CodeReference.md ← THIS FILE
```

## Key Code Patterns

### 1. Service Composition (Facade Pattern)

**Before (Monolithic):**
```csharp
public sealed class MusicPlayer : IMusicPlayer
{
    private IOutputDevice? _outputDevice;
    private Playback? _currentPlayback;
    private MidiFile? _currentMidiFile;
    private float _volume;
    private bool _isPlaying;
    private bool _isPaused;
    // ... 40+ private fields and methods
    // All responsibilities mixed together
}
```

**After (Facade with Composition):**
```csharp
public sealed class MusicPlayer : IMusicPlayer
{
    private readonly ILogger<MusicPlayer> _logger;
    private readonly IMusicFileManager _fileManager;          // ← File operations
    private readonly IMusicStateManager _stateManager;        // ← State tracking
    private readonly IMusicVolumeController _volumeController;// ← Volume control
    private readonly IMusicPlaybackEngine _playbackEngine;    // ← Playback control
    private readonly IMusicTransitionHandler _transitionHandler; // ← Transitions
    private readonly SemaphoreSlim _operationLock;
    private bool _disposed;

    // Constructor with service injection
    public MusicPlayer(
        ILogger<MusicPlayer> logger,
        IMusicFileManager fileManager,
        IMusicStateManager stateManager,
        IMusicVolumeController volumeController,
        IMusicPlaybackEngine playbackEngine,
        IMusicTransitionHandler transitionHandler)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _fileManager = fileManager ?? throw new ArgumentNullException(nameof(fileManager));
        _stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
        _volumeController = volumeController ?? throw new ArgumentNullException(nameof(volumeController));
        _playbackEngine = playbackEngine ?? throw new ArgumentNullException(nameof(playbackEngine));
        _transitionHandler = transitionHandler ?? throw new ArgumentNullException(nameof(transitionHandler));

        _operationLock = new SemaphoreSlim(1, 1);
        _playbackEngine.PlaybackFinished += OnPlaybackFinished;

        _logger.LogInformation("MusicPlayer initialized with composed services");
    }
}
```

### 2. Property Delegation

**Before:**
```csharp
public float Volume
{
    get => _volume;
    set
    {
        _volume = Math.Clamp(value, 0.0f, 1.0f);
        _logger.LogDebug("Music volume set to {Volume}", _volume);
    }
}
```

**After:**
```csharp
// Simple delegation to service
public float Volume
{
    get => _volumeController.Volume;
    set => _volumeController.Volume = value;
}

// Service handles the logic
public class MusicVolumeController : IMusicVolumeController
{
    public float Volume
    {
        get => _volume;
        set
        {
            _volume = Math.Clamp(value, 0.0f, 1.0f);
            _logger.LogDebug("Music volume set to {Volume}", _volume);
        }
    }
}
```

### 3. Method Delegation

**Before (Monolithic PlayAsync):**
```csharp
public async Task PlayAsync(AudioTrack track, CancellationToken cancellationToken = default)
{
    await _playbackLock.WaitAsync(cancellationToken);
    try
    {
        // Stop current playback
        StopInternal();

        // Load MIDI file from cache or disk
        var fullPath = Path.Combine(_settings.AssetBasePath, track.FilePath);
        if (!File.Exists(fullPath)) throw new AudioLoadException(...);
        var midiFile = MidiFile.Read(fullPath);
        _cache.Set(track.FilePath, midiFile, fileInfo.Length);

        // Initialize output device
        if (_outputDevice == null)
        {
            _outputDevice = OutputDevice.GetByIndex(_settings.MidiOutputDevice);
        }

        // Create and start playback
        _currentPlayback = midiFile.GetPlayback(_outputDevice);
        _currentPlayback.Loop = track.Loop || _isLooping;
        _currentPlayback.Finished += OnPlaybackFinished;
        _currentPlayback.Start();

        // Update state
        _currentMidiFile = midiFile;
        _currentTrackPath = track.FilePath;
        _currentAudioTrack = track;
        _isPlaying = true;
        _isPaused = false;
        track.LastPlayedAt = DateTime.UtcNow;
        track.PlayCount++;
    }
    finally { _playbackLock.Release(); }
}
```

**After (Coordinated Delegation):**
```csharp
public async Task PlayAsync(AudioTrack track, CancellationToken cancellationToken = default)
{
    await _operationLock.WaitAsync(cancellationToken);
    try
    {
        _logger.LogInformation("Playing music: {TrackName}, Loop: {Loop}", track.Name, track.Loop);

        // Delegate file loading to FileManager
        var midiFile = await _fileManager.LoadMidiFileAsync(track.FilePath, cancellationToken);

        // Delegate device initialization to PlaybackEngine
        if (!_playbackEngine.IsInitialized)
        {
            await _playbackEngine.InitializeAsync();
        }

        // Delegate playback control to PlaybackEngine
        _playbackEngine.StartPlayback(midiFile, track.Loop || _stateManager.IsLooping);

        // Delegate state updates to StateManager
        _stateManager.SetPlaying(track, midiFile);

        _logger.LogInformation("Music playback started: {Track}", track.Name);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to play music: {TrackName}", track.Name);
        throw new AudioPlaybackException($"Failed to play music: {track.Name}", ex);
    }
    finally { _operationLock.Release(); }
}
```

### 4. Interface-Based Design

**Service Interface Example:**
```csharp
/// <summary>
/// Manages MIDI file loading, caching, and validation.
/// </summary>
public interface IMusicFileManager
{
    Task<MidiFile> LoadMidiFileAsync(string assetPath, CancellationToken cancellationToken = default);
    MidiFile LoadMidiFromBytes(byte[] midiData);
    Task<MidiFile> LoadMidiFromFileAsync(string filePath, CancellationToken cancellationToken = default);
    bool IsCached(string assetPath);
    void ClearCache();
}
```

**Service Implementation Example:**
```csharp
public sealed class MusicFileManager : IMusicFileManager
{
    private readonly ILogger<MusicFileManager> _logger;
    private readonly AudioSettings _settings;
    private readonly AudioCache _cache;

    public MusicFileManager(
        ILogger<MusicFileManager> logger,
        IOptions<AudioSettings> settings,
        AudioCache cache)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    public async Task<MidiFile> LoadMidiFileAsync(string assetPath, CancellationToken cancellationToken = default)
    {
        // Check cache first
        if (_cache.TryGet<MidiFile>(assetPath, out var cachedFile) && cachedFile != null)
        {
            _logger.LogDebug("MIDI file loaded from cache: {AssetPath}", assetPath);
            return cachedFile;
        }

        // Load from disk and cache
        var fullPath = Path.Combine(_settings.AssetBasePath, assetPath);
        if (!File.Exists(fullPath))
        {
            throw new AudioLoadException(assetPath, new FileNotFoundException($"MIDI file not found: {fullPath}"));
        }

        var midiFile = await Task.Run(() => MidiFile.Read(fullPath), cancellationToken);
        var fileInfo = new FileInfo(fullPath);
        _cache.Set(assetPath, midiFile, fileInfo.Length);

        _logger.LogDebug("MIDI file loaded from disk and cached: {AssetPath}", assetPath);
        return midiFile;
    }

    // ... other methods
}
```

### 5. Event Delegation

**Before:**
```csharp
public event EventHandler<TrackTransitionEventArgs>? TrackTransitioning;

// Raised directly in MusicPlayer
TrackTransitioning?.Invoke(this, new TrackTransitionEventArgs { ... });
```

**After:**
```csharp
// Delegate event subscription to service
public event EventHandler<TrackTransitionEventArgs>? TrackTransitioning
{
    add => _transitionHandler.TrackTransitioning += value;
    remove => _transitionHandler.TrackTransitioning -= value;
}

// Service handles event raising
public class MusicTransitionHandler : IMusicTransitionHandler
{
    public event EventHandler<TrackTransitionEventArgs>? TrackTransitioning;

    public async Task TransitionAsync(...)
    {
        // Raise event in service
        TrackTransitioning?.Invoke(this, new TrackTransitionEventArgs { ... });
        // ... transition logic
    }
}
```

## Dependency Injection Pattern

### Service Registration

```csharp
// In your DI setup (Program.cs or Startup.cs)
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAudioServices(this IServiceCollection services)
    {
        // Register music services
        services.AddSingleton<AudioCache>();
        services.AddScoped<IMusicFileManager, MusicFileManager>();
        services.AddScoped<IMusicStateManager, MusicStateManager>();
        services.AddScoped<IMusicVolumeController, MusicVolumeController>();
        services.AddScoped<IMusicPlaybackEngine, MusicPlaybackEngine>();
        services.AddScoped<IMusicTransitionHandler, MusicTransitionHandler>();

        // Register facade (composes all services)
        services.AddScoped<IMusicPlayer, MusicPlayer>();

        return services;
    }
}
```

### Constructor Injection

```csharp
// Consumer code - no changes needed!
public class GameAudioManager
{
    private readonly IMusicPlayer _musicPlayer;

    public GameAudioManager(IMusicPlayer musicPlayer)
    {
        _musicPlayer = musicPlayer; // DI injects MusicPlayer facade
    }

    public async Task PlayBattleMusic()
    {
        // Same API as before - zero breaking changes
        await _musicPlayer.PlayAsync(new AudioTrack
        {
            Name = "Battle Theme",
            FilePath = "music/battle.mid",
            Loop = true
        });
    }
}
```

## Testing Patterns

### Unit Testing Individual Services

```csharp
public class MusicVolumeControllerTests
{
    [Fact]
    public void SetVolume_WithValidValue_ShouldUpdateVolume()
    {
        // Arrange
        var logger = new Mock<ILogger<MusicVolumeController>>();
        var settings = Options.Create(new AudioSettings { MusicVolume = 1.0f });
        var controller = new MusicVolumeController(logger.Object, settings);

        // Act
        controller.SetVolume(0.5f);

        // Assert
        controller.Volume.Should().Be(0.5f);
    }

    [Fact]
    public void SetVolume_AboveMax_ShouldClampToOne()
    {
        // Arrange
        var logger = new Mock<ILogger<MusicVolumeController>>();
        var settings = Options.Create(new AudioSettings { MusicVolume = 1.0f });
        var controller = new MusicVolumeController(logger.Object, settings);

        // Act
        controller.SetVolume(1.5f);

        // Assert
        controller.Volume.Should().Be(1.0f);
    }
}
```

### Integration Testing with Mocked Services

```csharp
public class MusicPlayerIntegrationTests
{
    [Fact]
    public async Task PlayAsync_WithAllServices_ShouldCoordinateCorrectly()
    {
        // Arrange
        var mockFileManager = new Mock<IMusicFileManager>();
        var mockStateManager = new Mock<IMusicStateManager>();
        var mockVolumeController = new Mock<IMusicVolumeController>();
        var mockPlaybackEngine = new Mock<IMusicPlaybackEngine>();
        var mockTransitionHandler = new Mock<IMusicTransitionHandler>();

        mockFileManager.Setup(x => x.LoadMidiFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestMidiFile());

        var musicPlayer = new MusicPlayer(
            Mock.Of<ILogger<MusicPlayer>>(),
            mockFileManager.Object,
            mockStateManager.Object,
            mockVolumeController.Object,
            mockPlaybackEngine.Object,
            mockTransitionHandler.Object);

        var track = new AudioTrack { Name = "Test", FilePath = "test.mid" };

        // Act
        await musicPlayer.PlayAsync(track);

        // Assert
        mockFileManager.Verify(x => x.LoadMidiFileAsync("test.mid", It.IsAny<CancellationToken>()), Times.Once);
        mockPlaybackEngine.Verify(x => x.StartPlayback(It.IsAny<MidiFile>(), It.IsAny<bool>()), Times.Once);
        mockStateManager.Verify(x => x.SetPlaying(track, It.IsAny<MidiFile>()), Times.Once);
    }
}
```

## Migration Checklist

### For Existing Code

- ✅ No changes needed to code using `IMusicPlayer`
- ✅ Public API is 100% backward compatible
- ✅ All method signatures unchanged
- ✅ All properties work identically

### For New Code

- ✅ Use interface-based injection (`IMusicPlayer`)
- ✅ Let DI container handle service composition
- ✅ Mock services individually in tests
- ✅ Follow dependency injection patterns

### For Tests

- ⚠️ Update test infrastructure if mocking was used
- ✅ Use service constructor for fine-grained control
- ✅ Mock individual services instead of entire MusicPlayer
- ✅ Test services independently for better isolation

## File Paths Reference

All file paths are absolute from project root:

### Interfaces
```
/mnt/c/Users/nate0/RiderProjects/PokeNET/PokeNET/PokeNET.Audio/Services/Music/IMusicFileManager.cs
/mnt/c/Users/nate0/RiderProjects/PokeNET/PokeNET/PokeNET.Audio/Services/Music/IMusicStateManager.cs
/mnt/c/Users/nate0/RiderProjects/PokeNET/PokeNET/PokeNET.Audio/Services/Music/IMusicVolumeController.cs
/mnt/c/Users/nate0/RiderProjects/PokeNET/PokeNET/PokeNET.Audio/Services/Music/IMusicPlaybackEngine.cs
/mnt/c/Users/nate0/RiderProjects/PokeNET/PokeNET/PokeNET.Audio/Services/Music/IMusicTransitionHandler.cs
```

### Implementations
```
/mnt/c/Users/nate0/RiderProjects/PokeNET/PokeNET/PokeNET.Audio/Services/Music/MusicFileManager.cs
/mnt/c/Users/nate0/RiderProjects/PokeNET/PokeNET/PokeNET.Audio/Services/Music/MusicStateManager.cs
/mnt/c/Users/nate0/RiderProjects/PokeNET/PokeNET/PokeNET.Audio/Services/Music/MusicVolumeController.cs
/mnt/c/Users/nate0/RiderProjects/PokeNET/PokeNET/PokeNET.Audio/Services/Music/MusicPlaybackEngine.cs
/mnt/c/Users/nate0/RiderProjects/PokeNET/PokeNET/PokeNET.Audio/Services/Music/MusicTransitionHandler.cs
```

### Facade
```
/mnt/c/Users/nate0/RiderProjects/PokeNET/PokeNET/PokeNET.Audio/Services/MusicPlayer.cs
```

### Documentation
```
/mnt/c/Users/nate0/RiderProjects/PokeNET/docs/architecture/refactoring/MusicPlayerRefactoring.md
/mnt/c/Users/nate0/RiderProjects/PokeNET/docs/architecture/refactoring/Summary.md
/mnt/c/Users/nate0/RiderProjects/PokeNET/docs/architecture/refactoring/CodeReference.md
```

## Conclusion

This refactoring demonstrates:
- **Clean Architecture** through layered service design
- **SOLID Principles** applied consistently
- **Dependency Injection** for loose coupling
- **Interface-Based Design** for testability
- **Facade Pattern** for API stability
- **Zero Breaking Changes** for smooth migration

Each service is now independently:
- Testable (unit tests for each service)
- Maintainable (smaller, focused classes)
- Reusable (services can be used independently)
- Extensible (new implementations can be swapped)

The refactoring improves code quality while maintaining complete backward compatibility.
