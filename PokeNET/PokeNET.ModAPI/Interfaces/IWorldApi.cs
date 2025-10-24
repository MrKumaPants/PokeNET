using PokeNET.ModAPI.DTOs;

namespace PokeNET.ModAPI.Interfaces;

/// <summary>
/// Provides query and access operations for the game world and ECS.
/// </summary>
/// <remarks>
/// This API enables efficient querying of entities and direct ECS access for advanced scenarios.
/// Query operations are optimized and return read-only snapshots.
/// </remarks>
/// <example>
/// <code>
/// // Query entities by predicate
/// var lowHealthEntities = api.WorldApi.QueryEntities(e =>
/// {
///     var health = api.EntityApi.GetComponent&lt;HealthComponent&gt;(e.EntityId);
///     return health?.CurrentHealth &lt; 20;
/// });
///
/// // Get all entities with a specific component
/// var pokemonEntities = api.WorldApi.GetEntitiesWithComponent&lt;PokemonComponent&gt;();
///
/// // Get total entity count
/// var count = api.WorldApi.GetEntityCount();
///
/// // Advanced: Direct ECS access
/// var world = api.WorldApi.GetWorld();
/// </code>
/// </example>
public interface IWorldApi
{
    /// <summary>
    /// Queries entities matching the specified predicate.
    /// </summary>
    /// <param name="predicate">The predicate to test entities against.</param>
    /// <returns>A read-only collection of matching entity definitions.</returns>
    /// <exception cref="ArgumentNullException">Thrown when predicate is null.</exception>
    IReadOnlyList<EntityDefinition> QueryEntities(Func<EntityDefinition, bool> predicate);

    /// <summary>
    /// Gets all entities that have the specified component type.
    /// </summary>
    /// <typeparam name="T">The component type to filter by.</typeparam>
    /// <returns>A read-only collection of entity definitions with the component.</returns>
    IReadOnlyList<EntityDefinition> GetEntitiesWithComponent<T>() where T : class;

    /// <summary>
    /// Gets the total number of active entities in the world.
    /// </summary>
    /// <returns>The entity count.</returns>
    int GetEntityCount();

    /// <summary>
    /// Gets the total number of entities with the specified tag.
    /// </summary>
    /// <param name="tag">The tag to count.</param>
    /// <returns>The number of entities with the tag.</returns>
    int GetEntityCountByTag(string tag);

    /// <summary>
    /// Gets direct access to the underlying ECS world for advanced operations.
    /// </summary>
    /// <remarks>
    /// WARNING: Direct ECS access bypasses API safety guarantees. Use with caution.
    /// This is intended for advanced mods that need maximum performance or access
    /// to features not yet exposed through the stable API.
    /// </remarks>
    /// <returns>The underlying ECS world instance.</returns>
    object GetWorld();
}
