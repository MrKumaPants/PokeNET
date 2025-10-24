using Xunit;
using FluentAssertions;
using PokeNET.Audio.Services;
using PokeNET.Audio.Abstractions;
using PokeNET.Audio.Mixing;
using PokeNET.Audio.Models;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Reflection;
using System.Linq;
using System.Threading.Tasks;
using Melanchall.DryWetMidi.Multimedia;

namespace PokeNET.Tests
{
    /// <summary>
    /// Regression tests to ensure architecture refactorings haven't broken existing functionality.
    /// These tests verify backwards compatibility and that public APIs remain unchanged.
    /// </summary>
    /// <remarks>
    /// Focus areas:
    /// - Public API surface area unchanged
    /// - Existing behavior preserved
    /// - Performance not degraded
    /// - No breaking changes to method signatures
    /// </remarks>
    public class ArchitectureRegressionTests
    {
        #region Public API Verification

        [Theory]
        [InlineData(typeof(IMusicPlayer))]
        [InlineData(typeof(IAudioMixer))]
        [InlineData(typeof(ISoundEffectPlayer))]
        [InlineData(typeof(IAudioConfiguration))]
        public void PublicInterface_ShouldNotHaveRemovedMethods(Type interfaceType)
        {
            // Arrange - Define expected minimum method counts based on original design
            var minimumMethodCounts = new Dictionary<Type, int>
            {
                { typeof(IMusicPlayer), 10 },        // Play, Stop, Pause, Resume, etc.
                { typeof(IAudioMixer), 8 },          // SetVolume, Mute, Duck, etc.
                { typeof(ISoundEffectPlayer), 5 },   // Play, Stop, SetVolume, etc.
                { typeof(IAudioConfiguration), 3 }   // Get/Set methods
            };

            // Act
            var methods = interfaceType.GetMethods(BindingFlags.Public | BindingFlags.Instance);
            var methodCount = methods.Length;

            // Assert
            methodCount.Should().BeGreaterOrEqualTo(minimumMethodCounts[interfaceType],
                $"{interfaceType.Name} should maintain at least the original method count");
        }

        [Fact]
        public void MusicPlayer_ShouldPreservePublicProperties()
        {
            // Arrange
            var expectedProperties = new[]
            {
                "IsPlaying",
                "IsPaused",
                "Volume",
                "IsLooping",
                "Tempo",
                "IsInitialized",
                "CurrentTrack"
            };

            var musicPlayerType = typeof(MusicPlayer);

            // Act
            var properties = musicPlayerType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(p => p.Name)
                .ToList();

            // Assert
            foreach (var expectedProp in expectedProperties)
            {
                properties.Should().Contain(expectedProp,
                    $"MusicPlayer should still have {expectedProp} property for backwards compatibility");
            }
        }

        [Fact]
        public void AudioMixer_ShouldPreserveChannelTypes()
        {
            // Arrange
            var expectedChannels = new[]
            {
                ChannelType.Master,
                ChannelType.Music,
                ChannelType.SoundEffects,
                ChannelType.Voice,
                ChannelType.Ambient
            };

            // Act - Create mixer and verify all channel types exist
            var mixer = new AudioMixer(Mock.Of<ILogger<AudioMixer>>());

            // Assert
            foreach (var channelType in expectedChannels)
            {
                Action act = () => mixer.GetChannel(channelType);
                act.Should().NotThrow($"AudioMixer should support {channelType} channel");
            }
        }

        [Fact]
        public void AudioManager_ShouldMaintainConstructorSignature()
        {
            // Arrange
            var constructors = typeof(AudioManager).GetConstructors(BindingFlags.Public | BindingFlags.Instance);

            // Act
            var primaryConstructor = constructors.FirstOrDefault(c => c.GetParameters().Length >= 3);

            // Assert
            primaryConstructor.Should().NotBeNull("AudioManager should have a constructor with at least 3 parameters");

            var parameters = primaryConstructor!.GetParameters();
            parameters.Should().Contain(p => p.ParameterType == typeof(ILogger<AudioManager>),
                "Constructor should accept ILogger<AudioManager>");
            parameters.Should().Contain(p => p.ParameterType == typeof(IAudioCache),
                "Constructor should accept IAudioCache");
        }

        #endregion

        #region Behavioral Regression Tests

        [Fact]
        public async Task MusicPlayer_PlayPauseResume_ShouldWorkAsExpected()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<MusicPlayer>>();
            var mockOutputDevice = new Mock<IOutputDevice>();
            var player = new MusicPlayer(mockLogger.Object, mockOutputDevice.Object);

            var track = new AudioTrack
            {
                FilePath = "test.mid",
                Duration = TimeSpan.FromSeconds(10)
            };

            await player.LoadAsync(track);

            // Act & Assert - Original behavior should be preserved
            await player.PlayAsync();
            player.IsPlaying.Should().BeTrue();
            player.IsPaused.Should().BeFalse();

