using Arch.Core;
using Arch.Core.Extensions;
using PokeNET.Domain.ECS.Components;
using System.Numerics;

namespace PokeNET.Domain.Input.Commands;

/// <summary>
/// Command for moving an entity in a specific direction using grid-based movement.
/// Supports undo functionality by storing previous direction.
/// Now works with Direction and MovementState components for Pokemon-style movement.
/// </summary>
public class MoveCommand : CommandBase
{
    private readonly Entity _entity;
    private readonly Vector2 _directionVector;
    private Direction _previousDirection;
    private bool _previousDirectionStored;

    /// <inheritdoc/>
    public override int Priority => 10; // Movement has high priority

    /// <inheritdoc/>
    public override bool SupportsUndo => true;

    /// <summary>
    /// Initializes a new move command.
    /// </summary>
    /// <param name="entity">The entity to move.</param>
    /// <param name="direction">The direction vector (will be converted to 8-directional).</param>
    public MoveCommand(Entity entity, Vector2 direction)
    {
        _entity = entity;
        _directionVector = direction;
        _previousDirectionStored = false;
    }

    /// <inheritdoc/>
    public override bool CanExecute(World world)
    {
        if (!base.CanExecute(world))
            return false;

        return world.IsAlive(_entity) &&
               world.Has<GridPosition>(_entity) &&
               world.Has<Direction>(_entity) &&
               world.Has<MovementState>(_entity);
    }

    /// <inheritdoc/>
    public override void Execute(World world)
    {
        ref var direction = ref world.Get<Direction>(_entity);
        ref var movementState = ref world.Get<MovementState>(_entity);

        // Store previous direction for undo
        _previousDirection = direction;
        _previousDirectionStored = true;

        // Convert vector to 8-directional direction
        direction = VectorToDirection(_directionVector);

        // Update movement mode based on input (could check for run button here)
        // For now, just use current movement state
        if (movementState.Mode == MovementMode.Idle && direction != Direction.None)
        {
            movementState.Mode = movementState.CanRun ? MovementMode.Running : MovementMode.Walking;
        }
        else if (direction == Direction.None)
        {
            movementState.Mode = MovementMode.Idle;
        }
    }

    /// <inheritdoc/>
    public override bool Undo(World world)
    {
        if (!_previousDirectionStored)
            return false;

        if (!world.IsAlive(_entity) || !world.Has<Direction>(_entity))
            return false;

        ref var direction = ref world.Get<Direction>(_entity);
        direction = _previousDirection;

        return true;
    }

    /// <summary>
    /// Converts a 2D vector to one of 8 directions.
    /// </summary>
    /// <param name="vector">Input direction vector</param>
    /// <returns>8-directional direction enum</returns>
    private static Direction VectorToDirection(Vector2 vector)
    {
        if (vector.LengthSquared() < 0.01f)
            return Direction.None;

        // Normalize the vector
        vector = Vector2.Normalize(vector);

        // Convert to angle in degrees (0 = right, 90 = up, etc.)
        float angle = MathF.Atan2(-vector.Y, vector.X) * (180f / MathF.PI);
        if (angle < 0) angle += 360f;

        // Convert angle to 8 directions (45-degree segments)
        // East: 337.5-22.5, NE: 22.5-67.5, North: 67.5-112.5, etc.
        if (angle >= 337.5f || angle < 22.5f)
            return Direction.East;
        else if (angle >= 22.5f && angle < 67.5f)
            return Direction.NorthEast;
        else if (angle >= 67.5f && angle < 112.5f)
            return Direction.North;
        else if (angle >= 112.5f && angle < 157.5f)
            return Direction.NorthWest;
        else if (angle >= 157.5f && angle < 202.5f)
            return Direction.West;
        else if (angle >= 202.5f && angle < 247.5f)
            return Direction.SouthWest;
        else if (angle >= 247.5f && angle < 292.5f)
            return Direction.South;
        else // 292.5f - 337.5f
            return Direction.SouthEast;
    }
}
