using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Melanchall.DryWetMidi.Multimedia;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.Core;
using PokeNET.Audio.Abstractions;
using PokeNET.Audio.Configuration;
using PokeNET.Audio.Exceptions;

namespace PokeNET.Audio.Services;

/// <summary>
/// MIDI-based music player with support for looping, crossfading, and volume control.
/// Uses DryWetMidi for MIDI file processing and playback.
/// NOTE: Full IMusicPlayer interface implementation pending - currently provides basic functionality.
/// </summary>
public sealed class MusicPlayer // TODO: Implement IMusicPlayer interface
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
    private string? _currentTrack;
    private bool _disposed;

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

    public string? CurrentTrack
    {
        get
        {
            ThrowIfDisposed();
            return _currentTrack;
        }
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

        _logger.LogInformation("MusicPlayer initialized");
    }

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

        await _playbackLock.WaitAsync(cancellationToken);
        try
        {
            _logger.LogInformation("Playing music: {AssetPath}, Loop: {Loop}, FadeIn: {FadeIn}ms",
                assetPath, loop, fadeInDuration);

            // Stop current playback
            StopInternal();

            // Load MIDI file (from cache or disk)
            var midiFile = await LoadMidiFileAsync(assetPath, cancellationToken);

            // Initialize output device if needed
            if (_outputDevice == null)
            {
                _outputDevice = OutputDevice.GetByIndex(_settings.MidiOutputDevice);
                _logger.LogInformation("MIDI output device initialized: {DeviceId}", _settings.MidiOutputDevice);
            }

            // Create playback
            _currentPlayback = midiFile.GetPlayback(_outputDevice);
            _currentPlayback.Loop = loop;

            // Handle playback finished event
            _currentPlayback.Finished += OnPlaybackFinished;

            // Start playback
            _currentPlayback.Start();

            _currentMidiFile = midiFile;
            _currentTrack = Path.GetFileName(assetPath);
            _isPlaying = true;
            _isPaused = false;

            // Apply fade-in if specified
            if (fadeInDuration > 0)
            {
                await FadeVolumeAsync(0.0f, _volume, fadeInDuration, cancellationToken);
            }

            _logger.LogInformation("Music playback started: {Track}", _currentTrack);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to play music: {AssetPath}", assetPath);
            throw new AudioPlaybackException($"Failed to play music: {assetPath}", ex);
        }
        finally
        {
            _playbackLock.Release();
        }
    }

    public void Stop(int fadeOutDuration = 0)
    {
        ThrowIfDisposed();

        _playbackLock.Wait();
        try
        {
            if (!_isPlaying)
            {
                return;
            }

            _logger.LogInformation("Stopping music: {Track}, FadeOut: {FadeOut}ms", _currentTrack, fadeOutDuration);

            if (fadeOutDuration > 0)
            {
                // Perform fade-out synchronously
                FadeVolumeAsync(_volume, 0.0f, fadeOutDuration, CancellationToken.None).GetAwaiter().GetResult();
            }

            StopInternal();
        }
        finally
        {
            _playbackLock.Release();
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

            _logger.LogInformation("Music paused: {Track}", _currentTrack);
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

            _logger.LogInformation("Music resumed: {Track}", _currentTrack);
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

            _logger.LogInformation("Crossfade completed to: {Track}", _currentTrack);
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
        _currentTrack = null;

        _logger.LogDebug("Music playback stopped internally");
    }

    /// <summary>
    /// Handles playback finished event.
    /// </summary>
    private void OnPlaybackFinished(object? sender, EventArgs e)
    {
        _logger.LogInformation("Music playback finished: {Track}", _currentTrack);

        _playbackLock.Wait();
        try
        {
            _isPlaying = false;
            _isPaused = false;
        }
        finally
        {
            _playbackLock.Release();
        }
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
