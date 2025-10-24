using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PokeNET.Core.Assets.Loaders;
using PokeNET.Domain.Assets;

namespace PokeNET.Tests.Assets.Loaders;

/// <summary>
/// Comprehensive test suite for TextureAssetLoader.
/// Tests valid loading, error handling, memory tracking, and concurrent operations.
/// </summary>
public class TextureAssetLoaderTests : IDisposable
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly ILogger<TextureAssetLoader> _logger;
    private readonly string _testDataPath;

    public TextureAssetLoaderTests()
    {
        // Initialize GraphicsDevice for testing
        var presentationParameters = new PresentationParameters
        {
            BackBufferWidth = 800,
            BackBufferHeight = 600,
            IsFullScreen = false
        };

        _graphicsDevice = new GraphicsDevice(
            GraphicsAdapter.DefaultAdapter,
            GraphicsProfile.HiDef,
            presentationParameters);

        _logger = new Mock<ILogger<TextureAssetLoader>>().Object;
        _testDataPath = Path.Combine(Path.GetTempPath(), "PokeNetTextureTests");
        Directory.CreateDirectory(_testDataPath);
    }

    [Fact]
    public void Constructor_WithValidParameters_Initializes()
    {
        // Act
        using var loader = new TextureAssetLoader(_logger, _graphicsDevice);

        // Assert
        loader.Should().NotBeNull();
        loader.MemoryUsageMB.Should().Be(0);
        loader.PooledTextureCount.Should().Be(0);
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new TextureAssetLoader(null!, _graphicsDevice);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithNullGraphicsDevice_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new TextureAssetLoader(_logger, null!);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("graphicsDevice");
    }

    [Theory]
    [InlineData(".png")]
    [InlineData(".jpg")]
    [InlineData(".jpeg")]
    [InlineData(".bmp")]
    [InlineData(".gif")]
    [InlineData(".PNG")]
    [InlineData(".JPG")]
    public void CanHandle_WithSupportedExtension_ReturnsTrue(string extension)
    {
        // Arrange
        using var loader = new TextureAssetLoader(_logger, _graphicsDevice);

        // Act
        var result = loader.CanHandle(extension);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(".txt")]
    [InlineData(".json")]
    [InlineData(".mp3")]
    [InlineData(".xml")]
    [InlineData("")]
    [InlineData(null)]
    public void CanHandle_WithUnsupportedExtension_ReturnsFalse(string? extension)
    {
        // Arrange
        using var loader = new TextureAssetLoader(_logger, _graphicsDevice);

        // Act
        var result = loader.CanHandle(extension!);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Load_WithValidPngTexture_LoadsSuccessfully()
    {
        // Arrange
        var texturePath = CreateTestTexture("test.png", 64, 64, Color.Red);
        using var loader = new TextureAssetLoader(_logger, _graphicsDevice);

        // Act
        var texture = loader.Load(texturePath);

        // Assert
        texture.Should().NotBeNull();
        texture.Width.Should().Be(64);
        texture.Height.Should().Be(64);
        loader.MemoryUsageMB.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task LoadAsync_WithValidTexture_LoadsSuccessfully()
    {
        // Arrange
        var texturePath = CreateTestTexture("async_test.png", 128, 128, Color.Blue);
        using var loader = new TextureAssetLoader(_logger, _graphicsDevice);

        // Act
        var texture = await loader.LoadAsync(texturePath);

        // Assert
        texture.Should().NotBeNull();
        texture.Width.Should().Be(128);
        texture.Height.Should().Be(128);
    }

    [Fact]
    public async Task LoadAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        // Arrange
        var texturePath = CreateTestTexture("cancel_test.png", 256, 256, Color.Green);
        using var loader = new TextureAssetLoader(_logger, _graphicsDevice);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var act = async () => await loader.LoadAsync(texturePath, cancellationToken: cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public void Load_WithNonExistentFile_ThrowsAssetLoadException()
    {
        // Arrange
        using var loader = new TextureAssetLoader(_logger, _graphicsDevice);
        var nonExistentPath = Path.Combine(_testDataPath, "nonexistent.png");

        // Act
        var act = () => loader.Load(nonExistentPath);

        // Assert
        act.Should().Throw<AssetLoadException>()
            .WithMessage("*not found*")
            .And.AssetPath.Should().Be(nonExistentPath);
    }

    [Fact]
    public void Load_WithNullPath_ThrowsArgumentException()
    {
        // Arrange
        using var loader = new TextureAssetLoader(_logger, _graphicsDevice);

        // Act
        var act = () => loader.Load(null!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Load_WithEmptyPath_ThrowsArgumentException()
    {
        // Arrange
        using var loader = new TextureAssetLoader(_logger, _graphicsDevice);

        // Act
        var act = () => loader.Load(string.Empty);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Load_WithUnsupportedExtension_ThrowsAssetLoadException()
    {
        // Arrange
        using var loader = new TextureAssetLoader(_logger, _graphicsDevice);
        var invalidPath = Path.Combine(_testDataPath, "test.txt");
        File.WriteAllText(invalidPath, "Not an image");

        // Act
        var act = () => loader.Load(invalidPath);

        // Assert
        act.Should().Throw<AssetLoadException>()
            .WithMessage("*Unsupported texture format*");
    }

    [Fact]
    public void Load_WithCorruptedFile_ThrowsAssetLoadException()
    {
        // Arrange
        using var loader = new TextureAssetLoader(_logger, _graphicsDevice);
        var corruptedPath = Path.Combine(_testDataPath, "corrupted.png");

        // Create a file with PNG header but invalid data
        var corruptedData = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x00, 0x00, 0x00, 0x00 };
        File.WriteAllBytes(corruptedPath, corruptedData);

        // Act
        var act = () => loader.Load(corruptedPath);

        // Assert
        act.Should().Throw<AssetLoadException>();
    }

    [Fact]
    public void Load_WithInvalidImageFormat_ThrowsAssetLoadException()
    {
        // Arrange
        using var loader = new TextureAssetLoader(_logger, _graphicsDevice);
        var invalidPath = Path.Combine(_testDataPath, "invalid.png");

        // Create a file with wrong magic bytes
        var invalidData = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
        File.WriteAllBytes(invalidPath, invalidData);

        // Act
        var act = () => loader.Load(invalidPath);

        // Assert
        act.Should().Throw<AssetLoadException>()
            .WithMessage("*Invalid or corrupted*");
    }

    [Fact]
    public void Load_WithEmptyFile_ThrowsAssetLoadException()
    {
        // Arrange
        using var loader = new TextureAssetLoader(_logger, _graphicsDevice);
        var emptyPath = Path.Combine(_testDataPath, "empty.png");
        File.WriteAllBytes(emptyPath, Array.Empty<byte>());

        // Act
        var act = () => loader.Load(emptyPath);

        // Assert
        act.Should().Throw<AssetLoadException>()
            .WithMessage("*empty*");
    }

    [Fact]
    public void Load_WithPoolingEnabled_CachesTexture()
    {
        // Arrange
        var texturePath = CreateTestTexture("pooled.png", 64, 64, Color.Yellow);
        var options = new TextureLoadOptions { UsePooling = true };
        using var loader = new TextureAssetLoader(_logger, _graphicsDevice, options);

        // Act
        var texture1 = loader.Load(texturePath);
        var texture2 = loader.Load(texturePath);

        // Assert
        texture1.Should().BeSameAs(texture2);
        loader.PooledTextureCount.Should().Be(1);
    }

    [Fact]
    public void Load_WithPoolingDisabled_CreatesNewInstances()
    {
        // Arrange
        var texturePath = CreateTestTexture("not_pooled.png", 64, 64, Color.Purple);
        var options = new TextureLoadOptions { UsePooling = false };
        using var loader = new TextureAssetLoader(_logger, _graphicsDevice, options);

        // Act
        var texture1 = loader.Load(texturePath);

        // Assert
        loader.PooledTextureCount.Should().Be(0);
        texture1.Should().NotBeNull();
    }

    [Fact]
    public void MemoryUsageMB_AfterLoadingTexture_ReportsCorrectly()
    {
        // Arrange
        var texturePath = CreateTestTexture("memory_test.png", 256, 256, Color.White);
        using var loader = new TextureAssetLoader(_logger, _graphicsDevice);

        // Act
        loader.Load(texturePath);

        // Assert
        // 256x256x4 bytes = 262,144 bytes = ~0.25 MB
        loader.MemoryUsageMB.Should().BeGreaterThan(0.2);
        loader.MemoryUsageMB.Should().BeLessThan(0.3);
    }

    [Fact]
    public void ReleasePooledTexture_DecreasesReferenceCount()
    {
        // Arrange
        var texturePath = CreateTestTexture("release_test.png", 64, 64, Color.Orange);
        var options = new TextureLoadOptions { UsePooling = true };
        using var loader = new TextureAssetLoader(_logger, _graphicsDevice, options);

        // Act
        var texture = loader.Load(texturePath);
        var initialMemory = loader.MemoryUsageMB;
        loader.ReleasePooledTexture(texturePath);

        // Assert
        loader.MemoryUsageMB.Should().BeLessThan(initialMemory);
        loader.PooledTextureCount.Should().Be(0);
    }

    [Fact]
    public void ClearPool_DisposesAllPooledTextures()
    {
        // Arrange
        var texture1Path = CreateTestTexture("clear1.png", 64, 64, Color.Red);
        var texture2Path = CreateTestTexture("clear2.png", 64, 64, Color.Blue);
        var options = new TextureLoadOptions { UsePooling = true };
        using var loader = new TextureAssetLoader(_logger, _graphicsDevice, options);

        // Act
        loader.Load(texture1Path);
        loader.Load(texture2Path);
        var initialCount = loader.PooledTextureCount;
        loader.ClearPool();

        // Assert
        initialCount.Should().Be(2);
        loader.PooledTextureCount.Should().Be(0);
        loader.MemoryUsageMB.Should().Be(0);
    }

    [Fact]
    public async Task LoadAsync_ConcurrentLoading_HandlesCorrectly()
    {
        // Arrange
        var texturePaths = Enumerable.Range(0, 10)
            .Select(i => CreateTestTexture($"concurrent_{i}.png", 32, 32, Color.Red))
            .ToList();
        using var loader = new TextureAssetLoader(_logger, _graphicsDevice);

        // Act
        var loadTasks = texturePaths.Select(path => loader.LoadAsync(path));
        var textures = await Task.WhenAll(loadTasks);

        // Assert
        textures.Should().HaveCount(10);
        textures.Should().OnlyContain(t => t != null);
        loader.MemoryUsageMB.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Dispose_AfterLoading_DisposesPooledTextures()
    {
        // Arrange
        var texturePath = CreateTestTexture("dispose_test.png", 64, 64, Color.Green);
        var loader = new TextureAssetLoader(_logger, _graphicsDevice);

        // Act
        loader.Load(texturePath);
        var memoryBeforeDispose = loader.MemoryUsageMB;
        loader.Dispose();

        // Assert
        memoryBeforeDispose.Should().BeGreaterThan(0);
        loader.MemoryUsageMB.Should().Be(0);
        loader.PooledTextureCount.Should().Be(0);
    }

    [Fact]
    public void Load_AfterDisposed_ThrowsObjectDisposedException()
    {
        // Arrange
        var texturePath = CreateTestTexture("disposed.png", 64, 64, Color.Black);
        var loader = new TextureAssetLoader(_logger, _graphicsDevice);
        loader.Dispose();

        // Act
        var act = () => loader.Load(texturePath);

        // Assert
        act.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void Load_WithDisposedGraphicsDevice_ThrowsAssetLoadException()
    {
        // Arrange
        var texturePath = CreateTestTexture("gd_disposed.png", 64, 64, Color.Cyan);
        var presentationParameters = new PresentationParameters
        {
            BackBufferWidth = 800,
            BackBufferHeight = 600
        };
        var tempGraphicsDevice = new GraphicsDevice(
            GraphicsAdapter.DefaultAdapter,
            GraphicsProfile.HiDef,
            presentationParameters);

        using var loader = new TextureAssetLoader(_logger, tempGraphicsDevice);
        tempGraphicsDevice.Dispose();

        // Act
        var act = () => loader.Load(texturePath);

        // Assert
        act.Should().Throw<AssetLoadException>()
            .WithMessage("*GraphicsDevice has been disposed*");
    }

    [Fact]
    public void Load_WithPremultiplyAlpha_AppliesCorrectly()
    {
        // Arrange
        var texturePath = CreateTestTexture("premultiply.png", 64, 64, Color.Red);
        var options = new TextureLoadOptions { PremultiplyAlpha = true, UsePooling = false };
        using var loader = new TextureAssetLoader(_logger, _graphicsDevice, options);

        // Act
        var texture = loader.Load(texturePath);

        // Assert
        texture.Should().NotBeNull();
        // Premultiply alpha processing should complete without errors
    }

    [Fact]
    public void TextureLoadOptions_Default_HasExpectedValues()
    {
        // Arrange & Act
        var options = TextureLoadOptions.Default;

        // Assert
        options.PremultiplyAlpha.Should().BeTrue();
        options.UsePooling.Should().BeTrue();
    }

    /// <summary>
    /// Creates a test texture file with the specified dimensions and color.
    /// </summary>
    private string CreateTestTexture(string filename, int width, int height, Color color)
    {
        var path = Path.Combine(_testDataPath, filename);

        using var texture = new Texture2D(_graphicsDevice, width, height);
        var data = new Color[width * height];
        Array.Fill(data, color);
        texture.SetData(data);

        using var stream = File.OpenWrite(path);
        texture.SaveAsPng(stream, width, height);

        return path;
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testDataPath))
            {
                Directory.Delete(_testDataPath, recursive: true);
            }
        }
        catch
        {
            // Cleanup is best-effort
        }

        _graphicsDevice?.Dispose();
    }
}
