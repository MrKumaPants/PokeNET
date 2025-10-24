using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Microsoft.Extensions.Logging;
using PokeNET.Audio.Abstractions;
using PokeNET.Audio.Models;

namespace PokeNET.Audio.Services.Music;

/// <summary>
/// Manages music playback state, current track information, and state queries.
/// Provides centralized state management for the music system.
/// </summary>
public sealed class MusicStateManager : IMusicStateManager
{
    private readonly ILogger<MusicStateManager> _logger;
    private bool _isPlaying;
    private bool _isPaused;
    private bool _isLooping;
    private AudioTrack? _currentTrack;
    private MidiFile? _currentMidiFile;

    public MusicStateManager(ILogger<MusicStateManager> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public PlaybackState State
    {
        get
        {
            if (_isPlaying && !_isPaused) return PlaybackState.Playing;
            if (_isPaused) return PlaybackState.Paused;
            return PlaybackState.Stopped;
        }
    }

    /// <inheritdoc/>
    public bool IsPlaying => _isPlaying && !_isPaused;

    /// <inheritdoc/>
    public bool IsPaused => _isPaused;

    /// <inheritdoc/>
    public AudioTrack? CurrentTrack => _currentTrack;

    /// <inheritdoc/>
    public MidiFile? CurrentMidiFile => _currentMidiFile;

    /// <inheritdoc/>
    public bool IsLoaded => _currentMidiFile != null;

    /// <inheritdoc/>
    public bool IsLooping
    {
        get => _isLooping;
        set
        {
            _isLooping = value;
            _logger.LogDebug("Music looping set to {IsLooping}", value);
        }
    }

    /// <inheritdoc/>
    public MusicState GetMusicState(TimeSpan position, float volume)
    {
        return new MusicState
        {
            CurrentTrack = _currentTrack,
            NextTrack = null, // TODO: Implement track queue
            State = State,
            Position = position,
            Volume = volume,
            IsLooping = _isLooping,
            IsMuted = volume == 0.0f,
            IsTransitioning = false,
            TransitionProgress = 0.0f
        };
    }

    /// <inheritdoc/>
    public void SetPlaying(AudioTrack track, MidiFile midiFile)
    {
        _currentTrack = track;
        _currentMidiFile = midiFile;
        _isPlaying = true;
        _isPaused = false;

        // Update track metadata
        track.LastPlayedAt = DateTime.UtcNow;
        track.PlayCount++;

        _logger.LogInformation("Music state: Playing - {Track}", track.Name);
    }

    /// <inheritdoc/>
    public void SetStopped()
    {
        _isPlaying = false;
        _isPaused = false;
        _currentTrack = null;
        _currentMidiFile = null;

        _logger.LogDebug("Music state: Stopped");
    }

    /// <inheritdoc/>
    public void SetPaused()
    {
        if (!_isPlaying || _isPaused)
        {
            return;
        }

        _isPaused = true;
        _logger.LogInformation("Music state: Paused - {Track}", _currentTrack?.Name);
    }

    /// <inheritdoc/>
    public void SetResumed()
    {
        if (!_isPaused)
        {
            return;
        }

        _isPaused = false;
        _logger.LogInformation("Music state: Resumed - {Track}", _currentTrack?.Name);
    }

    /// <inheritdoc/>
    public void SetLoaded(AudioTrack track, MidiFile midiFile)
    {
        _currentTrack = track;
        _currentMidiFile = midiFile;
        _isPlaying = false;
        _isPaused = false;

        _logger.LogInformation("Music loaded: {Track}", track.Name);
    }

    /// <inheritdoc/>
    public int GetTrackCount()
    {
        return _currentMidiFile?.GetTrackChunks().Count() ?? 0;
    }

    /// <inheritdoc/>
    public TimeSpan GetDuration()
    {
        if (_currentMidiFile == null)
        {
            return TimeSpan.Zero;
        }

        try
        {
            return _currentMidiFile.GetDuration<MetricTimeSpan>();
        }
        catch
        {
            return TimeSpan.Zero;
        }
    }
}
