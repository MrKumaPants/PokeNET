using Melanchall.DryWetMidi.Multimedia;

namespace PokeNET.Audio.Abstractions;

/// <summary>
/// Interface for MIDI output devices.
/// Extends DryWetMidi's IOutputDevice to provide testability.
/// Named IMidiOutputDevice to avoid namespace conflicts with DryWetMidi.
/// </summary>
public interface IMidiOutputDevice : Melanchall.DryWetMidi.Multimedia.IOutputDevice
{
    // Inherits all methods from DryWetMidi.IOutputDevice:
    // - void SendEvent(MidiEvent midiEvent)
    // - void PrepareForEventsSending()
    // - IDisposable.Dispose()
}
