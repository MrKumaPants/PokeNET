namespace PokeNET.Domain.Saving;

/// <summary>
/// Validates save files for integrity, version compatibility, and corruption.
/// </summary>
public interface ISaveValidator
{
    /// <summary>
    /// Validates save data without fully deserializing.
    /// </summary>
    /// <param name="data">Serialized save data.</param>
    /// <returns>Validation result with diagnostic information.</returns>
    SaveValidationResult Validate(byte[] data);

    /// <summary>
    /// Validates a deserialized snapshot for logical consistency.
    /// </summary>
    /// <param name="snapshot">Game state snapshot to validate.</param>
    /// <returns>Validation result.</returns>
    SaveValidationResult ValidateSnapshot(GameStateSnapshot snapshot);

    /// <summary>
    /// Checks if a save version is compatible with current game version.
    /// </summary>
    /// <param name="saveVersion">Save file version.</param>
    /// <returns>True if compatible; false if migration or rejection needed.</returns>
    bool IsVersionCompatible(Version saveVersion);

    /// <summary>
    /// Gets the minimum supported save version.
    /// </summary>
    /// <returns>Minimum version that can be loaded.</returns>
    Version GetMinimumSupportedVersion();

    /// <summary>
    /// Gets the current save version used for new saves.
    /// </summary>
    /// <returns>Current save version.</returns>
    Version GetCurrentVersion();
}
