using System.Text.Json;
using Microsoft.Extensions.Logging;
using PokeNET.Domain.Saving;

namespace PokeNET.Saving.Providers;

/// <summary>
/// Local filesystem-based save file provider.
/// Stores save files in a configurable directory with metadata sidecar files.
/// </summary>
public class FileSystemSaveFileProvider : ISaveFileProvider
{
    private readonly ILogger<FileSystemSaveFileProvider> _logger;
    private readonly string _saveDirectory;
    private readonly string _metadataDirectory;

    private const string SaveFileExtension = ".sav";
    private const string MetadataFileExtension = ".meta.json";

    public FileSystemSaveFileProvider(ILogger<FileSystemSaveFileProvider> logger, string? saveDirectory = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Default to user's Documents/PokeNET/Saves if not specified
        _saveDirectory = saveDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "PokeNET",
            "Saves");

        _metadataDirectory = Path.Combine(_saveDirectory, "Metadata");

        // Ensure directories exist
        Directory.CreateDirectory(_saveDirectory);
        Directory.CreateDirectory(_metadataDirectory);

        _logger.LogInformation("Save directory initialized: {SaveDirectory}", _saveDirectory);
    }

    /// <inheritdoc/>
    public async Task<bool> WriteAsync(string slotId, byte[] data, SaveMetadata metadata, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slotId))
            throw new ArgumentException("Slot ID cannot be null or empty", nameof(slotId));

        if (data == null || data.Length == 0)
            throw new ArgumentException("Save data cannot be null or empty", nameof(data));

        if (metadata == null)
            throw new ArgumentNullException(nameof(metadata));

        try
        {
            var saveFilePath = GetSaveFilePath(slotId);
            var metadataFilePath = GetMetadataFilePath(slotId);

            _logger.LogInformation("Writing save file to {Path} ({SizeBytes} bytes)", saveFilePath, data.Length);

            // Write save data
            await File.WriteAllBytesAsync(saveFilePath, data, cancellationToken);

            // Update metadata with file size and last modified
            metadata.FileSizeBytes = data.Length;
            metadata.LastModified = DateTime.UtcNow;

            // Write metadata
            var metadataJson = JsonSerializer.Serialize(metadata, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            await File.WriteAllTextAsync(metadataFilePath, metadataJson, cancellationToken);

            _logger.LogInformation("Successfully wrote save file and metadata for slot {SlotId}", slotId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write save file for slot {SlotId}", slotId);
            throw new SaveException($"Failed to write save file for slot {slotId}", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<byte[]> ReadAsync(string slotId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slotId))
            throw new ArgumentException("Slot ID cannot be null or empty", nameof(slotId));

        var saveFilePath = GetSaveFilePath(slotId);

        if (!File.Exists(saveFilePath))
        {
            _logger.LogWarning("Save file not found for slot {SlotId}: {Path}", slotId, saveFilePath);
            throw new SaveNotFoundException(slotId);
        }

        try
        {
            _logger.LogInformation("Reading save file from {Path}", saveFilePath);

            var data = await File.ReadAllBytesAsync(saveFilePath, cancellationToken);

            _logger.LogInformation("Successfully read save file for slot {SlotId} ({SizeBytes} bytes)", slotId, data.Length);
            return data;
        }
        catch (SaveNotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read save file for slot {SlotId}", slotId);
            throw new SaveException($"Failed to read save file for slot {slotId}", ex);
        }
    }

    /// <inheritdoc/>
    public Task<bool> DeleteAsync(string slotId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slotId))
            throw new ArgumentException("Slot ID cannot be null or empty", nameof(slotId));

        var saveFilePath = GetSaveFilePath(slotId);
        var metadataFilePath = GetMetadataFilePath(slotId);

        if (!File.Exists(saveFilePath))
        {
            _logger.LogWarning("Save file not found for deletion, slot {SlotId}", slotId);
            return Task.FromResult(false);
        }

        try
        {
            _logger.LogInformation("Deleting save file for slot {SlotId}", slotId);

            // Delete save file
            File.Delete(saveFilePath);

            // Delete metadata if exists
            if (File.Exists(metadataFilePath))
            {
                File.Delete(metadataFilePath);
            }

            _logger.LogInformation("Successfully deleted save file for slot {SlotId}", slotId);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete save file for slot {SlotId}", slotId);
            throw new SaveException($"Failed to delete save file for slot {slotId}", ex);
        }
    }

    /// <inheritdoc/>
    public Task<bool> ExistsAsync(string slotId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slotId))
            return Task.FromResult(false);

        var saveFilePath = GetSaveFilePath(slotId);
        return Task.FromResult(File.Exists(saveFilePath));
    }

    /// <inheritdoc/>
    public async Task<SaveMetadata?> GetMetadataAsync(string slotId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slotId))
            return null;

        var metadataFilePath = GetMetadataFilePath(slotId);

        if (!File.Exists(metadataFilePath))
        {
            _logger.LogDebug("Metadata file not found for slot {SlotId}", slotId);
            return null;
        }

        try
        {
            var metadataJson = await File.ReadAllTextAsync(metadataFilePath, cancellationToken);
            var metadata = JsonSerializer.Deserialize<SaveMetadata>(metadataJson);

            return metadata;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read metadata for slot {SlotId}", slotId);
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<SaveMetadata>> GetAllMetadataAsync(CancellationToken cancellationToken = default)
    {
        var metadataList = new List<SaveMetadata>();

        try
        {
            var metadataFiles = Directory.GetFiles(_metadataDirectory, $"*{MetadataFileExtension}");

            foreach (var metadataFile in metadataFiles)
            {
                try
                {
                    var metadataJson = await File.ReadAllTextAsync(metadataFile, cancellationToken);
                    var metadata = JsonSerializer.Deserialize<SaveMetadata>(metadataJson);

                    if (metadata != null)
                    {
                        metadataList.Add(metadata);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to read metadata file: {Path}", metadataFile);
                    // Continue with other files
                }
            }

            _logger.LogInformation("Found {Count} save files with metadata", metadataList.Count);
            return metadataList;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all metadata");
            return new List<SaveMetadata>();
        }
    }

    /// <inheritdoc/>
    public async Task<bool> CopyToAsync(string slotId, string destinationPath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slotId))
            throw new ArgumentException("Slot ID cannot be null or empty", nameof(slotId));

        if (string.IsNullOrWhiteSpace(destinationPath))
            throw new ArgumentException("Destination path cannot be null or empty", nameof(destinationPath));

        var saveFilePath = GetSaveFilePath(slotId);

        if (!File.Exists(saveFilePath))
        {
            throw new SaveNotFoundException(slotId);
        }

        try
        {
            _logger.LogInformation("Copying save file from {Source} to {Destination}", saveFilePath, destinationPath);

            // Ensure destination directory exists
            var destinationDir = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrEmpty(destinationDir))
            {
                Directory.CreateDirectory(destinationDir);
            }

            await Task.Run(() => File.Copy(saveFilePath, destinationPath, overwrite: true), cancellationToken);

            _logger.LogInformation("Successfully copied save file for slot {SlotId}", slotId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to copy save file for slot {SlotId}", slotId);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> CopyFromAsync(string sourcePath, string targetSlotId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
            throw new ArgumentException("Source path cannot be null or empty", nameof(sourcePath));

        if (string.IsNullOrWhiteSpace(targetSlotId))
            throw new ArgumentException("Target slot ID cannot be null or empty", nameof(targetSlotId));

        if (!File.Exists(sourcePath))
        {
            throw new FileNotFoundException($"Source file not found: {sourcePath}");
        }

        try
        {
            var targetPath = GetSaveFilePath(targetSlotId);

            _logger.LogInformation("Importing save file from {Source} to slot {SlotId}", sourcePath, targetSlotId);

            await Task.Run(() => File.Copy(sourcePath, targetPath, overwrite: true), cancellationToken);

            _logger.LogInformation("Successfully imported save file to slot {SlotId}", targetSlotId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import save file from {Source}", sourcePath);
            return false;
        }
    }

    /// <inheritdoc/>
    public string GetSaveFilePath(string slotId)
    {
        if (string.IsNullOrWhiteSpace(slotId))
            throw new ArgumentException("Slot ID cannot be null or empty", nameof(slotId));

        // Sanitize slot ID to prevent directory traversal
        var sanitizedSlotId = SanitizeSlotId(slotId);
        return Path.Combine(_saveDirectory, $"{sanitizedSlotId}{SaveFileExtension}");
    }

    private string GetMetadataFilePath(string slotId)
    {
        var sanitizedSlotId = SanitizeSlotId(slotId);
        return Path.Combine(_metadataDirectory, $"{sanitizedSlotId}{MetadataFileExtension}");
    }

    private static string SanitizeSlotId(string slotId)
    {
        // Remove any path separators and invalid filename characters
        var invalidChars = Path.GetInvalidFileNameChars();
        return new string(slotId.Where(c => !invalidChars.Contains(c)).ToArray());
    }
}
