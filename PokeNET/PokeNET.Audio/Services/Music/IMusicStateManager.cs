using Melanchall.DryWetMidi.Core;
using PokeNET.Audio.Abstractions;
using PokeNET.Audio.Models;

namespace PokeNET.Audio.Services.Music;

/// <summary>
/// Manages music playback state, current track information, and state queries.
/// Provides centralized state management for the music system.
/// </summary>
public interface IMusicStateManager
{
    /// <summary>
    /// Gets the current playback state.
    /// </summary>
    PlaybackState State { get; }

    /// <summary>
    /// Gets whether music is currently playing (and not paused).
    /// </summary>
    bool IsPlaying { get; }

    /// <summary>
    /// Gets whether music is currently paused.
    /// </summary>
    bool IsPaused { get; }

    /// <summary>
    /// Gets the current audio track.
    /// </summary>
    AudioTrack? CurrentTrack { get; }

    /// <summary>
    /// Gets the current MIDI file.
    /// </summary>
    MidiFile? CurrentMidiFile { get; }

    /// <summary>
    /// Gets whether a track is currently loaded.
    /// </summary>
    bool IsLoaded { get; }

    /// <summary>
    /// Gets whether looping is enabled.
    /// </summary>
    bool IsLooping { get; set; }

    /// <summary>
    /// Gets the complete music state.
    /// </summary>
    /// <param name="position">Current playback position</param>
    /// <param name="volume">Current volume</param>
    /// <returns>Complete music state</returns>
    MusicState GetMusicState(TimeSpan position, float volume);

    /// <summary>
    /// Updates the state when playback starts.
    /// </summary>
    /// <param name="track">Track that started playing</param>
    /// <param name="midiFile">MIDI file being played</param>
    void SetPlaying(AudioTrack track, MidiFile midiFile);

    /// <summary>
    /// Updates the state when playback stops.
    /// </summary>
    void SetStopped();

    /// <summary>
    /// Updates the state when playback pauses.
    /// </summary>
    void SetPaused();

    /// <summary>
    /// Updates the state when playback resumes.
    /// </summary>
    void SetResumed();

    /// <summary>
    /// Loads a track without starting playback.
    /// </summary>
    /// <param name="track">Track to load</param>
    /// <param name="midiFile">MIDI file loaded</param>
    void SetLoaded(AudioTrack track, MidiFile midiFile);

    /// <summary>
    /// Gets the track count from the current MIDI file.
    /// </summary>
    int GetTrackCount();

    /// <summary>
    /// Gets the duration of the current MIDI file.
    /// </summary>
    TimeSpan GetDuration();
}
