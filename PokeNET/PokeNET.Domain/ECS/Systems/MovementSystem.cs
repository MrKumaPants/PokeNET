using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Extensions.Logging;
using PokeNET.Domain.ECS.Components;
using PokeNET.Domain.ECS.Events;

namespace PokeNET.Domain.ECS.Systems;

/// <summary>
/// System responsible for updating entity positions based on velocity, acceleration, and constraints.
/// Implements frame-rate independent movement with optional physics features.
///
/// Architecture:
/// - Queries entities with Position + Velocity components
/// - Optionally processes Acceleration, Friction, and MovementConstraint components
/// - Emits MovementEvent for other systems to react to position changes
///
/// Features:
/// - Frame-rate independent (delta time based)
/// - Maximum velocity clamping
/// - Boundary constraints
/// - Acceleration integration
/// - Friction/damping
/// - Movement event emission for collision detection and animations
///
/// Performance:
/// - Uses efficient Arch queries with component filtering
/// - Processes entities in parallel-safe batches
/// - Zero allocation for component updates
/// </summary>
public class MovementSystem : SystemBase
{
    private readonly IEventBus? _eventBus;
    private QueryDescription _movementQuery;
    private QueryDescription _acceleratedMovementQuery;
    private QueryDescription _frictionMovementQuery;
    private QueryDescription _constrainedMovementQuery;
    private int _entitiesProcessed;

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
        // Basic movement: Position + Velocity
        _movementQuery = new QueryDescription()
            .WithAll<Position, Velocity>();

        // Accelerated movement: Position + Velocity + Acceleration
        _acceleratedMovementQuery = new QueryDescription()
            .WithAll<Position, Velocity, Acceleration>();

        // Movement with friction: Position + Velocity + Friction
        _frictionMovementQuery = new QueryDescription()
            .WithAll<Position, Velocity, Friction>();

        // Constrained movement: Position + Velocity + MovementConstraint
        _constrainedMovementQuery = new QueryDescription()
            .WithAll<Position, Velocity, MovementConstraint>();

        Logger.LogInformation("MovementSystem initialized with event bus: {HasEventBus}", _eventBus != null);
    }

    /// <inheritdoc/>
    protected override void OnUpdate(float deltaTime)
    {
        _entitiesProcessed = 0;

        // Process acceleration first (affects velocity)
        ProcessAcceleration(deltaTime);

        // Apply friction (affects velocity)
        ProcessFriction(deltaTime);

        // Apply velocity to position with constraints
        ProcessConstrainedMovement(deltaTime);

        // Apply basic movement to remaining entities
        ProcessBasicMovement(deltaTime);

        if (_entitiesProcessed > 0)
        {
            Logger.LogDebug("MovementSystem processed {Count} entities in {Time:F4}s", _entitiesProcessed, deltaTime);
        }
    }

    /// <summary>
    /// Applies acceleration to velocity.
    /// Formula: velocity += acceleration * deltaTime
    /// </summary>
    private void ProcessAcceleration(float deltaTime)
    {
        World.Query(in _acceleratedMovementQuery, (ref Velocity velocity, ref Acceleration acceleration) =>
        {
            velocity.X += acceleration.X * deltaTime;
            velocity.Y += acceleration.Y * deltaTime;
        });
    }

    /// <summary>
    /// Applies friction/damping to velocity.
    /// Formula: velocity *= (1 - friction * deltaTime)
    /// </summary>
    private void ProcessFriction(float deltaTime)
    {
        World.Query(in _frictionMovementQuery, (ref Velocity velocity, ref Friction friction) =>
        {
            var dampingFactor = 1f - (friction.Coefficient * deltaTime);
            dampingFactor = Math.Max(0f, dampingFactor); // Prevent negative

            velocity.X *= dampingFactor;
            velocity.Y *= dampingFactor;

            // Stop very slow velocities to prevent floating point drift
            const float minVelocity = 0.01f;
            if (MathF.Abs(velocity.X) < minVelocity) velocity.X = 0f;
            if (MathF.Abs(velocity.Y) < minVelocity) velocity.Y = 0f;
        });
    }

    /// <summary>
    /// Applies velocity to position with boundary and speed constraints.
    /// </summary>
    private void ProcessConstrainedMovement(float deltaTime)
    {
        World.Query(in _constrainedMovementQuery, (Entity entity, ref Position position, ref Velocity velocity, ref MovementConstraint constraint) =>
        {
            var oldX = position.X;
            var oldY = position.Y;
            var wasConstrained = false;

            // Apply velocity clamping if specified
            if (constraint.HasVelocityLimit)
            {
                var speed = velocity.Magnitude;
                if (speed > constraint.MaxVelocity!.Value)
                {
                    var normalized = velocity.Normalized();
                    velocity.X = normalized.X * constraint.MaxVelocity.Value;
                    velocity.Y = normalized.Y * constraint.MaxVelocity.Value;
                    wasConstrained = true;
                }
            }

            // Calculate new position
            var newX = position.X + velocity.X * deltaTime;
            var newY = position.Y + velocity.Y * deltaTime;

            // Apply boundary constraints
            if (constraint.HasBoundaries)
            {
                if (constraint.MinX.HasValue && newX < constraint.MinX.Value)
                {
                    newX = constraint.MinX.Value;
                    velocity.X = 0f; // Stop horizontal movement
                    wasConstrained = true;
                }
                if (constraint.MaxX.HasValue && newX > constraint.MaxX.Value)
                {
                    newX = constraint.MaxX.Value;
                    velocity.X = 0f;
                    wasConstrained = true;
                }
                if (constraint.MinY.HasValue && newY < constraint.MinY.Value)
                {
                    newY = constraint.MinY.Value;
                    velocity.Y = 0f; // Stop vertical movement
                    wasConstrained = true;
                }
                if (constraint.MaxY.HasValue && newY > constraint.MaxY.Value)
                {
                    newY = constraint.MaxY.Value;
                    velocity.Y = 0f;
                    wasConstrained = true;
                }
            }

            // Update position
            position.X = newX;
            position.Y = newY;
            _entitiesProcessed++;

            // Emit movement event if position changed
            if ((position.X != oldX || position.Y != oldY) && _eventBus != null)
            {
                _eventBus.Publish(new MovementEvent(entity, oldX, oldY, position.X, position.Y, deltaTime, wasConstrained));
            }
        });
    }

    /// <summary>
    /// Applies basic movement (velocity to position) for entities without constraints.
    /// </summary>
    private void ProcessBasicMovement(float deltaTime)
    {
        // Query entities that have Position + Velocity but NOT MovementConstraint
        var basicQuery = new QueryDescription()
            .WithAll<Position, Velocity>()
            .WithNone<MovementConstraint>();

        World.Query(in basicQuery, (Entity entity, ref Position position, ref Velocity velocity) =>
        {
            var oldX = position.X;
            var oldY = position.Y;

            // Apply velocity
            position.X += velocity.X * deltaTime;
            position.Y += velocity.Y * deltaTime;
            _entitiesProcessed++;

            // Emit movement event if position changed
            if ((position.X != oldX || position.Y != oldY) && _eventBus != null)
            {
                _eventBus.Publish(new MovementEvent(entity, oldX, oldY, position.X, position.Y, deltaTime, false));
            }
        });
    }

    /// <summary>
    /// Gets statistics about entities processed in the last frame.
    /// Useful for debugging and performance monitoring.
    /// </summary>
    public int GetProcessedCount() => _entitiesProcessed;
}
