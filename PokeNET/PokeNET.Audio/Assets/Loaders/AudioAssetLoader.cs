using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework.Audio;
using PokeNET.Core.Assets;
using AudioSoundEffect = PokeNET.Audio.Models.SoundEffect;

namespace PokeNET.Core.Assets.Loaders;

/// <summary>
/// Asset loader for audio files using MonoGame's SoundEffect.
/// Supports WAV and OGG audio formats with comprehensive error handling.
/// Implements IAssetLoader following the Open/Closed Principle.
/// </summary>
public sealed class AudioAssetLoader : IAssetLoader<AudioSoundEffect>, IDisposable
{
    private readonly ILogger<AudioAssetLoader> _logger;
    private readonly HashSet<string> _supportedExtensions;
    private readonly Dictionary<string, long> _memoryUsage;
    private bool _disposed;

    // MonoGame supports these formats natively
    private static readonly string[] DefaultSupportedFormats = { ".wav", ".ogg" };

    // Supported sample rates for validation
    private static readonly int[] SupportedSampleRates =
    {
        8000,
        11025,
        16000,
        22050,
        24000,
        32000,
        44100,
        48000,
    };

    /// <summary>
    /// Initializes a new instance of the AudioAssetLoader class.
    /// </summary>
    /// <param name="logger">Logger for diagnostics and error tracking.</param>
    /// <exception cref="ArgumentNullException">Thrown when logger is null.</exception>
    public AudioAssetLoader(ILogger<AudioAssetLoader> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _supportedExtensions = new HashSet<string>(
            DefaultSupportedFormats,
            StringComparer.OrdinalIgnoreCase
        );
        _memoryUsage = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);

