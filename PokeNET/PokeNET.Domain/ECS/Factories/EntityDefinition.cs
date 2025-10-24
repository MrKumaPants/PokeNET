namespace PokeNET.Domain.ECS.Factories;

/// <summary>
/// Immutable data class defining an entity template with components and metadata.
/// Follows the Single Responsibility Principle - only describes entity structure.
/// </summary>
public sealed record EntityDefinition
{
    /// <summary>
    /// The descriptive name of this entity type.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// The collection of components to add to the entity.
    /// Components must be structs suitable for ECS.
    /// </summary>
    public IReadOnlyList<object> Components { get; init; } = Array.Empty<object>();

    /// <summary>
    /// Optional metadata for factory-specific logic or serialization.
    /// </summary>
    public IReadOnlyDictionary<string, object> Metadata { get; init; } =
        new Dictionary<string, object>();

    /// <summary>
    /// Creates a new entity definition.
    /// </summary>
    /// <param name="name">The entity type name.</param>
    /// <param name="components">The components to include.</param>
    /// <param name="metadata">Optional metadata.</param>
    public EntityDefinition(
        string name,
        IEnumerable<object> components,
        IReadOnlyDictionary<string, object>? metadata = null)
    {
        Name = name;
        Components = components.ToList();
        Metadata = metadata ?? new Dictionary<string, object>();
    }

    /// <summary>
    /// Creates a copy with additional components.
    /// </summary>
    /// <param name="additionalComponents">Components to add.</param>
    /// <returns>New definition with combined components.</returns>
    public EntityDefinition WithComponents(params object[] additionalComponents)
    {
        return this with
        {
            Components = Components.Concat(additionalComponents).ToList()
        };
    }

    /// <summary>
    /// Creates a copy with additional metadata.
    /// </summary>
    /// <param name="key">Metadata key.</param>
    /// <param name="value">Metadata value.</param>
    /// <returns>New definition with updated metadata.</returns>
    public EntityDefinition WithMetadata(string key, object value)
    {
        var newMetadata = new Dictionary<string, object>(Metadata)
        {
            [key] = value
        };
        return this with { Metadata = newMetadata };
    }

    /// <summary>
    /// Validates that the definition has required data.
    /// </summary>
    /// <returns>True if valid.</returns>
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(Name) && Components.Count > 0;
    }

    public override string ToString() =>
        $"{Name} ({Components.Count} components, {Metadata.Count} metadata)";
}
