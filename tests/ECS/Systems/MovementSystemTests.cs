using Arch.Core;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PokeNET.Core.ECS;
using PokeNET.Domain.ECS.Components;
using PokeNET.Domain.ECS.Events;
using PokeNET.Domain.ECS.Systems;
using Xunit;

namespace PokeNET.Tests.ECS.Systems;

/// <summary>
/// Comprehensive tests for MovementSystem covering tile-to-tile movement, collision detection,
/// 8-directional movement, smooth interpolation, and movement state transitions.
/// Target: >80% code coverage
/// </summary>
public class MovementSystemTests : IDisposable
{
    private readonly World _world;
    private readonly Mock<ILogger<MovementSystem>> _mockLogger;
    private readonly Mock<IEventBus> _mockEventBus;
    private readonly MovementSystem _movementSystem;

    public MovementSystemTests()
    {
        _world = World.Create();
        _mockLogger = new Mock<ILogger<MovementSystem>>();
        _mockEventBus = new Mock<IEventBus>();
        _movementSystem = new MovementSystem(_mockLogger.Object, _mockEventBus.Object);
        _movementSystem.Initialize(_world);
    }

    public void Dispose()
    {
        _movementSystem.Dispose();
        World.Destroy(_world);
        GC.SuppressFinalize(this);
    }

    #region Initialization Tests

