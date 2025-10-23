namespace PokeNET.Domain.Saving;

/// <summary>
/// File I/O abstraction for save file storage.
/// Allows swapping between local filesystem, cloud storage, etc.
/// </summary>
public interface ISaveFileProvider
{
    /// <summary>
    /// Writes save data to a slot.
    /// </summary>
    /// <param name="slotId">Save slot identifier.</param>
    /// <param name="data">Serialized save data.</param>
    /// <param name="metadata">Save metadata for the slot.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if write succeeded.</returns>
    Task<bool> WriteAsync(string slotId, byte[] data, SaveMetadata metadata, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads save data from a slot.
    /// </summary>
    /// <param name="slotId">Save slot identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Serialized save data.</returns>
    /// <exception cref="SaveNotFoundException">Save file not found.</exception>
    Task<byte[]> ReadAsync(string slotId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a save slot.
    /// </summary>
    /// <param name="slotId">Save slot identifier to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if deleted; false if not found.</returns>
    Task<bool> DeleteAsync(string slotId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a save slot exists.
    /// </summary>
    /// <param name="slotId">Save slot identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if save exists.</returns>
    Task<bool> ExistsAsync(string slotId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets metadata for a save slot without loading full save data.
    /// </summary>
    /// <param name="slotId">Save slot identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Save metadata or null if slot is empty.</returns>
    Task<SaveMetadata?> GetMetadataAsync(string slotId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets metadata for all save slots.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of save metadata for all slots.</returns>
    Task<IReadOnlyList<SaveMetadata>> GetAllMetadataAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Copies a save file to an external location (backup/export).
    /// </summary>
    /// <param name="slotId">Source save slot.</param>
    /// <param name="destinationPath">Destination file path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if copy succeeded.</returns>
    Task<bool> CopyToAsync(string slotId, string destinationPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Imports a save file from an external location.
    /// </summary>
    /// <param name="sourcePath">Source file path.</param>
    /// <param name="targetSlotId">Target save slot.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if import succeeded.</returns>
    Task<bool> CopyFromAsync(string sourcePath, string targetSlotId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the full file path for a save slot (for debugging/diagnostics).
    /// </summary>
    /// <param name="slotId">Save slot identifier.</param>
    /// <returns>Full file path.</returns>
    string GetSaveFilePath(string slotId);
}
