using Xunit;
using Moq;
using FluentAssertions;
using PokeNET.Audio.Services.Managers;
using PokeNET.Audio.Abstractions;
using Microsoft.Extensions.Logging;

namespace PokeNET.Tests.Audio.Managers;

/// <summary>
/// Comprehensive tests for AudioCacheCoordinator.
/// Verifies cache preloading, clearing, and size tracking.
/// </summary>
public class AudioCacheCoordinatorTests
{
    private readonly Mock<ILogger<AudioCacheCoordinator>> _mockLogger;
    private readonly Mock<IAudioCache> _mockCache;

    public AudioCacheCoordinatorTests()
    {
        _mockLogger = new Mock<ILogger<AudioCacheCoordinator>>();
        _mockCache = new Mock<IAudioCache>();
    }

    private AudioCacheCoordinator CreateCoordinator()
    {
        return new AudioCacheCoordinator(
            _mockLogger.Object,
            _mockCache.Object
        );
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidDependencies_ShouldInitialize()
    {
        // Act
        var coordinator = CreateCoordinator();

        // Assert
        coordinator.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act
        Action act = () => new AudioCacheCoordinator(
            null!,
            _mockCache.Object
        );

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithNullCache_ShouldThrowArgumentNullException()
    {
        // Act
        Action act = () => new AudioCacheCoordinator(
            _mockLogger.Object,
            null!
        );

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("cache");
    }

    #endregion

    #region PreloadAsync Tests

    [Fact]
    public async Task PreloadAsync_WithValidPath_ShouldLoadIntoCache()
    {
        // Arrange
        var coordinator = CreateCoordinator();
        var assetPath = "audio/music.mid";
        _mockCache.Setup(c => c.GetOrLoadAsync<object>(assetPath, It.IsAny<Func<Task<object>>>()))
            .ReturnsAsync(new object());

        // Act
        await coordinator.PreloadAsync(assetPath);

        // Assert
        _mockCache.Verify(c => c.GetOrLoadAsync<object>(assetPath, It.IsAny<Func<Task<object>>>()), Times.Once);
    }

    [Fact]
    public async Task PreloadAsync_WithInvalidPath_ShouldThrowException()
    {
        // Arrange
        var coordinator = CreateCoordinator();
        var assetPath = "invalid/path.mid";
        _mockCache.Setup(c => c.GetOrLoadAsync<object>(assetPath, It.IsAny<Func<Task<object>>>()))
            .ThrowsAsync(new FileNotFoundException());

        // Act
        Func<Task> act = async () => await coordinator.PreloadAsync(assetPath);

        // Assert
        await act.Should().ThrowAsync<FileNotFoundException>();
    }

    #endregion

    #region PreloadMultipleAsync Tests

    [Fact]
    public async Task PreloadMultipleAsync_WithValidPaths_ShouldLoadAll()
    {
        // Arrange
        var coordinator = CreateCoordinator();
        var assetPaths = new[] { "audio/1.mid", "audio/2.mid", "audio/3.mid" };
        _mockCache.Setup(c => c.GetOrLoadAsync<object>(It.IsAny<string>(), It.IsAny<Func<Task<object>>>()))
            .ReturnsAsync(new object());

        // Act
        await coordinator.PreloadMultipleAsync(assetPaths);

        // Assert
        foreach (var path in assetPaths)
        {
            _mockCache.Verify(c => c.GetOrLoadAsync<object>(path, It.IsAny<Func<Task<object>>>()), Times.Once);
        }
    }

    [Fact]
    public async Task PreloadMultipleAsync_WithEmptyArray_ShouldDoNothing()
    {
        // Arrange
        var coordinator = CreateCoordinator();
        var assetPaths = Array.Empty<string>();

        // Act
        await coordinator.PreloadMultipleAsync(assetPaths);

        // Assert
        _mockCache.Verify(c => c.GetOrLoadAsync<object>(It.IsAny<string>(), It.IsAny<Func<Task<object>>>()), Times.Never);
    }

    [Fact]
    public async Task PreloadMultipleAsync_WithNull_ShouldDoNothing()
    {
        // Arrange
        var coordinator = CreateCoordinator();

        // Act
        await coordinator.PreloadMultipleAsync(null!);

        // Assert
        _mockCache.Verify(c => c.GetOrLoadAsync<object>(It.IsAny<string>(), It.IsAny<Func<Task<object>>>()), Times.Never);
    }

    #endregion

    #region ClearAsync Tests

    [Fact]
    public async Task ClearAsync_ShouldClearCache()
    {
        // Arrange
        var coordinator = CreateCoordinator();

        // Act
        await coordinator.ClearAsync();

        // Assert
        _mockCache.Verify(c => c.ClearAsync(), Times.Once);
    }

    #endregion

    #region GetSize Tests

    [Fact]
    public void GetSize_ShouldReturnCacheSize()
    {
        // Arrange
        var coordinator = CreateCoordinator();
        var expectedSize = 1024 * 1024; // 1MB
        _mockCache.Setup(c => c.GetSize()).Returns(expectedSize);

        // Act
        var size = coordinator.GetSize();

        // Assert
        size.Should().Be(expectedSize);
    }

    #endregion
}