            await player.PauseAsync();
            player.IsPlaying.Should().BeFalse();
            player.IsPaused.Should().BeTrue();

            await player.ResumeAsync();
            player.IsPlaying.Should().BeTrue();
            player.IsPaused.Should().BeFalse();

            await player.StopAsync();
            player.IsPlaying.Should().BeFalse();
            player.GetPosition().Should().Be(TimeSpan.Zero);
        }

        [Fact]
        public void AudioMixer_VolumeCalculation_ShouldUseCorrectFormula()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<AudioMixer>>();
            var mixer = new AudioMixer(mockLogger.Object);

            // Act
            mixer.SetMasterVolume(0.8f);
            mixer.SetChannelVolume(ChannelType.Music, 0.5f);

            var effectiveVolume = mixer.GetEffectiveVolume(ChannelType.Music);

            // Assert - Formula: effective = master * channel
            effectiveVolume.Should().BeApproximately(0.4f, 0.01f,
                "Effective volume calculation formula should remain: master * channel");
        }

        [Fact]
        public void AudioMixer_MuteChannel_ShouldZeroEffectiveVolume()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<AudioMixer>>();
            var mixer = new AudioMixer(mockLogger.Object);
            mixer.SetChannelVolume(ChannelType.Music, 0.8f);

            // Act
            mixer.MuteChannel(ChannelType.Music);

            // Assert
            var channel = mixer.GetChannel(ChannelType.Music);
            channel.IsMuted.Should().BeTrue();
            channel.EffectiveVolume.Should().Be(0.0f,
                "Muted channels should have zero effective volume");
        }

        [Fact]
        public void AudioMixer_UnmuteChannel_ShouldRestoreOriginalVolume()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<AudioMixer>>();
            var mixer = new AudioMixer(mockLogger.Object);
            mixer.SetChannelVolume(ChannelType.Music, 0.8f);
            mixer.MuteChannel(ChannelType.Music);

            // Act
            mixer.UnmuteChannel(ChannelType.Music);

            // Assert
            var channel = mixer.GetChannel(ChannelType.Music);
            channel.IsMuted.Should().BeFalse();
            channel.Volume.Should().Be(0.8f,
                "Unmuting should restore the original volume value");
        }

        [Fact]
        public void MusicPlayer_VolumeClamp_ShouldEnforceRange()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<MusicPlayer>>();
            var mockOutputDevice = new Mock<IOutputDevice>();
            var player = new MusicPlayer(mockLogger.Object, mockOutputDevice.Object);

            // Act & Assert - Above maximum
            player.SetVolume(1.5f);
            player.Volume.Should().Be(1.0f, "Volume should clamp to maximum of 1.0");

            // Act & Assert - Below minimum
            player.SetVolume(-0.5f);
            player.Volume.Should().Be(0.0f, "Volume should clamp to minimum of 0.0");

            // Act & Assert - Valid range
            player.SetVolume(0.7f);
            player.Volume.Should().Be(0.7f, "Valid volumes should be accepted");
        }

        [Fact]
        public async Task MusicPlayer_StopAndPlay_ShouldResetPosition()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<MusicPlayer>>();
            var mockOutputDevice = new Mock<IOutputDevice>();
            var player = new MusicPlayer(mockLogger.Object, mockOutputDevice.Object);

            var track = new AudioTrack
            {
                FilePath = "test.mid",
                Duration = TimeSpan.FromSeconds(10)
            };

            await player.LoadAsync(track);
            await player.PlayAsync();

            // Simulate playback
            await Task.Delay(100);

            // Act
            await player.StopAsync();

            // Assert
            player.GetPosition().Should().Be(TimeSpan.Zero,
                "Stop should reset playback position to zero");
        }

        #endregion

        #region Performance Regression Tests

        [Fact]
        public void AudioMixer_UpdatePerformance_ShouldNotRegress()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<AudioMixer>>();
            var mixer = new AudioMixer(mockLogger.Object);

            // Setup fading on multiple channels
            mixer.GetChannel(ChannelType.Music).FadeTo(1.0f);
            mixer.GetChannel(ChannelType.SoundEffects).FadeTo(1.0f);
            mixer.GetChannel(ChannelType.Voice).FadeTo(1.0f);

            var iterations = 1000;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Act
            for (int i = 0; i < iterations; i++)
            {
                mixer.Update(0.016f); // 60 FPS
            }

            stopwatch.Stop();

            // Assert - Performance baseline: 1000 updates should take < 100ms
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(100,
                "Mixer update performance should not regress (target: 1000 updates < 100ms)");
        }

        [Fact]
        public async Task MusicPlayer_LoadTime_ShouldNotRegress()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<MusicPlayer>>();
            var mockOutputDevice = new Mock<IOutputDevice>();
            var player = new MusicPlayer(mockLogger.Object, mockOutputDevice.Object);

            var track = new AudioTrack
            {
                FilePath = "test.mid",
                Duration = TimeSpan.FromSeconds(10)
            };

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Act
            await player.LoadAsync(track);

            stopwatch.Stop();

            // Assert - Loading should be fast (<50ms for small MIDI)
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(50,
                "MIDI loading time should not regress (target: <50ms for small files)");
        }

        [Fact]
        public void AudioMixer_ChannelCreation_ShouldBeEfficient()
        {
            // Arrange & Act
            var mockLogger = new Mock<ILogger<AudioMixer>>();
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            var mixer = new AudioMixer(mockLogger.Object);

            stopwatch.Stop();

            // Assert - Mixer creation with all channels should be instant
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(10,
                "AudioMixer creation should be nearly instant");
            mixer.Channels.Should().HaveCount(5, "Should create all 5 default channels");
        }

        #endregion

        #region Data Integrity Tests

        [Fact]
        public void AudioMixer_SaveAndLoadConfiguration_ShouldPreserveState()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<AudioMixer>>();
            var mixer = new AudioMixer(mockLogger.Object);

            mixer.SetMasterVolume(0.9f);
            mixer.SetChannelVolume(ChannelType.Music, 0.7f);
            mixer.SetChannelVolume(ChannelType.SoundEffects, 0.5f);
            mixer.MuteChannel(ChannelType.Voice);

            // Act - Save configuration
            var config = mixer.SaveConfiguration();

            // Create new mixer and load configuration
            var newMixer = new AudioMixer(mockLogger.Object);
            newMixer.LoadConfiguration(config);

            // Assert - All settings should be preserved
            newMixer.GetChannel(ChannelType.Master).Volume.Should().Be(0.9f);
            newMixer.GetChannel(ChannelType.Music).Volume.Should().Be(0.7f);
            newMixer.GetChannel(ChannelType.SoundEffects).Volume.Should().Be(0.5f);
            newMixer.GetChannel(ChannelType.Voice).IsMuted.Should().BeTrue();
        }

        [Fact]
        public async Task MusicPlayer_LoadUnload_ShouldPreserveSettings()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<MusicPlayer>>();
            var mockOutputDevice = new Mock<IOutputDevice>();
            var player = new MusicPlayer(mockLogger.Object, mockOutputDevice.Object);

            player.SetVolume(0.6f);
            player.SetLoop(true);
            player.SetTempo(1.2f);

            var track1 = new AudioTrack
            {
                FilePath = "track1.mid",
                Duration = TimeSpan.FromSeconds(10)
            };

            var track2 = new AudioTrack
            {
                FilePath = "track2.mid",
                Duration = TimeSpan.FromSeconds(15)
            };

            // Act - Load different tracks
            await player.LoadAsync(track1);
            await player.LoadAsync(track2);

            // Assert - Settings should persist across track loads
            player.Volume.Should().Be(0.6f);
            player.IsLooping.Should().BeTrue();
            player.Tempo.Should().Be(1.2f);
        }

        #endregion

        #region Error Handling Regression Tests

        [Fact]
        public async Task MusicPlayer_PlayWithoutLoad_ShouldThrowExpectedException()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<MusicPlayer>>();
            var mockOutputDevice = new Mock<IOutputDevice>();
            var player = new MusicPlayer(mockLogger.Object, mockOutputDevice.Object);

            // Act
            Func<Task> act = async () => await player.PlayAsync();

            // Assert - Should throw InvalidOperationException (not changed)
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*No MIDI*");
        }

        [Fact]
        public async Task MusicPlayer_LoadInvalidMidi_ShouldThrowExpectedException()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<MusicPlayer>>();
            var mockOutputDevice = new Mock<IOutputDevice>();
            var player = new MusicPlayer(mockLogger.Object, mockOutputDevice.Object);

            var invalidTrack = new AudioTrack
            {
                FilePath = "invalid.mid"
            };

            // Act
            Func<Task> act = async () => await player.LoadAsync(invalidTrack);

            // Assert - Should throw appropriate exception
            await act.Should().ThrowAsync<Exception>();
        }

        [Fact]
        public void AudioMixer_GetInvalidChannel_ShouldThrowExpectedException()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<AudioMixer>>();
            var mixer = new AudioMixer(mockLogger.Object);

            // Act
            Action act = () => mixer.GetChannel((ChannelType)999);

            // Assert - Should throw ArgumentException
            act.Should().Throw<ArgumentException>();
        }

        #endregion

        #region Helper Methods

        private byte[] CreateMinimalMidiData()
        {
            // Create minimal valid MIDI file for testing
            return new byte[]
            {
                0x4D, 0x54, 0x68, 0x64, // "MThd" header
                0x00, 0x00, 0x00, 0x06, // Header length
                0x00, 0x00,             // Format type 0
                0x00, 0x01,             // One track
                0x00, 0x60,             // 96 ticks per quarter note
                0x4D, 0x54, 0x72, 0x6B, // "MTrk" track header
                0x00, 0x00, 0x00, 0x04, // Track length
                0x00, 0xFF, 0x2F, 0x00  // End of track
            };
        }

        #endregion
    }
}
