using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Extensions.Logging;
using PokeNET.Core.ECS.Components;
using PokeNET.Core.ECS.Events;

namespace PokeNET.Core.ECS.Systems;

/// <summary>
/// System responsible for Pokemon-style tile-based movement with smooth interpolation.
/// Implements discrete tile-to-tile movement with collision detection and 8-directional support.
///
/// Architecture:
/// - Uses cached queries from QueryExtensions (zero allocations per frame)
/// - Checks collision with TileCollider before movement
/// - Smoothly interpolates between tiles for animation
/// - Emits MovementEvent for other systems to react to position changes
///
/// Features:
/// - 8-directional movement (cardinal + diagonal)
/// - Tile-based collision detection
/// - Smooth interpolation animation
/// - Frame-rate independent
/// - Support for different movement speeds (walk, run, surf, etc.)
///
/// Performance Optimizations:
/// - Inherits from Arch's BaseSystem&lt;World, float&gt; for lifecycle hooks
/// - Uses QueryExtensions.MovementQuery and ColliderQuery (cached, zero-allocation)
/// - BeforeUpdate: Resets per-frame counters
/// - Update: Processes movement and collision
/// - AfterUpdate: Logs performance metrics
/// - Automatic performance tracking with frame timing
/// </summary>
public partial class MovementSystem : BaseSystem<World, float>
{
    private readonly ILogger _logger;
    private readonly IEventBus? _eventBus;
    private Dictionary<int, HashSet<(int x, int y)>>? _collisionGrid; // Spatial partitioning cache
    private int _entitiesProcessed;
    private int _movementBlocked;
    private float _deltaTime; // Store deltaTime for access in query methods

    /// <summary>
    /// Initializes the movement system with logging and optional event bus.
    /// </summary>
    /// <param name="world">The ECS world instance.</param>
    /// <param name="logger">Logger instance.</param>
    /// <param name="eventBus">Optional event bus for movement notifications.</param>
    public MovementSystem(World world, ILogger<MovementSystem> logger, IEventBus? eventBus = null)
        : base(world)
    {
        _logger = logger;
        _eventBus = eventBus;
        _logger.LogInformation(
            "MovementSystem initialized with Arch BaseSystem and cached queries"
        );
    }

    /// <summary>
    /// BeforeUpdate: Prepare movement queries and reset counters.
    /// Zero-allocation preparation phase.
    /// </summary>
    public override void BeforeUpdate(in float deltaTime)
    {
        // Reset per-frame counters
        _entitiesProcessed = 0;
        _movementBlocked = 0;

        // Build collision grid for efficient spatial queries
        BuildCollisionGrid();
    }

    /// <summary>
    /// Update: Process movement with source-generated queries.
    /// Main movement processing using Arch.System.SourceGenerator.
    /// </summary>
    public override void Update(in float deltaTime)
    {
        // Store deltaTime for access in query methods
        _deltaTime = deltaTime;

        // Call source-generated query method
        // Generator creates: ProcessMovementQuery(World world)
        ProcessMovementQuery(World);
    }

    /// <summary>
    /// AfterUpdate: Update spatial partitioning and log metrics.
    /// </summary>
    public override void AfterUpdate(in float deltaTime)
    {
        // Additional logging for movement-specific metrics
        if (_entitiesProcessed > 0 || _movementBlocked > 0)
        {
            _logger.LogDebug(
                "MovementSystem - Moved: {Moved}, Blocked: {Blocked}, Collision Grid Cells: {Cells}",
                _entitiesProcessed,
                _movementBlocked,
                _collisionGrid?.Sum(kvp => kvp.Value.Count) ?? 0
            );
        }
    }

    /// <summary>
    /// Source-generated query for grid-based movement with collision detection and smooth interpolation.
    /// Uses Arch.System.SourceGenerator for zero-allocation, compile-time optimized queries.
    /// Generated method: ProcessMovementQuery(World world)
    /// </summary>
    [Query]
    [All<GridPosition, Direction, MovementState>]
    private void ProcessMovement(
        in Entity entity,
        ref GridPosition gridPos,
        ref Direction direction,
        ref MovementState movementState
    )
    {
        // Skip if movement is disabled
        if (!movementState.CanMove)
            return;

        // If not moving, check if we should start moving based on direction
        if (!gridPos.IsMoving && direction != Direction.None)
        {
            // Calculate target tile based on direction
            var (dx, dy) = direction.ToOffset();
            int targetX = gridPos.TileX + dx;
            int targetY = gridPos.TileY + dy;

            // Check collision at target tile using spatial grid
            if (CanMoveTo(targetX, targetY, gridPos.MapId, entity))
            {
                // Start movement to target tile
                gridPos.TargetTileX = targetX;
                gridPos.TargetTileY = targetY;
                gridPos.InterpolationProgress = 0.0f;
                _entitiesProcessed++;
            }
            else
            {
                _movementBlocked++;
            }
        }
        // Continue interpolation if already moving
        else if (gridPos.IsMoving)
        {
            // Calculate interpolation progress based on movement speed
            float progressPerSecond = movementState.MovementSpeed; // tiles per second
            gridPos.InterpolationProgress += progressPerSecond * _deltaTime;

            // Clamp to 1.0 and snap to target tile when complete
            if (gridPos.InterpolationProgress >= 1.0f)
            {
                gridPos.InterpolationProgress = 1.0f;
                gridPos.TileX = gridPos.TargetTileX;
                gridPos.TileY = gridPos.TargetTileY;

                // Emit movement complete event
                if (_eventBus != null)
                {
                    _eventBus.Publish(
                        new MovementEvent(
                            entity,
                            gridPos.TileX - direction.ToOffset().dx,
                            gridPos.TileY - direction.ToOffset().dy,
                            gridPos.TileX,
                            gridPos.TileY,
                            _deltaTime,
                            true
                        )
                    );
                }
            }
        }
    }