    [Fact]
    public void Initialize_WithValidWorld_InitializesSuccessfully()
    {
        // Arrange
        using var world = World.Create();
        using var system = new MovementSystem(_mockLogger.Object);

        // Act
        var act = () => system.Initialize(world);

        // Assert
        act.Should().NotThrow();
        system.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void Priority_ShouldBeEarly_ForMovementBeforeRendering()
    {
        // Act & Assert
        _movementSystem.Priority.Should().Be(10, "movement should process early in update cycle");
    }

    #endregion

    #region Tile-to-Tile Movement Tests

    [Fact]
    public void Update_EntityWithDirectionNorth_MovesUpOneTile()
    {
        // Arrange
        var entity = _world.Create(
            new GridPosition(5, 5, 0),
            Direction.North,
            new MovementState(MovementMode.Walking, canRun: false)
        );

        // Act
        _movementSystem.Update(1.0f);
        ref var gridPos = ref _world.Get<GridPosition>(entity);

        // Assert
        gridPos.TargetTileX.Should().Be(5, "X should not change for North");
        gridPos.TargetTileY.Should().Be(4, "Y should decrease by 1 for North");
        gridPos.InterpolationProgress.Should().Be(0.0f, "interpolation should start");
        gridPos.IsMoving.Should().BeTrue();
    }

    [Fact]
    public void Update_EntityWithDirectionEast_MovesRightOneTile()
    {
        // Arrange
        var entity = _world.Create(
            new GridPosition(5, 5, 0),
            Direction.East,
            new MovementState(MovementMode.Walking, canRun: false)
        );

        // Act
        _movementSystem.Update(1.0f);
        ref var gridPos = ref _world.Get<GridPosition>(entity);

        // Assert
        gridPos.TargetTileX.Should().Be(6, "X should increase by 1 for East");
        gridPos.TargetTileY.Should().Be(5, "Y should not change for East");
        gridPos.IsMoving.Should().BeTrue();
    }

    [Fact]
    public void Update_EntityWithDirectionNorthEast_MovesDiagonally()
    {
        // Arrange
        var entity = _world.Create(
            new GridPosition(5, 5, 0),
            Direction.NorthEast,
            new MovementState(MovementMode.Walking, canRun: false)
        );

        // Act
        _movementSystem.Update(1.0f);
        ref var gridPos = ref _world.Get<GridPosition>(entity);

        // Assert
        gridPos.TargetTileX.Should().Be(6, "X should increase for NorthEast");
        gridPos.TargetTileY.Should().Be(4, "Y should decrease for NorthEast");
        gridPos.IsMoving.Should().BeTrue();
    }

    [Fact]
    public void Update_EntityWithDirectionNone_DoesNotMove()
    {
        // Arrange
        var entity = _world.Create(
            new GridPosition(5, 5, 0),
            Direction.None,
            new MovementState(MovementMode.Walking, canRun: false)
        );

        // Act
        _movementSystem.Update(1.0f);
        ref var gridPos = ref _world.Get<GridPosition>(entity);

        // Assert
        gridPos.IsMoving.Should().BeFalse();
        gridPos.TileX.Should().Be(5);
        gridPos.TileY.Should().Be(5);
    }

    #endregion

    #region Collision Detection Tests

    [Fact]
    public void Update_CollisionWithSolidObject_BlocksMovement()
    {
        // Arrange - Create entity trying to move
        var movingEntity = _world.Create(
            new GridPosition(5, 5, 0),
            Direction.East,
            new MovementState(MovementMode.Walking, canRun: false)
        );

        // Create solid collider at target position (6, 5)
        var obstacle = _world.Create(
            new GridPosition(6, 5, 0),
            new TileCollider(CollisionLayer.Terrain, isSolid: true)
        );

        // Act
        _movementSystem.Update(1.0f);
        ref var gridPos = ref _world.Get<GridPosition>(movingEntity);

        // Assert
        gridPos.IsMoving.Should().BeFalse("movement should be blocked by solid collider");
        gridPos.TileX.Should().Be(5, "entity should remain at original position");
        gridPos.TileY.Should().Be(5);
        _movementSystem.GetBlockedCount().Should().Be(1);
    }

    [Fact]
    public void Update_CollisionWithNonSolidObject_AllowsMovement()
    {
        // Arrange
        var movingEntity = _world.Create(
            new GridPosition(5, 5, 0),
            Direction.East,
            new MovementState(MovementMode.Walking, canRun: false)
        );

        // Create non-solid collider at target position (grass, water, etc.)
        var grass = _world.Create(
            new GridPosition(6, 5, 0),
            new TileCollider(CollisionLayer.Terrain, isSolid: false)
        );

        // Act
        _movementSystem.Update(1.0f);
        ref var gridPos = ref _world.Get<GridPosition>(movingEntity);

        // Assert
        gridPos.IsMoving.Should().BeTrue("movement should not be blocked by non-solid");
        gridPos.TargetTileX.Should().Be(6);
        gridPos.TargetTileY.Should().Be(5);
    }

    [Fact]
    public void Update_CollisionOnDifferentMap_DoesNotBlock()
    {
        // Arrange
        var movingEntity = _world.Create(
            new GridPosition(5, 5, mapId: 0),
            Direction.East,
            new MovementState(MovementMode.Walking, canRun: false)
        );

        // Create collider at same tile but different map
        var obstacleOtherMap = _world.Create(
            new GridPosition(6, 5, mapId: 1),
            new TileCollider(CollisionLayer.Terrain, isSolid: true)
        );

        // Act
        _movementSystem.Update(1.0f);
        ref var gridPos = ref _world.Get<GridPosition>(movingEntity);

        // Assert
        gridPos.IsMoving.Should().BeTrue("different map should not cause collision");
        gridPos.TargetTileX.Should().Be(6);
    }

    #endregion

    #region Smooth Interpolation Tests

    [Fact]
    public void Update_WhileMoving_InterpolatesProgressBasedOnSpeed()
    {
        // Arrange
        var entity = _world.Create(
            new GridPosition(5, 5, 0),
            Direction.East,
            new MovementState(MovementMode.Walking, canRun: false) // 4 tiles/sec
        );

        // Start movement
        _movementSystem.Update(0.1f);

        // Act - Continue movement
        _movementSystem.Update(0.1f); // 0.1 * 4 = 0.4 progress per update
        ref var gridPos = ref _world.Get<GridPosition>(entity);

        // Assert
        gridPos.InterpolationProgress.Should().BeGreaterThan(0.0f);
        gridPos.IsMoving.Should().BeTrue();
    }

    [Fact]
    public void Update_InterpolationComplete_SnapsToTargetTile()
    {
        // Arrange
        var entity = _world.Create(
            new GridPosition(5, 5, 0)
            {
                TargetTileX = 6,
                TargetTileY = 5,
                InterpolationProgress = 0.9f
            },
            Direction.East,
            new MovementState(MovementMode.Running, canRun: true) // 8 tiles/sec
        );

        // Act - Large enough delta to complete interpolation
        _movementSystem.Update(0.2f); // 0.2 * 8 = 1.6, will clamp to 1.0
        ref var gridPos = ref _world.Get<GridPosition>(entity);

        // Assert
        gridPos.InterpolationProgress.Should().Be(1.0f);
        gridPos.TileX.Should().Be(6, "should snap to target tile");
        gridPos.TileY.Should().Be(5);
        gridPos.IsMoving.Should().BeFalse();
    }

    [Fact]
    public void GetInterpolatedPosition_WhenIdle_ReturnsWorldPosition()
    {
        // Arrange
        var gridPos = new GridPosition(5, 5, 0);

        // Act
        var interpolated = MovementSystem.GetInterpolatedPosition(gridPos);

        // Assert
        interpolated.X.Should().Be(80f, "5 * 16 = 80");
        interpolated.Y.Should().Be(80f);
    }

    [Fact]
    public void GetInterpolatedPosition_WhenMovingHalfway_ReturnsMiddlePosition()
    {
        // Arrange
        var gridPos = new GridPosition(5, 5, 0)
        {
            TargetTileX = 6,
            TargetTileY = 5,
            InterpolationProgress = 0.5f
        };

        // Act
        var interpolated = MovementSystem.GetInterpolatedPosition(gridPos);

        // Assert
        interpolated.X.Should().Be(88f, "halfway between 80 and 96");
        interpolated.Y.Should().Be(80f);
    }

    #endregion

    #region Movement State Transitions Tests

    [Fact]
    public void Update_WhenCanMoveIsFalse_DoesNotStartMovement()
    {
        // Arrange
        var entity = _world.Create(
            new GridPosition(5, 5, 0),
            Direction.East,
            new MovementState(MovementMode.Walking, canRun: false) { CanMove = false }
        );

        // Act
        _movementSystem.Update(1.0f);
        ref var gridPos = ref _world.Get<GridPosition>(entity);

        // Assert
        gridPos.IsMoving.Should().BeFalse("CanMove is false");
        _movementSystem.GetProcessedCount().Should().Be(0);
    }

    [Fact]
    public void Update_TransitionFromWalkToRun_UsesNewSpeed()
    {
        // Arrange
        var entity = _world.Create(
            new GridPosition(5, 5, 0),
            Direction.East,
            new MovementState(MovementMode.Walking, canRun: false) // 4 tiles/sec
        );

        // Start walking
        _movementSystem.Update(0.1f);

        // Act - Change to running speed mid-movement
        ref var movementState = ref _world.Get<MovementState>(entity);
        movementState.Mode = MovementMode.Running;
        movementState.MovementSpeed = 8.0f; // 8 tiles/sec
        _movementSystem.Update(0.1f);

        ref var gridPos = ref _world.Get<GridPosition>(entity);

        // Assert
        gridPos.InterpolationProgress.Should().BeGreaterThan(0.4f, "running speed should interpolate faster");
    }

    [Fact]
    public void Update_SurfingMode_ProcessesMovement()
    {
        // Arrange
        var entity = _world.Create(
            new GridPosition(5, 5, 0),
            Direction.South,
            new MovementState(MovementMode.Surfing, canRun: false) // 6 tiles/sec
        );

        // Act
        _movementSystem.Update(1.0f);
        ref var gridPos = ref _world.Get<GridPosition>(entity);

        // Assert
        gridPos.IsMoving.Should().BeTrue();
        gridPos.TargetTileX.Should().Be(5);
        gridPos.TargetTileY.Should().Be(6, "should move south");
    }

    #endregion

    #region Event Bus Tests

    [Fact]
    public void Update_MovementComplete_PublishesMovementEvent()
    {
        // Arrange
        var entity = _world.Create(
            new GridPosition(5, 5, 0)
            {
                TargetTileX = 6,
                TargetTileY = 5,
                InterpolationProgress = 0.95f
            },
            Direction.East,
            new MovementState(MovementMode.Running, canRun: true) // Fast speed to complete
        );

        // Act
        _movementSystem.Update(0.1f);

        // Assert
        _mockEventBus.Verify(
            eb => eb.Publish(It.Is<MovementEvent>(e =>
                e.Entity == entity &&
                e.NewX == 6 &&
                e.NewY == 5)),
            Times.Once,
            "should publish movement complete event"
        );
    }

    #endregion

    #region Performance Tests

    [Fact]
    public void Update_MultipleEntitiesMoving_ProcessesAll()
    {
        // Arrange - Create 10 entities moving
        for (int i = 0; i < 10; i++)
        {
            _world.Create(
                new GridPosition(i, i, 0),
                Direction.East,
                new MovementState(MovementMode.Walking, canRun: false)
            );
        }

        // Act
        _movementSystem.Update(1.0f);

        // Assert
        _movementSystem.GetProcessedCount().Should().Be(10, "should process all moving entities");
    }

    [Fact]
    public void GetBlockedCount_AfterCollisions_ReturnsCorrectCount()
    {
        // Arrange - Create 3 entities blocked by obstacles
        for (int i = 0; i < 3; i++)
        {
            _world.Create(
                new GridPosition(i, 0, 0),
                Direction.East,
                new MovementState(MovementMode.Walking, canRun: false)
            );

            _world.Create(
                new GridPosition(i + 1, 0, 0),
                new TileCollider(CollisionLayer.Terrain, isSolid: true)
            );
        }

        // Act
        _movementSystem.Update(1.0f);

        // Assert
        _movementSystem.GetBlockedCount().Should().Be(3);
    }

    #endregion
}
