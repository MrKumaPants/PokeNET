using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Extensions.Logging;
using PokeNET.Domain.ECS.Components;
using PokeNET.Domain.ECS.Events;
using System.Numerics;

namespace PokeNET.Domain.ECS.Systems;

/// <summary>
/// System responsible for Pokemon-style tile-based movement with smooth interpolation.
/// Implements discrete tile-to-tile movement with collision detection and 8-directional support.
///
/// Architecture:
/// - Queries entities with GridPosition + Direction + MovementState components
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
/// Performance:
/// - Uses efficient Arch queries with component filtering
/// - Collision checks only against entities on target tile
/// - Zero allocation for component updates
/// </summary>
public class MovementSystem : SystemBase
{
    private readonly IEventBus? _eventBus;
    private QueryDescription _movementQuery;
    private QueryDescription _colliderQuery;
    private int _entitiesProcessed;
    private int _movementBlocked;

    /// <inheritdoc/>
    public override int Priority => 10; // Early in update cycle, before rendering

    /// <summary>
    /// Initializes the movement system with logging and optional event bus.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="eventBus">Optional event bus for movement notifications.</param>
    public MovementSystem(ILogger<MovementSystem> logger, IEventBus? eventBus = null)
        : base(logger)
    {
        _eventBus = eventBus;
    }

    /// <inheritdoc/>
    protected override void OnInitialize()
    {
        // Grid movement: GridPosition + Direction + MovementState
        _movementQuery = new QueryDescription()
            .WithAll<GridPosition, Direction, MovementState>();

        // Collision detection: GridPosition + TileCollider
        _colliderQuery = new QueryDescription()
            .WithAll<GridPosition, TileCollider>();

        Logger.LogInformation("MovementSystem initialized for Pokemon-style tile-based movement");
    }

    /// <inheritdoc/>
    protected override void OnUpdate(float deltaTime)
    {
        _entitiesProcessed = 0;
        _movementBlocked = 0;

        // Process tile-to-tile movement with interpolation
        ProcessGridMovement(deltaTime);

        if (_entitiesProcessed > 0 || _movementBlocked > 0)
        {
            Logger.LogDebug("MovementSystem processed {Moved} entities, blocked {Blocked} movements",
                _entitiesProcessed, _movementBlocked);
        }
    }

    /// <summary>
    /// Processes grid-based movement with collision detection and smooth interpolation.
    /// </summary>
    private void ProcessGridMovement(float deltaTime)
    {
        World.Query(in _movementQuery, (Entity entity, ref GridPosition gridPos, ref Direction direction, ref MovementState movementState) =>
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

                // Check collision at target tile
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
                gridPos.InterpolationProgress += progressPerSecond * deltaTime;

                // Clamp to 1.0 and snap to target tile when complete
                if (gridPos.InterpolationProgress >= 1.0f)
                {
                    gridPos.InterpolationProgress = 1.0f;
                    gridPos.TileX = gridPos.TargetTileX;
                    gridPos.TileY = gridPos.TargetTileY;

                    // Emit movement complete event
                    if (_eventBus != null)
                    {
                        _eventBus.Publish(new MovementEvent(
                            entity,
                            gridPos.TileX - direction.ToOffset().dx,
                            gridPos.TileY - direction.ToOffset().dy,
                            gridPos.TileX,
                            gridPos.TileY,
                            deltaTime,
                            true));
                    }
                }
            }
        });
    }

    /// <summary>
    /// Checks if an entity can move to the specified tile coordinates.
    /// Performs collision detection against solid colliders.
    /// </summary>
    /// <param name="targetX">Target tile X coordinate</param>
    /// <param name="targetY">Target tile Y coordinate</param>
    /// <param name="mapId">Map ID to check</param>
    /// <param name="movingEntity">Entity attempting to move</param>
    /// <returns>True if movement is allowed, false if blocked</returns>
    private bool CanMoveTo(int targetX, int targetY, int mapId, Entity movingEntity)
    {
        bool canMove = true;

        // Check all entities with colliders at the target position
        World.Query(in _colliderQuery, (Entity entity, ref GridPosition gridPos, ref TileCollider collider) =>
        {
            // Skip self-collision
            if (entity == movingEntity)
                return;

            // Skip if different map
            if (gridPos.MapId != mapId)
                return;

            // Check if collider is at target tile and is solid
            if (gridPos.TileX == targetX && gridPos.TileY == targetY && collider.IsSolid)
            {
                canMove = false;
                // Trigger collision event
                collider.TriggerCollision((uint)movingEntity.Id);
            }
        });

        return canMove;
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
}
