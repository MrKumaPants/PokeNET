namespace PokeNET.Domain.Saving;

/// <summary>
/// Serialization strategy interface for save files.
/// Supports multiple formats (JSON, binary, etc.) through implementation.
/// </summary>
public interface ISaveSerializer
{
    /// <summary>
    /// Serializes a game state snapshot to bytes.
    /// </summary>
    /// <param name="snapshot">The snapshot to serialize.</param>
    /// <returns>Serialized bytes ready for file writing.</returns>
    /// <exception cref="SerializationException">Serialization failed.</exception>
    byte[] Serialize(GameStateSnapshot snapshot);

    /// <summary>
    /// Deserializes bytes to a game state snapshot.
    /// </summary>
    /// <param name="data">Serialized save data.</param>
    /// <returns>Deserialized game state snapshot.</returns>
    /// <exception cref="SerializationException">Deserialization failed.</exception>
    /// <exception cref="SaveCorruptedException">Data is corrupted or invalid.</exception>
    GameStateSnapshot Deserialize(byte[] data);

    /// <summary>
    /// Serializes a game state snapshot to a string (for JSON format).
    /// </summary>
    /// <param name="snapshot">The snapshot to serialize.</param>
    /// <returns>Serialized string (JSON).</returns>
    string SerializeToString(GameStateSnapshot snapshot);

    /// <summary>
    /// Deserializes a string to a game state snapshot (for JSON format).
    /// </summary>
    /// <param name="data">Serialized save data as string.</param>
    /// <returns>Deserialized game state snapshot.</returns>
    GameStateSnapshot DeserializeFromString(string data);

    /// <summary>
    /// Computes a checksum for data integrity validation.
    /// </summary>
    /// <param name="data">Data to compute checksum for.</param>
    /// <returns>Checksum hash string.</returns>
    string ComputeChecksum(byte[] data);

    /// <summary>
    /// Validates a checksum against data.
    /// </summary>
    /// <param name="data">Data to validate.</param>
    /// <param name="expectedChecksum">Expected checksum value.</param>
    /// <returns>True if checksum matches; false otherwise.</returns>
    bool ValidateChecksum(byte[] data, string expectedChecksum);
}
