using Xunit;
using Moq;
using FluentAssertions;
using PokeNET.Audio.Services.Managers;
using PokeNET.Audio.Abstractions;
using PokeNET.Audio.Models;
using Microsoft.Extensions.Logging;

namespace PokeNET.Tests.Audio.Managers;

/// <summary>
/// Comprehensive tests for AudioStateManager.
/// Verifies state tracking, event coordination, and global controls.
/// </summary>
public class AudioStateManagerTests
{
    private readonly Mock<ILogger<AudioStateManager>> _mockLogger;
    private readonly Mock<IMusicPlayer> _mockMusicPlayer;
    private readonly Mock<ISoundEffectPlayer> _mockSfxPlayer;

    public AudioStateManagerTests()
    {
        _mockLogger = new Mock<ILogger<AudioStateManager>>();
        _mockMusicPlayer = new Mock<IMusicPlayer>();
        _mockSfxPlayer = new Mock<ISoundEffectPlayer>();
    }

    private AudioStateManager CreateManager()
    {
        return new AudioStateManager(
            _mockLogger.Object,
            _mockMusicPlayer.Object,
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
        manager.IsInitialized.Should().BeFalse();
        manager.CurrentMusicTrack.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act
        Action act = () => new AudioStateManager(
            null!,
            _mockMusicPlayer.Object,
            _mockSfxPlayer.Object
        );

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    #endregion

    #region Initialization Tests

    [Fact]
    public void SetInitialized_WithTrue_ShouldUpdateState()
    {
        // Arrange
        var manager = CreateManager();

        // Act
        manager.SetInitialized(true);

        // Assert
        manager.IsInitialized.Should().BeTrue();
    }

    [Fact]
    public void SetInitialized_WithFalse_ShouldUpdateState()
    {
        // Arrange
        var manager = CreateManager();
        manager.SetInitialized(true);

        // Act
        manager.SetInitialized(false);

        // Assert
        manager.IsInitialized.Should().BeFalse();
    }

    #endregion

    #region Track Management Tests

    [Fact]
    public void SetCurrentMusicTrack_ShouldUpdateTrack()
    {
        // Arrange
        var manager = CreateManager();
        var trackName = "music/battle.mid";

        // Act
        manager.SetCurrentMusicTrack(trackName);

        // Assert
        manager.CurrentMusicTrack.Should().Be(trackName);
    }

    [Fact]
    public void SetCurrentAmbientTrack_ShouldUpdateTrack()
    {
        // Arrange
        var manager = CreateManager();
        var trackName = "ambient/forest.wav";

        // Act
        manager.SetCurrentAmbientTrack(trackName);

        // Assert
        manager.CurrentAmbientTrack.Should().Be(trackName);
    }

    #endregion

    #region Event Tests

    [Fact]
    public void RaiseStateChanged_ShouldTriggerEvent()
    {
        // Arrange
        var manager = CreateManager();
        AudioStateChangedEventArgs? capturedArgs = null;
        manager.StateChanged += (sender, args) => capturedArgs = args;

        // Act
        manager.RaiseStateChanged(PlaybackState.Playing, PlaybackState.Paused);

        // Assert
        capturedArgs.Should().NotBeNull();
        capturedArgs!.PreviousState.Should().Be(PlaybackState.Playing);
        capturedArgs.NewState.Should().Be(PlaybackState.Paused);
    }

    [Fact]
    public void RaiseError_ShouldTriggerEvent()
    {
        // Arrange
        var manager = CreateManager();
        AudioErrorEventArgs? capturedArgs = null;
        manager.ErrorOccurred += (sender, args) => capturedArgs = args;

        // Act
        var exception = new Exception("Test error");
        manager.RaiseError("Test message", exception);

        // Assert
        capturedArgs.Should().NotBeNull();
        capturedArgs!.Message.Should().Be("Test message");
        capturedArgs.Exception.Should().Be(exception);
    }

    [Fact]
    public void RaiseError_WithoutException_ShouldStillWork()
    {
        // Arrange
        var manager = CreateManager();
        AudioErrorEventArgs? capturedArgs = null;
        manager.ErrorOccurred += (sender, args) => capturedArgs = args;

        // Act
        manager.RaiseError("Test message");

        // Assert
        capturedArgs.Should().NotBeNull();
        capturedArgs!.Message.Should().Be("Test message");
        capturedArgs.Exception.Should().BeNull();
    }

    #endregion

    #region Global Control Tests

    [Fact]
    public void PauseAll_ShouldPauseMusicPlayer()
    {
        // Arrange
        var manager = CreateManager();

        // Act
        manager.PauseAll();

        // Assert
        _mockMusicPlayer.Verify(m => m.Pause(), Times.Once);
    }

    [Fact]
    public void ResumeAll_ShouldResumeMusicPlayer()
    {
        // Arrange
        var manager = CreateManager();

        // Act
        manager.ResumeAll();

        // Assert
        _mockMusicPlayer.Verify(m => m.Resume(), Times.Once);
    }

    [Fact]
    public void StopAll_ShouldStopBothPlayers()
    {
        // Arrange
        var manager = CreateManager();

        // Act
        manager.StopAll();

        // Assert
        _mockMusicPlayer.Verify(m => m.Stop(), Times.Once);
        _mockSfxPlayer.Verify(s => s.StopAll(), Times.Once);
    }

    #endregion
}
