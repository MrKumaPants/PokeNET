using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using PokeNET.Domain.Saving;

namespace PokeNET.Saving.Serializers;

/// <summary>
/// JSON-based save file serializer using System.Text.Json.
/// Provides human-readable save files with optional formatting.
/// </summary>
public class JsonSaveSerializer : ISaveSerializer
{
    private readonly ILogger<JsonSaveSerializer> _logger;
    private readonly JsonSerializerOptions _options;

    public JsonSaveSerializer(ILogger<JsonSaveSerializer> logger, bool prettyPrint = false)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _options = new JsonSerializerOptions
        {
            WriteIndented = prettyPrint,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters =
            {
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
            }
        };
    }

    /// <inheritdoc/>
    public byte[] Serialize(GameStateSnapshot snapshot)
    {
        if (snapshot == null)
            throw new ArgumentNullException(nameof(snapshot));

        try
        {
            _logger.LogDebug("Serializing game state snapshot to JSON");

            var json = JsonSerializer.Serialize(snapshot, _options);
            return Encoding.UTF8.GetBytes(json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to serialize game state snapshot");
            throw new SerializationException("Failed to serialize save data", ex);
        }
    }

    /// <inheritdoc/>
    public GameStateSnapshot Deserialize(byte[] data)
    {
        if (data == null || data.Length == 0)
            throw new ArgumentException("Save data cannot be null or empty", nameof(data));

        try
        {
            _logger.LogDebug("Deserializing game state snapshot from JSON ({SizeBytes} bytes)", data.Length);

            var json = Encoding.UTF8.GetString(data);
            var snapshot = JsonSerializer.Deserialize<GameStateSnapshot>(json, _options);

            if (snapshot == null)
                throw new SaveCorruptedException("unknown", "Deserialization returned null");

            return snapshot;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize save data - JSON parse error");
            throw new SaveCorruptedException("unknown", $"JSON parse error: {ex.Message}");
        }
        catch (Exception ex) when (ex is not SaveCorruptedException)
        {
            _logger.LogError(ex, "Failed to deserialize save data");
            throw new SerializationException("Failed to deserialize save data", ex);
        }
    }

    /// <inheritdoc/>
    public string SerializeToString(GameStateSnapshot snapshot)
    {
        if (snapshot == null)
            throw new ArgumentNullException(nameof(snapshot));

        try
        {
            return JsonSerializer.Serialize(snapshot, _options);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to serialize game state snapshot to string");
            throw new SerializationException("Failed to serialize save data to string", ex);
        }
    }

    /// <inheritdoc/>
    public GameStateSnapshot DeserializeFromString(string data)
    {
        if (string.IsNullOrWhiteSpace(data))
            throw new ArgumentException("Save data cannot be null or empty", nameof(data));

        try
        {
            var snapshot = JsonSerializer.Deserialize<GameStateSnapshot>(data, _options);

            if (snapshot == null)
                throw new SaveCorruptedException("unknown", "Deserialization returned null");

            return snapshot;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize save data from string - JSON parse error");
            throw new SaveCorruptedException("unknown", $"JSON parse error: {ex.Message}");
        }
        catch (Exception ex) when (ex is not SaveCorruptedException)
        {
            _logger.LogError(ex, "Failed to deserialize save data from string");
            throw new SerializationException("Failed to deserialize save data from string", ex);
        }
    }

    /// <inheritdoc/>
    public string ComputeChecksum(byte[] data)
    {
        if (data == null || data.Length == 0)
            throw new ArgumentException("Data cannot be null or empty", nameof(data));

        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(data);
        return Convert.ToBase64String(hashBytes);
    }

    /// <inheritdoc/>
    public bool ValidateChecksum(byte[] data, string expectedChecksum)
    {
        if (data == null || data.Length == 0)
            throw new ArgumentException("Data cannot be null or empty", nameof(data));

        if (string.IsNullOrWhiteSpace(expectedChecksum))
            return false;

        try
        {
            var actualChecksum = ComputeChecksum(data);
            var isValid = string.Equals(actualChecksum, expectedChecksum, StringComparison.Ordinal);

            if (!isValid)
            {
                _logger.LogWarning(
                    "Checksum validation failed. Expected: {Expected}, Actual: {Actual}",
                    expectedChecksum,
                    actualChecksum);
            }

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during checksum validation");
            return false;
        }
    }
}
