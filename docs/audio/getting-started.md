# Audio System - Getting Started Guide

## Introduction

Welcome to the PokeNET Audio System! This guide will help you get started with audio playback and procedural music generation in your mods.

The PokeNET audio system provides:
- **Traditional Audio Playback**: Music and sound effects
- **Procedural Music Generation**: Dynamic, context-aware soundscapes using DryWetMidi
- **Channel Mixing**: Professional audio mixing with ducking and fading
- **Performance Optimized**: Efficient resource management and caching

## Quick Start

### 1. Playing Your First Sound

```csharp
using PokeNET.ModApi;

public class MyAudioMod : Mod
{
    public override void OnInitialize(IModApi api)
    {
        // Play a simple sound effect
        api.Audio.PlaySound("move_hit");

        // Play with custom volume
        api.Audio.PlaySound("explosion", volume: 0.8f);

        // Play positional (3D) sound
        api.Audio.PlaySound(
            "creature_cry",
            new Vector2(100, 200),
            volume: 1.0f);
    }
}
```

### 2. Playing Music

```csharp
public override void OnMapEnter(IModApi api, string mapId)
{
    // Simple music playback
    api.Audio.PlayMusic("route_theme", loop: true);

    // With smooth fade-in (2 seconds)
    api.Audio.PlayMusic("battle_theme", loop: true, fadeInTime: 2.0f);
}

public override void OnMapExit(IModApi api, string mapId)
{
    // Stop music with fade-out
    api.Audio.StopMusic(fadeOutTime: 1.5f);
}
```

### 3. Volume Control

```csharp
// Set master volume (affects all audio)
api.Audio.MasterVolume = 0.7f; // 70%

// Set music volume
api.Audio.MusicVolume = 0.8f; // 80%

// Set sound effects volume
api.Audio.SoundVolume = 0.9f; // 90%

// Check if music is playing
if (api.Audio.IsMusicPlaying)
{
    api.Logger.LogInformation("Music is currently playing");
}
```

## Your First Procedural Music

### Simple Battle Music

```csharp
using PokeNET.ModApi.Audio;
using Melanchall.DryWetMidi.MusicTheory;

public void CreateSimpleBattleMusic(IAudioApi audio)
{
    // 1. Create music settings
    var settings = new MusicSettings
    {
        Tempo = 140,                        // Fast tempo for battle
        TimeSignature = new TimeSignature(4, 4),
        Key = NoteName.E,                   // E minor
        Scale = ScaleType.Minor,
        Mood = MusicMood.Tense,
        Measures = 16,                      // 16 measures
        Complexity = 60                     // Medium complexity
    };

    // 2. Generate procedural music
    var music = audio.CreateProceduralMusic(settings);

    // 3. Add a melody track
    music.AddTrack("melody", new TrackSettings
    {
        Instrument = MidiProgram.Violin,
        Volume = 0.8f,
        Pattern = PatternType.Melodic,
        Notes = new[] { NoteName.E, NoteName.G, NoteName.A, NoteName.B },
        Density = 8,                        // 8 notes per measure
        OctaveRange = (4, 6)
    });

    // 4. Add a bass track
    music.AddTrack("bass", new TrackSettings
    {
        Instrument = MidiProgram.AcousticBass,
        Volume = 0.9f,
        Pattern = PatternType.Rhythmic,
        Notes = new[] { NoteName.E, NoteName.B },
        Density = 4,
        OctaveRange = (2, 3)
    });

    // 5. Add drums
    music.AddTrack("drums", new TrackSettings
    {
        Instrument = MidiProgram.DrumSet,
        Volume = 0.7f,
        Pattern = PatternType.Percussive,
        Density = 16
    });

    // 6. Play the music
    audio.PlayProceduralMusic(music, loop: true);
}
```

## Audio Configuration

### appsettings.json Configuration

```json
{
  "Audio": {
    "EnableMusic": true,
    "EnableSoundEffects": true,
    "Quality": "Medium",
    "MaxConcurrentSounds": 16,
    "BufferSize": 2048,
    "SampleRate": 44100,
    "DefaultMasterVolume": 0.8,
    "DefaultMusicVolume": 0.7,
    "DefaultSfxVolume": 1.0,
    "EnableCompression": true,
    "EnableCaching": true,
    "MaxCacheSizeMB": 64
  }
}
```

### Quality Presets

**Low Quality** (Best for low-end systems):
- Sample Rate: 22,050 Hz
- Buffer Size: 1024 samples
- Max Concurrent Sounds: 8
- Cache Size: 32 MB

**Medium Quality** (Balanced):
- Sample Rate: 44,100 Hz
- Buffer Size: 2048 samples
- Max Concurrent Sounds: 16
- Cache Size: 64 MB

**High Quality** (Best quality):
- Sample Rate: 48,000 Hz
- Buffer Size: 4096 samples
- Max Concurrent Sounds: 32
- Cache Size: 128 MB

## Common Patterns

### Music State Machine

