using System.IO;
using Melanchall.DryWetMidi.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PokeNET.Audio.Configuration;
using PokeNET.Audio.Exceptions;

namespace PokeNET.Audio.Services.Music;

/// <summary>
/// Manages MIDI file loading, caching, and validation.
/// Handles file I/O and caching strategies for music files.
/// </summary>
public sealed class MusicFileManager : IMusicFileManager
{
    private readonly ILogger<MusicFileManager> _logger;
    private readonly AudioSettings _settings;
    private readonly AudioCache _cache;

    public MusicFileManager(
        ILogger<MusicFileManager> logger,
        IOptions<AudioSettings> settings,
        AudioCache cache)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    /// <inheritdoc/>
    public async Task<MidiFile> LoadMidiFileAsync(string assetPath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(assetPath))
        {
            throw new ArgumentException("Asset path cannot be null or whitespace", nameof(assetPath));
        }

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

    /// <inheritdoc/>
    public MidiFile LoadMidiFromBytes(byte[] midiData)
    {
        if (midiData == null)
        {
            throw new ArgumentNullException(nameof(midiData));
        }

        try
        {
            using var memoryStream = new MemoryStream(midiData);
            return MidiFile.Read(memoryStream);
        }
        catch (Exception ex)
        {
            throw new AudioLoadException("memory", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<MidiFile> LoadMidiFromFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path cannot be null or whitespace", nameof(filePath));
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"MIDI file not found: {filePath}");
        }

        try
        {
            var midiData = await File.ReadAllBytesAsync(filePath, cancellationToken);
            return LoadMidiFromBytes(midiData);
        }
        catch (Exception ex) when (ex is not FileNotFoundException)
        {
            throw new AudioLoadException(filePath, ex);
        }
    }

    /// <inheritdoc/>
    public bool IsCached(string assetPath)
    {
        return _cache.TryGet<MidiFile>(assetPath, out _);
    }

    /// <inheritdoc/>
    public void ClearCache()
    {
        _cache.Clear();
        _logger.LogInformation("MIDI file cache cleared");
    }
}
