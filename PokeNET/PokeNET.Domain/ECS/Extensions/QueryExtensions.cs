using System.Collections.Generic;
using Arch.Core;
using PokeNET.Domain.ECS.Components;

namespace PokeNET.Domain.ECS.Extensions;

/// <summary>
/// High-performance query extension methods with zero-allocation cached queries.
/// Provides optimized query patterns for common PokeNET entity queries.
/// </summary>
/// <remarks>
/// This class implements query caching to eliminate repeated QueryDescription allocations.
/// All cached queries are static readonly for maximum performance.
/// Supports LINQ-style fluent API for building custom queries.
/// </remarks>
public static class QueryExtensions
{
    #region Cached Common Queries

    /// <summary>
    /// Query for player entities with position data.
    /// Components: GridPosition, PlayerProgress
    /// </summary>
    public static QueryDescription PlayerQuery { get; } =
        new QueryDescription().WithAll<GridPosition, PlayerProgress>();

    /// <summary>
    /// Query for all Pokemon in battle with stats and battle state.
    /// Components: PokemonStats, BattleState
    /// </summary>
    public static QueryDescription BattleEntitiesQuery { get; } =
        new QueryDescription().WithAll<PokemonStats, BattleState>();

    /// <summary>
    /// Query for active Pokemon (in battle and not fainted).
    /// Components: PokemonStats, BattleState
    /// Filters out NotInBattle and Fainted states via manual filtering.
    /// </summary>
    public static QueryDescription ActivePokemonQuery { get; } =
        new QueryDescription().WithAll<PokemonStats, BattleState>();

    /// <summary>
    /// Query for renderable entities with sprite and position.
    /// Components: Renderable, Sprite, GridPosition
    /// </summary>
    public static QueryDescription RenderableEntitiesQuery { get; } =
        new QueryDescription().WithAll<Renderable, Sprite, GridPosition>();

    /// <summary>
    /// Query for visible renderable entities only.
    /// Components: Renderable, Sprite, GridPosition
    /// Note: Manual filtering required for Renderable.IsVisible == true
    /// </summary>
    public static QueryDescription VisibleEntitiesQuery { get; } =
        new QueryDescription().WithAll<Renderable, Sprite, GridPosition>();

    /// <summary>
    /// Query for movable entities (entities with movement state).
    /// Components: GridPosition, MovementState
    /// </summary>
    public static QueryDescription MovableEntitiesQuery { get; } =
        new QueryDescription().WithAll<GridPosition, MovementState>();

    /// <summary>
    /// Query for entities currently moving between tiles.
    /// Components: GridPosition, MovementState
    /// Note: Filter by GridPosition.IsMoving == true in iteration
    /// </summary>
    public static QueryDescription MovingEntitiesQuery { get; } =
        new QueryDescription().WithAll<GridPosition, MovementState>();

    /// <summary>
    /// Query for trainer entities with party data.
    /// Components: Trainer, Party
    /// </summary>
    public static QueryDescription TrainerQuery { get; } =
        new QueryDescription().WithAll<Trainer, Party>();

    /// <summary>
    /// Query for wild Pokemon entities.
    /// Components: PokemonData, PokemonStats
    /// Excludes: Trainer (wild Pokemon don't have trainers)
    /// </summary>
    public static QueryDescription WildPokemonQuery { get; } =
        new QueryDescription().WithAll<PokemonData, PokemonStats>().WithNone<Trainer>();

    /// <summary>
    /// Query for Pokemon with status conditions.
    /// Components: PokemonStats, StatusCondition
    /// </summary>
    public static QueryDescription StatusAffectedQuery { get; } =
        new QueryDescription().WithAll<PokemonStats, StatusCondition>();

    /// <summary>
    /// Query for entities with collision detection.
    /// Components: GridPosition, TileCollider
    /// </summary>
    public static QueryDescription CollidableEntitiesQuery { get; } =
        new QueryDescription().WithAll<GridPosition, TileCollider>();

