# AudioAssetLoader Integration Guide

## Overview
AudioAssetLoader is a production-ready MonoGame audio asset loader that supports WAV and OGG formats with comprehensive error handling, async loading, and memory tracking.

## Location
- **Implementation**: `/PokeNET/PokeNET.Core/Assets/Loaders/AudioAssetLoader.cs`
- **Tests**: `/tests/Assets/Loaders/AudioAssetLoaderTests.cs`

## Features
1. **Format Support**: WAV and OGG audio files
2. **Async Loading**: Fully async with cancellation token support
3. **Error Handling**: Comprehensive validation and error reporting
4. **Memory Tracking**: Tracks buffer sizes for all loaded audio
5. **MonoGame Integration**: Uses MonoGame's SoundEffect for audio loading
6. **Sample Rate Validation**: Validates common sample rates (8kHz - 48kHz)
7. **Logging**: Detailed diagnostic logging for debugging

## DI Registration

### Option 1: Service Collection Extension (Recommended)
Add to your DI configuration class (e.g., `ServiceCollectionExtensions.cs`):

```csharp
using Microsoft.Extensions.DependencyInjection;
using PokeNET.Core.Assets.Loaders;
using PokeNET.Domain.Assets;
using AudioSoundEffect = PokeNET.Audio.Models.SoundEffect;

public static class AssetServiceExtensions
{
    public static IServiceCollection AddAssetLoaders(this IServiceCollection services)
    {
        // Register audio asset loader as singleton
        services.AddSingleton<IAssetLoader<AudioSoundEffect>, AudioAssetLoader>();

        // Add other asset loaders here...

        return services;
    }
}
```

Then call in your startup:
```csharp
services.AddAssetLoaders();
```

### Option 2: Direct Registration
In your `Program.cs` or startup configuration:

```csharp
using PokeNET.Core.Assets.Loaders;
using PokeNET.Domain.Assets;
using AudioSoundEffect = PokeNET.Audio.Models.SoundEffect;

// Register as singleton for best performance
builder.Services.AddSingleton<IAssetLoader<AudioSoundEffect>, AudioAssetLoader>();
```

### Option 3: Named Registration (Multiple Loaders)
If you have multiple audio loaders:

```csharp
services.AddSingleton<AudioAssetLoader>();
services.AddSingleton<IAssetLoader<AudioSoundEffect>>(sp =>
    sp.GetRequiredService<AudioAssetLoader>());
```

## Usage Example

### Basic Usage with DI
```csharp
public class AudioService
{
    private readonly IAssetLoader<AudioSoundEffect> _audioLoader;

    public AudioService(IAssetLoader<AudioSoundEffect> audioLoader)
    {
        _audioLoader = audioLoader;
    }

    public async Task<AudioSoundEffect> LoadSoundAsync(string path)
    {
        return await _audioLoader.LoadAsync(path);
    }
}
```

### Direct Usage
```csharp
using PokeNET.Core.Assets.Loaders;
using PokeNET.Domain.Assets;

var logger = serviceProvider.GetRequiredService<ILogger<AudioAssetLoader>>();
using var loader = new AudioAssetLoader(logger);

// Check format support
if (loader.CanHandle(".wav"))
{
    // Load audio file
    var soundEffect = await loader.LoadAsync("assets/sounds/jump.wav");

    Console.WriteLine($"Loaded: {soundEffect.Name}");
    Console.WriteLine($"Duration: {soundEffect.Duration.TotalSeconds:F2}s");
    Console.WriteLine($"Sample Rate: {soundEffect.SampleRate}Hz");
    Console.WriteLine($"Channels: {soundEffect.Channels}");

    // Check memory usage
    long memoryUsed = loader.GetMemoryUsage("assets/sounds/jump.wav");
    Console.WriteLine($"Memory: {memoryUsed} bytes");
}
```

### With Cancellation
```csharp
using var cts = new CancellationTokenSource();
cts.CancelAfter(TimeSpan.FromSeconds(5));

try
{
    var sound = await loader.LoadAsync("large-file.ogg", cts.Token);
}
catch (OperationCanceledException)
{
    Console.WriteLine("Loading cancelled");
}
```

## Integration with AudioManager

The AudioAssetLoader is designed to work with the existing AudioManager. Update AudioManager to use the loader:

```csharp
public class AudioManager : IAudioManager
{
    private readonly IAssetLoader<AudioSoundEffect> _audioLoader;

    public AudioManager(
        ILogger<AudioManager> logger,
        IAudioCache cache,
        IMusicPlayer musicPlayer,
        ISoundEffectPlayer sfxPlayer,
        IAssetLoader<AudioSoundEffect> audioLoader)
    {
        // ... existing initialization
        _audioLoader = audioLoader;
    }

    public async Task PlaySoundEffectAsync(string sfxName, float volume = 1.0f,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Use asset loader instead of TODO placeholder
            var soundEffect = await _cache.GetOrLoadAsync<SoundEffect>(sfxName, async () =>
            {
                return await _audioLoader.LoadAsync(sfxName, cancellationToken);
            });

            await _sfxPlayer.PlayAsync(soundEffect, volume, priority: 0, cancellationToken);
        }
        catch (AssetLoadException ex)
        {
            _logger.LogError(ex, "Failed to load sound effect: {SfxName}", sfxName);
            throw;
        }
    }
}
```

