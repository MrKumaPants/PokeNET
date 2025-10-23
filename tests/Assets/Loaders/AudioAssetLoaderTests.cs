using Microsoft.Extensions.Logging;
using Moq;
using PokeNET.Core.Assets.Loaders;
using PokeNET.Domain.Assets;
using Xunit;

namespace PokeNET.Core.Tests.Assets.Loaders;

/// <summary>
/// Unit tests for AudioAssetLoader.
/// Tests audio loading, error handling, format validation, and memory tracking.
/// </summary>
public sealed class AudioAssetLoaderTests : IDisposable
{
    private readonly Mock<ILogger<AudioAssetLoader>> _mockLogger;
    private readonly AudioAssetLoader _loader;
    private readonly string _testDataDirectory;

    public AudioAssetLoaderTests()
    {
        _mockLogger = new Mock<ILogger<AudioAssetLoader>>();
        _loader = new AudioAssetLoader(_mockLogger.Object);

        // Create test data directory
        _testDataDirectory = Path.Combine(Path.GetTempPath(), "PokeNET_AudioTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDataDirectory);
    }

    public void Dispose()
    {
        _loader.Dispose();

        // Clean up test directory
        if (Directory.Exists(_testDataDirectory))
        {
            try
            {
                Directory.Delete(_testDataDirectory, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new AudioAssetLoader(null!));
    }

    [Theory]
    [InlineData(".wav", true)]
    [InlineData(".WAV", true)]
    [InlineData(".ogg", true)]
    [InlineData(".OGG", true)]
    [InlineData("wav", true)]
    [InlineData("ogg", true)]
    [InlineData(".mp3", false)]
    [InlineData(".flac", false)]
    [InlineData(".aac", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void CanHandle_WithVariousExtensions_ReturnsExpectedResult(string extension, bool expected)
    {
        // Act
        var result = _loader.CanHandle(extension);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task LoadAsync_WithNullPath_ThrowsAssetLoadException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<AssetLoadException>(
            async () => await _loader.LoadAsync(null!));

        Assert.Contains("cannot be null or empty", exception.Message);
    }

    [Fact]
    public async Task LoadAsync_WithEmptyPath_ThrowsAssetLoadException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<AssetLoadException>(
            async () => await _loader.LoadAsync(string.Empty));

        Assert.Contains("cannot be null or empty", exception.Message);
    }

    [Fact]
    public async Task LoadAsync_WithNonExistentFile_ThrowsAssetLoadException()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_testDataDirectory, "nonexistent.wav");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<AssetLoadException>(
            async () => await _loader.LoadAsync(nonExistentPath));

        Assert.Contains("not found", exception.Message);
        Assert.Equal(nonExistentPath, exception.AssetPath);
    }

    [Fact]
    public async Task LoadAsync_WithUnsupportedFormat_ThrowsAssetLoadException()
    {
        // Arrange
        var mp3Path = Path.Combine(_testDataDirectory, "test.mp3");
        await File.WriteAllBytesAsync(mp3Path, new byte[] { 0xFF, 0xFB, 0x90, 0x00 }); // MP3 header

        // Act & Assert
        var exception = await Assert.ThrowsAsync<AssetLoadException>(
            async () => await _loader.LoadAsync(mp3Path));

        Assert.Contains("Unsupported audio format", exception.Message);
        Assert.Contains(".mp3", exception.Message);
    }

    [Fact]
    public async Task LoadAsync_WithEmptyFile_ThrowsAssetLoadException()
    {
        // Arrange
        var emptyPath = Path.Combine(_testDataDirectory, "empty.wav");
        await File.WriteAllBytesAsync(emptyPath, Array.Empty<byte>());

        // Act & Assert
        var exception = await Assert.ThrowsAsync<AssetLoadException>(
            async () => await _loader.LoadAsync(emptyPath));

        Assert.Contains("empty", exception.Message.ToLowerInvariant());
    }

    [Fact]
    public async Task LoadAsync_WithCorruptedWavFile_ThrowsAssetLoadException()
    {
        // Arrange
        var corruptPath = Path.Combine(_testDataDirectory, "corrupt.wav");
        // Write invalid WAV data (just random bytes)
        await File.WriteAllBytesAsync(corruptPath, new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05 });

        // Act & Assert
        var exception = await Assert.ThrowsAsync<AssetLoadException>(
            async () => await _loader.LoadAsync(corruptPath));

        Assert.Contains("corrupted", exception.Message.ToLowerInvariant());
    }

    [Fact]
    public async Task LoadAsync_WithValidWavFile_LoadsSuccessfully()
    {
        // Arrange
        var wavPath = Path.Combine(_testDataDirectory, "valid.wav");
        await CreateValidWavFile(wavPath);

        // Act
        var soundEffect = await _loader.LoadAsync(wavPath);

        // Assert
        Assert.NotNull(soundEffect);
        Assert.Equal("valid", soundEffect.Name);
        Assert.Equal(wavPath, soundEffect.FilePath);
        Assert.True(soundEffect.Duration > TimeSpan.Zero);
        Assert.True(soundEffect.IsPreloaded);
        Assert.Equal(44100, soundEffect.SampleRate);
        Assert.InRange(soundEffect.Channels, 1, 2);
        Assert.True(soundEffect.Metadata.ContainsKey("BufferSize"));
        Assert.True(soundEffect.Metadata.ContainsKey("Format"));
        Assert.True(soundEffect.Metadata.ContainsKey("LoadTime"));
    }

    [Fact]
    public void Load_SynchronousWrapper_LoadsSuccessfully()
    {
        // Arrange
        var wavPath = Path.Combine(_testDataDirectory, "sync_valid.wav");
        CreateValidWavFile(wavPath).GetAwaiter().GetResult();

        // Act
        var soundEffect = _loader.Load(wavPath);

        // Assert
        Assert.NotNull(soundEffect);
        Assert.Equal("sync_valid", soundEffect.Name);
    }

    [Fact]
    public async Task LoadAsync_WithCancellationToken_ThrowsOperationCanceledException()
    {
        // Arrange
        var wavPath = Path.Combine(_testDataDirectory, "cancel.wav");
        await CreateValidWavFile(wavPath);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await _loader.LoadAsync(wavPath, cts.Token));
    }