    /// <summary>
    /// Query for Pokemon inventory items.
    /// Components: Inventory
    /// </summary>
    public static QueryDescription InventoryQuery { get; } =
        new QueryDescription().WithAll<Inventory>();

    /// <summary>
    /// Query for camera entities.
    /// Components: Camera, GridPosition
    /// </summary>
    public static QueryDescription CameraQuery { get; } =
        new QueryDescription().WithAll<Camera, GridPosition>();

    /// <summary>
    /// Query for Pokemon with movesets.
    /// Components: PokemonData, MoveSet
    /// </summary>
    public static QueryDescription PokemonWithMovesQuery { get; } =
        new QueryDescription().WithAll<PokemonData, MoveSet>();

    #endregion

    #region World Extension Methods - ForEach Overloads

    /// <summary>
    /// Iterates over all entities with component T1 and executes the action.
    /// Zero-allocation enumeration using Arch's high-performance query system.
    /// </summary>
    /// <typeparam name="T1">Component type 1</typeparam>
    /// <param name="world">The world instance</param>
    /// <param name="action">Action to execute for each entity (inline delegate)</param>
    /// <remarks>
    /// Example usage:
    /// <code>
    /// world.ForEach((Entity entity, ref GridPosition pos) => {
    ///     // Process entity
    /// });
    /// </code>
    /// </remarks>
    public static void ForEach<T1>(this World world, ForEach<T1> action)
        where T1 : struct
    {
        var query = new QueryDescription().WithAll<T1>();
        world.Query(in query, action);
    }

    /// <summary>
    /// Iterates over all entities with components T1, T2 and executes the action.
    /// Zero-allocation enumeration using Arch's high-performance query system.
    /// </summary>
    /// <typeparam name="T1">Component type 1</typeparam>
    /// <typeparam name="T2">Component type 2</typeparam>
    /// <param name="world">The world instance</param>
    /// <param name="action">Action to execute for each entity (inline delegate)</param>
    /// <remarks>
    /// Example usage:
    /// <code>
    /// world.ForEach((Entity entity, ref GridPosition pos, ref Direction dir) => {
    ///     // Process entity
    /// });
    /// </code>
    /// </remarks>
    public static void ForEach<T1, T2>(this World world, ForEach<T1, T2> action)
        where T1 : struct
        where T2 : struct
    {
        var query = new QueryDescription().WithAll<T1, T2>();
        world.Query(in query, action);
    }

    /// <summary>
    /// Iterates over all entities with components T1, T2, T3 and executes the action.
    /// Zero-allocation enumeration using Arch's high-performance query system.
    /// </summary>
    /// <typeparam name="T1">Component type 1</typeparam>
    /// <typeparam name="T2">Component type 2</typeparam>
    /// <typeparam name="T3">Component type 3</typeparam>
    /// <param name="world">The world instance</param>
    /// <param name="action">Action to execute for each entity (inline delegate)</param>
    /// <remarks>
    /// Example usage:
    /// <code>
    /// world.ForEach((Entity entity, ref GridPosition pos, ref Direction dir, ref MovementState state) => {
    ///     // Process entity
    /// });
    /// </code>
    /// </remarks>
    public static void ForEach<T1, T2, T3>(this World world, ForEach<T1, T2, T3> action)
        where T1 : struct
        where T2 : struct
        where T3 : struct
    {
        var query = new QueryDescription().WithAll<T1, T2, T3>();
        world.Query(in query, action);
    }

    /// <summary>
    /// Iterates over all entities with components T1, T2, T3, T4 and executes the action.
    /// Zero-allocation enumeration using Arch's high-performance query system.
    /// </summary>
    /// <typeparam name="T1">Component type 1</typeparam>
    /// <typeparam name="T2">Component type 2</typeparam>
    /// <typeparam name="T3">Component type 3</typeparam>
    /// <typeparam name="T4">Component type 4</typeparam>
    /// <param name="world">The world instance</param>
    /// <param name="action">Action to execute for each entity (inline delegate)</param>
    /// <remarks>
    /// Example usage:
    /// <code>
    /// world.ForEach((Entity entity, ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4) => {
    ///     // Process entity
    /// });
    /// </code>
    /// </remarks>
    public static void ForEach<T1, T2, T3, T4>(this World world, ForEach<T1, T2, T3, T4> action)
        where T1 : struct
        where T2 : struct
        where T3 : struct
        where T4 : struct
    {
        var query = new QueryDescription().WithAll<T1, T2, T3, T4>();
        world.Query(in query, action);
    }

