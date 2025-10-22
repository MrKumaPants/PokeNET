namespace PokeNET.Domain.Modding;

/// <summary>
/// API for interacting with the ECS (Entity Component System) world.
/// </summary>
/// <remarks>
/// <para>
/// This API provides safe access to entities and components for mods.
/// It abstracts the underlying Arch ECS implementation while maintaining performance.
/// </para>
/// <para>
/// Key concepts:
/// - **Entity**: A unique identifier (basically an int)
/// - **Component**: Data attached to an entity (struct or class)
/// - **Query**: Find entities with specific components
/// </para>
/// </remarks>
public interface IEntityApi
{
    /// <summary>
    /// Creates a new entity with the specified components.
    /// </summary>
    /// <param name="components">Components to attach to the entity.</param>
    /// <returns>The created entity.</returns>
    /// <example>
    /// <code>
    /// var entity = context.Entities.CreateEntity(
    ///     new Position(10, 20),
    ///     new Sprite("pikachu.png"),
    ///     new Health(100)
    /// );
    /// </code>
    /// </example>
    Entity CreateEntity(params object[] components);

    /// <summary>
    /// Destroys an entity and removes all its components.
    /// </summary>
    /// <param name="entity">The entity to destroy.</param>
    void DestroyEntity(Entity entity);

    /// <summary>
    /// Checks if an entity exists and is valid.
    /// </summary>
    /// <param name="entity">The entity to check.</param>
    /// <returns>True if the entity exists; otherwise false.</returns>
    bool EntityExists(Entity entity);

    /// <summary>
    /// Adds a component to an existing entity.
    /// </summary>
    /// <typeparam name="T">Component type.</typeparam>
    /// <param name="entity">The entity.</param>
    /// <param name="component">The component data.</param>
    void AddComponent<T>(Entity entity, T component);

    /// <summary>
    /// Removes a component from an entity.
    /// </summary>
    /// <typeparam name="T">Component type.</typeparam>
    /// <param name="entity">The entity.</param>
    void RemoveComponent<T>(Entity entity);

    /// <summary>
    /// Checks if an entity has a specific component.
    /// </summary>
    /// <typeparam name="T">Component type.</typeparam>
    /// <param name="entity">The entity.</param>
    /// <returns>True if the entity has the component; otherwise false.</returns>
    bool HasComponent<T>(Entity entity);

    /// <summary>
    /// Gets a component from an entity.
    /// </summary>
    /// <typeparam name="T">Component type.</typeparam>
    /// <param name="entity">The entity.</param>
    /// <returns>The component data.</returns>
    /// <exception cref="InvalidOperationException">Entity doesn't have the component.</exception>
    ref T GetComponent<T>(Entity entity);

    /// <summary>
    /// Tries to get a component from an entity.
    /// </summary>
    /// <typeparam name="T">Component type.</typeparam>
    /// <param name="entity">The entity.</param>
    /// <param name="component">The component data if found.</param>
    /// <returns>True if the entity has the component; otherwise false.</returns>
    bool TryGetComponent<T>(Entity entity, out T component);

    /// <summary>
    /// Sets/updates a component on an entity.
    /// </summary>
    /// <typeparam name="T">Component type.</typeparam>
    /// <param name="entity">The entity.</param>
    /// <param name="component">The new component data.</param>
    /// <remarks>
    /// If the entity doesn't have the component, it will be added.
    /// </remarks>
    void SetComponent<T>(Entity entity, T component);

    /// <summary>
    /// Queries entities with specific components.
    /// </summary>
    /// <typeparam name="T1">First required component type.</typeparam>
    /// <returns>Query builder for further filtering.</returns>
    /// <example>
    /// <code>
    /// // Find all entities with Position and Velocity
    /// var query = context.Entities.Query&lt;Position, Velocity&gt;();
    /// foreach (var (entity, pos, vel) in query)
    /// {
    ///     pos.X += vel.DX;
    ///     pos.Y += vel.DY;
    /// }
    /// </code>
    /// </example>
    IEntityQuery<T1> Query<T1>();

    /// <summary>
    /// Queries entities with two specific components.
    /// </summary>
    IEntityQuery<T1, T2> Query<T1, T2>();

    /// <summary>
    /// Queries entities with three specific components.
    /// </summary>
    IEntityQuery<T1, T2, T3> Query<T1, T2, T3>();

    /// <summary>
    /// Gets the total count of entities in the world.
    /// </summary>
    int EntityCount { get; }

    /// <summary>
    /// Gets all entities (use sparingly, prefer queries).
    /// </summary>
    IEnumerable<Entity> GetAllEntities();
}

/// <summary>
/// Represents an entity in the ECS world.
/// </summary>
/// <remarks>
/// An entity is a lightweight identifier (typically an int). All entity data
/// is stored in components.
/// </remarks>
public readonly struct Entity : IEquatable<Entity>
{
    /// <summary>
    /// Internal entity ID.
    /// </summary>
    public int Id { get; }

    /// <summary>
    /// Initializes a new entity with the specified ID.
    /// </summary>
    public Entity(int id) => Id = id;

    /// <summary>
    /// Represents a null/invalid entity.
    /// </summary>
    public static Entity Null => new(0);

    /// <summary>
    /// Checks if this entity is null/invalid.
    /// </summary>
    public bool IsNull => Id == 0;

    public bool Equals(Entity other) => Id == other.Id;
    public override bool Equals(object? obj) => obj is Entity other && Equals(other);
    public override int GetHashCode() => Id;
    public override string ToString() => $"Entity({Id})";

    public static bool operator ==(Entity left, Entity right) => left.Equals(right);
    public static bool operator !=(Entity left, Entity right) => !left.Equals(right);
}

/// <summary>
/// Query for entities with one component type.
/// </summary>
public interface IEntityQuery<T1> : IEnumerable<(Entity entity, T1 component1)>
{
    /// <summary>
    /// Adds a filter to exclude entities with a specific component.
    /// </summary>
    IEntityQuery<T1> Without<TExclude>();

    /// <summary>
    /// Gets the count of entities matching this query.
    /// </summary>
    int Count();

    /// <summary>
    /// Gets the first entity matching this query.
    /// </summary>
    (Entity entity, T1 component1)? FirstOrDefault();
}

/// <summary>
/// Query for entities with two component types.
/// </summary>
public interface IEntityQuery<T1, T2> : IEnumerable<(Entity entity, T1 component1, T2 component2)>
{
    /// <summary>
    /// Adds a filter to exclude entities with a specific component.
    /// </summary>
    IEntityQuery<T1, T2> Without<TExclude>();

    /// <summary>
    /// Gets the count of entities matching this query.
    /// </summary>
    int Count();

    /// <summary>
    /// Gets the first entity matching this query.
    /// </summary>
    (Entity entity, T1 component1, T2 component2)? FirstOrDefault();
}

/// <summary>
/// Query for entities with three component types.
/// </summary>
public interface IEntityQuery<T1, T2, T3> : IEnumerable<(Entity entity, T1 component1, T2 component2, T3 component3)>
{
    /// <summary>
    /// Adds a filter to exclude entities with a specific component.
    /// </summary>
    IEntityQuery<T1, T2, T3> Without<TExclude>();

    /// <summary>
    /// Gets the count of entities matching this query.
    /// </summary>
    int Count();

    /// <summary>
    /// Gets the first entity matching this query.
    /// </summary>
    (Entity entity, T1 component1, T2 component2, T3 component3)? FirstOrDefault();
}
