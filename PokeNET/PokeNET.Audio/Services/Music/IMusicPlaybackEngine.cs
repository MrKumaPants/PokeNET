using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.Multimedia;
using PokeNET.Audio.Models;

namespace PokeNET.Audio.Services.Music;

/// <summary>
/// Core music playback engine handling MIDI playback operations.
/// Manages output device, playback lifecycle, and playback control.
/// </summary>
public interface IMusicPlaybackEngine
{
    /// <summary>
    /// Event raised when playback finishes.
    /// </summary>
    event EventHandler<EventArgs>? PlaybackFinished;

    /// <summary>
    /// Gets whether the engine is initialized.
    /// </summary>
    bool IsInitialized { get; }

    /// <summary>
    /// Initializes the playback engine and output device.
    /// </summary>
    Task InitializeAsync();

    /// <summary>
    /// Starts playback of a MIDI file.
    /// </summary>
    /// <param name="midiFile">MIDI file to play</param>
    /// <param name="loop">Whether to loop playback</param>
    void StartPlayback(MidiFile midiFile, bool loop);

    /// <summary>
    /// Stops the current playback.
    /// </summary>
    void StopPlayback();

    /// <summary>
    /// Pauses the current playback.
    /// </summary>
    void PausePlayback();

    /// <summary>
    /// Resumes paused playback.
    /// </summary>
    void ResumePlayback();

    /// <summary>
    /// Seeks to a specific position in the current playback.
    /// </summary>
    /// <param name="position">Target position</param>
    void Seek(TimeSpan position);

    /// <summary>
    /// Gets the current playback position.
    /// </summary>
    TimeSpan GetPosition();

    /// <summary>
    /// Gets whether playback is currently active.
    /// </summary>
    bool HasActivePlayback { get; }

    /// <summary>
    /// Creates a playback instance for a MIDI file without starting it.
    /// </summary>
    /// <param name="midiFile">MIDI file to prepare</param>
    /// <param name="loop">Whether to enable looping</param>
    void PreparePlayback(MidiFile midiFile, bool loop);
}