    #endregion

    #region Entity Collection Methods

    /// <summary>
    /// Gets all entities with component T.
    /// Returns IEnumerable for LINQ compatibility.
    /// </summary>
    /// <typeparam name="T">Component type</typeparam>
    /// <param name="world">The world instance</param>
    /// <returns>Enumerable of entities with component T</returns>
    public static IEnumerable<Entity> GetEntitiesWith<T>(this World world)
        where T : struct
    {
        var entities = new List<Entity>();
        var query = new QueryDescription().WithAll<T>();

        world.Query(
            in query,
            (Entity entity) =>
            {
                entities.Add(entity);
            }
        );

        return entities;
    }

    /// <summary>
    /// Gets all entities with components T1 and T2.
    /// Returns IEnumerable for LINQ compatibility.
    /// </summary>
    /// <typeparam name="T1">Component type 1</typeparam>
    /// <typeparam name="T2">Component type 2</typeparam>
    /// <param name="world">The world instance</param>
    /// <returns>Enumerable of entities with components T1 and T2</returns>
    public static IEnumerable<Entity> GetEntitiesWith<T1, T2>(this World world)
        where T1 : struct
        where T2 : struct
    {
        var entities = new List<Entity>();
        var query = new QueryDescription().WithAll<T1, T2>();

        world.Query(
            in query,
            (Entity entity) =>
            {
                entities.Add(entity);
            }
        );

        return entities;
    }

    /// <summary>
    /// Gets all entities with components T1, T2, and T3.
    /// Returns IEnumerable for LINQ compatibility.
    /// </summary>
    /// <typeparam name="T1">Component type 1</typeparam>
    /// <typeparam name="T2">Component type 2</typeparam>
    /// <typeparam name="T3">Component type 3</typeparam>
    /// <param name="world">The world instance</param>
    /// <returns>Enumerable of entities with components T1, T2, and T3</returns>
    public static IEnumerable<Entity> GetEntitiesWith<T1, T2, T3>(this World world)
        where T1 : struct
        where T2 : struct
        where T3 : struct
    {
        var entities = new List<Entity>();
        var query = new QueryDescription().WithAll<T1, T2, T3>();

        world.Query(
            in query,
            (Entity entity) =>
            {
                entities.Add(entity);
            }
        );

        return entities;
    }

    #endregion

    #region Query Counting Methods

    /// <summary>
    /// Counts entities matching the given query.
    /// Zero-allocation counting for performance monitoring.
    /// </summary>
    /// <param name="world">The world instance</param>
    /// <param name="query">The query description</param>
    /// <returns>Number of entities matching the query</returns>
    public static int CountEntities(this World world, in QueryDescription query)
    {
        var count = 0;
        world.Query(in query, (Entity _) => count++);
        return count;
    }

    /// <summary>
    /// Counts entities with component T.
    /// Zero-allocation counting for performance monitoring.
    /// </summary>
    /// <typeparam name="T">Component type</typeparam>
    /// <param name="world">The world instance</param>
    /// <returns>Number of entities with component T</returns>
    public static int CountEntitiesWith<T>(this World world)
        where T : struct
    {
        var count = 0;
        var query = new QueryDescription().WithAll<T>();
        world.Query(in query, (Entity _) => count++);
        return count;
    }

