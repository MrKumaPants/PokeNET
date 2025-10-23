// SOLID Principles Applied:
// - Single Responsibility: API only handles safe ECS world interaction
// - Open/Closed: Extensible through composition (add new API surfaces without modifying)
// - Liskov Substitution: Implementations can add features but must support base contract
// - Interface Segregation: Split into focused sub-interfaces (IEntityApi, IComponentApi, etc.)
// - Dependency Inversion: Scripts depend on abstraction, not concrete ECS implementation

using System;
using System.Collections.Generic;

namespace PokeNET.Scripting.Abstractions;

/// <summary>
/// Main API interface for scripts to interact with the ECS world safely.
/// Provides controlled access to entities, components, systems, and events.
/// </summary>
/// <remarks>
/// <para><b>Design Philosophy:</b></para>
/// <list type="bullet">
///   <item>Scripts cannot directly access the ECS World - must go through this API</item>
///   <item>All operations are validated and sandboxed for safety</item>
///   <item>Follows "principle of least privilege" - only necessary operations exposed</item>
///   <item>Segregated into focused sub-APIs for different concerns</item>
/// </list>
/// <para><b>Security Boundaries:</b></para>
/// <list type="bullet">
///   <item>Scripts cannot access internal engine components</item>
///   <item>Scripts cannot break ECS invariants</item>
///   <item>Scripts cannot cause memory leaks or corruption</item>
///   <item>All queries are validated and bounded</item>
/// </list>
/// <para><b>SOLID Alignment:</b></para>
/// <list type="bullet">
///   <item><b>ISP:</b> Interface segregation through sub-APIs (Entities, Components, Events)</item>
///   <item><b>SRP:</b> Each sub-API has a single, well-defined responsibility</item>
///   <item><b>OCP:</b> New APIs can be added through composition without modification</item>
/// </list>
/// </remarks>
public interface IScriptApi
{
    /// <summary>
    /// Gets the entity API for querying and manipulating entities.
    /// </summary>
    IEntityApi Entities { get; }

    /// <summary>
    /// Gets the component API for adding, removing, and querying components.
    /// </summary>
    IComponentApi Components { get; }

    /// <summary>
    /// Gets the event API for subscribing to and emitting game events.
    /// </summary>
    IEventApi Events { get; }

    /// <summary>
    /// Gets the query API for performing complex ECS queries.
    /// </summary>
    IQueryApi Queries { get; }

    /// <summary>
    /// Gets the resource API for accessing singleton game resources.
    /// </summary>
    IResourceApi Resources { get; }
}

/// <summary>
/// API for entity operations.
/// Follows Interface Segregation Principle by focusing only on entity lifecycle.
/// </summary>
public interface IEntityApi
{
    /// <summary>
    /// Creates a new entity in the world.
    /// </summary>
    /// <returns>A unique entity identifier.</returns>
    /// <remarks>
    /// The entity is initially created without any components.
    /// Use <see cref="IComponentApi"/> to add components.
    /// </remarks>
    ulong CreateEntity();

    /// <summary>
    /// Creates a new entity with a human-readable name.
    /// </summary>
    /// <param name="name">The name for the entity (for debugging/logging).</param>
    /// <returns>A unique entity identifier.</returns>
    ulong CreateEntity(string name);

    /// <summary>
    /// Destroys an entity and all its components.
    /// </summary>
    /// <param name="entityId">The entity to destroy.</param>
    /// <returns>True if the entity existed and was destroyed; otherwise false.</returns>
    /// <remarks>
    /// This operation is deferred until the end of the current system execution.
    /// Components are automatically cleaned up.
    /// </remarks>
    bool DestroyEntity(ulong entityId);

    /// <summary>
    /// Checks if an entity exists in the world.
    /// </summary>
    /// <param name="entityId">The entity to check.</param>
    /// <returns>True if the entity exists; otherwise false.</returns>
    bool EntityExists(ulong entityId);

    /// <summary>
    /// Gets the name of an entity (if it has one).
    /// </summary>
    /// <param name="entityId">The entity to query.</param>
    /// <returns>The entity name, or null if no name was set.</returns>
    string? GetEntityName(ulong entityId);

