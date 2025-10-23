# Audio Configuration Reference

## Overview

The PokeNET audio system can be configured through `appsettings.json` or programmatically. This guide covers all configuration options and their effects.

## Configuration File

### Location

```
YourGame/
├── appsettings.json          # Main configuration
├── appsettings.Development.json  # Development overrides
└── appsettings.Production.json   # Production overrides
```

### Complete Configuration Example

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

## Configuration Options

### EnableMusic

**Type**: `boolean`
**Default**: `true`
**Description**: Enable or disable background music playback globally.

```json
{
  "Audio": {
    "EnableMusic": true
  }
}
```

**Use Cases:**
- Disable music during development/testing
- Allow users to disable music while keeping sound effects
- Reduce resource usage on low-end systems

### EnableSoundEffects

**Type**: `boolean`
**Default**: `true`
**Description**: Enable or disable sound effects playback globally.

```json
{
  "Audio": {
    "EnableSoundEffects": true
  }
}
```

**Use Cases:**
- Silent mode for accessibility
- Development testing without sound effects
- Reduce CPU usage

### Quality

**Type**: `enum` (`Low`, `Medium`, `High`)
**Default**: `Medium`
**Description**: Audio quality preset that automatically configures multiple settings.

```json
{
  "Audio": {
    "Quality": "Medium"
  }
}
```

**Quality Presets:**

| Setting | Low | Medium | High |
|---------|-----|--------|------|
| Sample Rate | 22,050 Hz | 44,100 Hz | 48,000 Hz |
| Buffer Size | 1024 | 2048 | 4096 |
| Max Concurrent Sounds | 8 | 16 | 32 |
| Compression | Enabled | Enabled | Disabled |
| Cache Size | 32 MB | 64 MB | 128 MB |

**Recommendations:**
- **Low**: Mobile devices, low-end systems, battery saving
- **Medium**: Desktop gaming, balanced performance
- **High**: High-end systems, audiophiles, content creation

### MaxConcurrentSounds

**Type**: `integer`
**Range**: `1-64`
**Default**: `16`
**Description**: Maximum number of sounds that can play simultaneously.

```json
{
  "Audio": {
    "MaxConcurrentSounds": 16
  }
}
```

**Effects:**
- Higher values = more sounds can play at once, more CPU/memory usage
- Lower values = some sounds may be dropped, better performance
- When limit reached, oldest or lowest priority sounds are stopped

**Recommendations:**
- **8**: Low-end systems, simple games
- **16**: Standard games, balanced
- **32**: Complex games, high-end systems

### BufferSize

**Type**: `integer`
**Range**: `128-8192` samples
**Default**: `2048`
**Description**: Audio buffer size per channel. Affects latency and stability.

```json
{
  "Audio": {
    "BufferSize": 2048
  }
}
```

**Effects:**
- **Smaller buffer** (128-1024): Lower latency, higher CPU usage, potential stuttering
- **Larger buffer** (4096-8192): Higher latency, lower CPU usage, more stable

**Latency calculation:**
```
Latency (ms) = (BufferSize / SampleRate) * 1000

Examples:
- 1024 @ 44100Hz = 23ms latency
- 2048 @ 44100Hz = 46ms latency
- 4096 @ 44100Hz = 93ms latency
```

**Recommendations:**
- **1024**: Rhythm games, real-time music apps (requires good CPU)
- **2048**: Standard games (balanced)
- **4096**: Lower-end systems, prioritize stability

### SampleRate

**Type**: `integer`
**Range**: `8000-96000` Hz
**Default**: `44100`
**Description**: Audio sample rate in Hertz.

```json
{
  "Audio": {
    "SampleRate": 44100
  }
}
```

**Common rates:**
- **22050 Hz**: Telephone quality, very low resource usage
- **44100 Hz**: CD quality, standard for games
- **48000 Hz**: Professional audio, slightly better quality
- **96000 Hz**: High-end audio (rarely needed for games)

**Effects:**
- Higher sample rate = better quality, more CPU/memory usage
- Most humans can't distinguish above 44100 Hz

**Recommendations:**
- **22050**: Low-end devices, retro aesthetic
- **44100**: Standard choice for most games
- **48000**: High-quality audio, professional production

### DefaultMasterVolume

**Type**: `float`
**Range**: `0.0-1.0`
**Default**: `0.8`
**Description**: Initial master volume (affects all audio).

```json
{
  "Audio": {
    "DefaultMasterVolume": 0.8
  }
}
```

**Volume calculation:**
```
Final Volume = MasterVolume × CategoryVolume × SoundVolume

Example:
MasterVolume = 0.8
MusicVolume = 0.7
Sound = 1.0
Final = 0.8 × 0.7 × 1.0 = 0.56 (56%)
```