    [Fact]
    public async Task GetMemoryUsage_AfterLoading_ReturnsCorrectSize()
    {
        // Arrange
        var wavPath = Path.Combine(_testDataDirectory, "memory_test.wav");
        await CreateValidWavFile(wavPath);

        // Act
        await _loader.LoadAsync(wavPath);
        var memoryUsage = _loader.GetMemoryUsage(wavPath);
        var totalMemory = _loader.GetTotalMemoryUsage();

        // Assert
        Assert.True(memoryUsage > 0);
        Assert.Equal(memoryUsage, totalMemory);
    }

    [Fact]
    public void GetMemoryUsage_ForNonLoadedFile_ReturnsZero()
    {
        // Arrange
        var nonLoadedPath = "nonloaded.wav";

        // Act
        var memoryUsage = _loader.GetMemoryUsage(nonLoadedPath);

        // Assert
        Assert.Equal(0, memoryUsage);
    }

    [Fact]
    public async Task GetTotalMemoryUsage_WithMultipleFiles_ReturnsSum()
    {
        // Arrange
        var path1 = Path.Combine(_testDataDirectory, "file1.wav");
        var path2 = Path.Combine(_testDataDirectory, "file2.wav");
        await CreateValidWavFile(path1);
        await CreateValidWavFile(path2);

        // Act
        await _loader.LoadAsync(path1);
        await _loader.LoadAsync(path2);
        var totalMemory = _loader.GetTotalMemoryUsage();
        var memory1 = _loader.GetMemoryUsage(path1);
        var memory2 = _loader.GetMemoryUsage(path2);

        // Assert
        Assert.Equal(memory1 + memory2, totalMemory);
        Assert.True(totalMemory > 0);
    }

