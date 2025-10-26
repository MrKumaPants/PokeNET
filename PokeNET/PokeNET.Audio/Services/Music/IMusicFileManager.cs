using Melanchall.DryWetMidi.Core;

namespace PokeNET.Audio.Services.Music;

/// <summary>
/// Manages MIDI file loading, caching, and validation.
/// Handles file I/O and caching strategies for music files.
/// </summary>
public interface IMusicFileManager
{
    /// <summary>
    /// Loads a MIDI file from the specified path, using cache if available.
    /// </summary>
    /// <param name="assetPath">Relative path to the MIDI file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The loaded MIDI file</returns>
    Task<MidiFile> LoadMidiFileAsync(
        string assetPath,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Loads a MIDI file from raw byte data.
    /// </summary>
    /// <param name="midiData">Raw MIDI file data</param>
    /// <returns>The loaded MIDI file</returns>
    MidiFile LoadMidiFromBytes(byte[] midiData);

    /// <summary>
    /// Loads a MIDI file from a file path.
    /// </summary>
    /// <param name="filePath">Absolute file path</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The loaded MIDI file</returns>
    Task<MidiFile> LoadMidiFromFileAsync(
        string filePath,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Checks if a MIDI file is cached.
    /// </summary>
    /// <param name="assetPath">Path to check</param>
    /// <returns>True if cached</returns>
    bool IsCached(string assetPath);

    /// <summary>
    /// Clears the MIDI file cache.
    /// </summary>
    void ClearCache();
}