## Error Handling

The loader throws `AssetLoadException` for all errors:

```csharp
try
{
    var sound = await loader.LoadAsync("audio.wav");
}
catch (AssetLoadException ex)
{
    Console.WriteLine($"Asset: {ex.AssetPath}");
    Console.WriteLine($"Error: {ex.Message}");

    // Check inner exception for details
    if (ex.InnerException != null)
    {
        Console.WriteLine($"Details: {ex.InnerException.Message}");
    }
}
catch (OperationCanceledException)
{
    Console.WriteLine("Loading was cancelled");
}
```

## Memory Tracking

```csharp
// Load multiple files
await loader.LoadAsync("sound1.wav");
await loader.LoadAsync("sound2.ogg");
await loader.LoadAsync("music.wav");

// Check total memory usage
long totalMemory = loader.GetTotalMemoryUsage();
Console.WriteLine($"Total audio memory: {totalMemory / 1024 / 1024:F2} MB");

// Check specific file
long sound1Memory = loader.GetMemoryUsage("sound1.wav");

// Clear tracking for unloaded files
loader.ClearMemoryTracking("sound1.wav");

// Clear all tracking
loader.ClearAllMemoryTracking();
```

## Mod Support

The loader works seamlessly with the modding system:

```csharp
// Mod can provide custom audio files
string modAudioPath = Path.Combine(modDirectory, "audio", "custom_sound.wav");

if (File.Exists(modAudioPath))
{
    var modSound = await loader.LoadAsync(modAudioPath);
    // Use mod audio
}
else
{
    var defaultSound = await loader.LoadAsync("assets/sounds/default.wav");
    // Use default audio
}
```

## Testing

Run the comprehensive test suite:

```bash
dotnet test --filter "FullyQualifiedName~AudioAssetLoaderTests"
```

### Test Coverage
- ✅ Constructor validation
- ✅ Format detection (WAV, OGG, unsupported)
- ✅ File validation (null, empty, missing)
- ✅ Corrupted file handling
- ✅ Empty file detection
- ✅ Valid audio loading
- ✅ Async/sync loading
- ✅ Cancellation support
- ✅ Memory tracking
- ✅ Disposal pattern

## Performance Considerations

1. **Singleton Registration**: Register as singleton to reuse the instance
2. **Memory Tracking**: Minimal overhead, uses Dictionary<string, long>
3. **Async Loading**: Non-blocking I/O for better performance
4. **Cancellation**: Supports cancellation at file I/O and processing stages
5. **Logging**: Debug-level logs for hot paths, Info for completion

## Supported Sample Rates
- 8000 Hz
- 11025 Hz
- 16000 Hz
- 22050 Hz (CD half-rate)
- 24000 Hz
- 32000 Hz
- 44100 Hz (CD quality, default)
- 48000 Hz (DVD/Studio quality)

## File Format Requirements

### WAV Files
- Standard RIFF/WAVE format
- PCM encoding
- 1-2 channels (mono/stereo)
- 8-bit or 16-bit samples

### OGG Files
- Ogg Vorbis format
- Lossy compression
- 1-2 channels (mono/stereo)
- Variable bitrate supported

## Troubleshooting

### "Audio file not found"
- Verify the file path is correct and absolute
- Check file permissions
- Ensure the file exists before loading

### "Unsupported audio format"
- Only WAV and OGG are supported
- Check file extension matches actual format
- Use CanHandle() to verify format support

### "Failed to load audio with MonoGame"
- File may be corrupted
- Format may not be standard WAV/OGG
- Try re-encoding the file with standard tools

### "Audio file is empty"
- File has zero bytes
- File may not have downloaded completely
- Regenerate or re-download the file

## Next Steps

1. ✅ Implementation complete
2. ✅ Tests complete
3. 📋 Register in DI container (use guide above)
4. 🔄 Update AudioManager to use loader
5. 🧪 Integration testing with AudioManager
6. 📦 Add to asset pipeline documentation

## Related Files
- `/PokeNET/PokeNET.Core/Assets/AssetManager.cs` - Main asset management
- `/PokeNET/PokeNET.Audio/Services/AudioManager.cs` - Audio system manager
- `/PokeNET/PokeNET.Domain/Assets/IAssetLoader.cs` - Loader interface
- `/PokeNET/PokeNET.Domain/Assets/AssetLoadException.cs` - Exception type
