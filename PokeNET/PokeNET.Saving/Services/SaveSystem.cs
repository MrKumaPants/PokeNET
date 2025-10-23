using System.Diagnostics;
using Microsoft.Extensions.Logging;
using PokeNET.Domain.Saving;

namespace PokeNET.Saving.Services;

/// <summary>
/// Main save system implementation that orchestrates save/load operations.
/// Coordinates between GameStateManager, SaveSerializer, SaveFileProvider, and SaveValidator.
/// </summary>
public class SaveSystem : ISaveSystem
{
    private readonly ILogger<SaveSystem> _logger;
    private readonly IGameStateManager _gameStateManager;
    private readonly ISaveSerializer _serializer;
    private readonly ISaveFileProvider _fileProvider;
    private readonly ISaveValidator _validator;
    private AutoSaveConfig _autoSaveConfig;
    private Timer? _autoSaveTimer;

    public SaveSystem(
        ILogger<SaveSystem> logger,
        IGameStateManager gameStateManager,
        ISaveSerializer serializer,
        ISaveFileProvider fileProvider,
        ISaveValidator validator)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _gameStateManager = gameStateManager ?? throw new ArgumentNullException(nameof(gameStateManager));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _fileProvider = fileProvider ?? throw new ArgumentNullException(nameof(fileProvider));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));

        _autoSaveConfig = new AutoSaveConfig
        {
            Enabled = false,
            IntervalSeconds = 300,
            SlotId = "autosave"
        };
    }

    /// <inheritdoc/>
    public async Task<SaveResult> SaveAsync(string slotId, string? description = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slotId))
            throw new ArgumentException("Slot ID cannot be null or empty", nameof(slotId));

        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Starting save operation for slot {SlotId}", slotId);

            // Create snapshot
            var snapshot = _gameStateManager.CreateSnapshot(description);

            // Serialize without checksum first
            var dataWithoutChecksum = _serializer.Serialize(snapshot);

            // Compute and set checksum
            snapshot.Checksum = _serializer.ComputeChecksum(dataWithoutChecksum);

            // Serialize again with checksum
            var data = _serializer.Serialize(snapshot);

            // Create metadata
            var metadata = CreateMetadataFromSnapshot(slotId, snapshot);

            // Write to file
            await _fileProvider.WriteAsync(slotId, data, metadata, cancellationToken);

            stopwatch.Stop();

            var result = new SaveResult
            {
                Success = true,
                SlotId = slotId,
                Duration = stopwatch.Elapsed,
                FileSizeBytes = data.Length
            };

            _logger.LogInformation(
                "Save completed successfully: Slot={SlotId}, Size={SizeBytes} bytes, Duration={DurationMs}ms",
                slotId,
                data.Length,
                stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(ex, "Save operation failed for slot {SlotId}", slotId);

            return new SaveResult
            {
                Success = false,
                SlotId = slotId,
                Duration = stopwatch.Elapsed,
                ErrorMessage = ex.Message,
                Exception = ex
            };
        }
    }

    /// <inheritdoc/>
    public async Task<LoadResult> LoadAsync(string slotId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slotId))
            throw new ArgumentException("Slot ID cannot be null or empty", nameof(slotId));

        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Starting load operation for slot {SlotId}", slotId);

            // Read save data
            var data = await _fileProvider.ReadAsync(slotId, cancellationToken);

            // Validate before deserializing
            var validationResult = _validator.Validate(data);

            if (!validationResult.IsValid)
            {
                throw new SaveValidationException(validationResult);
            }

            // Deserialize
            var snapshot = _serializer.Deserialize(data);

            // Validate snapshot structure
            if (!_gameStateManager.ValidateSnapshot(snapshot))
            {
                throw new SaveValidationException("Snapshot validation failed", new SaveValidationResult
                {
                    IsValid = false,
                    Errors = { "Snapshot structure is invalid" }
                });
            }

            // Restore game state
            _gameStateManager.RestoreSnapshot(snapshot);

            stopwatch.Stop();

            var result = new LoadResult
            {
                Success = true,
                SlotId = slotId,
                GameState = snapshot,
                Duration = stopwatch.Elapsed,
                WasMigrated = false, // No migration implemented yet
                OriginalVersion = snapshot.SaveVersion
            };

            _logger.LogInformation(
                "Load completed successfully: Slot={SlotId}, Duration={DurationMs}ms",
                slotId,
                stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(ex, "Load operation failed for slot {SlotId}", slotId);

            return new LoadResult
            {
                Success = false,
                SlotId = slotId,
                Duration = stopwatch.Elapsed,
                ErrorMessage = ex.Message,
                Exception = ex
            };
        }
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteAsync(string slotId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slotId))
            throw new ArgumentException("Slot ID cannot be null or empty", nameof(slotId));

        _logger.LogInformation("Deleting save slot {SlotId}", slotId);

        return await _fileProvider.DeleteAsync(slotId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<SaveMetadata>> GetSaveSlotsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Retrieving all save slots");

        var metadata = await _fileProvider.GetAllMetadataAsync(cancellationToken);

        _logger.LogInformation("Found {Count} save slots", metadata.Count);

        return metadata;
    }

    /// <inheritdoc/>
    public async Task<SaveMetadata?> GetSaveMetadataAsync(string slotId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slotId))
            return null;

        _logger.LogDebug("Retrieving metadata for slot {SlotId}", slotId);

        return await _fileProvider.GetMetadataAsync(slotId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<SaveValidationResult> ValidateAsync(string slotId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slotId))
            throw new ArgumentException("Slot ID cannot be null or empty", nameof(slotId));

        _logger.LogInformation("Validating save slot {SlotId}", slotId);

        try
        {
            var data = await _fileProvider.ReadAsync(slotId, cancellationToken);
            return _validator.Validate(data);
        }
        catch (SaveNotFoundException)
        {
            return new SaveValidationResult
            {
                Exists = false,
                IsValid = false,
                Errors = { $"Save file not found: {slotId}" }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Validation failed for slot {SlotId}", slotId);

            return new SaveValidationResult
            {
                Exists = true,
                IsValid = false,
                Errors = { $"Validation error: {ex.Message}" }
            };
        }
    }

    /// <inheritdoc/>
    public void ConfigureAutoSave(bool enabled, int intervalSeconds = 300)
    {
        if (intervalSeconds < 30)
            throw new ArgumentException("Auto-save interval must be at least 30 seconds", nameof(intervalSeconds));

        _logger.LogInformation("Configuring auto-save: Enabled={Enabled}, Interval={Interval}s", enabled, intervalSeconds);

        _autoSaveConfig.Enabled = enabled;
        _autoSaveConfig.IntervalSeconds = intervalSeconds;

        // Stop existing timer
        _autoSaveTimer?.Dispose();
        _autoSaveTimer = null;

        // Start new timer if enabled
        if (enabled)
        {
            var interval = TimeSpan.FromSeconds(intervalSeconds);
            _autoSaveTimer = new Timer(AutoSaveCallback, null, interval, interval);

            _logger.LogInformation("Auto-save timer started with {Interval}s interval", intervalSeconds);
        }
        else
        {
            _logger.LogInformation("Auto-save disabled");
        }
    }

    /// <inheritdoc/>
    public AutoSaveConfig GetAutoSaveConfig()
    {
        return _autoSaveConfig;
    }

    /// <inheritdoc/>
    public async Task<bool> ExportSaveAsync(string slotId, string destinationPath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slotId))
            throw new ArgumentException("Slot ID cannot be null or empty", nameof(slotId));

        if (string.IsNullOrWhiteSpace(destinationPath))
            throw new ArgumentException("Destination path cannot be null or empty", nameof(destinationPath));

        _logger.LogInformation("Exporting save from slot {SlotId} to {Destination}", slotId, destinationPath);

        return await _fileProvider.CopyToAsync(slotId, destinationPath, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<ImportResult> ImportSaveAsync(string sourcePath, string targetSlotId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
            throw new ArgumentException("Source path cannot be null or empty", nameof(sourcePath));

        if (string.IsNullOrWhiteSpace(targetSlotId))
            throw new ArgumentException("Target slot ID cannot be null or empty", nameof(targetSlotId));

        _logger.LogInformation("Importing save from {Source} to slot {SlotId}", sourcePath, targetSlotId);

        try
        {
            // Validate source file
            var data = await File.ReadAllBytesAsync(sourcePath, cancellationToken);
            var validationResult = _validator.Validate(data);

            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Import validation failed for {Source}", sourcePath);

                return new ImportResult
                {
                    Success = false,
                    TargetSlotId = targetSlotId,
                    ValidationResult = validationResult,
                    ErrorMessage = "Save file validation failed"
                };
            }

            // Copy file
            var copySuccess = await _fileProvider.CopyFromAsync(sourcePath, targetSlotId, cancellationToken);

            if (!copySuccess)
            {
                return new ImportResult
                {
                    Success = false,
                    TargetSlotId = targetSlotId,
                    ValidationResult = validationResult,
                    ErrorMessage = "Failed to copy save file"
                };
            }

            _logger.LogInformation("Successfully imported save to slot {SlotId}", targetSlotId);

            return new ImportResult
            {
                Success = true,
                TargetSlotId = targetSlotId,
                ValidationResult = validationResult
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Import failed from {Source}", sourcePath);

            return new ImportResult
            {
                Success = false,
                TargetSlotId = targetSlotId,
                ValidationResult = new SaveValidationResult
                {
                    IsValid = false,
                    Errors = { ex.Message }
                },
                ErrorMessage = ex.Message
            };
        }
    }

    private void AutoSaveCallback(object? state)
    {
        if (!_autoSaveConfig.Enabled)
            return;

        _logger.LogInformation("Auto-save triggered");

        try
        {
            // Run auto-save asynchronously (fire and forget)
            _ = Task.Run(async () =>
            {
                var result = await SaveAsync(_autoSaveConfig.SlotId, "Auto-save");

                if (result.Success)
                {
                    _autoSaveConfig.LastAutoSave = DateTime.UtcNow;
                    _logger.LogInformation("Auto-save completed successfully");
                }
                else
                {
                    _logger.LogWarning("Auto-save failed: {Error}", result.ErrorMessage);
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Auto-save callback failed");
        }
    }

    private static SaveMetadata CreateMetadataFromSnapshot(string slotId, GameStateSnapshot snapshot)
    {
        return new SaveMetadata
        {
            SlotId = slotId,
            PlayerName = snapshot.Player?.Name ?? "Unknown",
            CurrentLocation = snapshot.Player?.CurrentMap ?? "Unknown",
            PlaytimeSeconds = snapshot.Player?.PlaytimeSeconds ?? 0,
            PartyCount = snapshot.Party?.Count ?? 0,
            BadgeCount = snapshot.Player?.BadgeCount ?? 0,
            PokedexCaught = snapshot.Pokedex?.TotalCaught ?? 0,
            PokedexSeen = snapshot.Pokedex?.TotalSeen ?? 0,
            CreatedAt = snapshot.CreatedAt,
            LastModified = DateTime.UtcNow,
            SaveVersion = snapshot.SaveVersion,
            Description = snapshot.Description,
            FileSizeBytes = 0, // Will be set by file provider
            IsCorrupted = false,
            RequiresMigration = false
        };
    }
}
