using Arch.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework;
using Moq;
using PokeNET.Core.ECS;
using PokeNET.Core.ECS.Factories;
using PokeNET.Domain.ECS.Components;
using PokeNET.Domain.ECS.Events;
using Xunit;

namespace PokeNET.Tests.ECS.Factories;

/// <summary>
/// Tests for specialized entity factories (Player, Enemy, Item, Projectile).
/// Validates factory-specific logic and template registration.
/// </summary>
public sealed class SpecializedFactoryTests : IDisposable
{
    private readonly World _world;
    private readonly Mock<IEventBus> _mockEventBus;

    public SpecializedFactoryTests()
    {
        _world = World.Create();
        _mockEventBus = new Mock<IEventBus>();
    }

    public void Dispose()
    {
        World.Destroy(_world);
    }

    #region PlayerEntityFactory Tests

    [Fact]
    public void PlayerFactory_ShouldRegisterDefaultTemplates()
    {
        // Arrange
        var logger = new Mock<ILogger<PlayerEntityFactory>>();
        var factory = new PlayerEntityFactory(logger.Object, _mockEventBus.Object);

        // Assert
        Assert.True(factory.HasTemplate("player_basic"));
        Assert.True(factory.HasTemplate("player_fast"));
        Assert.True(factory.HasTemplate("player_tank"));
    }

    [Fact]
    public void PlayerFactory_CreateBasicPlayer_ShouldHaveRequiredComponents()
    {
        // Arrange
        var logger = new Mock<ILogger<PlayerEntityFactory>>();
        var factory = new PlayerEntityFactory(logger.Object, _mockEventBus.Object);
        var spawnPos = new Vector2(100, 200);

        // Act
        var player = factory.CreateBasicPlayer(_world, spawnPos);

        // Assert
        Assert.True(_world.IsAlive(player));
        Assert.True(_world.Has<Position>(player));
        Assert.True(_world.Has<Health>(player));
        Assert.True(_world.Has<Sprite>(player));
        Assert.True(_world.Has<Renderable>(player));

        ref var position = ref _world.Get<Position>(player);
        Assert.Equal(100f, position.X);
        Assert.Equal(200f, position.Y);

        ref var health = ref _world.Get<Health>(player);
        Assert.Equal(100, health.Current);
    }

    [Fact]
    public void PlayerFactory_CreateFastPlayer_ShouldHaveBalancedStats()
    {
        // Arrange
        var logger = new Mock<ILogger<PlayerEntityFactory>>();
        var factory = new PlayerEntityFactory(logger.Object);
        var spawnPos = new Vector2(0, 0);

        // Act
        var fastPlayer = factory.CreateFastPlayer(_world, spawnPos);

        // Assert
        Assert.True(_world.IsAlive(fastPlayer));
        Assert.True(_world.Has<Position>(fastPlayer));
        Assert.True(_world.Has<Health>(fastPlayer));

        ref var health = ref _world.Get<Health>(fastPlayer);
        Assert.Equal(75, health.Current); // Lower health for balance
    }

    [Fact]
    public void PlayerFactory_CreateTankPlayer_ShouldHaveHighHealth()
    {
        // Arrange
        var logger = new Mock<ILogger<PlayerEntityFactory>>();
        var factory = new PlayerEntityFactory(logger.Object);
        var spawnPos = new Vector2(0, 0);

        // Act
        var tankPlayer = factory.CreateTankPlayer(_world, spawnPos);

        // Assert
        Assert.True(_world.IsAlive(tankPlayer));
        Assert.True(_world.Has<Health>(tankPlayer));

        ref var health = ref _world.Get<Health>(tankPlayer);
        Assert.Equal(200, health.Current);
    }

    #endregion

    #region EnemyEntityFactory Tests

    [Fact]
    public void EnemyFactory_ShouldRegisterDefaultTemplates()
    {
        // Arrange
        var logger = new Mock<ILogger<EnemyEntityFactory>>();
        var factory = new EnemyEntityFactory(logger.Object);

        // Assert
        Assert.True(factory.HasTemplate("enemy_weak"));
        Assert.True(factory.HasTemplate("enemy_standard"));
        Assert.True(factory.HasTemplate("enemy_elite"));
    }