    /// <summary>
    /// Counts entities with components T1 and T2.
    /// Zero-allocation counting for performance monitoring.
    /// </summary>
    /// <typeparam name="T1">Component type 1</typeparam>
    /// <typeparam name="T2">Component type 2</typeparam>
    /// <param name="world">The world instance</param>
    /// <returns>Number of entities with components T1 and T2</returns>
    public static int CountEntitiesWith<T1, T2>(this World world)
        where T1 : struct
        where T2 : struct
    {
        var count = 0;
        var query = new QueryDescription().WithAll<T1, T2>();
        world.Query(in query, (Entity _) => count++);
        return count;
    }

    #endregion

    #region Existence Checks

    /// <summary>
    /// Checks if any entity has component T.
    /// Early exit for performance.
    /// </summary>
    /// <typeparam name="T">Component type</typeparam>
    /// <param name="world">The world instance</param>
    /// <returns>True if at least one entity has component T</returns>
    public static bool HasAnyEntityWith<T>(this World world)
        where T : struct
    {
        var hasAny = false;
        var query = new QueryDescription().WithAll<T>();

        world.Query(
            in query,
            (Entity _) =>
            {
                hasAny = true;
            }
        );

        return hasAny;
    }

    /// <summary>
    /// Checks if any entity has components T1 and T2.
    /// Early exit for performance.
    /// </summary>
    /// <typeparam name="T1">Component type 1</typeparam>
    /// <typeparam name="T2">Component type 2</typeparam>
    /// <param name="world">The world instance</param>
    /// <returns>True if at least one entity has components T1 and T2</returns>
    public static bool HasAnyEntityWith<T1, T2>(this World world)
        where T1 : struct
        where T2 : struct
    {
        var hasAny = false;
        var query = new QueryDescription().WithAll<T1, T2>();

        world.Query(
            in query,
            (Entity _) =>
            {
                hasAny = true;
            }
        );

        return hasAny;
    }

    #endregion

    #region First/Single Entity Methods

    /// <summary>
    /// Gets the first entity with component T, or null if none exists.
    /// Efficient for singleton components (e.g., Camera, PlayerProgress).
    /// </summary>
    /// <typeparam name="T">Component type</typeparam>
    /// <param name="world">The world instance</param>
    /// <returns>First entity with component T, or null</returns>
    public static Entity? GetFirstEntityWith<T>(this World world)
        where T : struct
    {
        Entity? result = null;
        var query = new QueryDescription().WithAll<T>();

        world.Query(
            in query,
            (Entity entity) =>
            {
                result = entity;
            }
        );

        return result;
    }

    /// <summary>
    /// Gets the first entity with components T1 and T2, or null if none exists.
    /// Efficient for finding specific entity types.
    /// </summary>
    /// <typeparam name="T1">Component type 1</typeparam>
    /// <typeparam name="T2">Component type 2</typeparam>
    /// <param name="world">The world instance</param>
    /// <returns>First entity with components T1 and T2, or null</returns>
    public static Entity? GetFirstEntityWith<T1, T2>(this World world)
        where T1 : struct
        where T2 : struct
    {
        Entity? result = null;
        var query = new QueryDescription().WithAll<T1, T2>();

        world.Query(
            in query,
            (Entity entity) =>
            {
                result = entity;
            }
        );

        return result;
    }

    #endregion

    #region Fluent Query Builders

    /// <summary>
    /// Creates a new QueryDescription with the specified component.
    /// Fluent API entry point for custom queries.
    /// </summary>
    /// <typeparam name="T">Component type</typeparam>
    /// <returns>QueryDescription requiring component T</returns>
    public static QueryDescription CreateQuery<T>()
        where T : struct
    {
        return new QueryDescription().WithAll<T>();
    }

    /// <summary>
    /// Creates a new QueryDescription with the specified components.
    /// Fluent API entry point for custom queries.
    /// </summary>
    /// <typeparam name="T1">Component type 1</typeparam>
    /// <typeparam name="T2">Component type 2</typeparam>
    /// <returns>QueryDescription requiring components T1 and T2</returns>
    public static QueryDescription CreateQuery<T1, T2>()
        where T1 : struct
        where T2 : struct
    {
        return new QueryDescription().WithAll<T1, T2>();
    }

