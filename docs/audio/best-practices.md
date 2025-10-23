# Audio Best Practices

## Overview

This guide covers best practices for using the PokeNET audio system effectively, including performance optimization, quality guidelines, and common patterns.

## Table of Contents

1. [Performance Optimization](#performance-optimization)
2. [Quality Guidelines](#quality-guidelines)
3. [Resource Management](#resource-management)
4. [Music Design](#music-design)
5. [Sound Effect Design](#sound-effect-design)
6. [Integration Patterns](#integration-patterns)
7. [Common Pitfalls](#common-pitfalls)

## Performance Optimization

### 1. Cache Procedural Music

**Problem**: Generating music every time is expensive

```csharp
// ❌ BAD: Regenerate music repeatedly
public void EnterBattle()
{
    var music = GenerateBattleMusic();  // Expensive!
    api.Audio.PlayProceduralMusic(music);
}

// ✅ GOOD: Cache generated music
private Dictionary<string, IProceduralMusic> _musicCache = new();

public void EnterBattle()
{
    if (!_musicCache.ContainsKey("battle"))
    {
        _musicCache["battle"] = GenerateBattleMusic();
    }
    api.Audio.PlayProceduralMusic(_musicCache["battle"]);
}
```

### 2. Limit Concurrent Sounds

**Problem**: Too many sounds playing simultaneously

```csharp
// ❌ BAD: No sound management
for (int i = 0; i < 100; i++)
{
    api.Audio.PlaySound("explosion");  // May drop sounds
}

// ✅ GOOD: Manage sound playback
private int _activeSounds = 0;
private const int MaxSounds = 8;

public void PlaySoundSafe(string soundId)
{
    if (_activeSounds < MaxSounds)
    {
        api.Audio.PlaySound(soundId);
        _activeSounds++;

        // Decrement after sound duration
        Task.Delay(soundDuration).ContinueWith(_ => _activeSounds--);
    }
}
```

### 3. Use Appropriate Quality Settings

```csharp
// ✅ GOOD: Adjust quality based on hardware
public AudioOptions GetOptimalSettings()
{
    var systemMemory = GetAvailableMemory();
    var cpuCores = Environment.ProcessorCount;

    if (systemMemory < 4096 || cpuCores < 4)
    {
        return new AudioOptions { Quality = AudioQuality.Low };
    }
    else if (systemMemory < 8192)
    {
        return new AudioOptions { Quality = AudioQuality.Medium };
    }
    else
    {
        return new AudioOptions { Quality = AudioQuality.High };
    }
}
```

### 4. Optimize Track Count

```csharp
// ❌ BAD: Too many tracks
public void CreateComplexMusic()
{
    var music = api.Audio.CreateProceduralMusic(settings);

    for (int i = 0; i < 20; i++)
    {
        music.AddTrack($"track{i}", trackSettings);
    }
}

// ✅ GOOD: 4-6 tracks maximum
public void CreateEfficientMusic()
{
    var music = api.Audio.CreateProceduralMusic(settings);

    music.AddTrack("ambiance", ambientSettings);  // 1
    music.AddTrack("melody", melodySettings);     // 2
    music.AddTrack("harmony", harmonySettings);   // 3
    music.AddTrack("bass", bassSettings);         // 4
    music.AddTrack("drums", drumSettings);        // 5
}
```

### 5. Use Object Pooling for Frequent Sounds

```csharp
// ✅ GOOD: Pool sound instances
public class SoundPool
{
    private Dictionary<string, Queue<SoundInstance>> _pools = new();
    private const int PoolSize = 5;

    public void PreloadSound(string soundId)
    {
        if (!_pools.ContainsKey(soundId))
        {
            _pools[soundId] = new Queue<SoundInstance>();
            for (int i = 0; i < PoolSize; i++)
            {
                _pools[soundId].Enqueue(LoadSound(soundId));
            }
        }
    }

    public void PlayPooledSound(string soundId)
    {
        if (_pools.TryGetValue(soundId, out var pool) && pool.Count > 0)
        {
            var instance = pool.Dequeue();
            instance.Play();
            pool.Enqueue(instance);  // Return to pool
        }
        else
        {
            api.Audio.PlaySound(soundId);  // Fallback
        }
    }
}
```

## Quality Guidelines

### 1. Use Smooth Transitions

```csharp
// ❌ BAD: Abrupt changes
public void ChangeMusic(string newTrack)
{
    api.Audio.StopMusic();
    api.Audio.PlayMusic(newTrack);
}

// ✅ GOOD: Smooth fading
public async Task ChangeMusicSmooth(string newTrack)
{
    api.Audio.StopMusic(fadeOutTime: 1.5f);
    await Task.Delay(1500);  // Wait for fade out
    api.Audio.PlayMusic(newTrack, fadeInTime: 1.5f);
}
```

### 2. Respect Volume Hierarchy

```csharp
// Volume hierarchy: Master > Category > Sound

// ✅ GOOD: Proper volume management
public void ConfigureVolumes()
{
    // Master controls overall volume
    api.Audio.MasterVolume = 0.8f;

    // Category volumes are relative to master
    api.Audio.MusicVolume = 0.7f;     // 56% final (0.8 * 0.7)
    api.Audio.SoundVolume = 1.0f;     // 80% final (0.8 * 1.0)

    // Individual sounds can scale further
    api.Audio.PlaySound("quiet", volume: 0.5f);  // 40% final
}
```

### 3. Use Ducking for Important Audio

```csharp
// ✅ GOOD: Duck music during dialog
public class DialogAudioManager
{
    private float _originalMusicVolume;

    public void StartDialog()
    {
        _originalMusicVolume = api.Audio.MusicVolume;
        api.Audio.MusicVolume *= 0.3f;  // Reduce to 30%

        api.Audio.PlaySound("dialog_beep");
    }

    public void EndDialog()
    {
        api.Audio.MusicVolume = _originalMusicVolume;
    }
}
```

### 4. Match Music to Context

```csharp
// ✅ GOOD: Context-aware music generation
public MusicSettings GetContextualSettings(GameContext context)
{
    return context.Type switch
    {
        ContextType.Battle => new MusicSettings
        {
            Tempo = 140 + (context.Intensity * 20),  // Dynamic tempo
            Mood = MusicMood.Tense,
            Complexity = 60 + context.Intensity
        },
        ContextType.Exploration => new MusicSettings
        {
            Tempo = 100,
            Mood = MusicMood.Calm,
            Complexity = 40
        },
        ContextType.Boss => new MusicSettings
        {
            Tempo = 160,
            Mood = MusicMood.Epic,
            Complexity = 80
        },
        _ => new MusicSettings()
    };
}
```

## Resource Management

### 1. Dispose Unused Music

```csharp
// ✅ GOOD: Proper disposal
public class MusicManager : IDisposable
{
    private IProceduralMusic _currentMusic;

    public void SwitchMusic(string newMusicKey)
    {
        // Dispose old music
        _currentMusic?.Dispose();

        // Create and play new music
        _currentMusic = CreateMusic(newMusicKey);
        api.Audio.PlayProceduralMusic(_currentMusic);
    }

    public void Dispose()
    {
        _currentMusic?.Dispose();
    }
}
```

### 2. Stop Audio When Leaving Scenes

```csharp
// ✅ GOOD: Clean up on scene exit
public class SceneAudioManager
{
    public void OnSceneEnter(string sceneName)
    {
        PlaySceneMusic(sceneName);
    }

    public void OnSceneExit(string sceneName)
    {
        // Stop all audio
        api.Audio.StopMusic(fadeOutTime: 1.0f);
        api.Audio.StopAllSounds();

        // Clear cache if needed
        ClearMusicCache();
    }
}
```

### 3. Manage Cache Size

```csharp
// ✅ GOOD: Monitor and manage cache
public class AudioCacheManager
{
    private const int MaxCacheSizeMB = 64;

    public void MonitorCache()
    {
        var currentSize = GetCacheSize();

        if (currentSize > MaxCacheSizeMB)
        {
            // Remove least recently used
            PurgeLRUEntries(currentSize - MaxCacheSizeMB);
        }
    }

    private void PurgeLRUEntries(int targetMB)
    {
        // Remove oldest cached sounds
        var entriesToRemove = CalculateLRU(targetMB);
        foreach (var entry in entriesToRemove)
        {
            RemoveFromCache(entry);
        }
    }
}
```

## Music Design

### 1. Layer Music Based on Intensity

```csharp
// ✅ GOOD: Progressive layering
public void GenerateLayeredMusic(float intensity)
{
    var music = api.Audio.CreateProceduralMusic(settings);

    // Layer 1: Always present
    music.AddTrack("ambiance", new TrackSettings
    {
        Instrument = MidiProgram.Pad2Warm,
        Volume = 0.6f,
        Pattern = PatternType.Ambient
    });

    // Layer 2: Add at 30% intensity
    if (intensity >= 0.3f)
    {
        music.AddTrack("melody", new TrackSettings
        {
            Instrument = MidiProgram.Violin,
            Volume = 0.7f * intensity,  // Scale with intensity
            Pattern = PatternType.Melodic
        });
    }

    // Layer 3: Add at 60% intensity
    if (intensity >= 0.6f)
    {
        music.AddTrack("drums", new TrackSettings
        {
            Instrument = MidiProgram.DrumSet,
            Volume = 0.8f,
            Pattern = PatternType.Percussive
        });
    }
}
```

### 2. Use Appropriate Scales

```csharp
// ✅ GOOD: Match scale to mood
public ScaleType GetScaleForMood(MusicMood mood)
{
    return mood switch
    {
        MusicMood.Happy => ScaleType.Major,
        MusicMood.Sad => ScaleType.Minor,
        MusicMood.Mysterious => ScaleType.Phrygian,
        MusicMood.Epic => ScaleType.Lydian,
        MusicMood.Calm => ScaleType.Pentatonic,
        MusicMood.Tense => ScaleType.Locrian,
        _ => ScaleType.Major
    };
}
```

### 3. Balance Track Volumes

```csharp
// ✅ GOOD: Balanced mix
public void CreateBalancedMusic()
{
    var music = api.Audio.CreateProceduralMusic(settings);

    // Background: Lowest volume
    music.AddTrack("ambiance", new TrackSettings
    {
        Volume = 0.4f,  // 40%
        Instrument = MidiProgram.Pad2Warm
    });

    // Melody: Featured, but not overwhelming
    music.AddTrack("melody", new TrackSettings
    {
        Volume = 0.7f,  // 70%
        Instrument = MidiProgram.Violin
    });

    // Bass: Strong foundation
    music.AddTrack("bass", new TrackSettings
    {
        Volume = 0.8f,  // 80%
        Instrument = MidiProgram.AcousticBass
    });

    // Drums: Prominent but controlled
    music.AddTrack("drums", new TrackSettings
    {
        Volume = 0.6f,  // 60%
        Instrument = MidiProgram.DrumSet
    });
}
```

## Sound Effect Design

### 1. Use Positional Audio Appropriately

```csharp
// ✅ GOOD: Positional for 3D space
public void PlayPositionalSound(Entity entity, string soundId)
{
    if (IsIn3DSpace())
    {
        var pos = entity.Get<Position>();
        api.Audio.PlaySound(soundId, new Vector2(pos.X, pos.Y));
    }
    else
    {
        // Non-positional for UI/menu sounds
        api.Audio.PlaySound(soundId);
    }
}
```

### 2. Vary Sound Effects

```csharp
// ✅ GOOD: Prevent repetition fatigue
public void PlayVariedSound(string baseSoundId)
{
    var variants = new[] { "a", "b", "c" };
    var variant = variants[Random.Shared.Next(variants.Length)];
    var soundId = $"{baseSoundId}_{variant}";

    // Also vary pitch slightly
    api.Audio.PlaySound(soundId, volume: Random.Shared.NextSingle() * 0.2f + 0.9f);
}
```

### 3. Prioritize Important Sounds

```csharp
// ✅ GOOD: Sound priority system
public enum SoundPriority
{
    Low,      // Ambient, background
    Medium,   // Common effects
    High,     // Important feedback
    Critical  // UI, dialog
}

public void PlayPrioritizedSound(string soundId, SoundPriority priority)
{
    if (CanPlaySound(priority))
    {
        api.Audio.PlaySound(soundId);
        RegisterSound(soundId, priority);
    }
}

private bool CanPlaySound(SoundPriority priority)
{
    var activeSounds = GetActiveeSoundCount();
    var maxSounds = GetMaxConcurrentSounds();

    // Always allow critical sounds
    if (priority == SoundPriority.Critical)
        return true;

    // Check if we have capacity
    if (activeSounds < maxSounds)
        return true;

    // Stop lower priority sound if needed
    return StopLowestPrioritySound(priority);
}
```

## Integration Patterns

### 1. Event-Driven Audio

```csharp
// ✅ GOOD: React to game events
public class EventAudioSystem
{
    public void Initialize(IModApi api)
    {
        api.Events.Subscribe<BattleStartEvent>(OnBattleStart);
        api.Events.Subscribe<BattleEndEvent>(OnBattleEnd);
        api.Events.Subscribe<ItemPickupEvent>(OnItemPickup);
        api.Events.Subscribe<DoorOpenEvent>(OnDoorOpen);
    }

    private void OnBattleStart(BattleStartEvent evt)
    {
        var intensity = CalculateIntensity(evt.Enemies);
        PlayBattleMusic(intensity);
    }

    private void OnBattleEnd(BattleEndEvent evt)
    {
        api.Audio.StopMusic(fadeOutTime: 2.0f);
        if (evt.Victory)
        {
            api.Audio.PlaySound("victory_fanfare");
        }
    }
}
```

### 2. State Machine Audio

```csharp
// ✅ GOOD: Audio state machine
public class AudioStateMachine
{
    private AudioState _currentState;

    public void UpdateState(GameState gameState)
    {
        var newAudioState = MapGameStateToAudio(gameState);

        if (newAudioState != _currentState)
        {
            TransitionState(_currentState, newAudioState);
            _currentState = newAudioState;
        }
    }

    private void TransitionState(AudioState from, AudioState to)
    {
        // Fade out current
        api.Audio.StopMusic(fadeOutTime: 1.0f);

        // Prepare new state
        Task.Delay(1000).ContinueWith(_ =>
        {
            PlayStateMusic(to);
        });
    }
}
```

## Common Pitfalls

### ❌ Pitfall 1: Not Disposing Resources

```csharp
// ❌ BAD: Memory leak
public void SwitchMusic()
{
    var newMusic = api.Audio.CreateProceduralMusic(settings);
    api.Audio.PlayProceduralMusic(newMusic);
    // Old music never disposed!
}

// ✅ GOOD: Proper disposal
private IProceduralMusic _currentMusic;

public void SwitchMusic()
{
    _currentMusic?.Dispose();
    _currentMusic = api.Audio.CreateProceduralMusic(settings);
    api.Audio.PlayProceduralMusic(_currentMusic);
}
```

### ❌ Pitfall 2: Ignoring User Preferences

```csharp
// ❌ BAD: Hardcoded volumes
public void PlaySound()
{
    api.Audio.PlaySound("explosion", volume: 1.0f);
}

// ✅ GOOD: Respect user settings
public void PlaySound()
{
    var userVolume = GetUserSoundVolume();
    api.Audio.SoundVolume = userVolume;
    api.Audio.PlaySound("explosion");
}
```

### ❌ Pitfall 3: Blocking Audio Operations

```csharp
// ❌ BAD: Synchronous generation
public void GenerateMusic()
{
    var music = ExpensiveGeneration();  // Blocks UI
    api.Audio.PlayProceduralMusic(music);
}

// ✅ GOOD: Async generation
public async Task GenerateMusicAsync()
{
    var music = await Task.Run(() => ExpensiveGeneration());
    api.Audio.PlayProceduralMusic(music);
}
```

### ❌ Pitfall 4: No Error Handling

```csharp
// ❌ BAD: No error handling
public void PlayMusic(string trackId)
{
    api.Audio.PlayMusic(trackId);  // What if file missing?
}

// ✅ GOOD: Graceful degradation
public void PlayMusic(string trackId)
{
    try
    {
        api.Audio.PlayMusic(trackId);
    }
    catch (FileNotFoundException)
    {
        api.Logger.LogWarning($"Music not found: {trackId}");
        // Fallback to default music
        api.Audio.PlayMusic("default_music");
    }
}
```

### ❌ Pitfall 5: Over-Engineering

```csharp
// ❌ BAD: Unnecessary complexity
public void PlaySimpleSound()
{
    var factory = new SoundFactory();
    var builder = factory.CreateBuilder();
    var sound = builder.WithId("click").Build();
    var player = new SoundPlayer(sound);
    player.Play();
}

// ✅ GOOD: Keep it simple
public void PlaySimpleSound()
{
    api.Audio.PlaySound("click");
}
```

## Performance Checklist

- [ ] Music is cached and reused
- [ ] Concurrent sound limit is enforced
- [ ] Unused audio resources are disposed
- [ ] Quality settings match target hardware
- [ ] Track count is limited to 4-6
- [ ] Sound pooling for frequently used effects
- [ ] Async operations for expensive tasks
- [ ] Memory usage is monitored

## Quality Checklist

- [ ] Smooth fade transitions between music
- [ ] Volume ducking for important audio
- [ ] Music matches game context
- [ ] Sound effects use appropriate priority
- [ ] Positional audio for spatial sounds
- [ ] Volume respects user preferences
- [ ] Error handling with graceful fallbacks

## Next Steps

- **[Getting Started](getting-started.md)** - Quick start guide
- **[Procedural Music Guide](procedural-music.md)** - Deep dive into music generation
- **[Configuration Reference](configuration.md)** - Complete configuration options
- **[API Reference](../api/audio.md)** - Complete API documentation

---

*Last Updated: 2025-10-22*
