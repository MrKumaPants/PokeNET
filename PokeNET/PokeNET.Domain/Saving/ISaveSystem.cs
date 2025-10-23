namespace PokeNET.Domain.Saving;

/// <summary>
/// Core save system interface for managing game state persistence.
/// Provides save, load, and validation operations with transaction support.
/// </summary>
/// <remarks>
/// <para>
/// The save system follows a layered architecture:
/// <list type="number">
/// <item>ISaveSystem - High-level save/load orchestration</item>
/// <item>IGameStateManager - State snapshot management</item>
/// <item>ISaveSerializer - Serialization strategy</item>
/// <item>ISaveFileProvider - File I/O abstraction</item>
/// <item>ISaveValidator - Data integrity validation</item>
/// </list>
/// </para>
/// </remarks>
public interface ISaveSystem
{
    /// <summary>
    /// Saves the current game state to the specified slot.
    /// </summary>
    /// <param name="slotId">Save slot identifier (1-N, or "quicksave"/"autosave").</param>
    /// <param name="description">Optional save description visible to player.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>SaveResult with success status and any errors.</returns>
    /// <exception cref="ArgumentException">Invalid slot ID.</exception>
    /// <exception cref="SaveException">Save operation failed.</exception>
    Task<SaveResult> SaveAsync(string slotId, string? description = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads game state from the specified slot.
    /// </summary>
    /// <param name="slotId">Save slot identifier to load from.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>LoadResult with loaded state or errors.</returns>
    /// <exception cref="ArgumentException">Invalid slot ID.</exception>
    /// <exception cref="SaveNotFoundException">Save file not found.</exception>
    /// <exception cref="SaveCorruptedException">Save file corrupted or invalid.</exception>
    Task<LoadResult> LoadAsync(string slotId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a save file from the specified slot.
    /// </summary>
    /// <param name="slotId">Save slot identifier to delete.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>True if deleted successfully; false if not found.</returns>
    Task<bool> DeleteAsync(string slotId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets metadata for all available save slots without loading full save data.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>List of save metadata (empty if no saves exist).</returns>
    Task<IReadOnlyList<SaveMetadata>> GetSaveSlotsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets metadata for a specific save slot without loading full save data.
    /// </summary>
    /// <param name="slotId">Save slot identifier.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Save metadata or null if slot is empty.</returns>
    Task<SaveMetadata?> GetSaveMetadataAsync(string slotId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a save file without loading it.
    /// Checks integrity, version compatibility, and corruption.
    /// </summary>
    /// <param name="slotId">Save slot identifier to validate.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>ValidationResult with detailed diagnostic information.</returns>
    Task<SaveValidationResult> ValidateAsync(string slotId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Enables or disables auto-save functionality.
    /// </summary>
    /// <param name="enabled">True to enable auto-save; false to disable.</param>
    /// <param name="intervalSeconds">Interval in seconds between auto-saves (default: 300 = 5 minutes).</param>
    void ConfigureAutoSave(bool enabled, int intervalSeconds = 300);

    /// <summary>
    /// Gets the current auto-save configuration.
    /// </summary>
    /// <returns>Auto-save configuration settings.</returns>
    AutoSaveConfig GetAutoSaveConfig();

    /// <summary>
    /// Exports a save file to external storage (cloud sync, backup, etc.).
    /// </summary>
    /// <param name="slotId">Save slot to export.</param>
    /// <param name="destinationPath">Destination file path.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>True if export succeeded.</returns>
    Task<bool> ExportSaveAsync(string slotId, string destinationPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Imports a save file from external storage.
    /// </summary>
    /// <param name="sourcePath">Source file path to import from.</param>
    /// <param name="targetSlotId">Target save slot for imported data.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>ImportResult with success status and validation info.</returns>
    Task<ImportResult> ImportSaveAsync(string sourcePath, string targetSlotId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a save operation.
/// </summary>
public class SaveResult
{
    /// <summary>Gets or sets whether the save operation succeeded.</summary>
    public bool Success { get; set; }

    /// <summary>Gets or sets the save slot that was written to.</summary>
    public string SlotId { get; set; } = null!;

    /// <summary>Gets or sets the time taken to perform the save operation.</summary>
    public TimeSpan Duration { get; set; }

    /// <summary>Gets or sets the size of the saved file in bytes.</summary>
    public long FileSizeBytes { get; set; }

    /// <summary>Gets or sets any error message if the operation failed.</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>Gets or sets the exception that caused the failure, if any.</summary>
    public Exception? Exception { get; set; }
}

/// <summary>
/// Result of a load operation.
/// </summary>
public class LoadResult
{
    /// <summary>Gets or sets whether the load operation succeeded.</summary>
    public bool Success { get; set; }

    /// <summary>Gets or sets the save slot that was loaded from.</summary>
    public string SlotId { get; set; } = null!;

    /// <summary>Gets or sets the loaded game state snapshot.</summary>
    public GameStateSnapshot? GameState { get; set; }

    /// <summary>Gets or sets the time taken to perform the load operation.</summary>
    public TimeSpan Duration { get; set; }

    /// <summary>Gets or sets whether any migration was performed during load.</summary>
    public bool WasMigrated { get; set; }

    /// <summary>Gets or sets the original save version before migration.</summary>
    public Version? OriginalVersion { get; set; }

    /// <summary>Gets or sets any error message if the operation failed.</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>Gets or sets the exception that caused the failure, if any.</summary>
    public Exception? Exception { get; set; }
}

/// <summary>
/// Result of a save validation operation.
/// </summary>
public class SaveValidationResult
{
    /// <summary>Gets or sets whether the save file is valid.</summary>
    public bool IsValid { get; set; }

    /// <summary>Gets or sets whether the save file exists.</summary>
    public bool Exists { get; set; }

    /// <summary>Gets or sets whether the checksum validation passed.</summary>
    public bool ChecksumValid { get; set; }

    /// <summary>Gets or sets whether the save version is compatible.</summary>
    public bool VersionCompatible { get; set; }

    /// <summary>Gets or sets the save file version.</summary>
    public Version? SaveVersion { get; set; }

    /// <summary>Gets or sets validation warnings (non-fatal issues).</summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>Gets or sets validation errors (fatal issues preventing load).</summary>
    public List<string> Errors { get; set; } = new();
}

/// <summary>
/// Result of a save import operation.
/// </summary>
public class ImportResult
{
    /// <summary>Gets or sets whether the import succeeded.</summary>
    public bool Success { get; set; }

    /// <summary>Gets or sets the target slot where save was imported.</summary>
    public string TargetSlotId { get; set; } = null!;

    /// <summary>Gets or sets the validation result from pre-import checks.</summary>
    public SaveValidationResult ValidationResult { get; set; } = null!;

    /// <summary>Gets or sets any error message if the import failed.</summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Auto-save configuration settings.
/// </summary>
public class AutoSaveConfig
{
    /// <summary>Gets or sets whether auto-save is enabled.</summary>
    public bool Enabled { get; set; }

    /// <summary>Gets or sets the interval in seconds between auto-saves.</summary>
    public int IntervalSeconds { get; set; } = 300;

    /// <summary>Gets or sets the save slot used for auto-saves.</summary>
    public string SlotId { get; set; } = "autosave";

    /// <summary>Gets or sets the last time an auto-save was performed.</summary>
    public DateTime? LastAutoSave { get; set; }
}