    [Fact]
    public async Task ClearMemoryTracking_RemovesSpecificPath()
    {
        // Arrange
        var wavPath = Path.Combine(_testDataDirectory, "clear_test.wav");
        await CreateValidWavFile(wavPath);
        await _loader.LoadAsync(wavPath);

        var beforeClear = _loader.GetMemoryUsage(wavPath);
        Assert.True(beforeClear > 0);

        // Act
        _loader.ClearMemoryTracking(wavPath);
        var afterClear = _loader.GetMemoryUsage(wavPath);

        // Assert
        Assert.Equal(0, afterClear);
    }

    [Fact]
    public async Task ClearAllMemoryTracking_RemovesAllPaths()
    {
        // Arrange
        var path1 = Path.Combine(_testDataDirectory, "clear1.wav");
        var path2 = Path.Combine(_testDataDirectory, "clear2.wav");
        await CreateValidWavFile(path1);
        await CreateValidWavFile(path2);
        await _loader.LoadAsync(path1);
        await _loader.LoadAsync(path2);

        var beforeClear = _loader.GetTotalMemoryUsage();
        Assert.True(beforeClear > 0);

        // Act
        _loader.ClearAllMemoryTracking();
        var afterClear = _loader.GetTotalMemoryUsage();

        // Assert
        Assert.Equal(0, afterClear);
    }

    [Fact]
    public void Dispose_WhenCalled_DisposesSuccessfully()
    {
        // Arrange
        var loader = new AudioAssetLoader(_mockLogger.Object);

        // Act
        loader.Dispose();

        // Assert
        Assert.Throws<ObjectDisposedException>(() => loader.GetTotalMemoryUsage());
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_DoesNotThrow()
    {
        // Arrange
        var loader = new AudioAssetLoader(_mockLogger.Object);

        // Act & Assert
        loader.Dispose();
        loader.Dispose(); // Should not throw
    }

    [Fact]
    public async Task LoadAsync_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var loader = new AudioAssetLoader(_mockLogger.Object);
        loader.Dispose();

        // Act & Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(
            async () => await loader.LoadAsync("test.wav"));
    }

    /// <summary>
    /// Creates a valid minimal WAV file for testing.
    /// This creates a 1-second 44.1kHz 16-bit mono sine wave.
    /// </summary>
    private async Task CreateValidWavFile(string path)
    {
        const int sampleRate = 44100;
        const short numChannels = 1;
        const short bitsPerSample = 16;
        const int frequency = 440; // A4 note
        const double durationSeconds = 0.1; // Short duration for tests

        int numSamples = (int)(sampleRate * durationSeconds);
        int dataSize = numSamples * numChannels * (bitsPerSample / 8);

        using var fs = new FileStream(path, FileMode.Create, FileAccess.Write);
        using var writer = new BinaryWriter(fs);

        // RIFF header
        writer.Write(new char[] { 'R', 'I', 'F', 'F' });
        writer.Write(36 + dataSize); // File size - 8
        writer.Write(new char[] { 'W', 'A', 'V', 'E' });

        // fmt chunk
        writer.Write(new char[] { 'f', 'm', 't', ' ' });
        writer.Write(16); // fmt chunk size
        writer.Write((short)1); // Audio format (1 = PCM)
        writer.Write(numChannels);
        writer.Write(sampleRate);
        writer.Write(sampleRate * numChannels * (bitsPerSample / 8)); // Byte rate
        writer.Write((short)(numChannels * (bitsPerSample / 8))); // Block align
        writer.Write(bitsPerSample);

        // data chunk
        writer.Write(new char[] { 'd', 'a', 't', 'a' });
        writer.Write(dataSize);

        // Write sine wave samples
        for (int i = 0; i < numSamples; i++)
        {
            double time = i / (double)sampleRate;
            double sample = Math.Sin(2.0 * Math.PI * frequency * time);
            short value = (short)(sample * short.MaxValue * 0.5); // 50% volume
            writer.Write(value);
        }

        await fs.FlushAsync();
    }
}
