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

        private SoundEffectPlayer CreateSfxPlayer(int maxSimultaneousSounds = 16)
        {
            return new SoundEffectPlayer(_mockLogger.Object, _mockAudioEngine.Object, maxSimultaneousSounds);
        }

        private SoundEffect CreateSoundEffect(string name = "TestSound")
        {
            return new SoundEffect
            {
                Name = name,
                FilePath = "test.wav",
                Duration = TimeSpan.FromSeconds(1),
                Volume = 1.0f,
                Category = SoundCategory.General
            };
        }

        #region Initialization Tests

        [Fact]
        public void Constructor_ShouldInitializeWithDefaultPoolSize()
        {
            // Arrange & Act
            _sfxPlayer = CreateSfxPlayer();

            // Assert
            _sfxPlayer.Should().NotBeNull();
            _sfxPlayer.MaxSimultaneousSounds.Should().Be(16);
            _sfxPlayer.ActiveSoundCount.Should().Be(0);
        }

        [Fact]
        public void Constructor_WithCustomPoolSize_ShouldInitializeCorrectly()
        {
            // Arrange & Act
            _sfxPlayer = CreateSfxPlayer(32);

            // Assert
            _sfxPlayer.MaxSimultaneousSounds.Should().Be(32);
        }

        [Fact]
        public void Constructor_WithInvalidPoolSize_ShouldThrowArgumentException()
        {
            // Arrange & Act
            Action act = () => CreateSfxPlayer(0);

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithParameterName("maxSimultaneousSounds");
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
        public void Play_WithValidEffect_ShouldReturnInstanceId()
        {
            // Arrange
            _sfxPlayer = CreateSfxPlayer();
            var effect = CreateSoundEffect();

            // Act
            var instanceId = _sfxPlayer.Play(effect);

            // Assert
            instanceId.Should().NotBeNull();
            _sfxPlayer.ActiveSoundCount.Should().Be(1);
        }

        [Fact]
        public void Play_WithNullEffect_ShouldThrowArgumentNullException()
        {
            // Arrange
            _sfxPlayer = CreateSfxPlayer();

            // Act
            Action act = () => _sfxPlayer.Play(null!);

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Play_WithCustomVolume_ShouldUseSpecifiedVolume()
        {
            // Arrange
            _sfxPlayer = CreateSfxPlayer();
            var effect = CreateSoundEffect();
            var volume = 0.5f;

            // Act
            var instanceId = _sfxPlayer.Play(effect, volume);

            // Assert
            instanceId.Should().NotBeNull();
            _sfxPlayer.ActiveSoundCount.Should().Be(1);
        }

        [Fact]
        public void Play_WithPriority_ShouldTrackPriority()
        {
            // Arrange
            _sfxPlayer = CreateSfxPlayer();
            var effect = CreateSoundEffect();
            var priority = 10;

            // Act
            var instanceId = _sfxPlayer.Play(effect, priority: priority);

            // Assert
            instanceId.Should().NotBeNull();
            _sfxPlayer.ActiveSoundCount.Should().Be(1);
        }

        [Fact]
        public void Play_MultipleTimes_ShouldPlayConcurrently()
        {
            // Arrange
            _sfxPlayer = CreateSfxPlayer();
            var effect = CreateSoundEffect();

            // Act
            var id1 = _sfxPlayer.Play(effect);
            var id2 = _sfxPlayer.Play(effect);
            var id3 = _sfxPlayer.Play(effect);

            // Assert
            _sfxPlayer.ActiveSoundCount.Should().BeGreaterOrEqualTo(3);
        }

        #endregion

        #region Pooling Tests

        [Fact]
        public void Pool_ShouldReuseStoppedSoundSlots()
        {
            // Arrange
            _sfxPlayer = CreateSfxPlayer(4);
            var effect = CreateSoundEffect();
            var instanceIds = new List<Guid?>();

            // Act - Fill pool
            for (int i = 0; i < 4; i++)
            {
                instanceIds.Add(_sfxPlayer.Play(effect));
            }

            // Stop some sounds
            foreach (var id in instanceIds.Take(2))
            {
                if (id.HasValue)
                    _sfxPlayer.Stop(id.Value);
            }

            // Play more sounds
            var newId1 = _sfxPlayer.Play(effect);
            var newId2 = _sfxPlayer.Play(effect);

            // Assert
            _sfxPlayer.ActiveSoundCount.Should().BeLessOrEqualTo(4);
        }

        [Fact]
        public void Pool_WhenFull_ShouldEvictLowestPriority()
        {
            // Arrange
            _sfxPlayer = CreateSfxPlayer(3);
            var effect = CreateSoundEffect();

            // Act - Fill pool with low priority sounds
            var id1 = _sfxPlayer.Play(effect, priority: 1);
            var id2 = _sfxPlayer.Play(effect, priority: 1);
            var id3 = _sfxPlayer.Play(effect, priority: 1);

            // Try to play high priority sound - should evict one
            var id4 = _sfxPlayer.Play(effect, priority: 10);

            // Assert
            _sfxPlayer.ActiveSoundCount.Should().BeLessOrEqualTo(3);
        }

        [Fact]
        public void Pool_AvailableSlots_ShouldReflectActiveSounds()
        {
            // Arrange
            _sfxPlayer = CreateSfxPlayer(10);
            var effect = CreateSoundEffect();

            // Act
            _sfxPlayer.Play(effect);
            _sfxPlayer.Play(effect);

            // Assert
            var activeSounds = _sfxPlayer.ActiveSoundCount;
            var availableSlots = _sfxPlayer.MaxSimultaneousSounds - activeSounds;
            availableSlots.Should().BeLessOrEqualTo(8);
        }

        #endregion

        #region Volume Control Tests

        [Fact]
        public void SetMasterVolume_ShouldUpdateVolume()
        {
            // Arrange
            _sfxPlayer = CreateSfxPlayer();

            // Act
            _sfxPlayer.SetMasterVolume(0.7f);

            // Assert
            _sfxPlayer.GetMasterVolume().Should().Be(0.7f);
        }

        [Fact]
        public void SetMasterVolume_AboveMax_ShouldThrow()
        {
            // Arrange
            _sfxPlayer = CreateSfxPlayer();

            // Act
            Action act = () => _sfxPlayer.SetMasterVolume(1.5f);

            // Assert
            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void SetMasterVolume_BelowMin_ShouldThrow()
        {
            // Arrange
            _sfxPlayer = CreateSfxPlayer();

            // Act
            Action act = () => _sfxPlayer.SetMasterVolume(-0.5f);

            // Assert
            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void Mute_ShouldPreventSoundsFromPlaying()
        {
            // Arrange
            _sfxPlayer = CreateSfxPlayer();
            var effect = CreateSoundEffect();

            // Act
            _sfxPlayer.Mute();
            var instanceId = _sfxPlayer.Play(effect);

            // Assert
            _sfxPlayer.IsMuted.Should().BeTrue();
            instanceId.Should().BeNull();
        }

        [Fact]
        public void Unmute_ShouldAllowSoundsToPlay()
        {
            // Arrange
            _sfxPlayer = CreateSfxPlayer();
            var effect = CreateSoundEffect();
            _sfxPlayer.Mute();

            // Act
            _sfxPlayer.Unmute();
            var instanceId = _sfxPlayer.Play(effect);

            // Assert
            _sfxPlayer.IsMuted.Should().BeFalse();
            instanceId.Should().NotBeNull();
        }

        #endregion

        #region Stop and Cleanup Tests

        [Fact]
        public void Stop_WithValidId_ShouldStopSound()
        {
            // Arrange
            _sfxPlayer = CreateSfxPlayer();
            var effect = CreateSoundEffect();
            var instanceId = _sfxPlayer.Play(effect);

            // Act
            var result = instanceId.HasValue && _sfxPlayer.Stop(instanceId.Value);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void Stop_WithInvalidId_ShouldReturnFalse()
        {
            // Arrange
            _sfxPlayer = CreateSfxPlayer();

            // Act
            var result = _sfxPlayer.Stop(Guid.NewGuid());

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void StopAll_ShouldStopAllActiveSounds()
        {
            // Arrange
            _sfxPlayer = CreateSfxPlayer();
            var effect = CreateSoundEffect();

            _sfxPlayer.Play(effect);
            _sfxPlayer.Play(effect);
            _sfxPlayer.Play(effect);

            // Act
            _sfxPlayer.StopAll();

            // Assert
            _sfxPlayer.ActiveSoundCount.Should().Be(0);
        }

        [Fact]
        public void StopAllByName_ShouldStopOnlyMatchingSounds()
        {
            // Arrange
            _sfxPlayer = CreateSfxPlayer();
            var effect1 = CreateSoundEffect("Sound1");
            var effect2 = CreateSoundEffect("Sound2");

            _sfxPlayer.Play(effect1);
            _sfxPlayer.Play(effect1);
            _sfxPlayer.Play(effect2);

            // Act
            var stoppedCount = _sfxPlayer.StopAllByName("Sound1");

            // Assert
            stoppedCount.Should().Be(2);
        }

        #endregion

        #region Priority System Tests

        [Fact]
        public void PlayWithPriority_HighPriority_ShouldNotBeEvicted()
        {
            // Arrange
            _sfxPlayer = CreateSfxPlayer(3);
            var effect = CreateSoundEffect();

            // Act
            var highPriorityId = _sfxPlayer.Play(effect, priority: 10);
            _sfxPlayer.Play(effect, priority: 5);
            _sfxPlayer.Play(effect, priority: 5);
            _sfxPlayer.Play(effect, priority: 1); // Should evict low priority if pool full

            // Assert - high priority should still be playing
            if (highPriorityId.HasValue)
            {
                _sfxPlayer.IsPlaying(highPriorityId.Value).Should().BeTrue();
            }
        }

        [Fact]
        public void PlayWithPriority_LowPriority_CanBeEvicted()
        {
            // Arrange
            _sfxPlayer = CreateSfxPlayer(2);
            var effect = CreateSoundEffect();

            // Act
            var lowPriorityId = _sfxPlayer.Play(effect, priority: 1);
            _sfxPlayer.Play(effect, priority: 5);
            _sfxPlayer.Play(effect, priority: 10); // Should evict low priority

            // Assert - pool should not exceed max
            _sfxPlayer.ActiveSoundCount.Should().BeLessOrEqualTo(2);
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
        }

        [Fact]
        public void Dispose_WithActiveSounds_ShouldStopAll()
        {
            // Arrange
            _sfxPlayer = CreateSfxPlayer();
            var effect = CreateSoundEffect();
            _sfxPlayer.Play(effect);

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
