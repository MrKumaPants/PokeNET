using Xunit;
using Moq;
using FluentAssertions;
using PokeNET.Audio.Services;
using PokeNET.Audio.Abstractions;
using PokeNET.Audio.Models;
using PokeNET.Audio.Mixing;
using Microsoft.Extensions.Logging;
using Melanchall.DryWetMidi.Multimedia;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace PokeNET.Tests.Audio
{
    /// <summary>
    /// Integration tests for the complete audio system pipeline.
    /// Tests MusicPlayer + AudioMixer + AudioManager working together.
    /// </summary>
    /// <remarks>
    /// These tests verify:
    /// - End-to-end audio playback
    /// - Volume propagation across layers
    /// - Crossfade transitions
    /// - Ducking coordination
    /// - Memory management
    /// - Performance requirements
    /// </remarks>
    public class AudioIntegrationTests : IDisposable
    {
        private readonly Mock<ILogger<AudioManager>> _mockManagerLogger;
        private readonly Mock<ILogger<MusicPlayer>> _mockPlayerLogger;
        private readonly Mock<ILogger<AudioMixer>> _mockMixerLogger;
        private readonly Mock<IAudioCache> _mockCache;
        private readonly Mock<IOutputDevice> _mockOutputDevice;
        private readonly Mock<IAudioVolumeManager> _mockVolumeManager;
        private readonly Mock<IAudioStateManager> _mockStateManager;
        private readonly Mock<IAudioCacheCoordinator> _mockCacheCoordinator;
        private readonly Mock<IAmbientAudioManager> _mockAmbientManager;

        private AudioManager? _audioManager;
        private MusicPlayer? _musicPlayer;
        private AudioMixer? _audioMixer;

        public AudioIntegrationTests()
        {
            _mockManagerLogger = new Mock<ILogger<AudioManager>>();
            _mockPlayerLogger = new Mock<ILogger<MusicPlayer>>();
            _mockMixerLogger = new Mock<ILogger<AudioMixer>>();
            _mockCache = new Mock<IAudioCache>();
            _mockOutputDevice = new Mock<IOutputDevice>();
            _mockVolumeManager = new Mock<IAudioVolumeManager>();
            _mockStateManager = new Mock<IAudioStateManager>();
            _mockCacheCoordinator = new Mock<IAudioCacheCoordinator>();
            _mockAmbientManager = new Mock<IAmbientAudioManager>();

            // Setup common mock behaviors
            _mockVolumeManager.Setup(v => v.MasterVolume).Returns(1.0f);
            _mockVolumeManager.Setup(v => v.MusicVolume).Returns(1.0f);
            _mockVolumeManager.Setup(v => v.SfxVolume).Returns(1.0f);
            _mockStateManager.Setup(s => s.IsInitialized).Returns(true);
            _mockStateManager.Setup(s => s.CurrentMusicTrack).Returns(string.Empty);
        }

        #region End-to-End Playback Tests

        [Fact]
        public async Task EndToEndMusicPlayback_ShouldCoordinateAllLayers()
        {
            // Arrange
            _audioMixer = new AudioMixer(_mockMixerLogger.Object);
            _musicPlayer = new MusicPlayer(_mockPlayerLogger.Object, _mockOutputDevice.Object);
            _audioManager = new AudioManager(
                _mockManagerLogger.Object,
                _mockCache.Object,
                _musicPlayer,
                Mock.Of<ISoundEffectPlayer>(),
                _mockVolumeManager.Object,
                _mockStateManager.Object,
                _mockCacheCoordinator.Object,
                _mockAmbientManager.Object
            );

            var musicFile = "test-music.mid";
            var musicData = CreateTestMusicTrack(musicFile);
            _mockCache.Setup(c => c.GetOrLoadAsync<AudioTrack>(musicFile, It.IsAny<Func<Task<AudioTrack>>>()))
                .ReturnsAsync(musicData);

            await _audioManager.InitializeAsync();

            // Act - Set volumes at different layers
            _audioMixer.SetMasterVolume(0.8f); // Master: 80%
            _audioManager.SetMusicVolume(0.5f); // Music channel: 50%
            await _audioManager.PlayMusicAsync(musicFile, loop: true);

            // Assert - Verify volume propagation
            var effectiveVolume = _audioMixer.GetEffectiveVolume(ChannelType.Music);
            effectiveVolume.Should().BeApproximately(0.4f, 0.01f); // 0.8 * 0.5 = 0.4

            _audioManager.IsMusicPlaying.Should().BeTrue();
            _musicPlayer.IsPlaying.Should().BeTrue();
            _musicPlayer.Volume.Should().BeApproximately(0.4f, 0.01f);
        }

        [Fact]
        public async Task MusicPlayback_WithMasterMute_ShouldSilenceAllChannels()
        {
            // Arrange
            _audioMixer = new AudioMixer(_mockMixerLogger.Object);
            _musicPlayer = new MusicPlayer(_mockPlayerLogger.Object, _mockOutputDevice.Object);
            _audioManager = new AudioManager(
                _mockManagerLogger.Object,
                _mockCache.Object,
                _musicPlayer,
                Mock.Of<ISoundEffectPlayer>(),
                _mockVolumeManager.Object,
                _mockStateManager.Object,
                _mockCacheCoordinator.Object,
                _mockAmbientManager.Object
            );

            var musicFile = "test-music.mid";
            var musicData = CreateTestMusicTrack(musicFile);
            _mockCache.Setup(c => c.GetOrLoadAsync<AudioTrack>(musicFile, It.IsAny<Func<Task<AudioTrack>>>()))
                .ReturnsAsync(musicData);

            await _audioManager.PlayMusicAsync(musicFile, loop: true);

            // Act - Mute master channel
            _audioMixer.MuteChannel(ChannelType.Master);

            // Assert
            var effectiveVolume = _audioMixer.GetEffectiveVolume(ChannelType.Music);
            effectiveVolume.Should().Be(0.0f);
            _musicPlayer.Volume.Should().Be(0.0f);
        }

        #endregion

        #region Crossfade Tests

        [Fact]
        public async Task CrossfadeTransition_ShouldMaintainVolumeConsistency()
        {
            // Arrange
            _audioMixer = new AudioMixer(_mockMixerLogger.Object);
            _musicPlayer = new MusicPlayer(_mockPlayerLogger.Object, _mockOutputDevice.Object);
            _audioManager = new AudioManager(
                _mockManagerLogger.Object,
                _mockCache.Object,
                _musicPlayer,
                Mock.Of<ISoundEffectPlayer>(),
                _mockVolumeManager.Object,
                _mockStateManager.Object,
                _mockCacheCoordinator.Object,
                _mockAmbientManager.Object
            );

            var track1 = CreateTestMusicTrack("track1.mid");
            var track2 = CreateTestMusicTrack("track2.mid");

            _mockCache.Setup(c => c.GetOrLoadAsync<AudioTrack>("track1.mid", It.IsAny<Func<Task<AudioTrack>>>()))
                .ReturnsAsync(track1);
            _mockCache.Setup(c => c.GetOrLoadAsync<AudioTrack>("track2.mid", It.IsAny<Func<Task<AudioTrack>>>()))
                .ReturnsAsync(track2);

            await _audioManager.PlayMusicAsync("track1.mid", loop: true);
            _audioManager.SetMusicVolume(1.0f);

            // Act - Crossfade to second track
            await _musicPlayer.TransitionToAsync(track2, useCrossfade: true);

            // Assert - During crossfade, combined volume should approximate target
            await Task.Delay(500); // Mid-crossfade
            var musicChannel = _audioMixer.GetChannel(ChannelType.Music);
            musicChannel.Volume.Should().BeInRange(0.8f, 1.2f); // Some tolerance for timing

            // Wait for crossfade completion
            await Task.Delay(600);
            _musicPlayer.CurrentTrack.Should().Be(track2);
        }

        [Fact]
        public async Task MultipleRapidCrossfades_ShouldNotCauseAudioArtifacts()
        {
            // Arrange
            _musicPlayer = new MusicPlayer(_mockPlayerLogger.Object, _mockOutputDevice.Object);
            var tracks = new[]
            {
                CreateTestMusicTrack("track1.mid"),
                CreateTestMusicTrack("track2.mid"),
                CreateTestMusicTrack("track3.mid")
            };

            await _musicPlayer.LoadAsync(tracks[0]);
            await _musicPlayer.PlayAsync();

            // Act - Rapid crossfades
            var crossfadeTasks = new[]
            {
                _musicPlayer.TransitionToAsync(tracks[1], useCrossfade: true),
                Task.Delay(100).ContinueWith(_ => _musicPlayer.TransitionToAsync(tracks[2], useCrossfade: true))
            };

            // Assert - Should not throw
            await Task.WhenAll(crossfadeTasks);
            _musicPlayer.IsPlaying.Should().BeTrue();
        }

        #endregion

        #region Ducking Tests

        [Fact]
        public async Task DuckMusicWhenDialoguePlays_ShouldLowerMusicVolume()
        {
            // Arrange
            _audioMixer = new AudioMixer(_mockMixerLogger.Object);
            _musicPlayer = new MusicPlayer(_mockPlayerLogger.Object, _mockOutputDevice.Object);
            _audioManager = new AudioManager(
                _mockManagerLogger.Object,
                _mockCache.Object,
                _musicPlayer,
                Mock.Of<ISoundEffectPlayer>(),
                _mockVolumeManager.Object,
                _mockStateManager.Object,
                _mockCacheCoordinator.Object,
                _mockAmbientManager.Object
            );

            var musicFile = "background-music.mid";
            var musicData = CreateTestMusicTrack(musicFile);
            _mockCache.Setup(c => c.GetOrLoadAsync<AudioTrack>(musicFile, It.IsAny<Func<Task<AudioTrack>>>()))
                .ReturnsAsync(musicData);

            await _audioManager.PlayMusicAsync(musicFile, loop: true);
            _audioManager.SetMusicVolume(1.0f);

            var initialVolume = _musicPlayer.Volume;

            // Act - Start ducking (simulating dialogue)
            _audioMixer.DuckMusic(duckLevel: 0.3f);
            _audioMixer.Update(0.016f); // Simulate one frame update

            // Assert
            var musicChannel = _audioMixer.GetChannel(ChannelType.Music);
            musicChannel.IsDucked.Should().BeTrue();
            musicChannel.CurrentVolume.Should().BeLessThan(initialVolume);
            musicChannel.EffectiveVolume.Should().BeApproximately(0.3f, 0.1f);

            // Act - Stop ducking
            _audioMixer.StopDucking(ChannelType.Music);

            // Simulate volume restoration over time
            for (int i = 0; i < 10; i++)
            {
                _audioMixer.Update(0.016f);
            }

            // Assert - Volume restored
            musicChannel.IsDucked.Should().BeFalse();
            musicChannel.CurrentVolume.Should().BeApproximately(initialVolume, 0.1f);
        }

        [Fact]
        public async Task DuckingWithFadeOut_ShouldCoordinateBothEffects()
        {
            // Arrange
            _audioMixer = new AudioMixer(_mockMixerLogger.Object);
            _musicPlayer = new MusicPlayer(_mockPlayerLogger.Object, _mockOutputDevice.Object);

            var musicFile = "test-music.mid";
            var musicData = CreateTestMusicTrack(musicFile);
            await _musicPlayer.LoadAsync(musicData);
            await _musicPlayer.PlayAsync();

            _musicPlayer.SetVolume(1.0f);

            // Act - Start ducking AND fade out simultaneously
            _audioMixer.DuckMusic(0.3f);
            await _audioMixer.FadeOutAsync(ChannelType.Music, duration: 1.0f);

            // Assert - Both effects should work together
            var musicChannel = _audioMixer.GetChannel(ChannelType.Music);
            musicChannel.IsDucked.Should().BeTrue();
            musicChannel.Volume.Should().BeApproximately(0.0f, 0.1f); // Faded out
        }

        #endregion

        #region Memory Management Tests

        [Fact]
        public async Task AudioPlaybackCycle_ShouldNotLeakMemory()
        {
            // Arrange
            var initialMemory = GC.GetTotalMemory(forceFullCollection: true);
            var iterations = 100;

            // Act - Repeated play-stop cycles
            for (int i = 0; i < iterations; i++)
            {
                using var mixer = new AudioMixer(_mockMixerLogger.Object);
                var player = new MusicPlayer(_mockPlayerLogger.Object, _mockOutputDevice.Object);

                var track = CreateTestMusicTrack($"track{i}.mid");
                await player.LoadAsync(track);
                await player.PlayAsync();
                await player.StopAsync();
                player.Dispose();
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var finalMemory = GC.GetTotalMemory(forceFullCollection: false);
            var memoryGrowth = finalMemory - initialMemory;

            // Assert - Memory growth should be minimal (<10MB for 100 cycles)
            memoryGrowth.Should().BeLessThan(10 * 1024 * 1024);
        }

        [Fact]
        public async Task DisposingAudioManager_ShouldCleanupAllResources()
        {
            // Arrange
            _audioMixer = new AudioMixer(_mockMixerLogger.Object);
            _musicPlayer = new MusicPlayer(_mockPlayerLogger.Object, _mockOutputDevice.Object);
            _audioManager = new AudioManager(
                _mockManagerLogger.Object,
                _mockCache.Object,
                _musicPlayer,
                Mock.Of<ISoundEffectPlayer>(),
                _mockVolumeManager.Object,
                _mockStateManager.Object,
                _mockCacheCoordinator.Object,
                _mockAmbientManager.Object
            );

            var musicFile = "test-music.mid";
            var musicData = CreateTestMusicTrack(musicFile);
            _mockCache.Setup(c => c.GetOrLoadAsync<AudioTrack>(musicFile, It.IsAny<Func<Task<AudioTrack>>>()))
                .ReturnsAsync(musicData);

            await _audioManager.PlayMusicAsync(musicFile, loop: true);

            // Act
            _audioManager.Dispose();

            // Assert
            _audioManager.IsInitialized.Should().BeFalse();
            _musicPlayer.IsPlaying.Should().BeFalse();

            // Attempting to play after disposal should throw
            Func<Task> act = async () => await _audioManager.PlayMusicAsync(musicFile, loop: true);
            await act.Should().ThrowAsync<ObjectDisposedException>();
        }

        #endregion

        #region Performance Tests

        [Fact]
        public void MixingMultipleChannels_ShouldMeetPerformanceBudget()
        {
            // Arrange
            _audioMixer = new AudioMixer(_mockMixerLogger.Object);

            // Setup all channels with fading
            _audioMixer.SetChannelVolume(ChannelType.Music, 0.0f);
            _audioMixer.SetChannelVolume(ChannelType.SoundEffects, 0.0f);
            _audioMixer.SetChannelVolume(ChannelType.Voice, 0.0f);
            _audioMixer.SetChannelVolume(ChannelType.Ambient, 0.0f);

            _audioMixer.GetChannel(ChannelType.Music).FadeTo(1.0f);
            _audioMixer.GetChannel(ChannelType.SoundEffects).FadeTo(1.0f);
            _audioMixer.GetChannel(ChannelType.Voice).FadeTo(1.0f);
            _audioMixer.GetChannel(ChannelType.Ambient).FadeTo(1.0f);

            var stopwatch = Stopwatch.StartNew();
            var iterations = 1000;

            // Act - Simulate real-time updates
            for (int i = 0; i < iterations; i++)
            {
                _audioMixer.Update(0.016f); // 60 FPS
            }

            stopwatch.Stop();

            // Assert - Should complete 1000 updates in < 1 second
            // Target: <1ms per update at 60 FPS
            var averageTimePerUpdate = stopwatch.ElapsedMilliseconds / (double)iterations;
            averageTimePerUpdate.Should().BeLessThan(1.0,
                $"Audio mixing should take <1ms per update, but took {averageTimePerUpdate:F3}ms on average");

            stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000,
                "1000 mixer updates should complete in less than 1 second");
        }

        [Fact]
        public async Task MusicLoadingAndPlayback_ShouldMeetPerformanceTargets()
        {
            // Arrange
            _musicPlayer = new MusicPlayer(_mockPlayerLogger.Object, _mockOutputDevice.Object);
            var musicData = CreateTestMusicTrack("perf-test.mid");

            var stopwatch = Stopwatch.StartNew();

            // Act
            await _musicPlayer.LoadAsync(musicData);
            await _musicPlayer.PlayAsync();

            stopwatch.Stop();

            // Assert - Loading and starting playback should be fast
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(100,
                "Music loading and playback start should take <100ms");
        }

        [Fact]
        public void VolumeCalculations_ShouldBeOptimized()
        {
            // Arrange
            _audioMixer = new AudioMixer(_mockMixerLogger.Object);
            _audioMixer.SetMasterVolume(0.8f);
            _audioMixer.SetChannelVolume(ChannelType.Music, 0.7f);
            _audioMixer.SetChannelVolume(ChannelType.SoundEffects, 0.6f);

            var stopwatch = Stopwatch.StartNew();
            var iterations = 10000;

            // Act - Repeated volume calculations
            for (int i = 0; i < iterations; i++)
            {
                _ = _audioMixer.GetEffectiveVolume(ChannelType.Music);
                _ = _audioMixer.GetEffectiveVolume(ChannelType.SoundEffects);
            }

            stopwatch.Stop();

            // Assert - 20,000 calculations should be near-instant
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(50,
                "Volume calculations should be highly optimized");
        }

        #endregion

        #region Stress Tests

        [Fact]
        public async Task RapidPlayStopCycles_ShouldRemainStable()
        {
            // Arrange
            _musicPlayer = new MusicPlayer(_mockPlayerLogger.Object, _mockOutputDevice.Object);
            var musicData = CreateTestMusicTrack("stress-test.mid");
            await _musicPlayer.LoadAsync(musicData);

            // Act - Rapid play/stop cycles
            for (int i = 0; i < 100; i++)
            {
                await _musicPlayer.PlayAsync();
                await _musicPlayer.StopAsync();
            }

            // Assert - Should not throw or leak resources
            _musicPlayer.IsPlaying.Should().BeFalse();
            _musicPlayer.CurrentTrack.Should().NotBeNull();
        }

        [Fact]
        public async Task ConcurrentVolumeChanges_ShouldBeThreadSafe()
        {
            // Arrange
            _audioMixer = new AudioMixer(_mockMixerLogger.Object);
            var tasks = new Task[10];

            // Act - Concurrent volume changes
            for (int i = 0; i < 10; i++)
            {
                tasks[i] = Task.Run(() =>
                {
                    for (int j = 0; j < 100; j++)
                    {
                        _audioMixer.SetMasterVolume((float)j / 100f);
                        _audioMixer.Update(0.016f);
                    }
                });
            }

            // Assert - Should not throw
            var waitTask = Task.WhenAll(tasks);
            var completed = await Task.WhenAny(waitTask, Task.Delay(TimeSpan.FromSeconds(5)));
            (completed == waitTask).Should().BeTrue();
        }

        #endregion

        #region Helper Methods

        private AudioTrack CreateTestMusicTrack(string filePath)
        {
            return new AudioTrack
            {
                Name = Path.GetFileNameWithoutExtension(filePath),
                FilePath = filePath,
                Duration = TimeSpan.FromSeconds(30),
                Type = TrackType.Music,
                Loop = false
            };
        }

        #endregion

        public void Dispose()
        {
            _audioManager?.Dispose();
            _musicPlayer?.Dispose();
            _audioMixer?.Dispose();
        }
    }
}
