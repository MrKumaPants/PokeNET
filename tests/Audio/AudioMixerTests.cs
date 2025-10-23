using Xunit;
using Moq;
using FluentAssertions;
using PokeNET.Audio.Mixing;
using PokeNET.Audio.Services;
using PokeNET.Audio.Abstractions;
using PokeNET.Audio.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace PokeNET.Tests.Audio
{
    /// <summary>
    /// Comprehensive tests for AudioMixer
    /// Tests volume control, ducking, and channel management
    /// </summary>
    public class AudioMixerTests : IDisposable
    {
        private readonly Mock<ILogger<AudioMixer>> _mockLogger;
        private AudioMixer? _mixer;

        public AudioMixerTests()
        {
            _mockLogger = new Mock<ILogger<AudioMixer>>();
        }

        private AudioMixer CreateMixer()
        {
            return new AudioMixer(_mockLogger.Object);
        }

        #region Initialization Tests

        [Fact]
        public void Constructor_ShouldInitializeDefaultChannels()
        {
            // Arrange & Act
            _mixer = CreateMixer();

            // Assert
            _mixer.Should().NotBeNull();
            _mixer.Channels.Should().HaveCount(5); // Master, Music, SFX, Voice, Ambient
            _mixer.GetChannel(ChannelType.Master).Should().NotBeNull();
            _mixer.GetChannel(ChannelType.Music).Should().NotBeNull();
            _mixer.GetChannel(ChannelType.SoundEffects).Should().NotBeNull();
        }

        [Fact]
        public void GetChannel_WithValidType_ShouldReturnChannel()
        {
            // Arrange
            _mixer = CreateMixer();

            // Act
            var musicChannel = _mixer.GetChannel(ChannelType.Music);

            // Assert
            musicChannel.Should().NotBeNull();
            musicChannel.Type.Should().Be(ChannelType.Music);
        }

        [Fact]
        public void GetChannel_WithInvalidType_ShouldThrowArgumentException()
        {
            // Arrange
            _mixer = CreateMixer();

            // Act
            Action act = () => _mixer.GetChannel((ChannelType)999);

            // Assert
            act.Should().Throw<ArgumentException>();
        }

        #endregion

        #region Volume Control Tests

        [Fact]
        public void SetMasterVolume_ShouldAffectAllChannels()
        {
            // Arrange
            _mixer = CreateMixer();
            _mixer.SetChannelVolume(ChannelType.Music, 0.8f);
            _mixer.SetChannelVolume(ChannelType.SoundEffects, 0.6f);

            // Act
            _mixer.SetMasterVolume(0.5f);

            // Assert
            var masterChannel = _mixer.GetChannel(ChannelType.Master);
            masterChannel.Volume.Should().Be(0.5f);

            // Effective volumes should be scaled by master
            _mixer.GetEffectiveVolume(ChannelType.Music).Should().BeApproximately(0.4f, 0.01f);
            _mixer.GetEffectiveVolume(ChannelType.SoundEffects).Should().BeApproximately(0.3f, 0.01f);
        }

        [Fact]
        public void SetChannelVolume_ShouldUpdateSpecificChannel()
        {
            // Arrange
            _mixer = CreateMixer();

            // Act
            _mixer.SetChannelVolume(ChannelType.Music, 0.7f);

            // Assert
            var musicChannel = _mixer.GetChannel(ChannelType.Music);
            musicChannel.Volume.Should().Be(0.7f);
        }

        [Fact]
        public void SetChannelVolume_AboveMax_ShouldClampToOne()
        {
            // Arrange
            _mixer = CreateMixer();

            // Act
            _mixer.SetChannelVolume(ChannelType.Music, 1.5f);

            // Assert
            var musicChannel = _mixer.GetChannel(ChannelType.Music);
            musicChannel.Volume.Should().Be(1.0f);
        }

        [Fact]
        public void SetChannelVolume_BelowMin_ShouldClampToZero()
        {
            // Arrange
            _mixer = CreateMixer();

            // Act
            _mixer.SetChannelVolume(ChannelType.Music, -0.5f);

            // Assert
            var musicChannel = _mixer.GetChannel(ChannelType.Music);
            musicChannel.Volume.Should().Be(0.0f);
        }

        [Fact]
        public void GetEffectiveVolume_ShouldMultiplyMasterAndChannel()
        {
            // Arrange
            _mixer = CreateMixer();
            _mixer.SetMasterVolume(0.8f);
            _mixer.SetChannelVolume(ChannelType.Music, 0.5f);

            // Act
            var effectiveVolume = _mixer.GetEffectiveVolume(ChannelType.Music);

            // Assert
            effectiveVolume.Should().BeApproximately(0.4f, 0.01f);
        }

        #endregion

        #region Mute Control Tests

        [Fact]
        public void MuteChannel_ShouldSetEffectiveVolumeToZero()
        {
            // Arrange
            _mixer = CreateMixer();
            _mixer.SetChannelVolume(ChannelType.Music, 0.8f);

            // Act
            _mixer.MuteChannel(ChannelType.Music);

            // Assert
            var musicChannel = _mixer.GetChannel(ChannelType.Music);
            musicChannel.IsMuted.Should().BeTrue();
            musicChannel.EffectiveVolume.Should().Be(0.0f);
        }

        [Fact]
        public void UnmuteChannel_ShouldRestoreVolume()
        {
            // Arrange
            _mixer = CreateMixer();
            _mixer.SetChannelVolume(ChannelType.Music, 0.8f);
            _mixer.MuteChannel(ChannelType.Music);

            // Act
            _mixer.UnmuteChannel(ChannelType.Music);

            // Assert
            var musicChannel = _mixer.GetChannel(ChannelType.Music);
            musicChannel.IsMuted.Should().BeFalse();
            musicChannel.Volume.Should().Be(0.8f);
        }

        [Fact]
        public void ToggleMute_ShouldSwitchMuteState()
        {
            // Arrange
            _mixer = CreateMixer();
            var initialState = _mixer.GetChannel(ChannelType.Music).IsMuted;

            // Act
            _mixer.ToggleMute(ChannelType.Music);
            var afterToggle = _mixer.GetChannel(ChannelType.Music).IsMuted;
            _mixer.ToggleMute(ChannelType.Music);
            var afterSecondToggle = _mixer.GetChannel(ChannelType.Music).IsMuted;

            // Assert
            afterToggle.Should().Be(!initialState);
            afterSecondToggle.Should().Be(initialState);
        }

        [Fact]
        public void MuteAll_ShouldMuteAllChannelsExceptMaster()
        {
            // Arrange
            _mixer = CreateMixer();

            // Act
            _mixer.MuteAll();

            // Assert
            _mixer.GetChannel(ChannelType.Music).IsMuted.Should().BeTrue();
            _mixer.GetChannel(ChannelType.SoundEffects).IsMuted.Should().BeTrue();
            _mixer.GetChannel(ChannelType.Voice).IsMuted.Should().BeTrue();
            _mixer.GetChannel(ChannelType.Ambient).IsMuted.Should().BeTrue();
        }

        #endregion

        #region Ducking Tests

        [Fact]
        public void SetDucking_ShouldReduceChannelVolume()
        {
            // Arrange
            _mixer = CreateMixer();
            _mixer.SetChannelVolume(ChannelType.Music, 1.0f);

            // Act
            _mixer.SetDucking(ChannelType.Music, true, 0.3f);

            // Assert
            var musicChannel = _mixer.GetChannel(ChannelType.Music);
            musicChannel.IsDucked.Should().BeTrue();
            musicChannel.DuckingLevel.Should().Be(0.3f);
        }

        [Fact]
        public void DuckMusic_WhenVoicePlays_ShouldLowerMusicVolume()
        {
            // Arrange
            _mixer = CreateMixer();
            _mixer.SetChannelVolume(ChannelType.Music, 1.0f);

            // Act
            _mixer.DuckMusic(duckLevel: 0.3f);

            // Assert
            var musicChannel = _mixer.GetChannel(ChannelType.Music);
            musicChannel.IsDucked.Should().BeTrue();
            musicChannel.EffectiveVolume.Should().BeLessThan(1.0f);
        }

        [Fact]
        public void StopDucking_ShouldRestoreOriginalVolume()
        {
            // Arrange
            _mixer = CreateMixer();
            _mixer.SetChannelVolume(ChannelType.Music, 0.8f);
            _mixer.DuckMusic(0.3f);

            // Act
            _mixer.StopDucking(ChannelType.Music);

            // Assert
            var musicChannel = _mixer.GetChannel(ChannelType.Music);
            musicChannel.IsDucked.Should().BeFalse();
            musicChannel.DuckingLevel.Should().Be(1.0f);
        }

        [Fact]
        public void Update_WithDucking_ShouldSmoothlyTransition()
        {
            // Arrange
            _mixer = CreateMixer();
            var musicChannel = _mixer.GetChannel(ChannelType.Music);
            musicChannel.SetVolumeInstant(1.0f);
            _mixer.SetDucking(ChannelType.Music, true, 0.3f);

            // Act
            _mixer.Update(0.1f); // Update with delta time

            // Assert
            musicChannel.CurrentVolume.Should().BeLessThan(musicChannel.Volume);
        }

        #endregion

        #region Fading Tests

        [Fact]
        public async Task FadeChannel_ShouldGraduallyChangeVolume()
        {
            // Arrange
            _mixer = CreateMixer();
            _mixer.SetChannelVolume(ChannelType.Music, 1.0f);

            // Act
            await _mixer.FadeChannelAsync(ChannelType.Music, 0.0f, duration: 1.0f);

            // Assert
            var musicChannel = _mixer.GetChannel(ChannelType.Music);
            musicChannel.Volume.Should().BeApproximately(0.0f, 0.1f);
        }

        [Fact]
        public async Task FadeIn_ShouldIncreaseVolumeFromZero()
        {
            // Arrange
            _mixer = CreateMixer();
            _mixer.SetChannelVolume(ChannelType.Music, 0.0f);

            // Act
            await _mixer.FadeInAsync(ChannelType.Music, targetVolume: 1.0f, duration: 1.0f);

            // Assert
            var musicChannel = _mixer.GetChannel(ChannelType.Music);
            musicChannel.Volume.Should().BeApproximately(1.0f, 0.1f);
        }

        [Fact]
        public async Task FadeOut_ShouldDecreaseVolumeToZero()
        {
            // Arrange
            _mixer = CreateMixer();
            _mixer.SetChannelVolume(ChannelType.Music, 1.0f);

            // Act
            await _mixer.FadeOutAsync(ChannelType.Music, duration: 1.0f);

            // Assert
            var musicChannel = _mixer.GetChannel(ChannelType.Music);
            musicChannel.Volume.Should().BeApproximately(0.0f, 0.1f);
        }

        [Fact]
        public void FadeTo_ShouldSetTargetVolume()
        {
            // Arrange
            _mixer = CreateMixer();
            var musicChannel = _mixer.GetChannel(ChannelType.Music);
            musicChannel.SetVolumeInstant(0.0f);

            // Act
            musicChannel.FadeTo(0.8f, fadeSpeed: 5.0f);
            _mixer.Update(0.5f); // Simulate time passing

            // Assert
            musicChannel.CurrentVolume.Should().BeGreaterThan(0.0f);
        }

        #endregion

        #region Update Tests

        [Fact]
        public void Update_ShouldUpdateAllChannels()
        {
            // Arrange
            _mixer = CreateMixer();
            var musicChannel = _mixer.GetChannel(ChannelType.Music);
            musicChannel.SetVolumeInstant(0.0f);
            musicChannel.FadeTo(1.0f, fadeSpeed: 2.0f);

            // Act
            _mixer.Update(0.5f);

            // Assert
            musicChannel.CurrentVolume.Should().BeGreaterThan(0.0f);
        }

        [Fact]
        public void Update_WithMultipleChannelsFading_ShouldUpdateAll()
        {
            // Arrange
            _mixer = CreateMixer();
            var musicChannel = _mixer.GetChannel(ChannelType.Music);
            var sfxChannel = _mixer.GetChannel(ChannelType.SoundEffects);

            musicChannel.SetVolumeInstant(0.0f);
            sfxChannel.SetVolumeInstant(1.0f);

            musicChannel.FadeTo(1.0f);
            sfxChannel.FadeTo(0.0f);

            // Act
            _mixer.Update(0.1f);

            // Assert
            musicChannel.CurrentVolume.Should().BeGreaterThan(0.0f);
            sfxChannel.CurrentVolume.Should().BeLessThan(1.0f);
        }

        #endregion

        #region Configuration Tests

        [Fact]
        public void SaveConfiguration_ShouldCaptureAllChannelStates()
        {
            // Arrange
            _mixer = CreateMixer();
            _mixer.SetChannelVolume(ChannelType.Music, 0.7f);
            _mixer.SetChannelVolume(ChannelType.SoundEffects, 0.5f);
            _mixer.MuteChannel(ChannelType.Voice);

            // Act
            var config = _mixer.SaveConfiguration();

            // Assert
            config.Should().NotBeNull();
            config.Channels.Should().HaveCount(5);
            config.Channels.First(c => c.Type == ChannelType.Music).Volume.Should().Be(0.7f);
            config.Channels.First(c => c.Type == ChannelType.Voice).IsMuted.Should().BeTrue();
        }

        [Fact]
        public void LoadConfiguration_ShouldRestoreChannelStates()
        {
            // Arrange
            _mixer = CreateMixer();
            var config = new MixerConfiguration
            {
                Channels = new[]
                {
                    new ChannelConfig { Type = ChannelType.Music, Name = "Music", Volume = 0.6f, IsMuted = false },
                    new ChannelConfig { Type = ChannelType.SoundEffects, Name = "SFX", Volume = 0.4f, IsMuted = true }
                }
            };

            // Act
            _mixer.LoadConfiguration(config);

            // Assert
            _mixer.GetChannel(ChannelType.Music).Volume.Should().Be(0.6f);
            _mixer.GetChannel(ChannelType.SoundEffects).Volume.Should().Be(0.4f);
            _mixer.GetChannel(ChannelType.SoundEffects).IsMuted.Should().BeTrue();
        }

        #endregion

        #region Event Tests

        [Fact]
        public void VolumeChanged_ShouldTriggerEvent()
        {
            // Arrange
            _mixer = CreateMixer();
            var musicChannel = _mixer.GetChannel(ChannelType.Music);
            var eventTriggered = false;
            float eventVolume = 0;

            musicChannel.OnVolumeChanged += (sender, volume) =>
            {
                eventTriggered = true;
                eventVolume = volume;
            };

            // Act
            musicChannel.Volume = 0.5f;

            // Assert
            eventTriggered.Should().BeTrue();
            eventVolume.Should().Be(0.5f);
        }

        [Fact]
        public void MuteChanged_ShouldTriggerEvent()
        {
            // Arrange
            _mixer = CreateMixer();
            var musicChannel = _mixer.GetChannel(ChannelType.Music);
            var eventTriggered = false;
            bool eventMuted = false;

            musicChannel.OnMuteChanged += (sender, isMuted) =>
            {
                eventTriggered = true;
                eventMuted = isMuted;
            };

            // Act
            musicChannel.IsMuted = true;

            // Assert
            eventTriggered.Should().BeTrue();
            eventMuted.Should().BeTrue();
        }

        #endregion

        #region Reset Tests

        [Fact]
        public void ResetChannel_ShouldRestoreDefaults()
        {
            // Arrange
            _mixer = CreateMixer();
            var musicChannel = _mixer.GetChannel(ChannelType.Music);
            musicChannel.Volume = 0.3f;
            musicChannel.IsMuted = true;
            musicChannel.SetDucking(true, 0.5f);

            // Act
            musicChannel.Reset();

            // Assert
            musicChannel.Volume.Should().Be(1.0f);
            musicChannel.IsMuted.Should().BeFalse();
            musicChannel.IsDucked.Should().BeFalse();
            musicChannel.DuckingLevel.Should().Be(1.0f);
        }

        [Fact]
        public void ResetAll_ShouldResetAllChannels()
        {
            // Arrange
            _mixer = CreateMixer();
            _mixer.SetChannelVolume(ChannelType.Music, 0.5f);
            _mixer.SetChannelVolume(ChannelType.SoundEffects, 0.3f);
            _mixer.MuteChannel(ChannelType.Voice);

            // Act
            _mixer.ResetAll();

            // Assert
            foreach (var channel in _mixer.Channels.Values)
            {
                if (channel.Type != ChannelType.Master)
                {
                    channel.Volume.Should().Be(1.0f);
                    channel.IsMuted.Should().BeFalse();
                }
            }
        }

        #endregion

        public void Dispose()
        {
            _mixer?.Dispose();
        }
    }
}
