using System.Text.Json.Serialization;

namespace PokeNET.ModAPI.DTOs;

/// <summary>
/// Represents a complete entity definition including metadata and components.
/// </summary>
/// <remarks>
/// This DTO is used for entity creation, serialization, and data transfer.
/// It provides a stable contract for entity representation across API versions.
/// </remarks>
/// <example>
/// <code>
/// var definition = new EntityDefinition
/// {
///     Name = "Pikachu",
///     Tag = "pokemon",
///     ComponentData = new List&lt;ComponentData&gt;
///     {
///         new ComponentData
///         {
///             Type = "HealthComponent",
///             Data = new { MaxHealth = 100, CurrentHealth = 100 }
///         }
///     }
/// };
/// </code>
/// </example>
[Serializable]
public class EntityDefinition
{
    /// <summary>
    /// Gets or sets the unique entity identifier.
    /// </summary>
    /// <remarks>
    /// Set to 0 for new entities; will be assigned during spawning.
    /// </remarks>
    [JsonPropertyName("entityId")]
    public int EntityId { get; set; }

    /// <summary>
    /// Gets or sets the entity name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the entity tag for categorization.
    /// </summary>
    /// <remarks>
    /// Common tags: "pokemon", "item", "npc", "player", "projectile"
    /// </remarks>
    [JsonPropertyName("tag")]
    public string Tag { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of component data attached to this entity.
    /// </summary>
    [JsonPropertyName("components")]
    public List<ComponentData> ComponentData { get; set; } = new();

    /// <summary>
    /// Converts this definition to a runtime entity representation.
    /// </summary>
    /// <returns>An object representing the entity in the ECS.</returns>
    /// <remarks>
    /// This method is used internally by the API implementation.
    /// Mod authors typically don't need to call this directly.
    /// </remarks>
    public object ToEntity()
    {
        // Implementation will be provided by the runtime
        throw new NotImplementedException("This method is implemented by the API runtime.");
    }
}
