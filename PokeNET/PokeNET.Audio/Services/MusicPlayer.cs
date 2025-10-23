using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Melanchall.DryWetMidi.Multimedia;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.Core;
using PokeNET.Audio.Abstractions;
using PokeNET.Audio.Configuration;
using PokeNET.Audio.Exceptions;
using PokeNET.Audio.Models;

namespace PokeNET.Audio.Services;

/// <summary>
/// MIDI-based music player with support for looping, crossfading, and volume control.
/// Uses DryWetMidi for MIDI file processing and playback.
/// Fully implements IMusicPlayer interface.
/// </summary>
public sealed class MusicPlayer : IMusicPlayer
{
    private readonly ILogger<MusicPlayer> _logger;
    private readonly AudioSettings _settings;
    private readonly AudioCache _cache;
    private readonly SemaphoreSlim _playbackLock;

    private IOutputDevice? _outputDevice;
    private Playback? _currentPlayback;
    private MidiFile? _currentMidiFile;
    private float _volume;
    private bool _isPlaying;
    private bool _isPaused;
    private string? _currentTrackPath;
    private AudioTrack? _currentAudioTrack;
    private bool _isLooping;
    private TimeSpan _crossfadeDuration;
    private bool _disposed;

    // Events from IMusicPlayer
    public event EventHandler<TrackCompletedEventArgs>? TrackCompleted;
    public event EventHandler<TrackTransitionEventArgs>? TrackTransitioning;

    // IAudioService properties
    public PlaybackState State
    {
        get
        {
            ThrowIfDisposed();
            if (_isPlaying && !_isPaused) return PlaybackState.Playing;
            if (_isPaused) return PlaybackState.Paused;
            return PlaybackState.Stopped;
        }
    }

    public bool IsPlaying
    {
        get
        {
            ThrowIfDisposed();
            return _isPlaying && !_isPaused;
        }
    }

    public bool IsPaused
    {
        get
        {
            ThrowIfDisposed();
            return _isPaused;
        }
    }

    // IMusicPlayer properties
    public AudioTrack? CurrentTrack
    {
        get
        {
            ThrowIfDisposed();
            return _currentAudioTrack;
        }
    }

    public AudioTrack? NextTrack
    {
        get
        {
            ThrowIfDisposed();
            return null; // TODO: Implement track queue
        }
    }

    public MusicState MusicState
    {
        get
        {
            ThrowIfDisposed();
            return new MusicState
            {
                CurrentTrack = _currentAudioTrack,
                NextTrack = null, // TODO: Implement track queue
                State = State,
                Position = GetPosition(),
                Volume = _volume,
                IsLooping = _isLooping,
                IsMuted = _volume == 0.0f,
                IsTransitioning = false,
                TransitionProgress = 0.0f
            };
        }
    }

    public bool IsLooping
    {
        get
        {
            ThrowIfDisposed();
            return _isLooping;
        }
        set
        {
            ThrowIfDisposed();
            _isLooping = value;
            if (_currentPlayback != null)
            {
                _currentPlayback.Loop = value;
            }
            _logger.LogDebug("Music looping set to {IsLooping}", value);
        }
    }

    public TimeSpan CrossfadeDuration
    {
        get
        {
            ThrowIfDisposed();
            return _crossfadeDuration;
        }
        set
        {
            ThrowIfDisposed();
            _crossfadeDuration = value;
            _logger.LogDebug("Crossfade duration set to {Duration}ms", value.TotalMilliseconds);
        }
    }

    public float Volume
    {
        get => _volume;
        set
        {
            ThrowIfDisposed();
            _volume = Math.Clamp(value, 0.0f, 1.0f);
            _logger.LogDebug("Music volume set to {Volume}", _volume);
        }
    }

    // Test compatibility constructor
    public MusicPlayer(ILogger<MusicPlayer> logger, IOutputDevice outputDevice)
        : this(logger,
               Options.Create(new AudioSettings { MusicVolume = 1.0f, MidiOutputDevice = 0 }),
               new AudioCache(NullLoggerFactory.Instance.CreateLogger<AudioCache>(), 50 * 1024 * 1024)) // 50MB cache
    {
        _outputDevice = outputDevice;
    }