    /// <summary>
    /// Sets the name of an entity.
    /// </summary>
    /// <param name="entityId">The entity to name.</param>
    /// <param name="name">The name to assign.</param>
    void SetEntityName(ulong entityId, string name);

    /// <summary>
    /// Clones an entity, creating a copy with all its components.
    /// </summary>
    /// <param name="entityId">The entity to clone.</param>
    /// <returns>The ID of the new cloned entity.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the entity doesn't exist.</exception>
    ulong CloneEntity(ulong entityId);
}

/// <summary>
/// API for component operations.
/// Provides type-safe component manipulation with validation.
/// </summary>
public interface IComponentApi
{
    /// <summary>
    /// Adds a component to an entity.
    /// </summary>
    /// <typeparam name="T">The component type (must be a struct).</typeparam>
    /// <param name="entityId">The entity to add the component to.</param>
    /// <param name="component">The component data.</param>
    /// <exception cref="InvalidOperationException">Thrown if entity doesn't exist or already has this component.</exception>
    void AddComponent<T>(ulong entityId, T component) where T : struct;

    /// <summary>
    /// Removes a component from an entity.
    /// </summary>
    /// <typeparam name="T">The component type.</typeparam>
    /// <param name="entityId">The entity to remove the component from.</param>
    /// <returns>True if the component was removed; false if it didn't exist.</returns>
    bool RemoveComponent<T>(ulong entityId) where T : struct;

    /// <summary>
    /// Gets a component from an entity.
    /// </summary>
    /// <typeparam name="T">The component type.</typeparam>
    /// <param name="entityId">The entity to query.</param>
    /// <returns>The component data.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the entity doesn't have this component.</exception>
    T GetComponent<T>(ulong entityId) where T : struct;

    /// <summary>
    /// Attempts to get a component from an entity.
    /// </summary>
    /// <typeparam name="T">The component type.</typeparam>
    /// <param name="entityId">The entity to query.</param>
    /// <param name="component">The component data if found.</param>
    /// <returns>True if the component exists; otherwise false.</returns>
    bool TryGetComponent<T>(ulong entityId, out T component) where T : struct;

    /// <summary>
    /// Updates a component on an entity.
    /// </summary>
    /// <typeparam name="T">The component type.</typeparam>
    /// <param name="entityId">The entity to update.</param>
    /// <param name="component">The new component data.</param>
    /// <exception cref="InvalidOperationException">Thrown if the entity doesn't have this component.</exception>
    void SetComponent<T>(ulong entityId, T component) where T : struct;

    /// <summary>
    /// Checks if an entity has a specific component.
    /// </summary>
    /// <typeparam name="T">The component type.</typeparam>
    /// <param name="entityId">The entity to check.</param>
    /// <returns>True if the entity has this component; otherwise false.</returns>
    bool HasComponent<T>(ulong entityId) where T : struct;

    /// <summary>
    /// Gets all component types attached to an entity.
    /// </summary>
    /// <param name="entityId">The entity to query.</param>
    /// <returns>A collection of component type names.</returns>
    IReadOnlyList<string> GetComponentTypes(ulong entityId);
}

/// <summary>
/// API for event operations.
/// Enables event-driven scripting with safe publish/subscribe.
/// </summary>
public interface IEventApi
{
    /// <summary>
    /// Subscribes to an event by type.
    /// </summary>
    /// <typeparam name="T">The event type.</typeparam>
    /// <param name="handler">The callback to invoke when the event occurs.</param>
    /// <returns>A subscription token that can be used to unsubscribe.</returns>
    /// <remarks>
    /// Subscriptions are automatically cleaned up when the script context is disposed.
    /// </remarks>
    IEventSubscription Subscribe<T>(Action<T> handler) where T : class;

    /// <summary>
    /// Subscribes to an event by name (for dynamic languages).
    /// </summary>
    /// <param name="eventName">The name of the event.</param>
    /// <param name="handler">The callback (receives the event as an object).</param>
    /// <returns>A subscription token.</returns>
    IEventSubscription Subscribe(string eventName, Action<object> handler);

