using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.Multimedia;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PokeNET.Audio.Configuration;
using PokeNET.Audio.Exceptions;

namespace PokeNET.Audio.Services.Music;

/// <summary>
/// Core music playback engine handling MIDI playback operations.
/// Manages output device, playback lifecycle, and playback control.
/// </summary>
public sealed class MusicPlaybackEngine : IMusicPlaybackEngine, IDisposable
{
    private readonly ILogger<MusicPlaybackEngine> _logger;
    private readonly AudioSettings _settings;
    private readonly SemaphoreSlim _playbackLock;

    private IOutputDevice? _outputDevice;
    private Playback? _currentPlayback;
    private bool _disposed;

    /// <inheritdoc/>
    public event EventHandler<EventArgs>? PlaybackFinished;

    public MusicPlaybackEngine(
        ILogger<MusicPlaybackEngine> logger,
        IOptions<AudioSettings> settings)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        _playbackLock = new SemaphoreSlim(1, 1);
    }

    // Test compatibility constructor
    public MusicPlaybackEngine(ILogger<MusicPlaybackEngine> logger, IOutputDevice outputDevice, AudioSettings settings)
        : this(logger, Options.Create(settings))
    {
        _outputDevice = outputDevice;
    }

    /// <inheritdoc/>
    public bool IsInitialized => _outputDevice != null;

    /// <inheritdoc/>
    public bool HasActivePlayback => _currentPlayback != null;

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        await _playbackLock.WaitAsync();
        try
        {
            if (_outputDevice == null)
            {
                _outputDevice = OutputDevice.GetByIndex(_settings.MidiOutputDevice);
                _logger.LogInformation("MIDI output device initialized: {DeviceId}", _settings.MidiOutputDevice);
            }
        }
        finally
        {
            _playbackLock.Release();
        }
    }

    /// <inheritdoc/>
    public void StartPlayback(MidiFile midiFile, bool loop)
    {
        ThrowIfDisposed();

        _playbackLock.Wait();
        try
        {
            // Initialize device if needed
            if (_outputDevice == null)
            {
                _outputDevice = OutputDevice.GetByIndex(_settings.MidiOutputDevice);
                _logger.LogInformation("MIDI output device initialized: {DeviceId}", _settings.MidiOutputDevice);
            }

            // Stop any existing playback
            StopPlaybackInternal();

            // Create and start new playback
            _currentPlayback = midiFile.GetPlayback(_outputDevice);
            _currentPlayback.Loop = loop;
            _currentPlayback.Finished += OnPlaybackFinished;
            _currentPlayback.Start();

            _logger.LogDebug("Playback started, Loop: {Loop}", loop);
        }
        finally
        {
            _playbackLock.Release();
        }
    }

    /// <inheritdoc/>
    public void StopPlayback()
    {
        ThrowIfDisposed();

        _playbackLock.Wait();
        try
        {
            StopPlaybackInternal();
        }
        finally
        {
            _playbackLock.Release();
        }
    }

    /// <inheritdoc/>
    public void PausePlayback()
    {
        ThrowIfDisposed();

        _playbackLock.Wait();
        try
        {
            _currentPlayback?.Stop();
            _logger.LogDebug("Playback paused");
        }
        finally
        {
            _playbackLock.Release();
        }
    }

    /// <inheritdoc/>
    public void ResumePlayback()
    {
        ThrowIfDisposed();

        _playbackLock.Wait();
        try
        {
            _currentPlayback?.Start();
            _logger.LogDebug("Playback resumed");
        }
        finally
        {
            _playbackLock.Release();
        }
    }

    /// <inheritdoc/>
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

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public void PreparePlayback(MidiFile midiFile, bool loop)
    {
        ThrowIfDisposed();

        _playbackLock.Wait();
        try
        {
            // Initialize device if needed
            if (_outputDevice == null)
            {
                _outputDevice = OutputDevice.GetByIndex(_settings.MidiOutputDevice);
                _logger.LogInformation("MIDI output device initialized: {DeviceId}", _settings.MidiOutputDevice);
            }

            // Stop any existing playback
            StopPlaybackInternal();

            // Create playback but don't start it
            _currentPlayback = midiFile.GetPlayback(_outputDevice);
            _currentPlayback.Loop = loop;
            _currentPlayback.Finished += OnPlaybackFinished;

            _logger.LogDebug("Playback prepared, Loop: {Loop}", loop);
        }
        finally
        {
            _playbackLock.Release();
        }
    }

    private void StopPlaybackInternal()
    {
        if (_currentPlayback != null)
        {
            _currentPlayback.Finished -= OnPlaybackFinished;
            _currentPlayback.Stop();
            _currentPlayback.Dispose();
            _currentPlayback = null;
        }

        _logger.LogDebug("Playback stopped internally");
    }

    private void OnPlaybackFinished(object? sender, EventArgs e)
    {
        _logger.LogInformation("Playback finished");
        PlaybackFinished?.Invoke(this, e);
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(MusicPlaybackEngine));
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
            StopPlaybackInternal();
            _outputDevice?.Dispose();
            _outputDevice = null;
            _disposed = true;

            _logger.LogInformation("MusicPlaybackEngine disposed");
        }
        finally
        {
            _playbackLock.Release();
            _playbackLock.Dispose();
        }
    }
}
