using Xunit;
using Moq;
using FluentAssertions;
using PokeNET.Audio.Services.Managers;
using PokeNET.Audio.Abstractions;
using PokeNET.Audio.Models;
using Microsoft.Extensions.Logging;

namespace PokeNET.Tests.Audio.Managers;

/// <summary>
/// Comprehensive tests for AmbientAudioManager.
/// Verifies ambient audio lifecycle and state management.
/// </summary>
public class AmbientAudioManagerTests
{
    private readonly Mock<ILogger<AmbientAudioManager>> _mockLogger;
    private readonly Mock<IAudioCache> _mockCache;
    private readonly Mock<ISoundEffectPlayer> _mockSfxPlayer;

    public AmbientAudioManagerTests()
    {
        _mockLogger = new Mock<ILogger<AmbientAudioManager>>();
        _mockCache = new Mock<IAudioCache>();
        _mockSfxPlayer = new Mock<ISoundEffectPlayer>();
    }

    private AmbientAudioManager CreateManager()
    {
        return new AmbientAudioManager(
            _mockLogger.Object,
            _mockCache.Object,
            _mockSfxPlayer.Object
        );
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidDependencies_ShouldInitialize()
    {
        // Act
        var manager = CreateManager();

        // Assert
        manager.Should().NotBeNull();
        manager.CurrentAmbientTrack.Should().BeEmpty();
        manager.CurrentVolume.Should().Be(1.0f);
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act
        Action act = () => new AmbientAudioManager(
            null!,
            _mockCache.Object,
            _mockSfxPlayer.Object
        );

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithNullCache_ShouldThrowArgumentNullException()
    {
        // Act
        Action act = () => new AmbientAudioManager(
            _mockLogger.Object,
            null!,
            _mockSfxPlayer.Object
        );

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("cache");
    }

    #endregion

    #region PlayAsync Tests

    [Fact]
    public async Task PlayAsync_WithValidAmbient_ShouldStartPlayback()
    {
        // Arrange
        var manager = CreateManager();
        var ambientName = "ambient/forest.wav";
        var volume = 0.7f;
        _mockCache.Setup(c => c.GetOrLoadAsync<AudioTrack>(ambientName, It.IsAny<Func<Task<AudioTrack>>>()))
            .ReturnsAsync(new AudioTrack { FilePath = ambientName });

        // Act
        await manager.PlayAsync(ambientName, volume);

        // Assert
        manager.CurrentAmbientTrack.Should().Be(ambientName);
        manager.CurrentVolume.Should().Be(volume);
        _mockSfxPlayer.Verify(s => s.PlayAsync(
            It.IsAny<SoundEffect>(),
            volume,
            -1,
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task PlayAsync_WithInvalidAmbient_ShouldThrowException()
    {
        // Arrange
        var manager = CreateManager();
        var ambientName = "invalid/ambient.wav";
        _mockCache.Setup(c => c.GetOrLoadAsync<AudioTrack>(ambientName, It.IsAny<Func<Task<AudioTrack>>>()))
            .ThrowsAsync(new FileNotFoundException());

        // Act
        Func<Task> act = async () => await manager.PlayAsync(ambientName, 1.0f);

        // Assert
        await act.Should().ThrowAsync<FileNotFoundException>();
    }

    #endregion

    #region PauseAsync Tests

    [Fact]
    public async Task PauseAsync_ShouldComplete()
    {
        // Arrange
        var manager = CreateManager();

        // Act
        await manager.PauseAsync();

        // Assert - Method should complete without error
        Assert.True(true);
    }

    #endregion

    #region ResumeAsync Tests

    [Fact]
    public async Task ResumeAsync_ShouldComplete()
    {
        // Arrange
        var manager = CreateManager();

        // Act
        await manager.ResumeAsync();

        // Assert - Method should complete without error
        Assert.True(true);
    }

    #endregion

    #region StopAsync Tests

    [Fact]
    public async Task StopAsync_ShouldClearCurrentTrack()
    {
        // Arrange
        var manager = CreateManager();
        var ambientName = "ambient/forest.wav";
        _mockCache.Setup(c => c.GetOrLoadAsync<AudioTrack>(ambientName, It.IsAny<Func<Task<AudioTrack>>>()))
            .ReturnsAsync(new AudioTrack { FilePath = ambientName });
        await manager.PlayAsync(ambientName, 1.0f);

        // Act
        await manager.StopAsync();

        // Assert
        manager.CurrentAmbientTrack.Should().BeEmpty();
    }

    #endregion
}