### DefaultMusicVolume

**Type**: `float`
**Range**: `0.0-1.0`
**Default**: `0.7`
**Description**: Initial music volume.

```json
{
  "Audio": {
    "DefaultMusicVolume": 0.7
  }
}
```

**Recommendations:**
- **0.5-0.6**: Music as background ambiance
- **0.7-0.8**: Standard music prominence
- **0.9-1.0**: Music-focused games

### DefaultSfxVolume

**Type**: `float`
**Range**: `0.0-1.0`
**Default**: `1.0`
**Description**: Initial sound effects volume.

```json
{
  "Audio": {
    "DefaultSfxVolume": 1.0
  }
}
```

**Recommendations:**
- **0.8-0.9**: Subtle sound effects
- **1.0**: Full impact sound effects
- Keep higher than music for important game feedback

### EnableCompression

**Type**: `boolean`
**Default**: `true`
**Description**: Enable audio data compression to reduce memory usage.

```json
{
  "Audio": {
    "EnableCompression": true
  }
}
```

**Effects:**
- **Enabled**: 50-70% less memory, slight CPU overhead for decompression
- **Disabled**: More memory usage, no decompression overhead

**Recommendations:**
- **Enable**: Most cases, especially mobile/limited RAM
- **Disable**: High-end systems, when CPU is constrained

### EnableCaching

**Type**: `boolean`
**Default**: `true`
**Description**: Cache frequently played sounds in memory.

```json
{
  "Audio": {
    "EnableCaching": true
  }
}
```

**Effects:**
- **Enabled**: Faster sound playback, uses more memory
- **Disabled**: Slower sound loading, less memory usage

**Caching strategy:**
1. First play: Load from disk, add to cache
2. Subsequent plays: Use cached version
3. Cache full: Remove least recently used sounds

### MaxCacheSizeMB

**Type**: `integer`
**Range**: `1-512` MB
**Default**: `64`
**Description**: Maximum cache size in megabytes.

```json
{
  "Audio": {
    "MaxCacheSizeMB": 64
  }
}
```

**Calculation:**
```
Approximate sound capacity:
- 1 second @ 44100Hz stereo ≈ 350 KB
- 10 second sound ≈ 3.5 MB
- 64 MB cache ≈ 18-20 sounds (10s each)
```

**Recommendations:**
- **32 MB**: Mobile, limited RAM systems
- **64 MB**: Standard desktop games
- **128 MB**: Games with many/long sound effects

## Programmatic Configuration

### Loading Configuration

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using PokeNET.Audio.Configuration;

public class AudioService
{
    private readonly AudioOptions _options;

    public AudioService(IOptions<AudioOptions> options)
    {
        _options = options.Value;

        // Apply quality preset
        _options.ApplyQualityPreset();

        // Validate configuration
        if (!_options.Validate(out var errors))
        {
            foreach (var error in errors)
            {
                Console.WriteLine($"Config error: {error}");
            }
        }
    }
}
```

### Modifying Configuration at Runtime

```csharp
// Get current configuration
var currentOptions = _options.Clone();

// Modify settings
currentOptions.MasterVolume = 0.5f;
currentOptions.EnableMusic = false;

// Apply changes
ApplyOptions(currentOptions);
```

### Creating Custom Presets

```csharp
public static class AudioPresets
{
    public static AudioOptions PerformancePreset()
    {
        return new AudioOptions
        {
            Quality = AudioQuality.Low,
            MaxConcurrentSounds = 8,
            BufferSize = 1024,
            SampleRate = 22050,
            EnableCompression = true,
            MaxCacheSizeMB = 32
        };
    }

    public static AudioOptions QualityPreset()
    {
        return new AudioOptions
        {
            Quality = AudioQuality.High,
            MaxConcurrentSounds = 32,
            BufferSize = 4096,
            SampleRate = 48000,
            EnableCompression = false,
            MaxCacheSizeMB = 128
        };
    }

    public static AudioOptions BalancedPreset()
    {
        return new AudioOptions
        {
            Quality = AudioQuality.Medium,
            MaxConcurrentSounds = 16,
            BufferSize = 2048,
            SampleRate = 44100,
            EnableCompression = true,
            MaxCacheSizeMB = 64
        };
    }

