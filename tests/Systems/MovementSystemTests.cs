using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using PokeNET.Domain.ECS.Components;
using PokeNET.Domain.ECS.Events;
using PokeNET.Domain.ECS.Systems;
using Xunit;

namespace PokeNET.Tests.Systems;

/// <summary>
/// Comprehensive tests for the MovementSystem.
/// Covers frame-rate independence, constraints, acceleration, friction, and event emission.
/// </summary>
public class MovementSystemTests : IDisposable
{
    private readonly World _world;
    private readonly MovementSystem _system;
    private readonly TestEventBus _eventBus;

    public MovementSystemTests()
    {
        _world = World.Create();
        _eventBus = new TestEventBus();
        _system = new MovementSystem(NullLogger<MovementSystem>.Instance, _eventBus);
        _system.Initialize(_world);
    }

    public void Dispose()
    {
        _system.Dispose();
        World.Destroy(_world);
    }

    [Fact]
    public void Update_WithBasicVelocity_MovesEntity()
    {
        // Arrange
        var entity = _world.Create(
            new Position(0f, 0f),
            new Velocity(100f, 50f)
        );

        // Act
        _system.Update(0.1f); // 100ms frame

        // Assert
        var position = _world.Get<Position>(entity);
        Assert.Equal(10f, position.X, 2); // 100 * 0.1 = 10
        Assert.Equal(5f, position.Y, 2);  // 50 * 0.1 = 5
    }

    [Fact]
    public void Update_IsDeltaTimeIndependent()
    {
        // Arrange
        var entity1 = _world.Create(new Position(0f, 0f), new Velocity(100f, 0f));
        var entity2 = _world.Create(new Position(0f, 0f), new Velocity(100f, 0f));

        // Act - Different frame times should produce same total movement
        _system.Update(1.0f); // 1 second frame for entity1

        var pos1 = _world.Get<Position>(entity1);

        // Reset entity2 and simulate with smaller frames
        _system.Update(0.5f); // Two 0.5s frames for entity2
        _system.Update(0.5f);

        var pos2 = _world.Get<Position>(entity2);

        // Assert - Should be approximately equal
        Assert.Equal(pos1.X, pos2.X, 1); // Within 1 pixel tolerance
    }

    [Fact]
    public void Update_WithAcceleration_IncreasesVelocity()
    {
        // Arrange - Entity starting at rest with acceleration
        var entity = _world.Create(
            new Position(0f, 0f),
            new Velocity(0f, 0f),
            new Acceleration(0f, 980f) // Gravity
        );

        // Act - Simulate 1 second
        _system.Update(1.0f);

        // Assert - Velocity should have increased
        var velocity = _world.Get<Velocity>(entity);
        Assert.Equal(0f, velocity.X, 2);
        Assert.Equal(980f, velocity.Y, 2); // v = at = 980 * 1

        // Position should have moved
        var position = _world.Get<Position>(entity);
        Assert.Equal(980f, position.Y, 2); // d = vt = 980 * 1
    }

    [Fact]
    public void Update_WithFriction_SlowsDownEntity()
    {
        // Arrange - Entity with initial velocity and friction
        var entity = _world.Create(
            new Position(0f, 0f),
            new Velocity(100f, 0f),
            new Friction(0.5f) // 50% friction
        );

        // Act - Simulate multiple frames
        var initialVelocity = _world.Get<Velocity>(entity);
        _system.Update(0.1f);
        var velocity1 = _world.Get<Velocity>(entity);
        _system.Update(0.1f);
        var velocity2 = _world.Get<Velocity>(entity);

        // Assert - Velocity should decrease each frame
        Assert.True(velocity1.X < initialVelocity.X, "Velocity should decrease after friction");
        Assert.True(velocity2.X < velocity1.X, "Velocity should continue decreasing");
        Assert.True(velocity2.X > 0, "Velocity should not reverse direction");
    }

    [Fact]
    public void Update_WithVeryLowVelocity_StopsToPreventDrift()
    {
        // Arrange - Entity with very slow velocity and high friction
        var entity = _world.Create(
            new Position(0f, 0f),
            new Velocity(0.1f, 0.1f), // Very slow
            new Friction(0.9f) // High friction
        );

        // Act - Simulate several frames
        for (int i = 0; i < 10; i++)
        {
            _system.Update(0.1f);
        }

        // Assert - Should stop completely to prevent floating point drift
        var velocity = _world.Get<Velocity>(entity);
        Assert.Equal(0f, velocity.X);
        Assert.Equal(0f, velocity.Y);
    }

    [Fact]
    public void Update_WithMaxVelocityConstraint_ClampsSpeed()
    {
        // Arrange - Entity with high velocity and speed limit
        var entity = _world.Create(
            new Position(0f, 0f),
            new Velocity(200f, 200f), // Fast velocity (magnitude ~283)
            new MovementConstraint(maxVelocity: 100f) // Speed limit
        );

        // Act
        _system.Update(0.1f);

        // Assert - Velocity magnitude should be clamped
        var velocity = _world.Get<Velocity>(entity);
        var magnitude = velocity.Magnitude;
        Assert.True(magnitude <= 100f, $"Velocity magnitude {magnitude} exceeds limit of 100");
        Assert.True(magnitude > 99f, "Velocity should be close to limit"); // Should be at the limit
    }

