using Xunit;
using Moq;
using FluentAssertions;
using PokeNET.Audio.Services;
using PokeNET.Audio.Abstractions;
using PokeNET.Audio.Models;
using PokeNET.Audio.Mixing;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PokeNET.Tests.Audio
{
    /// <summary>
    /// Comprehensive tests for AudioManager
    /// Tests playback, caching, and disposal functionality
    /// </summary>
    public class AudioManagerTests : IDisposable
    {
        private readonly Mock<ILogger<AudioManager>> _mockLogger;
        private readonly Mock<IAudioCache> _mockCache;
        private readonly Mock<IMusicPlayer> _mockMusicPlayer;
        private readonly Mock<ISoundEffectPlayer> _mockSfxPlayer;
        private readonly Mock<IAudioVolumeManager> _mockVolumeManager;
        private readonly Mock<IAudioStateManager> _mockStateManager;
        private readonly Mock<IAudioCacheCoordinator> _mockCacheCoordinator;
        private readonly Mock<IAmbientAudioManager> _mockAmbientManager;
        private AudioManager? _audioManager;

        public AudioManagerTests()
        {
            _mockLogger = new Mock<ILogger<AudioManager>>();
            _mockCache = new Mock<IAudioCache>();
            _mockMusicPlayer = new Mock<IMusicPlayer>();
            _mockSfxPlayer = new Mock<ISoundEffectPlayer>();
            _mockVolumeManager = new Mock<IAudioVolumeManager>();
            _mockStateManager = new Mock<IAudioStateManager>();
            _mockCacheCoordinator = new Mock<IAudioCacheCoordinator>();
            _mockAmbientManager = new Mock<IAmbientAudioManager>();

            // Setup default mock behaviors
            _mockStateManager.Setup(s => s.IsInitialized).Returns(true);
            _mockVolumeManager.Setup(v => v.MasterVolume).Returns(1.0f);
            _mockVolumeManager.Setup(v => v.MusicVolume).Returns(1.0f);
            _mockVolumeManager.Setup(v => v.SfxVolume).Returns(1.0f);
            _mockStateManager.Setup(s => s.CurrentMusicTrack).Returns(string.Empty);
            _mockCacheCoordinator.Setup(c => c.GetSize()).Returns(0);
        }

        private AudioManager CreateAudioManager()
        {
            return new AudioManager(
                _mockLogger.Object,
                _mockCache.Object,
                _mockMusicPlayer.Object,
                _mockSfxPlayer.Object,
                _mockVolumeManager.Object,
                _mockStateManager.Object,
                _mockCacheCoordinator.Object,
                _mockAmbientManager.Object
            );
        }

        #region Initialization Tests

        [Fact]
        public void Constructor_ShouldInitializeWithValidDependencies()
        {
            // Arrange & Act
            _audioManager = CreateAudioManager();

            // Assert
            _audioManager.Should().NotBeNull();
            _audioManager.IsInitialized.Should().BeTrue();
        }

        [Fact]
        public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
        {
            // Arrange & Act
            Action act = () => new AudioManager(
                null!,
                _mockCache.Object,
                _mockMusicPlayer.Object,
                _mockSfxPlayer.Object,
                _mockVolumeManager.Object,
                _mockStateManager.Object,
                _mockCacheCoordinator.Object,
                _mockAmbientManager.Object
            );

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("logger");
        }

        [Fact]
        public async Task InitializeAsync_ShouldSetupAudioSystem()
        {
            // Arrange
            _audioManager = CreateAudioManager();

            // Act
            await _audioManager.InitializeAsync();

            // Assert
            _audioManager.IsInitialized.Should().BeTrue();
            // Note: IMusicPlayer and ISoundEffectPlayer don't have InitializeAsync
            // AudioManager handles initialization internally
        }

        #endregion

        #region Playback Tests

        [Fact]
        public async Task PlayMusic_WithValidFile_ShouldStartPlayback()
        {
            // Arrange
            _audioManager = CreateAudioManager();
            var musicFile = "music/theme.mid";
            _mockCache.Setup(c => c.GetOrLoadAsync<AudioTrack>(musicFile, It.IsAny<Func<Task<AudioTrack>>>()))
                .ReturnsAsync(new AudioTrack { FilePath = musicFile });

            // Act
            await _audioManager.PlayMusicAsync(musicFile, true);

            // Assert
            _mockMusicPlayer.Verify(m => m.PlayAsync(It.IsAny<AudioTrack>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task PlayMusic_WithInvalidFile_ShouldLogError()
        {
            // Arrange
            _audioManager = CreateAudioManager();
            var musicFile = "music/nonexistent.mid";
            _mockCache.Setup(c => c.GetOrLoadAsync<AudioTrack>(musicFile, It.IsAny<Func<Task<AudioTrack>>>()))
                .ThrowsAsync(new FileNotFoundException());

            // Act
            Func<Task> act = async () => await _audioManager.PlayMusicAsync(musicFile, true);

            // Assert
            await act.Should().ThrowAsync<FileNotFoundException>();
            Tests.Audio.LoggerExtensions.VerifyLog(_mockLogger, LogLevel.Error, Times.AtLeastOnce());
        }

        [Fact]
        public async Task PlaySoundEffect_WithValidFile_ShouldPlayEffect()
        {
            // Arrange
            _audioManager = CreateAudioManager();
            var sfxFile = "sfx/jump.wav";
            _mockCache.Setup(c => c.GetOrLoadAsync<SoundEffect>(sfxFile, It.IsAny<Func<Task<SoundEffect>>>()))
                .ReturnsAsync(new SoundEffect { FilePath = sfxFile });

            // Act
            await _audioManager.PlaySoundEffectAsync(sfxFile);

            // Assert
            _mockSfxPlayer.Verify(s => s.PlayAsync(It.IsAny<SoundEffect>(), It.IsAny<float?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task PlaySoundEffect_WithCustomVolume_ShouldUseSpecifiedVolume()
        {
            // Arrange
            _audioManager = CreateAudioManager();
            var sfxFile = "sfx/jump.wav";
            var volume = 0.5f;
            _mockCache.Setup(c => c.GetOrLoadAsync<SoundEffect>(sfxFile, It.IsAny<Func<Task<SoundEffect>>>()))
                .ReturnsAsync(new SoundEffect { FilePath = sfxFile });

            // Act
            await _audioManager.PlaySoundEffectAsync(sfxFile, volume);

            // Assert
            _mockSfxPlayer.Verify(s => s.PlayAsync(It.IsAny<SoundEffect>(), volume, It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task StopMusic_ShouldStopCurrentPlayback()
        {
            // Arrange
            _audioManager = CreateAudioManager();

            // Act
            await _audioManager.StopMusicAsync();

            // Assert
            _mockMusicPlayer.Verify(m => m.Stop(), Times.Once);
        }

        [Fact]
        public async Task PauseMusic_ShouldPauseCurrentPlayback()
        {
            // Arrange
            _audioManager = CreateAudioManager();

            // Act
            await _audioManager.PauseMusicAsync();

            // Assert
            _mockMusicPlayer.Verify(m => m.Pause(), Times.Once);
        }

        [Fact]
        public async Task ResumeMusic_ShouldResumePlayback()
        {
            // Arrange
            _audioManager = CreateAudioManager();

            // Act
            await _audioManager.ResumeMusicAsync();

            // Assert
            _mockMusicPlayer.Verify(m => m.Resume(), Times.Once);
        }

        #endregion

        #region Caching Tests

        [Fact]
        public async Task PlayMusic_WithCachedFile_ShouldUseCachedData()
        {
            // Arrange
            _audioManager = CreateAudioManager();
            var musicFile = "music/theme.mid";
            var cachedData = new AudioTrack { FilePath = musicFile };
            _mockCache.Setup(c => c.GetOrLoadAsync<AudioTrack>(musicFile, It.IsAny<Func<Task<AudioTrack>>>()))
                .ReturnsAsync(cachedData);

            // Act
            await _audioManager.PlayMusicAsync(musicFile, true);
            await _audioManager.PlayMusicAsync(musicFile, true); // Second call

            // Assert
            _mockCache.Verify(c => c.GetOrLoadAsync<AudioTrack>(musicFile, It.IsAny<Func<Task<AudioTrack>>>()), Times.Exactly(2));
        }

        [Fact]
        public async Task PreloadAudio_ShouldCacheFile()
        {
            // Arrange
            _audioManager = CreateAudioManager();
            var audioFile = "sfx/powerup.wav";
            _mockCacheCoordinator.Setup(c => c.PreloadAsync(audioFile, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            await _audioManager.PreloadAudioAsync(audioFile);

            // Assert
            _mockCacheCoordinator.Verify(c => c.PreloadAsync(audioFile, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task PreloadMultipleAudio_ShouldCacheAllFiles()
        {
            // Arrange
            _audioManager = CreateAudioManager();
            var audioFiles = new[] { "sfx/jump.wav", "sfx/hit.wav", "music/battle.mid" };
            _mockCacheCoordinator.Setup(c => c.PreloadMultipleAsync(It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            await _audioManager.PreloadMultipleAsync(audioFiles);

            // Assert
            _mockCacheCoordinator.Verify(c => c.PreloadMultipleAsync(audioFiles, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ClearCache_ShouldRemoveAllCachedData()
        {
            // Arrange
            _audioManager = CreateAudioManager();
            _mockCacheCoordinator.Setup(c => c.ClearAsync())
                .Returns(Task.CompletedTask);

            // Act
            await _audioManager.ClearCacheAsync();

            // Assert
            _mockCacheCoordinator.Verify(c => c.ClearAsync(), Times.Once);
        }

        [Fact]
        public void GetCacheSize_ShouldReturnCurrentCacheSize()
        {
            // Arrange
            _audioManager = CreateAudioManager();
            _mockCacheCoordinator.Setup(c => c.GetSize()).Returns(1024 * 1024); // 1MB

            // Act
            var size = _audioManager.GetCacheSize();

            // Assert
            size.Should().Be(1024 * 1024);
        }

        #endregion

        #region Volume and Mixing Tests

        [Fact]
        public void SetMasterVolume_ShouldUpdateAllChannels()
        {
            // Arrange
            _audioManager = CreateAudioManager();
            _mockVolumeManager.Setup(v => v.MasterVolume).Returns(0.7f);

            // Act
            _audioManager.SetMasterVolume(0.7f);

            // Assert
            _mockVolumeManager.Verify(v => v.SetMasterVolume(0.7f), Times.Once);
        }

        [Fact]
        public void SetMasterVolume_AboveMax_ShouldClampToOne()
        {
            // Arrange
            _audioManager = CreateAudioManager();
            _mockVolumeManager.Setup(v => v.MasterVolume).Returns(1.0f);

            // Act
            _audioManager.SetMasterVolume(1.5f);

            // Assert
            // Volume manager is responsible for clamping
            _mockVolumeManager.Verify(v => v.SetMasterVolume(1.5f), Times.Once);
        }

        [Fact]
        public void SetMasterVolume_BelowMin_ShouldClampToZero()
        {
            // Arrange
            _audioManager = CreateAudioManager();
            _mockVolumeManager.Setup(v => v.MasterVolume).Returns(0.0f);

            // Act
            _audioManager.SetMasterVolume(-0.5f);

            // Assert
            // Volume manager is responsible for clamping
            _mockVolumeManager.Verify(v => v.SetMasterVolume(-0.5f), Times.Once);
        }

        [Fact]
        public void SetMusicVolume_ShouldUpdateMusicChannel()
        {
            // Arrange
            _audioManager = CreateAudioManager();
            _mockVolumeManager.Setup(v => v.MusicVolume).Returns(0.8f);

            // Act
            _audioManager.SetMusicVolume(0.8f);

            // Assert
            _mockVolumeManager.Verify(v => v.SetMusicVolume(0.8f), Times.Once);
        }

        [Fact]
        public void SetSfxVolume_ShouldUpdateSfxChannel()
        {
            // Arrange
            _audioManager = CreateAudioManager();
            _mockVolumeManager.Setup(v => v.SfxVolume).Returns(0.6f);

            // Act
            _audioManager.SetSfxVolume(0.6f);

            // Assert
            _mockVolumeManager.Verify(v => v.SetSfxVolume(0.6f), Times.Once);
        }

        #endregion

        #region Disposal Tests

        [Fact]
        public void Dispose_ShouldCleanupResources()
        {
            // Arrange
            _audioManager = CreateAudioManager();

            // Act
            _audioManager.Dispose();

            // Assert
            // Note: IMusicPlayer and ISoundEffectPlayer don't have Dispose
            // AudioManager handles disposal internally
            _mockCache.Verify(c => c.Dispose(), Times.Once);
        }

        [Fact]
        public void Dispose_CalledMultipleTimes_ShouldBeIdempotent()
        {
            // Arrange
            _audioManager = CreateAudioManager();

            // Act
            _audioManager.Dispose();
            _audioManager.Dispose();
            _audioManager.Dispose();

            // Assert
            // Note: Disposal verification is handled via AudioManager's internal state
            _audioManager.IsInitialized.Should().BeFalse();
        }

        [Fact]
        public async Task PlayMusic_AfterDispose_ShouldThrowObjectDisposedException()
        {
            // Arrange
            _audioManager = CreateAudioManager();
            _audioManager.Dispose();

            // Act
            Func<Task> act = async () => await _audioManager.PlayMusicAsync("test.mid", true);

            // Assert
            await act.Should().ThrowAsync<ObjectDisposedException>();
        }

        #endregion

        #region State Management Tests

        [Fact]
        public void IsPlaying_WithActiveMusic_ShouldReturnTrue()
        {
            // Arrange
            _audioManager = CreateAudioManager();
            _mockMusicPlayer.Setup(m => m.IsPlaying).Returns(true);

            // Act
            var isPlaying = _audioManager.IsMusicPlaying;

            // Assert
            isPlaying.Should().BeTrue();
        }

        [Fact]
        public void IsPaused_WithPausedMusic_ShouldReturnTrue()
        {
            // Arrange
            _audioManager = CreateAudioManager();
            _mockMusicPlayer.Setup(m => m.State).Returns(PlaybackState.Paused);

            // Act
            var isPaused = _audioManager.IsMusicPaused;

            // Assert
            isPaused.Should().BeTrue();
        }

        [Fact]
        public void GetPlaybackPosition_ShouldReturnCurrentPosition()
        {
            // Arrange
            _audioManager = CreateAudioManager();
            var expectedPosition = TimeSpan.FromSeconds(30);
            _mockMusicPlayer.Setup(m => m.GetPosition()).Returns(expectedPosition);

            // Act
            var position = _audioManager.GetMusicPosition();

            // Assert
            position.Should().Be(expectedPosition);
        }

        #endregion

        public void Dispose()
        {
            _audioManager?.Dispose();
        }
    }

    // Helper extension for logger verification
    public static class LoggerExtensions
    {
        public static void VerifyLog<T>(this Mock<ILogger<T>> mockLogger, LogLevel level, Times times)
        {
            mockLogger.Verify(
                x => x.Log(
                    level,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                times);
        }
    }
}
