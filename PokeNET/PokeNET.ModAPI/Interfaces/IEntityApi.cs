using PokeNET.ModAPI.DTOs;

namespace PokeNET.ModAPI.Interfaces;

/// <summary>
/// Provides operations for creating, destroying, and managing entities and their components.
/// </summary>
/// <remarks>
/// This API wraps the underlying ECS system to provide a stable interface for mods.
/// All entity operations are thread-safe and respect game lifecycle.
/// </remarks>
/// <example>
/// <code>
/// // Spawn a new entity with components
/// var definition = new EntityDefinition
/// {
///     Name = "WildPikachu",
///     Tag = "pokemon",
///     ComponentData = new List&lt;ComponentData&gt;
///     {
///         new ComponentData { Type = "HealthComponent", Data = new { MaxHealth = 100, CurrentHealth = 100 } },
///         new ComponentData { Type = "PositionComponent", Data = new { X = 10.0f, Y = 5.0f, Z = 0.0f } }
///     }
/// };
/// var entityId = api.EntityApi.SpawnEntity(definition);
/// </code>
/// </example>
public interface IEntityApi
{
    /// <summary>
    /// Spawns a new entity in the world from a definition.
    /// </summary>
    /// <param name="definition">Entity definition containing name, tag, and initial components.</param>
    /// <returns>The unique identifier of the spawned entity.</returns>
    /// <exception cref="ArgumentNullException">Thrown when definition is null.</exception>
    int SpawnEntity(EntityDefinition definition);

    /// <summary>
    /// Destroys an entity and removes it from the world.
    /// </summary>
    /// <param name="entityId">The unique identifier of the entity to destroy.</param>
    /// <returns>True if the entity was destroyed, false if it didn't exist.</returns>
    bool DestroyEntity(int entityId);

    /// <summary>
    /// Retrieves an entity definition for the given entity ID.
    /// </summary>
    /// <param name="entityId">The unique identifier of the entity.</param>
    /// <returns>The entity definition, or null if not found.</returns>
    EntityDefinition? GetEntity(int entityId);

    /// <summary>
    /// Adds a component to an existing entity.
    /// </summary>
    /// <typeparam name="T">The component type to add.</typeparam>
    /// <param name="entityId">The entity to add the component to.</param>
    /// <param name="data">The component data.</param>
    /// <exception cref="InvalidOperationException">Thrown when entity doesn't exist or already has the component.</exception>
    void AddComponent<T>(int entityId, T data) where T : class;

    /// <summary>
    /// Removes a component from an entity.
    /// </summary>
    /// <typeparam name="T">The component type to remove.</typeparam>
    /// <param name="entityId">The entity to remove the component from.</param>
    /// <returns>True if the component was removed, false if it didn't exist.</returns>
    bool RemoveComponent<T>(int entityId) where T : class;

    /// <summary>
    /// Retrieves a component from an entity.
    /// </summary>
    /// <typeparam name="T">The component type to retrieve.</typeparam>
    /// <param name="entityId">The entity to get the component from.</param>
    /// <returns>The component data, or null if not found.</returns>
    T? GetComponent<T>(int entityId) where T : class;
}
