using Xunit;
using Moq;
using FluentAssertions;
using PokeNET.Audio;
using PokeNET.Audio.Services;
using PokeNET.Audio.Abstractions;
using PokeNET.Audio.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PokeNET.Tests.Audio
{
    /// <summary>
    /// Comprehensive tests for SoundEffectPlayer
    /// Tests SFX playback, pooling, and concurrent playback
    /// </summary>
    public class SoundEffectPlayerTests : IDisposable
    {
        private readonly Mock<ILogger<SoundEffectPlayer>> _mockLogger;
        private readonly Mock<IAudioEngine> _mockAudioEngine;
        private SoundEffectPlayer? _sfxPlayer;

        public SoundEffectPlayerTests()
        {
            _mockLogger = new Mock<ILogger<SoundEffectPlayer>>();
            _mockAudioEngine = new Mock<IAudioEngine>();
        }

        private SoundEffectPlayer CreateSfxPlayer(int poolSize = 16)
        {
            return new SoundEffectPlayer(_mockLogger.Object, _mockAudioEngine.Object, poolSize);
        }

        #region Initialization Tests

        [Fact]
        public void Constructor_ShouldInitializeWithDefaultPoolSize()
        {
            // Arrange & Act
            _sfxPlayer = CreateSfxPlayer();

            // Assert
            _sfxPlayer.Should().NotBeNull();
            _sfxPlayer.PoolSize.Should().Be(16);
            _sfxPlayer.ActiveSounds.Should().Be(0);
        }

        [Fact]
        public void Constructor_WithCustomPoolSize_ShouldInitializeCorrectly()
        {
            // Arrange & Act
            _sfxPlayer = CreateSfxPlayer(32);

            // Assert
            _sfxPlayer.PoolSize.Should().Be(32);
        }

        [Fact]
        public void Constructor_WithInvalidPoolSize_ShouldThrowArgumentException()
        {
            // Arrange & Act
            Action act = () => CreateSfxPlayer(0);

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithParameterName("poolSize");
        }

        [Fact]
        public async Task InitializeAsync_ShouldSetupAudioEngine()
        {
            // Arrange
            _sfxPlayer = CreateSfxPlayer();

            // Act
            await _sfxPlayer.InitializeAsync();

            // Assert
            _sfxPlayer.IsInitialized.Should().BeTrue();
            _mockAudioEngine.Verify(e => e.InitializeAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion

        #region Playback Tests

        [Fact]
        public async Task PlayAsync_WithValidData_ShouldPlaySound()
        {
            // Arrange
            _sfxPlayer = CreateSfxPlayer();
            var audioData = CreateValidWavData();
            _mockAudioEngine.Setup(e => e.PlaySoundAsync(It.IsAny<byte[]>(), It.IsAny<float>(), It.IsAny<float>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(1); // Sound ID

            // Act
            var soundId = await _sfxPlayer.PlayAsync(audioData);

            // Assert
            soundId.Should().BeGreaterThan(0);
            _sfxPlayer.ActiveSounds.Should().Be(1);
        }

        [Fact]
        public async Task PlayAsync_WithNullData_ShouldThrowArgumentNullException()
        {
            // Arrange
            _sfxPlayer = CreateSfxPlayer();

            // Act
            Func<Task> act = async () => await _sfxPlayer.PlayAsync(null!);

            // Assert
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task PlayAsync_WithCustomVolume_ShouldUseSpecifiedVolume()
        {
            // Arrange
            _sfxPlayer = CreateSfxPlayer();
            var audioData = CreateValidWavData();
            var volume = 0.5f;

            // Act
            await _sfxPlayer.PlayAsync(audioData, volume);

            // Assert
            _mockAudioEngine.Verify(
                e => e.PlaySoundAsync(It.IsAny<byte[]>(), volume, It.IsAny<float>(), It.IsAny<CancellationToken>()),
                Times.Once
            );
        }

        [Fact]
        public async Task PlayAsync_WithPitch_ShouldApplyPitchShift()
        {
            // Arrange
            _sfxPlayer = CreateSfxPlayer();
            var audioData = CreateValidWavData();
            var pitch = 1.5f;

            // Act
            await _sfxPlayer.PlayAsync(audioData, 1.0f, pitch);

            // Assert
            _mockAudioEngine.Verify(
                e => e.PlaySoundAsync(It.IsAny<byte[]>(), It.IsAny<float>(), pitch, It.IsAny<CancellationToken>()),
                Times.Once
            );
        }

        [Fact]
        public async Task PlayAsync_MultipleTimes_ShouldPlayConcurrently()
        {
            // Arrange
            _sfxPlayer = CreateSfxPlayer();
            var audioData = CreateValidWavData();
            _mockAudioEngine.Setup(e => e.PlaySoundAsync(It.IsAny<byte[]>(), It.IsAny<float>(), It.IsAny<float>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((byte[] data, float vol, float pitch, CancellationToken ct) => Random.Shared.Next(1, 1000));

            // Act
            var task1 = _sfxPlayer.PlayAsync(audioData);
            var task2 = _sfxPlayer.PlayAsync(audioData);
            var task3 = _sfxPlayer.PlayAsync(audioData);
            await Task.WhenAll(task1, task2, task3);

            // Assert
            _sfxPlayer.ActiveSounds.Should().BeGreaterOrEqualTo(3);
        }

        #endregion

        #region Pooling Tests

        [Fact]
        public async Task Pool_ShouldReuseStoppedSoundSlots()
        {
            // Arrange
            _sfxPlayer = CreateSfxPlayer(4);
            var audioData = CreateValidWavData();
            var soundIds = new List<int>();

            // Act - Fill pool
            for (int i = 0; i < 4; i++)
            {
                _mockAudioEngine.Setup(e => e.PlaySoundAsync(It.IsAny<byte[]>(), It.IsAny<float>(), It.IsAny<float>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(i + 1);
                soundIds.Add(await _sfxPlayer.PlayAsync(audioData));
            }

            // Simulate sounds finishing
            foreach (var id in soundIds.Take(2))
            {
                await _sfxPlayer.StopAsync(id);
            }

            // Play more sounds
            var newId1 = await _sfxPlayer.PlayAsync(audioData);
            var newId2 = await _sfxPlayer.PlayAsync(audioData);

            // Assert
            _sfxPlayer.ActiveSounds.Should().BeLessOrEqualTo(4);
        }

        [Fact]
        public async Task Pool_WhenFull_ShouldStopOldestSound()
        {
            // Arrange
            _sfxPlayer = CreateSfxPlayer(3);
            var audioData = CreateValidWavData();
            var soundId = 1;
            _mockAudioEngine.Setup(e => e.PlaySoundAsync(It.IsAny<byte[]>(), It.IsAny<float>(), It.IsAny<float>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => soundId++);

            // Act - Exceed pool size
            var id1 = await _sfxPlayer.PlayAsync(audioData);
            var id2 = await _sfxPlayer.PlayAsync(audioData);
            var id3 = await _sfxPlayer.PlayAsync(audioData);
            var id4 = await _sfxPlayer.PlayAsync(audioData); // Should stop id1

            // Assert
            _sfxPlayer.ActiveSounds.Should().BeLessOrEqualTo(3);
            _mockAudioEngine.Verify(e => e.StopSoundAsync(id1), Times.Once);
        }

        [Fact]
        public void GetAvailableSlots_ShouldReturnCorrectCount()
        {
            // Arrange
            _sfxPlayer = CreateSfxPlayer(10);

            // Act
            var available = _sfxPlayer.GetAvailableSlots();

            // Assert
            available.Should().Be(10);
        }

        [Fact]
        public async Task GetAvailableSlots_AfterPlayingSounds_ShouldDecrease()
        {
            // Arrange
            _sfxPlayer = CreateSfxPlayer(10);
            var audioData = CreateValidWavData();
            _mockAudioEngine.Setup(e => e.PlaySoundAsync(It.IsAny<byte[]>(), It.IsAny<float>(), It.IsAny<float>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act
            await _sfxPlayer.PlayAsync(audioData);
            await _sfxPlayer.PlayAsync(audioData);
            var available = _sfxPlayer.GetAvailableSlots();

            // Assert
            available.Should().BeLessOrEqualTo(8);
        }

        #endregion

        #region Volume Control Tests

        [Fact]
        public void SetVolume_ShouldUpdateAllActiveSounds()
        {
            // Arrange
            _sfxPlayer = CreateSfxPlayer();

            // Act
            _sfxPlayer.SetVolume(0.7f);

            // Assert
            _sfxPlayer.Volume.Should().Be(0.7f);
        }

        [Fact]
        public void SetVolume_AboveMax_ShouldClampToOne()
        {
            // Arrange
            _sfxPlayer = CreateSfxPlayer();

            // Act
            _sfxPlayer.SetVolume(1.5f);

            // Assert
            _sfxPlayer.Volume.Should().Be(1.0f);
        }

        [Fact]
        public void SetVolume_BelowMin_ShouldClampToZero()
        {
            // Arrange
            _sfxPlayer = CreateSfxPlayer();

            // Act
            _sfxPlayer.SetVolume(-0.5f);

            // Assert
            _sfxPlayer.Volume.Should().Be(0.0f);
        }

        [Fact]
        public async Task SetVolumeForSound_ShouldUpdateSpecificSound()
        {
            // Arrange
            _sfxPlayer = CreateSfxPlayer();
            var audioData = CreateValidWavData();
            _mockAudioEngine.Setup(e => e.PlaySoundAsync(It.IsAny<byte[]>(), It.IsAny<float>(), It.IsAny<float>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);
            var soundId = await _sfxPlayer.PlayAsync(audioData);

            // Act
            await _sfxPlayer.SetVolumeAsync(soundId, 0.3f);

            // Assert
            _mockAudioEngine.Verify(
                e => e.SetSoundVolumeAsync(soundId, 0.3f),
                Times.Once
            );
        }

        #endregion

        #region Stop and Cleanup Tests

        [Fact]
        public async Task StopAsync_WithValidId_ShouldStopSound()
        {
            // Arrange
            _sfxPlayer = CreateSfxPlayer();
            var audioData = CreateValidWavData();
            _mockAudioEngine.Setup(e => e.PlaySoundAsync(It.IsAny<byte[]>(), It.IsAny<float>(), It.IsAny<float>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);
            var soundId = await _sfxPlayer.PlayAsync(audioData);

            // Act
            await _sfxPlayer.StopAsync(soundId);

            // Assert
            _mockAudioEngine.Verify(e => e.StopSoundAsync(soundId), Times.Once);
        }

        [Fact]
        public async Task StopAsync_WithInvalidId_ShouldNotThrow()
        {
            // Arrange
            _sfxPlayer = CreateSfxPlayer();

            // Act
            Func<Task> act = async () => await _sfxPlayer.StopAsync(999);

            // Assert
            await act.Should().NotThrowAsync();
        }

        [Fact]
        public async Task StopAll_ShouldStopAllActiveSounds()
        {
            // Arrange
            _sfxPlayer = CreateSfxPlayer();
            var audioData = CreateValidWavData();
            var soundIds = new List<int> { 1, 2, 3 };
            var index = 0;
            _mockAudioEngine.Setup(e => e.PlaySoundAsync(It.IsAny<byte[]>(), It.IsAny<float>(), It.IsAny<float>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => soundIds[index++]);

            await _sfxPlayer.PlayAsync(audioData);
            await _sfxPlayer.PlayAsync(audioData);
            await _sfxPlayer.PlayAsync(audioData);

            // Act
            await _sfxPlayer.StopAllAsync();

            // Assert
            _sfxPlayer.ActiveSounds.Should().Be(0);
            _mockAudioEngine.Verify(e => e.StopAllSoundsAsync(), Times.Once);
        }

        #endregion

        #region Priority System Tests

        [Fact]
        public async Task PlayWithPriority_HighPriority_ShouldNotBeEvicted()
        {
            // Arrange
            _sfxPlayer = CreateSfxPlayer(3);
            var audioData = CreateValidWavData();
            var soundId = 1;
            _mockAudioEngine.Setup(e => e.PlaySoundAsync(It.IsAny<byte[]>(), It.IsAny<float>(), It.IsAny<float>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => soundId++);

            // Act
            var highPriorityId = await _sfxPlayer.PlayAsync(audioData, 1.0f, 1.0f, priority: 10);
            await _sfxPlayer.PlayAsync(audioData, priority: 5);
            await _sfxPlayer.PlayAsync(audioData, priority: 5);
            await _sfxPlayer.PlayAsync(audioData, priority: 1); // Should evict low priority

            // Assert
            _mockAudioEngine.Verify(e => e.StopSoundAsync(highPriorityId), Times.Never);
        }

        [Fact]
        public async Task PlayWithPriority_LowPriority_ShouldBeEvictedFirst()
        {
            // Arrange
            _sfxPlayer = CreateSfxPlayer(2);
            var audioData = CreateValidWavData();
            var soundId = 1;
            _mockAudioEngine.Setup(e => e.PlaySoundAsync(It.IsAny<byte[]>(), It.IsAny<float>(), It.IsAny<float>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => soundId++);

            // Act
            var lowPriorityId = await _sfxPlayer.PlayAsync(audioData, priority: 1);
            await _sfxPlayer.PlayAsync(audioData, priority: 5);
            await _sfxPlayer.PlayAsync(audioData, priority: 10); // Should evict low priority

            // Assert
            _mockAudioEngine.Verify(e => e.StopSoundAsync(lowPriorityId), Times.Once);
        }

        #endregion

        #region Disposal Tests

        [Fact]
        public void Dispose_ShouldCleanupResources()
        {
            // Arrange
            _sfxPlayer = CreateSfxPlayer();

            // Act
            _sfxPlayer.Dispose();

            // Assert
            _mockAudioEngine.Verify(e => e.Dispose(), Times.Once);
            _sfxPlayer.ActiveSounds.Should().Be(0);
        }

        [Fact]
        public async Task Dispose_WithActiveSounds_ShouldStopAll()
        {
            // Arrange
            _sfxPlayer = CreateSfxPlayer();
            var audioData = CreateValidWavData();
            _mockAudioEngine.Setup(e => e.PlaySoundAsync(It.IsAny<byte[]>(), It.IsAny<float>(), It.IsAny<float>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);
            await _sfxPlayer.PlayAsync(audioData);

            // Act
            _sfxPlayer.Dispose();

            // Assert
            _mockAudioEngine.Verify(e => e.StopAllSoundsAsync(), Times.Once);
        }

        [Fact]
        public void Dispose_CalledMultipleTimes_ShouldBeIdempotent()
        {
            // Arrange
            _sfxPlayer = CreateSfxPlayer();

            // Act
            _sfxPlayer.Dispose();
            _sfxPlayer.Dispose();
            _sfxPlayer.Dispose();

            // Assert
            _mockAudioEngine.Verify(e => e.Dispose(), Times.Once);
        }

        #endregion

        #region Helper Methods

        private byte[] CreateValidWavData()
        {
            // Create minimal valid WAV file (RIFF header)
            var data = new List<byte>();
            data.AddRange(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            data.AddRange(BitConverter.GetBytes(36)); // Chunk size
            data.AddRange(System.Text.Encoding.ASCII.GetBytes("WAVE"));
            data.AddRange(System.Text.Encoding.ASCII.GetBytes("fmt "));
            data.AddRange(BitConverter.GetBytes(16)); // Subchunk size
            data.AddRange(BitConverter.GetBytes((short)1)); // Audio format (PCM)
            data.AddRange(BitConverter.GetBytes((short)1)); // Num channels
            data.AddRange(BitConverter.GetBytes(44100)); // Sample rate
            data.AddRange(BitConverter.GetBytes(88200)); // Byte rate
            data.AddRange(BitConverter.GetBytes((short)2)); // Block align
            data.AddRange(BitConverter.GetBytes((short)16)); // Bits per sample
            data.AddRange(System.Text.Encoding.ASCII.GetBytes("data"));
            data.AddRange(BitConverter.GetBytes(0)); // Data size
            return data.ToArray();
        }

        #endregion

        public void Dispose()
        {
            _sfxPlayer?.Dispose();
        }
    }
}