    /// <summary>
    /// Unsubscribes from an event.
    /// </summary>
    /// <param name="subscription">The subscription to cancel.</param>
    void Unsubscribe(IEventSubscription subscription);

    /// <summary>
    /// Emits an event to all subscribers.
    /// </summary>
    /// <typeparam name="T">The event type.</typeparam>
    /// <param name="eventData">The event data to send.</param>
    /// <remarks>
    /// Events are processed asynchronously to avoid blocking the script.
    /// </remarks>
    void Emit<T>(T eventData) where T : class;

    /// <summary>
    /// Emits an event by name (for dynamic languages).
    /// </summary>
    /// <param name="eventName">The name of the event.</param>
    /// <param name="eventData">The event data.</param>
    void Emit(string eventName, object eventData);
}

/// <summary>
/// Represents a subscription to an event.
/// </summary>
public interface IEventSubscription : IDisposable
{
    /// <summary>
    /// Gets the unique identifier for this subscription.
    /// </summary>
    Guid SubscriptionId { get; }

    /// <summary>
    /// Gets the event name this subscription is for.
    /// </summary>
    string EventName { get; }

    /// <summary>
    /// Gets a value indicating whether this subscription is still active.
    /// </summary>
    bool IsActive { get; }
}

/// <summary>
/// API for complex ECS queries.
/// Follows builder pattern for composable query construction.
/// </summary>
public interface IQueryApi
{
    /// <summary>
    /// Creates a new query builder for entities with specific components.
    /// </summary>
    /// <returns>A query builder for fluent query construction.</returns>
    IQueryBuilder CreateQuery();

    /// <summary>
    /// Gets all entities that have a specific component type.
    /// </summary>
    /// <typeparam name="T">The component type to filter by.</typeparam>
    /// <returns>Collection of entity IDs that have the component.</returns>
    IReadOnlyList<ulong> GetEntitiesWith<T>() where T : struct;

    /// <summary>
    /// Gets the count of entities matching a component filter.
    /// </summary>
    /// <typeparam name="T">The component type.</typeparam>
    /// <returns>Number of entities with this component.</returns>
    int CountEntitiesWith<T>() where T : struct;
}

/// <summary>
/// Fluent builder for constructing complex ECS queries.
/// </summary>
public interface IQueryBuilder
{
    /// <summary>
    /// Filters for entities that have a specific component.
    /// </summary>
    /// <typeparam name="T">The required component type.</typeparam>
    /// <returns>This builder for chaining.</returns>
    IQueryBuilder With<T>() where T : struct;

    /// <summary>
    /// Filters for entities that do NOT have a specific component.
    /// </summary>
    /// <typeparam name="T">The excluded component type.</typeparam>
    /// <returns>This builder for chaining.</returns>
    IQueryBuilder Without<T>() where T : struct;

    /// <summary>
    /// Limits the number of results returned.
    /// </summary>
    /// <param name="limit">Maximum number of entities to return.</param>
    /// <returns>This builder for chaining.</returns>
    IQueryBuilder Limit(int limit);

    /// <summary>
    /// Executes the query and returns matching entities.
    /// </summary>
    /// <returns>Collection of entity IDs matching the query.</returns>
    IReadOnlyList<ulong> Execute();
}

/// <summary>
/// API for accessing singleton game resources.
/// Resources are global state accessible to all systems and scripts.
/// </summary>
public interface IResourceApi
{
    /// <summary>
    /// Gets a singleton resource from the world.
    /// </summary>
    /// <typeparam name="T">The resource type.</typeparam>
    /// <returns>The resource instance.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the resource doesn't exist.</exception>
    T GetResource<T>() where T : class;

    /// <summary>
    /// Attempts to get a singleton resource from the world.
    /// </summary>
    /// <typeparam name="T">The resource type.</typeparam>
    /// <param name="resource">The resource if found.</param>
    /// <returns>True if the resource exists; otherwise false.</returns>
    bool TryGetResource<T>(out T? resource) where T : class;

    /// <summary>
    /// Checks if a resource exists in the world.
    /// </summary>
    /// <typeparam name="T">The resource type.</typeparam>
    /// <returns>True if the resource exists; otherwise false.</returns>
    bool HasResource<T>() where T : class;
}