    [Fact]
    public void Update_WithBoundaryConstraints_StopsAtEdges()
    {
        // Arrange - Entity near boundary with velocity moving outside
        var entity = _world.Create(
            new Position(95f, 5f),
            new Velocity(100f, -100f), // Moving right and up
            new MovementConstraint(minX: 0f, maxX: 100f, minY: 0f, maxY: 100f)
        );

        // Act
        _system.Update(0.1f);

        // Assert - Should stop at boundaries
        var position = _world.Get<Position>(entity);
        var velocity = _world.Get<Velocity>(entity);

        Assert.Equal(100f, position.X); // Clamped to max X
        Assert.Equal(0f, position.Y);   // Clamped to min Y
        Assert.Equal(0f, velocity.X);   // Velocity stopped
        Assert.Equal(0f, velocity.Y);   // Velocity stopped
    }

    [Fact]
    public void Update_WithRectangularBoundary_KeepsEntityInside()
    {
        // Arrange - Entity with rectangular boundary
        var entity = _world.Create(
            new Position(50f, 50f),
            new Velocity(500f, 500f), // Fast velocity
            MovementConstraint.Rectangle(0f, 0f, 100f, 100f)
        );

        // Act - Simulate multiple frames trying to escape
        for (int i = 0; i < 10; i++)
        {
            _system.Update(0.1f);
        }

        // Assert - Should be within boundaries
        var position = _world.Get<Position>(entity);
        Assert.InRange(position.X, 0f, 100f);
        Assert.InRange(position.Y, 0f, 100f);
    }

    [Fact]
    public void Update_WithComplexPhysics_IntegratesAllSystems()
    {
        // Arrange - Entity with acceleration, friction, and constraints (like a platformer character)
        var entity = _world.Create(
            new Position(50f, 0f),
            new Velocity(0f, 0f),
            new Acceleration(0f, 980f), // Gravity
            new Friction(0.2f), // Air resistance
            new MovementConstraint(minY: 0f, maxY: 1000f, maxVelocity: 500f) // Ground and terminal velocity
        );

        // Act - Simulate falling
        for (int i = 0; i < 20; i++) // 2 seconds total
        {
            _system.Update(0.1f);
        }

        // Assert
        var position = _world.Get<Position>(entity);
        var velocity = _world.Get<Velocity>(entity);

        // Should have fallen due to gravity
        Assert.True(position.Y > 0f, "Entity should have fallen");

        // Should not exceed terminal velocity
        Assert.True(velocity.Y <= 500f, $"Velocity {velocity.Y} exceeds terminal velocity");

        // Should not go below ground
        Assert.True(position.Y >= 0f, "Entity should not go below ground");
    }

    [Fact]
    public void Update_EmitsMovementEvents()
    {
        // Arrange
        var entity = _world.Create(
            new Position(0f, 0f),
            new Velocity(100f, 50f)
        );

        _eventBus.ClearEvents();

        // Act
        _system.Update(0.1f);

        // Assert - Should have emitted one movement event
        Assert.Single(_eventBus.Events);
        var movementEvent = Assert.IsType<MovementEvent>(_eventBus.Events[0]);
        Assert.Equal(0f, movementEvent.OldX);
        Assert.Equal(0f, movementEvent.OldY);
        Assert.Equal(10f, movementEvent.NewX, 2);
        Assert.Equal(5f, movementEvent.NewY, 2);
        Assert.Equal(0.1f, movementEvent.DeltaTime);
    }

    [Fact]
    public void Update_WithConstraintHit_EmitsConstrainedEvent()
    {
        // Arrange
        var entity = _world.Create(
            new Position(95f, 50f),
            new Velocity(100f, 0f),
            new MovementConstraint(maxX: 100f)
        );

        _eventBus.ClearEvents();

        // Act
        _system.Update(0.1f);

        // Assert
        var movementEvent = Assert.IsType<MovementEvent>(_eventBus.Events[0]);
        Assert.True(movementEvent.WasConstrained, "Event should indicate constraint was applied");
    }

    [Fact]
    public void Update_MultipleEntities_ProcessesAllConcurrently()
    {
        // Arrange - Create multiple entities
        const int entityCount = 100;
        for (int i = 0; i < entityCount; i++)
        {
            _world.Create(
                new Position(i * 10f, i * 10f),
                new Velocity(50f, 50f)
            );
        }

        // Act
        _system.Update(0.1f);

        // Assert - All entities should have moved
        Assert.Equal(entityCount, _system.GetProcessedCount());
        Assert.Equal(entityCount, _eventBus.Events.Count);
    }

    [Fact]
    public void Update_WithoutVelocityComponent_IgnoresEntity()
    {
        // Arrange - Entity with position but no velocity
        _world.Create(new Position(0f, 0f));
        var movingEntity = _world.Create(new Position(0f, 0f), new Velocity(100f, 0f));

        // Act
        _system.Update(0.1f);

        // Assert - Should only process the entity with velocity
        Assert.Equal(1, _system.GetProcessedCount());
    }

    [Fact]
    public void Update_WhenDisabled_DoesNotProcess()
    {
        // Arrange
        _world.Create(new Position(0f, 0f), new Velocity(100f, 0f));
        _system.IsEnabled = false;

        // Act
        _system.Update(0.1f);

        // Assert
        Assert.Equal(0, _system.GetProcessedCount());
        Assert.Empty(_eventBus.Events);
    }

    [Fact]
    public void Priority_ReturnsCorrectValue()
    {
        // Arrange & Act & Assert
        Assert.Equal(10, _system.Priority); // Early in update cycle
    }
}

/// <summary>
/// Test implementation of IEventBus for capturing events.
/// </summary>
internal class TestEventBus : IEventBus
{
    public List<IGameEvent> Events { get; } = new();

    public void Publish<TEvent>(TEvent gameEvent) where TEvent : IGameEvent
    {
        Events.Add(gameEvent);
    }

    public void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : IGameEvent
    {
        // Not needed for tests
    }

    public void Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : IGameEvent
    {
        // Not needed for tests
    }

    public void ClearEvents() => Events.Clear();
}