    /// <summary>
    /// Source-generated query for building spatial partitioning grid.
    /// Generated method: BuildCollisionGridQuery(World world)
    /// </summary>
    [Query]
    [All<GridPosition, TileCollider>]
    private void PopulateCollisionGrid(ref GridPosition gridPos, ref TileCollider collider)
    {
        if (!collider.IsSolid)
            return;

        if (!_collisionGrid!.TryGetValue(gridPos.MapId, out var mapSet))
        {
            mapSet = new HashSet<(int x, int y)>();
            _collisionGrid[gridPos.MapId] = mapSet;
        }

        mapSet.Add((gridPos.TileX, gridPos.TileY));
    }

    /// <summary>
    /// Builds a spatial partitioning grid for efficient collision detection.
    /// Called in BeforeUpdate to prepare collision data.
    /// </summary>
    private void BuildCollisionGrid()
    {
        // Initialize or clear the collision grid
        _collisionGrid ??= new Dictionary<int, HashSet<(int x, int y)>>();

        foreach (var mapSet in _collisionGrid.Values)
        {
            mapSet.Clear();
        }

        // Call source-generated query to populate collision grid
        PopulateCollisionGridQuery(World);
    }

    // Store collision check context for source-generated query
    private int _targetX,
        _targetY,
        _mapId;
    private Entity _movingEntity;

    /// <summary>
    /// Source-generated query for collision triggering.
    /// Generated method: TriggerCollisionQuery(World world)
    /// </summary>
    [Query]
    [All<GridPosition, TileCollider>]
    private void CheckCollision(
        in Entity entity,
        ref GridPosition gridPos,
        ref TileCollider collider
    )
    {
        if (
            entity != _movingEntity
            && gridPos.MapId == _mapId
            && gridPos.TileX == _targetX
            && gridPos.TileY == _targetY
            && collider.IsSolid
        )
        {
            collider.TriggerCollision((uint)_movingEntity.Id);
        }
    }

    /// <summary>
    /// Checks if an entity can move to the specified tile coordinates.
    /// Uses spatial partitioning grid for O(1) lookup instead of O(n) query.
    /// </summary>
    /// <param name="targetX">Target tile X coordinate</param>
    /// <param name="targetY">Target tile Y coordinate</param>
    /// <param name="mapId">Map ID to check</param>
    /// <param name="movingEntity">Entity attempting to move</param>
    /// <returns>True if movement is allowed, false if blocked</returns>
    private bool CanMoveTo(int targetX, int targetY, int mapId, Entity movingEntity)
    {
        // Use spatial grid for fast O(1) lookup
        if (_collisionGrid != null && _collisionGrid.TryGetValue(mapId, out var mapSet))
        {
            if (mapSet.Contains((targetX, targetY)))
            {
                // Store context for source-generated query
                _targetX = targetX;
                _targetY = targetY;
                _mapId = mapId;
                _movingEntity = movingEntity;

                // Trigger collision event on the blocking entity using source-generated query
                CheckCollisionQuery(World);

                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Gets the interpolated world position for smooth animation between tiles.
    /// </summary>
    /// <param name="gridPos">Grid position with interpolation data</param>
    /// <returns>Interpolated world position in pixels</returns>
    public static Vector2 GetInterpolatedPosition(GridPosition gridPos)
    {
        if (!gridPos.IsMoving)
            return gridPos.WorldPosition;

        // Linear interpolation between current and target tile
        float startX = gridPos.TileX * 16f;
        float startY = gridPos.TileY * 16f;
        float endX = gridPos.TargetTileX * 16f;
        float endY = gridPos.TargetTileY * 16f;

        float interpolatedX = startX + (endX - startX) * gridPos.InterpolationProgress;
        float interpolatedY = startY + (endY - startY) * gridPos.InterpolationProgress;

        return new Vector2(interpolatedX, interpolatedY);
    }

    /// <summary>
    /// Gets statistics about entities processed in the last frame.
    /// Useful for debugging and performance monitoring.
    /// </summary>
    public int GetProcessedCount() => _entitiesProcessed;

    /// <summary>
    /// Gets the number of movement attempts that were blocked by collision.
    /// </summary>
    public int GetBlockedCount() => _movementBlocked;

    /// <summary>
    /// Cleanup resources on disposal.
    /// </summary>
    public override void Dispose()
    {
        _collisionGrid?.Clear();
        _collisionGrid = null;
        base.Dispose();
    }
}
