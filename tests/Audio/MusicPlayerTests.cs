using Xunit;
using Moq;
using FluentAssertions;
using PokeNET.Audio;
using PokeNET.Audio.Services;
using PokeNET.Audio.Abstractions;
using PokeNET.Audio.Models;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Multimedia;
using Melanchall.DryWetMidi.Interaction;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;

namespace PokeNET.Tests.Audio
{
    /// <summary>
    /// Comprehensive tests for MusicPlayer
    /// Tests MIDI loading, playback control, and DryWetMidi integration
    /// </summary>
    public class MusicPlayerTests : IDisposable
    {
        private readonly Mock<ILogger<MusicPlayer>> _mockLogger;
        private readonly Mock<OutputDevice> _mockOutputDevice;
        private readonly Mock<Playback> _mockPlayback;
        private MusicPlayer? _musicPlayer;

        public MusicPlayerTests()
        {
            _mockLogger = new Mock<ILogger<MusicPlayer>>();
            _mockOutputDevice = new Mock<OutputDevice>();
            _mockPlayback = new Mock<Playback>();
        }

        private MusicPlayer CreateMusicPlayer()
        {
            return new MusicPlayer(_mockLogger.Object, _mockOutputDevice.Object);
        }

        #region Initialization Tests

        [Fact]
        public void Constructor_ShouldInitializeWithDefaultValues()
        {
            // Arrange & Act
            _musicPlayer = CreateMusicPlayer();

            // Assert
            _musicPlayer.Should().NotBeNull();
            _musicPlayer.IsPlaying.Should().BeFalse();
            _musicPlayer.IsPaused.Should().BeFalse();
            _musicPlayer.Volume.Should().Be(1.0f);
        }

        [Fact]
        public async Task InitializeAsync_ShouldSetupMidiDevice()
        {
            // Arrange
            _musicPlayer = CreateMusicPlayer();

            // Act
            await _musicPlayer.InitializeAsync();

            // Assert
            _musicPlayer.IsInitialized.Should().BeTrue();
        }

        [Fact]
        public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
        {
            // Arrange & Act
            Action act = () => new MusicPlayer(null!, _mockOutputDevice.Object);

            // Assert
            act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
        }

        #endregion

        #region MIDI Loading Tests

        [Fact]
        public async Task LoadMidi_WithValidFile_ShouldLoadSuccessfully()
        {
            // Arrange
            _musicPlayer = CreateMusicPlayer();
            var midiData = CreateValidMidiData();

            // Act
            await _musicPlayer.LoadMidiAsync(midiData);

            // Assert
            _musicPlayer.CurrentMidi.Should().NotBeNull();
            _musicPlayer.IsLoaded.Should().BeTrue();
        }

        [Fact]
        public async Task LoadMidi_WithInvalidData_ShouldThrowException()
        {
            // Arrange
            _musicPlayer = CreateMusicPlayer();
            var invalidData = new byte[] { 0x00, 0x00, 0x00, 0x00 };

            // Act
            Func<Task> act = async () => await _musicPlayer.LoadMidiAsync(invalidData);

            // Assert
            await act.Should().ThrowAsync<InvalidDataException>();
        }

