using System.Text.Json;
using System.Text.Json.Serialization;

namespace PokeNET.ModAPI.DTOs;

/// <summary>
/// Represents component data for serialization and transfer.
/// </summary>
/// <remarks>
/// This DTO enables type-safe component serialization while maintaining flexibility
/// for dynamic component types. Data is stored as a JSON-serializable object.
/// </remarks>
/// <example>
/// <code>
/// // Create component data with anonymous object
/// var healthData = new ComponentData
/// {
///     Type = "HealthComponent",
///     Data = new { MaxHealth = 100, CurrentHealth = 85 }
/// };
///
/// // Serialize to JSON
/// var json = healthData.ToJson();
///
/// // Deserialize from JSON
/// var restored = ComponentData.FromJson(json);
///
/// // Access typed data
/// var health = healthData.GetData&lt;HealthComponent&gt;();
/// </code>
/// </example>
[Serializable]
public class ComponentData
{
    /// <summary>
    /// Gets or sets the component type name.
    /// </summary>
    /// <remarks>
    /// Should match the component class name (e.g., "HealthComponent", "PositionComponent").
    /// Used for component resolution and type checking.
    /// </remarks>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the component data as a serializable object.
    /// </summary>
    /// <remarks>
    /// Can be an anonymous object, dictionary, or any JSON-serializable type.
    /// </remarks>
    [JsonPropertyName("data")]
    public object? Data { get; set; }

    /// <summary>
    /// Serializes this component data to JSON.
    /// </summary>
    /// <returns>JSON string representation.</returns>
    public string ToJson()
    {
        return JsonSerializer.Serialize(this, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    /// <summary>
    /// Deserializes component data from JSON.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized component data.</returns>
    /// <exception cref="JsonException">Thrown when JSON is invalid.</exception>
    public static ComponentData FromJson(string json)
    {
        var result = JsonSerializer.Deserialize<ComponentData>(json, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        return result ?? throw new JsonException("Failed to deserialize ComponentData.");
    }

    /// <summary>
    /// Attempts to get the data as the specified type.
    /// </summary>
    /// <typeparam name="T">The target type.</typeparam>
    /// <returns>The typed data, or default if conversion fails.</returns>
    public T? GetData<T>() where T : class
    {
        if (Data == null) return null;

        if (Data is T typed)
            return typed;

        try
        {
            var json = JsonSerializer.Serialize(Data);
            return JsonSerializer.Deserialize<T>(json);
        }
        catch
        {
            return null;
        }
    }
}