    public static AudioOptions MobilePreset()
    {
        return new AudioOptions
        {
            Quality = AudioQuality.Low,
            MaxConcurrentSounds = 6,
            BufferSize = 1024,
            SampleRate = 22050,
            EnableCompression = true,
            MaxCacheSizeMB = 16,
            DefaultMasterVolume = 0.7f  // Lower for battery
        };
    }
}
```

## Environment-Specific Configuration

### Development Configuration

```json
// appsettings.Development.json
{
  "Audio": {
    "EnableMusic": false,        // Disable for testing
    "EnableSoundEffects": true,
    "Quality": "Low",             // Faster iteration
    "MaxConcurrentSounds": 8
  }
}
```

### Production Configuration

```json
// appsettings.Production.json
{
  "Audio": {
    "EnableMusic": true,
    "EnableSoundEffects": true,
    "Quality": "Medium",
    "EnableCaching": true,
    "EnableCompression": true
  }
}
```

## Performance Tuning

### Low-End Systems

```json
{
  "Audio": {
    "Quality": "Low",
    "MaxConcurrentSounds": 6,
    "BufferSize": 1024,
    "SampleRate": 22050,
    "EnableCompression": true,
    "MaxCacheSizeMB": 32
  }
}
```

### High-End Systems

```json
{
  "Audio": {
    "Quality": "High",
    "MaxConcurrentSounds": 32,
    "BufferSize": 4096,
    "SampleRate": 48000,
    "EnableCompression": false,
    "MaxCacheSizeMB": 128
  }
}
```

### Balanced Configuration

```json
{
  "Audio": {
    "Quality": "Medium",
    "MaxConcurrentSounds": 16,
    "BufferSize": 2048,
    "SampleRate": 44100,
    "EnableCompression": true,
    "EnableCaching": true,
    "MaxCacheSizeMB": 64
  }
}
```

## Troubleshooting Configuration Issues

### Audio Stuttering

**Problem**: Audio cuts out or stutters

**Solutions:**
```json
{
  "Audio": {
    "BufferSize": 4096,  // Increase buffer
    "MaxConcurrentSounds": 8  // Reduce concurrent sounds
  }
}
```

### High Latency

**Problem**: Delay between action and sound

**Solutions:**
```json
{
  "Audio": {
    "BufferSize": 1024,  // Decrease buffer
    "SampleRate": 44100   // Don't use higher rates
  }
}
```

### High Memory Usage

**Problem**: Audio using too much RAM

**Solutions:**
```json
{
  "Audio": {
    "EnableCompression": true,
    "MaxCacheSizeMB": 32,
    "MaxConcurrentSounds": 8
  }
}
```

### Poor Sound Quality

**Problem**: Audio sounds muffled or distorted

**Solutions:**
```json
{
  "Audio": {
    "Quality": "High",
    "SampleRate": 48000,
    "EnableCompression": false
  }
}
```

## Validation

### Configuration Validation

```csharp
var options = new AudioOptions();

if (options.Validate(out var errors))
{
    Console.WriteLine("Configuration is valid!");
}
else
{
    Console.WriteLine("Configuration errors:");
    foreach (var error in errors)
    {
        Console.WriteLine($"  - {error}");
    }
}
```

### Validation Rules

- `MaxConcurrentSounds`: 1-64
- `BufferSize`: 128-8192
- `SampleRate`: 8000-96000
- `DefaultMasterVolume`: 0.0-1.0
- `DefaultMusicVolume`: 0.0-1.0
- `DefaultSfxVolume`: 0.0-1.0
- `MaxCacheSizeMB`: 1-512

## Best Practices

### 1. Start with Quality Presets

```json
{
  "Audio": {
    "Quality": "Medium"  // Let it configure other settings
  }
}
```

### 2. Profile Before Optimizing

Test with default settings first, only optimize if needed.

### 3. Use Environment-Specific Configs

```
appsettings.json               # Base config
appsettings.Development.json   # Dev overrides
appsettings.Production.json    # Production overrides
```

### 4. Document Custom Configurations

```json
{
  "Audio": {
    // Using higher buffer for stability on older hardware
    "BufferSize": 4096,
    // Lower sample rate to match source audio quality
    "SampleRate": 22050
  }
}
```

### 5. Validate Configuration on Startup

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddOptions<AudioOptions>()
        .Configure<IConfiguration>((settings, configuration) =>
        {
            configuration.GetSection(AudioOptions.SectionName)
                .Bind(settings);

            // Validate
            if (!settings.Validate(out var errors))
            {
                throw new InvalidOperationException(
                    $"Invalid audio configuration: {string.Join(", ", errors)}");
            }
        });
}
```

## Next Steps

- **[Getting Started](getting-started.md)** - Quick start guide
- **[Best Practices](best-practices.md)** - Performance and quality tips
- **[API Reference](../api/audio.md)** - Complete API documentation

---

*Last Updated: 2025-10-22*