        [Fact]
        public async Task LoadMidi_WithNullData_ShouldThrowArgumentNullException()
        {
            // Arrange
            _musicPlayer = CreateMusicPlayer();

            // Act
            Func<Task> act = async () => await _musicPlayer.LoadMidiAsync(null!);

            // Assert
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task LoadMidi_FromFilePath_ShouldLoadSuccessfully()
        {
            // Arrange
            _musicPlayer = CreateMusicPlayer();
            var tempFile = Path.GetTempFileName();
            File.WriteAllBytes(tempFile, CreateValidMidiData());

            try
            {
                // Act
                await _musicPlayer.LoadMidiFromFileAsync(tempFile);

                // Assert
                _musicPlayer.IsLoaded.Should().BeTrue();
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public async Task LoadMidi_FromNonExistentFile_ShouldThrowFileNotFoundException()
        {
            // Arrange
            _musicPlayer = CreateMusicPlayer();

            // Act
            Func<Task> act = async () => await _musicPlayer.LoadMidiFromFileAsync("nonexistent.mid");

            // Assert
            await act.Should().ThrowAsync<FileNotFoundException>();
        }

        #endregion

        #region Playback Control Tests

        [Fact]
        public async Task Play_WithLoadedMidi_ShouldStartPlayback()
        {
            // Arrange
            _musicPlayer = CreateMusicPlayer();
            await _musicPlayer.LoadMidiAsync(CreateValidMidiData());

            // Act
            await _musicPlayer.PlayAsync();

            // Assert
            _musicPlayer.IsPlaying.Should().BeTrue();
            _musicPlayer.IsPaused.Should().BeFalse();
        }

        [Fact]
        public async Task Play_WithoutLoadedMidi_ShouldThrowInvalidOperationException()
        {
            // Arrange
            _musicPlayer = CreateMusicPlayer();

            // Act
            Func<Task> act = async () => await _musicPlayer.PlayAsync();

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*No MIDI*");
        }

        [Fact]
        public async Task Pause_WhilePlaying_ShouldPausePlayback()
        {
            // Arrange
            _musicPlayer = CreateMusicPlayer();
            await _musicPlayer.LoadMidiAsync(CreateValidMidiData());
            await _musicPlayer.PlayAsync();

            // Act
            await _musicPlayer.PauseAsync();

            // Assert
            _musicPlayer.IsPaused.Should().BeTrue();
            _musicPlayer.IsPlaying.Should().BeFalse();
        }

        [Fact]
        public async Task Resume_WhilePaused_ShouldResumePlayback()
        {
            // Arrange
            _musicPlayer = CreateMusicPlayer();
            await _musicPlayer.LoadMidiAsync(CreateValidMidiData());
            await _musicPlayer.PlayAsync();
            await _musicPlayer.PauseAsync();

            // Act
            await _musicPlayer.ResumeAsync();

            // Assert
            _musicPlayer.IsPlaying.Should().BeTrue();
            _musicPlayer.IsPaused.Should().BeFalse();
        }

        [Fact]
        public async Task Stop_WhilePlaying_ShouldStopPlayback()
        {
            // Arrange
            _musicPlayer = CreateMusicPlayer();
            await _musicPlayer.LoadMidiAsync(CreateValidMidiData());
            await _musicPlayer.PlayAsync();

            // Act
            await _musicPlayer.StopAsync();

            // Assert
            _musicPlayer.IsPlaying.Should().BeFalse();
            _musicPlayer.GetPosition().Should().Be(TimeSpan.Zero);
        }

        [Fact]
        public async Task Seek_ToValidPosition_ShouldUpdatePosition()
        {
            // Arrange
            _musicPlayer = CreateMusicPlayer();
            await _musicPlayer.LoadMidiAsync(CreateValidMidiData());
            var targetPosition = TimeSpan.FromSeconds(5);

            // Act
            await _musicPlayer.SeekAsync(targetPosition);

            // Assert
            _musicPlayer.GetPosition().Should().BeCloseTo(targetPosition, TimeSpan.FromMilliseconds(100));
        }

        [Fact]
        public async Task Seek_BeyondDuration_ShouldClampToEnd()
        {
            // Arrange
            _musicPlayer = CreateMusicPlayer();
            await _musicPlayer.LoadMidiAsync(CreateValidMidiData());
            var duration = _musicPlayer.GetDuration();

            // Act
            await _musicPlayer.SeekAsync(duration + TimeSpan.FromSeconds(10));

            // Assert
            _musicPlayer.GetPosition().Should().Be(duration);
        }

        [Fact]
        public async Task Seek_ToNegativePosition_ShouldClampToZero()
        {
            // Arrange
            _musicPlayer = CreateMusicPlayer();
            await _musicPlayer.LoadMidiAsync(CreateValidMidiData());

            // Act
            await _musicPlayer.SeekAsync(TimeSpan.FromSeconds(-5));

            // Assert
            _musicPlayer.GetPosition().Should().Be(TimeSpan.Zero);
        }

        #endregion

        #region Volume Control Tests

        [Fact]
        public void SetVolume_WithValidValue_ShouldUpdateVolume()
        {
            // Arrange
            _musicPlayer = CreateMusicPlayer();

            // Act
            _musicPlayer.SetVolume(0.5f);

            // Assert
            _musicPlayer.Volume.Should().Be(0.5f);
        }

        [Fact]
        public void SetVolume_AboveMax_ShouldClampToOne()
        {
            // Arrange
            _musicPlayer = CreateMusicPlayer();

            // Act
            _musicPlayer.SetVolume(1.5f);

            // Assert
            _musicPlayer.Volume.Should().Be(1.0f);
        }

        [Fact]
        public void SetVolume_BelowMin_ShouldClampToZero()
        {
            // Arrange
            _musicPlayer = CreateMusicPlayer();

            // Act
            _musicPlayer.SetVolume(-0.5f);

            // Assert
            _musicPlayer.Volume.Should().Be(0.0f);
        }

        [Fact]
        public async Task SetVolume_WhilePlaying_ShouldUpdateImmediately()
        {
            // Arrange
            _musicPlayer = CreateMusicPlayer();
            await _musicPlayer.LoadMidiAsync(CreateValidMidiData());
            await _musicPlayer.PlayAsync();

            // Act
            _musicPlayer.SetVolume(0.3f);

            // Assert
            _musicPlayer.Volume.Should().Be(0.3f);
        }

        #endregion

        #region Loop Control Tests

        [Fact]
        public void SetLoop_ToTrue_ShouldEnableLooping()
        {
            // Arrange
            _musicPlayer = CreateMusicPlayer();

            // Act
            _musicPlayer.SetLoop(true);

            // Assert
            _musicPlayer.IsLooping.Should().BeTrue();
        }

        [Fact]
        public void SetLoop_ToFalse_ShouldDisableLooping()
        {
            // Arrange
            _musicPlayer = CreateMusicPlayer();
            _musicPlayer.SetLoop(true);

            // Act
            _musicPlayer.SetLoop(false);

            // Assert
            _musicPlayer.IsLooping.Should().BeFalse();
        }

        [Fact]
        public async Task PlaybackEnd_WithLoopEnabled_ShouldRestart()
        {
            // Arrange
            _musicPlayer = CreateMusicPlayer();
            await _musicPlayer.LoadMidiAsync(CreateValidMidiData());
            _musicPlayer.SetLoop(true);

            // Act
            await _musicPlayer.PlayAsync();
            _musicPlayer.SimulatePlaybackEnd(); // Test helper

            // Assert
            _musicPlayer.IsPlaying.Should().BeTrue();
            _musicPlayer.GetPosition().Should().Be(TimeSpan.Zero);
        }

        #endregion

        #region Tempo Control Tests

        [Fact]
        public void SetTempo_WithValidValue_ShouldUpdateTempo()
        {
            // Arrange
            _musicPlayer = CreateMusicPlayer();

            // Act
            _musicPlayer.SetTempo(1.5f);

            // Assert
            _musicPlayer.Tempo.Should().Be(1.5f);
        }

        [Fact]
        public void SetTempo_ToZero_ShouldThrowArgumentException()
        {
            // Arrange
            _musicPlayer = CreateMusicPlayer();

            // Act
            Action act = () => _musicPlayer.SetTempo(0);

            // Assert
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void SetTempo_ToNegative_ShouldThrowArgumentException()
        {
            // Arrange
            _musicPlayer = CreateMusicPlayer();

            // Act
            Action act = () => _musicPlayer.SetTempo(-1.0f);

            // Assert
            act.Should().Throw<ArgumentException>();
        }

        #endregion

        #region Metadata Tests

        [Fact]
        public async Task GetDuration_WithLoadedMidi_ShouldReturnCorrectDuration()
        {
            // Arrange
            _musicPlayer = CreateMusicPlayer();
            await _musicPlayer.LoadMidiAsync(CreateValidMidiData());

            // Act
            var duration = _musicPlayer.GetDuration();

            // Assert
            duration.Should().BeGreaterThan(TimeSpan.Zero);
        }

        [Fact]
        public void GetDuration_WithoutLoadedMidi_ShouldReturnZero()
        {
            // Arrange
            _musicPlayer = CreateMusicPlayer();

            // Act
            var duration = _musicPlayer.GetDuration();

            // Assert
            duration.Should().Be(TimeSpan.Zero);
        }

        [Fact]
        public async Task GetTrackCount_WithLoadedMidi_ShouldReturnCorrectCount()
        {
            // Arrange
            _musicPlayer = CreateMusicPlayer();
            await _musicPlayer.LoadMidiAsync(CreateValidMidiData());

            // Act
            var trackCount = _musicPlayer.GetTrackCount();

            // Assert
            trackCount.Should().BeGreaterThan(0);
        }

        #endregion

        #region Disposal Tests

        [Fact]
        public void Dispose_ShouldCleanupResources()
        {
            // Arrange
            _musicPlayer = CreateMusicPlayer();

            // Act
            _musicPlayer.Dispose();

            // Assert
            _mockOutputDevice.Verify(d => d.Dispose(), Times.Once);
        }

        [Fact]
        public async Task Dispose_WhilePlaying_ShouldStopPlayback()
        {
            // Arrange
            _musicPlayer = CreateMusicPlayer();
            await _musicPlayer.LoadMidiAsync(CreateValidMidiData());
            await _musicPlayer.PlayAsync();

            // Act
            _musicPlayer.Dispose();

            // Assert
            _musicPlayer.IsPlaying.Should().BeFalse();
        }

        [Fact]
        public void Dispose_CalledMultipleTimes_ShouldBeIdempotent()
        {
            // Arrange
            _musicPlayer = CreateMusicPlayer();

            // Act
            _musicPlayer.Dispose();
            _musicPlayer.Dispose();
            _musicPlayer.Dispose();

            // Assert
            _mockOutputDevice.Verify(d => d.Dispose(), Times.Once);
        }

        #endregion

        #region Helper Methods

        private byte[] CreateValidMidiData()
        {
            // Create minimal valid MIDI file
            using var memoryStream = new MemoryStream();
            var midiFile = new MidiFile(
                new TrackChunk(
                    new TextEvent("Test Track"),
                    new NoteOnEvent((SevenBitNumber)60, (SevenBitNumber)100),
                    new NoteOffEvent((SevenBitNumber)60, (SevenBitNumber)0) { DeltaTime = 1000 }
                )
            );
            midiFile.Write(memoryStream);
            return memoryStream.ToArray();
        }

        #endregion

        public void Dispose()
        {
            _musicPlayer?.Dispose();
        }
    }
}