    [Fact]
    public void EnemyFactory_CreateWeakEnemy_ShouldHaveLowStats()
    {
        // Arrange
        var logger = new Mock<ILogger<EnemyEntityFactory>>();
        var factory = new EnemyEntityFactory(logger.Object);
        var spawnPos = new Vector2(50, 50);

        // Act
        var enemy = factory.CreateWeakEnemy(_world, spawnPos);

        // Assert
        Assert.True(_world.IsAlive(enemy));
        Assert.True(_world.Has<Health>(enemy));
        Assert.True(_world.Has<Position>(enemy));

        ref var health = ref _world.Get<Health>(enemy);
        Assert.Equal(30, health.Current);
    }

    [Fact]
    public void EnemyFactory_CreateEliteEnemy_ShouldHaveStats()
    {
        // Arrange
        var logger = new Mock<ILogger<EnemyEntityFactory>>();
        var factory = new EnemyEntityFactory(logger.Object);
        var spawnPos = new Vector2(100, 100);

        // Act
        var elite = factory.CreateEliteEnemy(_world, spawnPos);

        // Assert
        Assert.True(_world.Has<Stats>(elite));

        ref var stats = ref _world.Get<Stats>(elite);
        Assert.Equal(20, stats.Attack);
        Assert.Equal(15, stats.Defense);

        ref var health = ref _world.Get<Health>(elite);
        Assert.Equal(150, health.Current);
    }

    [Fact]
    public void EnemyFactory_CreateBossEnemy_ShouldHaveCustomName()
    {
        // Arrange
        var logger = new Mock<ILogger<EnemyEntityFactory>>();
        var factory = new EnemyEntityFactory(logger.Object, _mockEventBus.Object);
        var spawnPos = new Vector2(200, 200);

        EntityCreatedEvent? createdEvent = null;
        _mockEventBus.Setup(x => x.Publish(It.IsAny<EntityCreatedEvent>()))
            .Callback<IGameEvent>(e => createdEvent = e as EntityCreatedEvent);

        // Act
        var boss = factory.CreateBossEnemy(_world, spawnPos, "DragonKing");

        // Assert
        Assert.NotNull(createdEvent);
        Assert.Contains("DragonKing", createdEvent!.EntityType);

        ref var health = ref _world.Get<Health>(boss);
        Assert.Equal(500, health.Current);
    }

    #endregion

    #region ItemEntityFactory Tests

    [Fact]
    public void ItemFactory_ShouldRegisterDefaultTemplates()
    {
        // Arrange
        var logger = new Mock<ILogger<ItemEntityFactory>>();
        var factory = new ItemEntityFactory(logger.Object);

        // Assert
        Assert.True(factory.HasTemplate("item_health_potion"));
        Assert.True(factory.HasTemplate("item_coin"));
        Assert.True(factory.HasTemplate("item_speed_boost"));
        Assert.True(factory.HasTemplate("item_shield"));
    }

    [Fact]
    public void ItemFactory_CreateHealthPotion_ShouldHavePositionAndSprite()
    {
        // Arrange
        var logger = new Mock<ILogger<ItemEntityFactory>>();
        var factory = new ItemEntityFactory(logger.Object);
        var position = new Vector2(150, 150);

        // Act
        var potion = factory.CreateHealthPotion(_world, position, 50);

        // Assert
        Assert.True(_world.IsAlive(potion));
        Assert.True(_world.Has<Position>(potion));
        Assert.True(_world.Has<Sprite>(potion));
        Assert.True(_world.Has<Renderable>(potion));

        ref var pos = ref _world.Get<Position>(potion);
        Assert.Equal(150f, pos.X);
        Assert.Equal(150f, pos.Y);
    }

    [Fact]
    public void ItemFactory_CreateCoin_ShouldBeRenderable()
    {
        // Arrange
        var logger = new Mock<ILogger<ItemEntityFactory>>();
        var factory = new ItemEntityFactory(logger.Object);
        var position = new Vector2(10, 20);

        // Act
        var coin = factory.CreateCoin(_world, position, 5);

        // Assert
        Assert.True(_world.Has<Renderable>(coin));
        ref var renderable = ref _world.Get<Renderable>(coin);
        Assert.True(renderable.IsVisible);
    }

    [Fact]
    public void ItemFactory_CreateKey_ShouldHaveUniqueId()
    {
        // Arrange
        var logger = new Mock<ILogger<ItemEntityFactory>>();
        var factory = new ItemEntityFactory(logger.Object, _mockEventBus.Object);
        var position = new Vector2(0, 0);

        EntityCreatedEvent? createdEvent = null;
        _mockEventBus.Setup(x => x.Publish(It.IsAny<EntityCreatedEvent>()))
            .Callback<IGameEvent>(e => createdEvent = e as EntityCreatedEvent);

        // Act
        var key = factory.CreateKey(_world, position, "GoldKey");

        // Assert
        Assert.NotNull(createdEvent);
        Assert.Contains("GoldKey", createdEvent!.EntityType);
    }

