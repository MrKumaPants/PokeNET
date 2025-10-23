using Arch.Core;
using Arch.Core.Extensions;
using PokeNET.Domain.ECS.Components;
using System.Numerics;

namespace PokeNET.Domain.Input.Commands;

/// <summary>
/// Command for moving an entity in a specific direction.
/// Supports undo functionality by storing previous position.
/// </summary>
public class MoveCommand : CommandBase
{
    private readonly Entity _entity;
    private readonly Vector2 _direction;
    private readonly float _speed;
    private Vector2? _previousPosition;

    /// <inheritdoc/>
    public override int Priority => 10; // Movement has high priority

    /// <inheritdoc/>
    public override bool SupportsUndo => true;

    /// <summary>
    /// Initializes a new move command.
    /// </summary>
    /// <param name="entity">The entity to move.</param>
    /// <param name="direction">The normalized direction vector.</param>
    /// <param name="speed">Movement speed.</param>
    public MoveCommand(Entity entity, Vector2 direction, float speed = 1.0f)
    {
        _entity = entity;
        _direction = Vector2.Normalize(direction);
        _speed = speed;
    }

    /// <inheritdoc/>
    public override bool CanExecute(World world)
    {
        if (!base.CanExecute(world))
            return false;

        return world.IsAlive(_entity) && world.Has<Position>(_entity) && world.Has<Velocity>(_entity);
    }

    /// <inheritdoc/>
    public override void Execute(World world)
    {
        ref var position = ref world.Get<Position>(_entity);
        ref var velocity = ref world.Get<Velocity>(_entity);

        // Store previous position for undo
        _previousPosition = new Vector2(position.X, position.Y);

        // Set velocity based on direction and speed
        velocity.X = _direction.X * _speed;
        velocity.Y = _direction.Y * _speed;
    }

    /// <inheritdoc/>
    public override bool Undo(World world)
    {
        if (!_previousPosition.HasValue)
            return false;

        if (!world.IsAlive(_entity) || !world.Has<Position>(_entity))
            return false;

        ref var position = ref world.Get<Position>(_entity);
        position.X = _previousPosition.Value.X;
        position.Y = _previousPosition.Value.Y;

        return true;
    }
}