```csharp
public class MusicManager
{
    private readonly IAudioApi _audio;
    private GameState _currentState;

    public void UpdateGameState(GameState newState)
    {
        if (newState == _currentState)
            return;

        var previousState = _currentState;
        _currentState = newState;

        // Fade out current music
        _audio.StopMusic(fadeOutTime: 1.0f);

        // Wait a moment, then start new music
        Task.Delay(1000).ContinueWith(_ =>
        {
            switch (newState)
            {
                case GameState.Battle:
                    _audio.PlayMusic("battle_theme", loop: true, fadeInTime: 1.0f);
                    break;

                case GameState.Exploration:
                    _audio.PlayMusic("route_theme", loop: true, fadeInTime: 1.5f);
                    break;

                case GameState.Town:
                    _audio.PlayMusic("town_theme", loop: true, fadeInTime: 2.0f);
                    break;
            }
        });
    }
}
```

### Positional Audio Helper

```csharp
public static class AudioHelper
{
    public static void PlaySoundAtEntity(
        IAudioApi audio,
        Entity entity,
        string soundId,
        float volume = 1.0f)
    {
        if (entity.Has<Position>())
        {
            var pos = entity.Get<Position>();
            audio.PlaySound(soundId, new Vector2(pos.X, pos.Y), volume);
        }
        else
        {
            // Fallback to non-positional
            audio.PlaySound(soundId, volume);
        }
    }
}

// Usage
AudioHelper.PlaySoundAtEntity(api.Audio, pokemonEntity, "pokemon_cry");
```

### Volume Ducking for Dialog

```csharp
public class DialogSystem
{
    private readonly IAudioApi _audio;
    private float _previousMusicVolume;

    public void StartDialog()
    {
        // Reduce music volume during dialog
        _previousMusicVolume = _audio.MusicVolume;
        _audio.MusicVolume *= 0.3f; // 30% of current volume
    }

    public void EndDialog()
    {
        // Restore music volume
        _audio.MusicVolume = _previousMusicVolume;
    }
}
```

## Audio Events

Listen for audio state changes:

```csharp
public override void OnInitialize(IModApi api)
{
    // Subscribe to music events
    api.Events.Subscribe<MusicStartedEvent>(evt =>
    {
        api.Logger.LogInformation($"Music started: {evt.TrackId}");
        ShowNotification($"Now playing: {evt.TrackId}");
    });

    api.Events.Subscribe<MusicStoppedEvent>(evt =>
    {
        api.Logger.LogInformation($"Music stopped: {evt.TrackId}");
    });

    api.Events.Subscribe<SoundPlayedEvent>(evt =>
    {
        api.Logger.LogDebug($"Sound: {evt.SoundId} at {evt.Position}");
    });
}
```

## Troubleshooting

### Audio Not Playing

1. **Check if audio is enabled**:
   ```csharp
   var settings = GetAudioSettings();
   if (!settings.EnableMusic)
   {
       api.Logger.LogWarning("Music is disabled in settings");
   }
   ```

2. **Check volume levels**:
   ```csharp
   api.Logger.LogInformation($"Master: {api.Audio.MasterVolume}");
   api.Logger.LogInformation($"Music: {api.Audio.MusicVolume}");
   api.Logger.LogInformation($"SFX: {api.Audio.SoundVolume}");
   ```

3. **Check concurrent sound limit**:
   ```csharp
   // If too many sounds are playing, some may be dropped
   // Reduce MaxConcurrentSounds or manage sound playback
   ```

### Choppy/Stuttering Audio

1. **Increase buffer size**:
   ```json
   "Audio": {
     "BufferSize": 4096  // Larger buffer = less stuttering, more latency
   }
   ```

2. **Reduce quality**:
   ```json
   "Audio": {
     "Quality": "Low"
   }
   ```

3. **Enable caching**:
   ```json
   "Audio": {
     "EnableCaching": true,
     "MaxCacheSizeMB": 128
   }
   ```

### High Memory Usage

1. **Enable compression**:
   ```json
   "Audio": {
     "EnableCompression": true
   }
   ```

2. **Reduce cache size**:
   ```json
   "Audio": {
     "MaxCacheSizeMB": 32
   }
   ```

3. **Limit concurrent sounds**:
   ```json
   "Audio": {
     "MaxConcurrentSounds": 8
   }
   ```

## Next Steps

- **[Procedural Music Guide](procedural-music.md)** - Deep dive into music generation
- **[Configuration Reference](configuration.md)** - Complete configuration options
- **[Best Practices](best-practices.md)** - Performance and quality tips
- **[API Reference](../api/audio.md)** - Complete API documentation

## Additional Resources

- [DryWetMidi Documentation](https://melanchall.github.io/drywetmidi/)
- [MIDI Instrument List](https://en.wikipedia.org/wiki/General_MIDI#Program_change_events)
- [Music Theory Basics](https://www.musictheory.net/)

---

*Last Updated: 2025-10-22*