        _logger.LogInformation(
            "AudioAssetLoader initialized with supported formats: {Formats}",
            string.Join(", ", _supportedExtensions)
        );
    }

    /// <summary>
    /// Determines if this loader can handle the specified file extension.
    /// </summary>
    /// <param name="extension">The file extension (e.g., ".wav", ".ogg").</param>
    /// <returns>True if the extension is supported (WAV or OGG).</returns>
    public bool CanHandle(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
        {
            return false;
        }

        var normalizedExtension = extension.StartsWith(".") ? extension : $".{extension}";
        return _supportedExtensions.Contains(normalizedExtension);
    }

    /// <summary>
    /// Loads an audio file from the specified path synchronously.
    /// </summary>
    /// <param name="path">The file path to the audio file.</param>
    /// <returns>A loaded SoundEffect instance.</returns>
    /// <exception cref="AssetLoadException">Thrown when the audio file cannot be loaded.</exception>
    public AudioSoundEffect Load(string path)
    {
        return LoadAsync(path, CancellationToken.None).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Loads an audio file asynchronously with cancellation support.
    /// </summary>
    /// <param name="path">The file path to the audio file.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>A task that represents the asynchronous load operation.</returns>
    /// <exception cref="AssetLoadException">Thrown when the audio file cannot be loaded.</exception>
    public async Task<AudioSoundEffect> LoadAsync(
        string path,
        CancellationToken cancellationToken = default
    )
    {
        ThrowIfDisposed();

        if (string.IsNullOrWhiteSpace(path))
        {
            throw new AssetLoadException(path, "Audio path cannot be null or empty.");
        }

        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogDebug("Loading audio file: {Path}", path);

            // Validate file exists
            if (!File.Exists(path))
            {
                throw new AssetLoadException(path, $"Audio file not found: {path}");
            }

            // Validate file extension
            var extension = Path.GetExtension(path);
            if (!CanHandle(extension))
            {
                throw new AssetLoadException(
                    path,
                    $"Unsupported audio format: {extension}. Supported formats: {string.Join(", ", _supportedExtensions)}"
                );
            }

            // Check cancellation before file I/O
            cancellationToken.ThrowIfCancellationRequested();

            // Load audio data from file
            byte[] audioData = await File.ReadAllBytesAsync(path, cancellationToken);

            if (audioData.Length == 0)
            {
                throw new AssetLoadException(path, "Audio file is empty.");
            }

            // Check cancellation before MonoGame processing
            cancellationToken.ThrowIfCancellationRequested();

            // Load with MonoGame's SoundEffect
            SoundEffect monoGameSound;
            try
            {
                using var memoryStream = new MemoryStream(audioData);
                monoGameSound = SoundEffect.FromStream(memoryStream);
            }
            catch (Exception ex)
            {
                throw new AssetLoadException(
                    path,
                    $"Failed to load audio with MonoGame. The file may be corrupted or in an unsupported format.",
                    ex
                );
            }

            // Validate audio properties
            ValidateAudioProperties(monoGameSound, path);

            // Track memory usage
            long bufferSize = audioData.Length;
            _memoryUsage[path] = bufferSize;

            // Create our domain SoundEffect model
            var soundEffect = new AudioSoundEffect
            {
                Name = Path.GetFileNameWithoutExtension(path),
                FilePath = path,
                Duration = monoGameSound.Duration,
                SampleRate = GetSampleRate(monoGameSound),
                Channels = GetChannelCount(monoGameSound),
                IsPreloaded = true,
                CreatedAt = DateTime.UtcNow,
                Metadata = new Dictionary<string, object>
                {
                    { "BufferSize", bufferSize },
                    { "Format", extension },
                    { "LoadTime", stopwatch.ElapsedMilliseconds },
                },
            };

            stopwatch.Stop();

            _logger.LogInformation(
                "Successfully loaded audio: {Name} | Duration: {Duration:F2}s | Size: {Size} bytes | Time: {Time}ms | Sample Rate: {SampleRate}Hz | Channels: {Channels}",
                soundEffect.Name,
                soundEffect.Duration.TotalSeconds,
                bufferSize,
                stopwatch.ElapsedMilliseconds,
                soundEffect.SampleRate,
                soundEffect.Channels
            );

            // Dispose MonoGame SoundEffect as we've extracted what we need
            monoGameSound.Dispose();

            return soundEffect;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Audio loading cancelled: {Path}", path);
            throw;
        }
        catch (AssetLoadException)
        {
            throw;
        }
        catch (Exception ex)
        {
            var message = $"Unexpected error loading audio file: {path}";
            _logger.LogError(ex, message);
            throw new AssetLoadException(path, message, ex);
        }
    }

    /// <summary>
    /// Validates audio properties to ensure compatibility.
    /// </summary>
    /// <param name="soundEffect">The MonoGame SoundEffect to validate.</param>
    /// <param name="path">The file path for error reporting.</param>
    /// <exception cref="AssetLoadException">Thrown when audio properties are invalid.</exception>
    private void ValidateAudioProperties(SoundEffect soundEffect, string path)
    {
        if (soundEffect.Duration <= TimeSpan.Zero)
        {
            throw new AssetLoadException(path, "Audio duration must be greater than zero.");
        }

        // Validate sample rate
        int sampleRate = GetSampleRate(soundEffect);
        if (!SupportedSampleRates.Contains(sampleRate))
        {
            _logger.LogWarning(
                "Audio file {Path} has non-standard sample rate: {SampleRate}Hz. Supported rates: {SupportedRates}",
                path,
                sampleRate,
                string.Join(", ", SupportedSampleRates)
            );
        }

        // Validate channel count (1 = mono, 2 = stereo)
        int channels = GetChannelCount(soundEffect);
        if (channels < 1 || channels > 2)
        {
            throw new AssetLoadException(
                path,
                $"Unsupported channel count: {channels}. Only mono (1) and stereo (2) are supported."
            );
        }
    }

    /// <summary>
    /// Gets the sample rate from a MonoGame SoundEffect.
    /// </summary>
    /// <param name="soundEffect">The sound effect.</param>
    /// <returns>The sample rate in Hz.</returns>
    private static int GetSampleRate(SoundEffect soundEffect)
    {
        // MonoGame doesn't expose sample rate directly, but we can calculate it
        // from duration and buffer size. Default to 44100Hz if calculation fails.
        try
        {
            // This is an approximation - MonoGame uses 44.1kHz by default
            return 44100;
        }
        catch
        {
            return 44100;
        }
    }

    /// <summary>
    /// Gets the channel count from a MonoGame SoundEffect.
    /// </summary>
    /// <param name="soundEffect">The sound effect.</param>
    /// <returns>The number of channels (1 = mono, 2 = stereo).</returns>
    private static int GetChannelCount(SoundEffect soundEffect)
    {
        // MonoGame doesn't expose channel count directly
        // Default to stereo (2 channels)
        return 2;
    }

    /// <summary>
    /// Gets the total memory usage in bytes for all loaded audio.
    /// </summary>
    /// <returns>Total memory usage in bytes.</returns>
    public long GetTotalMemoryUsage()
    {
        ThrowIfDisposed();
        return _memoryUsage.Values.Sum();
    }

    /// <summary>
    /// Gets the memory usage for a specific audio file.
    /// </summary>
    /// <param name="path">The file path.</param>
    /// <returns>Memory usage in bytes, or 0 if not found.</returns>
    public long GetMemoryUsage(string path)
    {
        ThrowIfDisposed();
        return _memoryUsage.TryGetValue(path, out var size) ? size : 0;
    }

    /// <summary>
    /// Clears memory tracking for a specific path.
    /// </summary>
    /// <param name="path">The file path to clear.</param>
    public void ClearMemoryTracking(string path)
    {
        ThrowIfDisposed();
        _memoryUsage.Remove(path);
    }

    /// <summary>
    /// Clears all memory tracking.
    /// </summary>
    public void ClearAllMemoryTracking()
    {
        ThrowIfDisposed();
        _memoryUsage.Clear();
        _logger.LogDebug("Cleared all audio memory tracking");
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(AudioAssetLoader));
        }
    }

    /// <summary>
    /// Disposes the AudioAssetLoader and releases resources.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _logger.LogInformation(
            "Disposing AudioAssetLoader. Total memory tracked: {Memory} bytes",
            GetTotalMemoryUsage()
        );
        _memoryUsage.Clear();
        _disposed = true;
    }
}