    public MusicPlayer(
        ILogger<MusicPlayer> logger,
        IOptions<AudioSettings> settings,
        AudioCache cache)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));

        _playbackLock = new SemaphoreSlim(1, 1);
        _volume = _settings.MusicVolume;
        _crossfadeDuration = TimeSpan.FromSeconds(1); // Default 1 second crossfade

        _logger.LogInformation("MusicPlayer initialized");
    }

    // IAudioService methods
    public async Task PlayAsync(AudioTrack track, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (track == null)
        {
            throw new ArgumentNullException(nameof(track));
        }

        await _playbackLock.WaitAsync(cancellationToken);
        try
        {
            _logger.LogInformation("Playing music: {TrackName}, Loop: {Loop}", track.Name, track.Loop);

            // Stop current playback
            StopInternal();

            // Load MIDI file
            var midiFile = await LoadMidiFileAsync(track.FilePath, cancellationToken);

            // Initialize output device if needed
            if (_outputDevice == null)
            {
                _outputDevice = OutputDevice.GetByIndex(_settings.MidiOutputDevice);
                _logger.LogInformation("MIDI output device initialized: {DeviceId}", _settings.MidiOutputDevice);
            }

            // Create playback
            _currentPlayback = midiFile.GetPlayback(_outputDevice);
            _currentPlayback.Loop = track.Loop || _isLooping;

            // Handle playback finished event
            _currentPlayback.Finished += OnPlaybackFinished;

            // Start playback
            _currentPlayback.Start();

            _currentMidiFile = midiFile;
            _currentTrackPath = track.FilePath;
            _currentAudioTrack = track;
            _isPlaying = true;
            _isPaused = false;

            // Update track metadata
            track.LastPlayedAt = DateTime.UtcNow;
            track.PlayCount++;

            _logger.LogInformation("Music playback started: {Track}", track.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to play music: {TrackName}", track.Name);
            throw new AudioPlaybackException($"Failed to play music: {track.Name}", ex);
        }
        finally
        {
            _playbackLock.Release();
        }
    }

    public TimeSpan GetPosition()
    {
        ThrowIfDisposed();

        if (_currentPlayback == null)
        {
            return TimeSpan.Zero;
        }

        try
        {
            var currentTime = _currentPlayback.GetCurrentTime<MetricTimeSpan>();
            return TimeSpan.FromMilliseconds(currentTime.TotalMicroseconds / 1000.0);
        }
        catch
        {
            return TimeSpan.Zero;
        }
    }

    public void Seek(TimeSpan position)
    {
        ThrowIfDisposed();

        _playbackLock.Wait();
        try
        {
            if (_currentPlayback == null)
            {
                throw new AudioPlaybackException("No track is currently loaded");
            }

            var metricTime = new MetricTimeSpan((long)(position.TotalMilliseconds * 1000));
            _currentPlayback.MoveToTime(metricTime);

            _logger.LogDebug("Seeked to position: {Position}", position);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to seek to position: {Position}", position);
            throw new AudioPlaybackException($"Failed to seek to position: {position}", ex);
        }
        finally
        {
            _playbackLock.Release();
        }
    }

    // IMusicPlayer methods
    public async Task LoadAsync(AudioTrack track, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (track == null)
        {
            throw new ArgumentNullException(nameof(track));
        }

        await _playbackLock.WaitAsync(cancellationToken);
        try
        {
            _logger.LogInformation("Loading music: {TrackName}", track.Name);

            // Stop current playback
            StopInternal();

            // Load MIDI file (but don't play it)
            var midiFile = await LoadMidiFileAsync(track.FilePath, cancellationToken);

            // Initialize output device if needed
            if (_outputDevice == null)
            {
                _outputDevice = OutputDevice.GetByIndex(_settings.MidiOutputDevice);
                _logger.LogInformation("MIDI output device initialized: {DeviceId}", _settings.MidiOutputDevice);
            }

            // Create playback (but don't start it)
            _currentPlayback = midiFile.GetPlayback(_outputDevice);
            _currentPlayback.Loop = track.Loop || _isLooping;
            _currentPlayback.Finished += OnPlaybackFinished;

            _currentMidiFile = midiFile;
            _currentTrackPath = track.FilePath;
            _currentAudioTrack = track;
            _isPlaying = false;
            _isPaused = false;

            _logger.LogInformation("Music loaded: {Track}", track.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load music: {TrackName}", track.Name);
            throw new AudioLoadException(track.FilePath, ex);
        }
        finally
        {
            _playbackLock.Release();
        }
    }

    public async Task TransitionToAsync(AudioTrack track, bool useCrossfade = true, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (track == null)
        {
            throw new ArgumentNullException(nameof(track));
        }

        await _playbackLock.WaitAsync(cancellationToken);
        try
        {
            _logger.LogInformation("Transitioning to music: {TrackName}, Crossfade: {UseCrossfade}",
                track.Name, useCrossfade);

            // Raise transition event
            TrackTransitioning?.Invoke(this, new TrackTransitionEventArgs
            {
                FromTrack = _currentAudioTrack,
                ToTrack = track,
                IsCrossfading = useCrossfade,
                Duration = useCrossfade ? _crossfadeDuration : TimeSpan.Zero
            });

            if (useCrossfade && _isPlaying)
            {
                // Perform crossfade
                var fadeOutTask = FadeVolumeAsync(_volume, 0.0f, (int)_crossfadeDuration.TotalMilliseconds, cancellationToken);
                await fadeOutTask;
            }

            // Play new track
            _playbackLock.Release(); // Release before calling PlayAsync
            await PlayAsync(track, cancellationToken);
            await _playbackLock.WaitAsync(cancellationToken);

            if (useCrossfade)
            {
                // Fade in new track
                await FadeVolumeAsync(0.0f, _settings.MusicVolume, (int)_crossfadeDuration.TotalMilliseconds, cancellationToken);
            }

            _logger.LogInformation("Transition completed to: {Track}", track.Name);
        }
        finally
        {
            if (_playbackLock.CurrentCount == 0)
            {
                _playbackLock.Release();
            }
        }
    }

    public async Task FadeOutAsync(TimeSpan duration, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        await _playbackLock.WaitAsync(cancellationToken);
        try
        {
            if (!_isPlaying)
            {
                return;
            }

            _logger.LogInformation("Fading out music: {Track}, Duration: {Duration}ms",
                _currentAudioTrack?.Name, duration.TotalMilliseconds);

            await FadeVolumeAsync(_volume, 0.0f, (int)duration.TotalMilliseconds, cancellationToken);

            StopInternal();
        }
        finally
        {
            _playbackLock.Release();
        }
    }

    public async Task FadeInAsync(TimeSpan duration, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        await _playbackLock.WaitAsync(cancellationToken);
        try
        {
            if (_currentPlayback == null)
            {
                throw new AudioPlaybackException("No track is currently loaded");
            }

            _logger.LogInformation("Fading in music: {Track}, Duration: {Duration}ms",
                _currentAudioTrack?.Name, duration.TotalMilliseconds);

            // Start playback if not already playing
            if (!_isPlaying)
            {
                _currentPlayback.Start();
                _isPlaying = true;
                _isPaused = false;
            }

            await FadeVolumeAsync(0.0f, _settings.MusicVolume, (int)duration.TotalMilliseconds, cancellationToken);
        }
        finally
        {
            _playbackLock.Release();
        }
    }

    public void SetVolume(float volume)
    {
        if (volume < 0.0f || volume > 1.0f)
        {
            throw new ArgumentOutOfRangeException(nameof(volume), "Volume must be between 0.0 and 1.0");
        }

        Volume = volume;
    }

    public float GetVolume()
    {
        return Volume;
    }

    // Legacy string-based PlayAsync (for backward compatibility)
    public async Task PlayAsync(
        string assetPath,
        bool loop = true,
        int fadeInDuration = 0,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (string.IsNullOrWhiteSpace(assetPath))
        {
            throw new ArgumentException("Asset path cannot be null or whitespace", nameof(assetPath));
        }

        // Create an AudioTrack wrapper and delegate to the main implementation
        var track = new AudioTrack
        {
            Name = Path.GetFileNameWithoutExtension(assetPath),
            FilePath = assetPath,
            Loop = loop,
            Type = TrackType.Music
        };

        // Use the IMusicPlayer implementation
        await PlayAsync(track, cancellationToken);

        // Apply fade-in if specified
        if (fadeInDuration > 0)
        {
            await FadeInAsync(TimeSpan.FromMilliseconds(fadeInDuration), cancellationToken);
        }
    }

    // IAudioService Stop implementation
    public void Stop()
    {
        ThrowIfDisposed();

        _playbackLock.Wait();
        try
        {
            if (!_isPlaying)
            {
                return;
            }

            _logger.LogInformation("Stopping music: {Track}", _currentAudioTrack?.Name);
            StopInternal();
        }
        finally
        {
            _playbackLock.Release();
        }
    }

    // Legacy Stop with fade-out (for backward compatibility)
    public void Stop(int fadeOutDuration = 0)
    {
        ThrowIfDisposed();

        if (fadeOutDuration > 0)
        {
            // Perform async fade-out
            FadeOutAsync(TimeSpan.FromMilliseconds(fadeOutDuration), CancellationToken.None)
                .GetAwaiter().GetResult();
        }
        else
        {
            Stop();
        }
    }

    public void Pause()
    {
        ThrowIfDisposed();

        _playbackLock.Wait();
        try
        {
            if (!_isPlaying || _isPaused)
            {
                return;
            }

            _currentPlayback?.Stop();
            _isPaused = true;

            _logger.LogInformation("Music paused: {Track}", _currentAudioTrack?.Name);
        }
        finally
        {
            _playbackLock.Release();
        }
    }

    public void Resume()
    {
        ThrowIfDisposed();

        _playbackLock.Wait();
        try
        {
            if (!_isPaused)
            {
                return;
            }

            _currentPlayback?.Start();
            _isPaused = false;

            _logger.LogInformation("Music resumed: {Track}", _currentAudioTrack?.Name);
        }
        finally
        {
            _playbackLock.Release();
        }
    }

    public async Task CrossfadeAsync(
        string assetPath,
        int crossfadeDuration = 1000,
        bool loop = true,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (string.IsNullOrWhiteSpace(assetPath))
        {
            throw new ArgumentException("Asset path cannot be null or whitespace", nameof(assetPath));
        }

        await _playbackLock.WaitAsync(cancellationToken);
        try
        {
            _logger.LogInformation("Crossfading to music: {AssetPath}, Duration: {Duration}ms", assetPath, crossfadeDuration);

            // Fade out current track
            if (_isPlaying)
            {
                var fadeOutTask = FadeVolumeAsync(_volume, 0.0f, crossfadeDuration, cancellationToken);
                await fadeOutTask;
            }

            // Play new track with fade-in
            await PlayAsync(assetPath, loop, crossfadeDuration, cancellationToken);

            _logger.LogInformation("Crossfade completed to: {Track}", _currentAudioTrack?.Name);
        }
        finally
        {
            _playbackLock.Release();
        }
    }

    /// <summary>
    /// Loads a MIDI file from cache or disk.
    /// </summary>
    private async Task<MidiFile> LoadMidiFileAsync(string assetPath, CancellationToken cancellationToken)
    {
        // Check cache first
        if (_cache.TryGet<MidiFile>(assetPath, out var cachedFile) && cachedFile != null)
        {
            _logger.LogDebug("MIDI file loaded from cache: {AssetPath}", assetPath);
            return cachedFile;
        }

        // Load from disk
        var fullPath = Path.Combine(_settings.AssetBasePath, assetPath);

        if (!File.Exists(fullPath))
        {
            throw new AudioLoadException(assetPath, new FileNotFoundException($"MIDI file not found: {fullPath}"));
        }

        try
        {
            var midiFile = await Task.Run(() => MidiFile.Read(fullPath), cancellationToken);

            // Cache the file
            var fileInfo = new FileInfo(fullPath);
            _cache.Set(assetPath, midiFile, fileInfo.Length);

            _logger.LogDebug("MIDI file loaded from disk and cached: {AssetPath}", assetPath);
            return midiFile;
        }
        catch (Exception ex)
        {
            throw new AudioLoadException(assetPath, ex);
        }
    }

    /// <summary>
    /// Fades volume from one level to another over a specified duration.
    /// </summary>
    private async Task FadeVolumeAsync(float fromVolume, float toVolume, int durationMs, CancellationToken cancellationToken)
    {
        const int stepMs = 50; // Update every 50ms
        var steps = durationMs / stepMs;
        var volumeStep = (toVolume - fromVolume) / steps;

        for (int i = 0; i < steps; i++)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            _volume = fromVolume + (volumeStep * i);
            await Task.Delay(stepMs, cancellationToken);
        }

        _volume = toVolume;
    }

    /// <summary>
    /// Internal stop method without locking (must be called within lock).
    /// </summary>
    private void StopInternal()
    {
        if (_currentPlayback != null)
        {
            _currentPlayback.Finished -= OnPlaybackFinished;
            _currentPlayback.Stop();
            _currentPlayback.Dispose();
            _currentPlayback = null;
        }

        _isPlaying = false;
        _isPaused = false;
        _currentTrackPath = null;
        _currentAudioTrack = null;

        _logger.LogDebug("Music playback stopped internally");
    }

    /// <summary>
    /// Handles playback finished event.
    /// </summary>
    private void OnPlaybackFinished(object? sender, EventArgs e)
    {
        _logger.LogInformation("Music playback finished: {Track}", _currentAudioTrack?.Name);

        _playbackLock.Wait();
        try
        {
            var completedTrack = _currentAudioTrack;
            var willLoop = _currentPlayback?.Loop ?? false;

            _isPlaying = false;
            _isPaused = false;

            // Fire TrackCompleted event if there's a subscriber
            if (completedTrack != null)
            {
                TrackCompleted?.Invoke(this, new TrackCompletedEventArgs
                {
                    Track = completedTrack,
                    WillLoop = willLoop
                });
            }
        }
        finally
        {
            _playbackLock.Release();
        }
    }

    // ==========================================================================
    // TEST COMPATIBILITY METHODS
    // These methods provide compatibility with the legacy test API
    // ==========================================================================

    public bool IsInitialized => true; // Always initialized via constructor
    public MidiFile? CurrentMidi => _currentMidiFile;
    public bool IsLoaded => _currentMidiFile != null;

    public Task InitializeAsync() => Task.CompletedTask; // No-op, initialized via constructor

    public async Task LoadMidiAsync(byte[] midiData)
    {
        ThrowIfDisposed();

        if (midiData == null)
        {
            throw new ArgumentNullException(nameof(midiData));
        }

        using var memoryStream = new MemoryStream(midiData);
        _currentMidiFile = MidiFile.Read(memoryStream);

        var track = new AudioTrack
        {
            Name = "loaded-track",
            FilePath = "memory",
            Loop = _isLooping
        };

        await LoadAsync(track);
    }

    public async Task LoadMidiFromFileAsync(string filePath)
    {
        ThrowIfDisposed();

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"MIDI file not found: {filePath}");
        }

        var midiData = await File.ReadAllBytesAsync(filePath);
        await LoadMidiAsync(midiData);
    }

    public Task PlayAsync() => PlayAsync(_currentAudioTrack ?? throw new InvalidOperationException("No MIDI file loaded"));
    public Task PauseAsync() { Pause(); return Task.CompletedTask; }
    public Task ResumeAsync() { Resume(); return Task.CompletedTask; }
    public Task StopAsync() { Stop(); return Task.CompletedTask; }
    public Task SeekAsync(TimeSpan position) { Seek(position); return Task.CompletedTask; }

    public TimeSpan GetDuration()
    {
        if (_currentMidiFile == null) return TimeSpan.Zero;
        return _currentMidiFile.GetDuration<MetricTimeSpan>();
    }

    public int GetTrackCount() => _currentMidiFile?.GetTrackChunks().Count() ?? 0;

    public void SetLoop(bool enabled)
    {
        IsLooping = enabled;
    }

    public float Tempo { get; private set; } = 1.0f;

    public void SetTempo(float tempo)
    {
        if (tempo <= 0)
        {
            throw new ArgumentException("Tempo must be greater than zero", nameof(tempo));
        }
        Tempo = tempo;
        _logger.LogDebug("Tempo set to {Tempo}", tempo);
    }

    public void SimulatePlaybackEnd()
    {
        // Test helper - simulate playback finishing
        OnPlaybackFinished(this, EventArgs.Empty);
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(MusicPlayer));
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _playbackLock.Wait();
        try
        {
            StopInternal();

            _outputDevice?.Dispose();
            _outputDevice = null;

            _playbackLock.Dispose();
            _disposed = true;

            _logger.LogInformation("MusicPlayer disposed");
        }
        finally
        {
            if (!_playbackLock.CurrentCount.Equals(0))
            {
                _playbackLock.Release();
            }
        }
    }
}
