using Xunit;
using Moq;
using FluentAssertions;
using PokeNET.Audio.Services.Managers;
using PokeNET.Audio.Abstractions;
using Microsoft.Extensions.Logging;

namespace PokeNET.Tests.Audio.Managers;

/// <summary>
/// Comprehensive tests for AudioVolumeManager.
/// Verifies volume control, ducking, and player synchronization.
/// </summary>
public class AudioVolumeManagerTests
{
    private readonly Mock<ILogger<AudioVolumeManager>> _mockLogger;
    private readonly Mock<IMusicPlayer> _mockMusicPlayer;
    private readonly Mock<ISoundEffectPlayer> _mockSfxPlayer;

    public AudioVolumeManagerTests()
    {
        _mockLogger = new Mock<ILogger<AudioVolumeManager>>();
        _mockMusicPlayer = new Mock<IMusicPlayer>();
        _mockSfxPlayer = new Mock<ISoundEffectPlayer>();
    }

    private AudioVolumeManager CreateManager()
    {
        return new AudioVolumeManager(
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
        manager.MasterVolume.Should().Be(1.0f);
        manager.MusicVolume.Should().Be(1.0f);
        manager.SfxVolume.Should().Be(1.0f);
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act
        Action act = () => new AudioVolumeManager(
            null!,
            _mockMusicPlayer.Object,
            _mockSfxPlayer.Object
        );

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    #endregion

    #region Master Volume Tests

    [Fact]
    public void SetMasterVolume_WithValidValue_ShouldUpdateVolume()
    {
        // Arrange
        var manager = CreateManager();

        // Act
        manager.SetMasterVolume(0.7f);

        // Assert
        manager.MasterVolume.Should().Be(0.7f);
        _mockMusicPlayer.Verify(m => m.SetVolume(0.7f), Times.Once);
        _mockSfxPlayer.Verify(s => s.SetMasterVolume(0.7f), Times.Once);
    }

    [Fact]
    public void SetMasterVolume_AboveMax_ShouldClampToOne()
    {
        // Arrange
        var manager = CreateManager();

        // Act
        manager.SetMasterVolume(1.5f);

        // Assert
        manager.MasterVolume.Should().Be(1.0f);
    }

    [Fact]
    public void SetMasterVolume_BelowMin_ShouldClampToZero()
    {
        // Arrange
        var manager = CreateManager();

        // Act
        manager.SetMasterVolume(-0.5f);

        // Assert
        manager.MasterVolume.Should().Be(0.0f);
    }

    #endregion

    #region Music Volume Tests

    [Fact]
    public void SetMusicVolume_WithValidValue_ShouldUpdateMusicPlayer()
    {
        // Arrange
        var manager = CreateManager();

        // Act
        manager.SetMusicVolume(0.8f);

        // Assert
        manager.MusicVolume.Should().Be(0.8f);
        _mockMusicPlayer.Verify(m => m.SetVolume(0.8f), Times.Once);
    }

    [Fact]
    public void SetMusicVolume_WithMasterVolume_ShouldApplyMultiplier()
    {
        // Arrange
        var manager = CreateManager();
        manager.SetMasterVolume(0.5f);
        _mockMusicPlayer.Reset();

        // Act
        manager.SetMusicVolume(0.8f);

        // Assert
        manager.MusicVolume.Should().Be(0.8f);
        _mockMusicPlayer.Verify(m => m.SetVolume(0.4f), Times.Once); // 0.8 * 0.5
    }

    #endregion

    #region SFX Volume Tests

    [Fact]
    public void SetSfxVolume_WithValidValue_ShouldUpdateSfxPlayer()
    {
        // Arrange
        var manager = CreateManager();

        // Act
        manager.SetSfxVolume(0.6f);

        // Assert
        manager.SfxVolume.Should().Be(0.6f);
        _mockSfxPlayer.Verify(s => s.SetMasterVolume(0.6f), Times.Once);
    }

    #endregion

    #region Ducking Tests

    [Fact]
    public void DuckMusic_ShouldLowerVolume()
    {
        // Arrange
        var manager = CreateManager();
        manager.SetMusicVolume(1.0f);
        _mockMusicPlayer.Reset();

        // Act
        manager.DuckMusic(0.5f); // Duck by 50%

        // Assert
        manager.OriginalMusicVolume.Should().Be(1.0f);
        _mockMusicPlayer.Verify(m => m.SetVolume(0.5f), Times.Once); // 1.0 * (1.0 - 0.5)
    }

    [Fact]
    public void StopDucking_ShouldRestoreOriginalVolume()
    {
        // Arrange
        var manager = CreateManager();
        manager.SetMusicVolume(0.8f);
        manager.DuckMusic(0.5f);
        _mockMusicPlayer.Reset();

        // Act
        manager.StopDucking();

        // Assert
        _mockMusicPlayer.Verify(m => m.SetVolume(0.8f), Times.Once);
    }

    [Fact]
    public void DuckMusic_WithFullDuck_ShouldMute()
    {
        // Arrange
        var manager = CreateManager();
        manager.SetMusicVolume(1.0f);
        _mockMusicPlayer.Reset();

        // Act
        manager.DuckMusic(1.0f);

        // Assert
        _mockMusicPlayer.Verify(m => m.SetVolume(0.0f), Times.Once);
    }

    #endregion

    #region ApplyVolumesToPlayers Tests

    [Fact]
    public void ApplyVolumesToPlayers_ShouldUpdateBothPlayers()
    {
        // Arrange
        var manager = CreateManager();
        manager.SetMusicVolume(0.7f);
        manager.SetSfxVolume(0.6f);
        manager.SetMasterVolume(0.8f);
        _mockMusicPlayer.Reset();
        _mockSfxPlayer.Reset();

        // Act
        manager.ApplyVolumesToPlayers();

        // Assert
        _mockMusicPlayer.Verify(m => m.SetVolume(0.56f), Times.Once); // 0.7 * 0.8
        _mockSfxPlayer.Verify(s => s.SetMasterVolume(0.48f), Times.Once); // 0.6 * 0.8
    }

    #endregion
}