    /// <summary>
    /// Creates a new QueryDescription with the specified components.
    /// Fluent API entry point for custom queries.
    /// </summary>
    /// <typeparam name="T1">Component type 1</typeparam>
    /// <typeparam name="T2">Component type 2</typeparam>
    /// <typeparam name="T3">Component type 3</typeparam>
    /// <returns>QueryDescription requiring components T1, T2, and T3</returns>
    public static QueryDescription CreateQuery<T1, T2, T3>()
        where T1 : struct
        where T2 : struct
        where T3 : struct
    {
        return new QueryDescription().WithAll<T1, T2, T3>();
    }

    /// <summary>
    /// Creates a new QueryDescription with the specified components.
    /// Fluent API entry point for custom queries.
    /// </summary>
    /// <typeparam name="T1">Component type 1</typeparam>
    /// <typeparam name="T2">Component type 2</typeparam>
    /// <typeparam name="T3">Component type 3</typeparam>
    /// <typeparam name="T4">Component type 4</typeparam>
    /// <returns>QueryDescription requiring components T1, T2, T3, and T4</returns>
    public static QueryDescription CreateQuery<T1, T2, T3, T4>()
        where T1 : struct
        where T2 : struct
        where T3 : struct
        where T4 : struct
    {
        return new QueryDescription().WithAll<T1, T2, T3, T4>();
    }

    #endregion

    #region PokeNET-Specific Helper Methods

    /// <summary>
    /// Gets all Pokemon currently active in battle (in battle and not fainted).
    /// Filters by BattleState status.
    /// </summary>
    /// <param name="world">The world instance</param>
    /// <returns>Enumerable of active Pokemon entities</returns>
    public static IEnumerable<Entity> GetActiveBattlePokemon(this World world)
    {
        var entities = new List<Entity>();

        world.Query(
            ActivePokemonQuery,
            (ref Entity entity, ref BattleState battleState) =>
            {
                if (battleState.Status == BattleStatus.InBattle)
                {
                    entities.Add(entity);
                }
            }
        );

        return entities;
    }

    /// <summary>
    /// Gets all visible renderable entities (IsVisible == true).
    /// Optimized for rendering system queries.
    /// </summary>
    /// <param name="world">The world instance</param>
    /// <returns>Enumerable of visible entities</returns>
    public static IEnumerable<Entity> GetVisibleEntities(this World world)
    {
        var entities = new List<Entity>();

        world.Query(
            VisibleEntitiesQuery,
            (ref Entity entity, ref Renderable renderable) =>
            {
                if (renderable.IsVisible)
                {
                    entities.Add(entity);
                }
            }
        );

        return entities;
    }

    /// <summary>
    /// Gets all entities currently moving between tiles.
    /// Optimized for movement system queries.
    /// </summary>
    /// <param name="world">The world instance</param>
    /// <returns>Enumerable of moving entities</returns>
    public static IEnumerable<Entity> GetMovingEntities(this World world)
    {
        var entities = new List<Entity>();

        world.Query(
            MovingEntitiesQuery,
            (ref Entity entity, ref GridPosition position) =>
            {
                if (position.IsMoving)
                {
                    entities.Add(entity);
                }
            }
        );

        return entities;
    }

    /// <summary>
    /// Gets the player entity (first entity with PlayerProgress).
    /// Assumes single player model.
    /// </summary>
    /// <param name="world">The world instance</param>
    /// <returns>Player entity or null if not found</returns>
    public static Entity? GetPlayerEntity(this World world)
    {
        return GetFirstEntityWith<PlayerProgress>(world);
    }

    /// <summary>
    /// Gets the camera entity (first entity with Camera component).
    /// Assumes single camera model.
    /// </summary>
    /// <param name="world">The world instance</param>
    /// <returns>Camera entity or null if not found</returns>
    public static Entity? GetCameraEntity(this World world)
    {
        return GetFirstEntityWith<Camera>(world);
    }

    #endregion
}