    #endregion

    #region ProjectileEntityFactory Tests

    [Fact]
    public void ProjectileFactory_ShouldRegisterDefaultTemplates()
    {
        // Arrange
        var logger = new Mock<ILogger<ProjectileEntityFactory>>();
        var factory = new ProjectileEntityFactory(logger.Object);

        // Assert
        Assert.True(factory.HasTemplate("projectile_bullet"));
        Assert.True(factory.HasTemplate("projectile_arrow"));
        Assert.True(factory.HasTemplate("projectile_fireball"));
    }

    [Fact]
    public void ProjectileFactory_CreateBullet_ShouldBeCreatedAtPosition()
    {
        // Arrange
        var logger = new Mock<ILogger<ProjectileEntityFactory>>();
        var factory = new ProjectileEntityFactory(logger.Object);
        var position = new Vector2(100, 100);
        var direction = new Vector2(1, 0); // Right

        // Act
        var bullet = factory.CreateBullet(_world, position, direction, 400f);

        // Assert
        Assert.True(_world.IsAlive(bullet));
        Assert.True(_world.Has<Position>(bullet));

        ref var pos = ref _world.Get<Position>(bullet);
        Assert.Equal(100f, pos.X);
        Assert.Equal(100f, pos.Y);
        Assert.Equal(0.6f, pos.Z); // Higher rendering layer
    }

    [Fact]
    public void ProjectileFactory_CreateArrow_ShouldBeOrientedToDirection()
    {
        // Arrange
        var logger = new Mock<ILogger<ProjectileEntityFactory>>();
        var factory = new ProjectileEntityFactory(logger.Object);
        var position = new Vector2(0, 0);
        var direction = new Vector2(1, -1); // Up-right

        // Act
        var arrow = factory.CreateArrow(_world, position, direction, 300f);

        // Assert
        Assert.True(_world.IsAlive(arrow));
        Assert.True(_world.Has<Sprite>(arrow));
        Assert.True(_world.Has<Position>(arrow));

        ref var sprite = ref _world.Get<Sprite>(arrow);
        Assert.NotEqual(0f, sprite.Rotation); // Should be rotated to face direction
    }

    [Fact]
    public void ProjectileFactory_CreateFireball_ShouldBeRenderable()
    {
        // Arrange
        var logger = new Mock<ILogger<ProjectileEntityFactory>>();
        var factory = new ProjectileEntityFactory(logger.Object);
        var position = new Vector2(50, 50);
        var direction = Vector2.One;

        // Act
        var fireball = factory.CreateFireball(_world, position, direction, 250f);

        // Assert
        Assert.True(_world.Has<Renderable>(fireball));
        Assert.True(_world.Has<Sprite>(fireball));

        ref var sprite = ref _world.Get<Sprite>(fireball);
        Assert.Equal(24, sprite.Width);
        Assert.Equal(24, sprite.Height);
    }

    [Fact]
    public void ProjectileFactory_CreateHomingMissile_ShouldBeCreated()
    {
        // Arrange
        var logger = new Mock<ILogger<ProjectileEntityFactory>>();
        var factory = new ProjectileEntityFactory(logger.Object);
        var position = new Vector2(0, 0);
        var direction = new Vector2(1, 0);

        // Act
        var missile = factory.CreateHomingMissile(_world, position, direction, 200f);

        // Assert
        Assert.True(_world.IsAlive(missile));
        Assert.True(_world.Has<Position>(missile));
        Assert.True(_world.Has<Sprite>(missile));
    }

    [Fact]
    public void ProjectileFactory_CreateProjectile_ShouldHandleAnyDirection()
    {
        // Arrange
        var logger = new Mock<ILogger<ProjectileEntityFactory>>();
        var factory = new ProjectileEntityFactory(logger.Object);
        var position = new Vector2(0, 0);
        var direction = new Vector2(3, 4); // Any direction

        // Act
        var bullet = factory.CreateBullet(_world, position, direction, 100f);

        // Assert
        Assert.True(_world.IsAlive(bullet));
        Assert.True(_world.Has<Position>(bullet));
        Assert.True(_world.Has<Sprite>(bullet));
    }

    #endregion
}
