using Arch.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework;
using Moq;
using PokeNET.Core.ECS;
using PokeNET.Core.ECS.Factories;
using PokeNET.Domain.ECS.Components;
using PokeNET.Domain.ECS.Events;
using PokeNET.Domain.ECS.Factories;
using Xunit;

namespace PokeNET.Tests.ECS.Factories;

/// <summary>
/// Tests for the EntityFactory base implementation and specialized factories.
/// Validates factory pattern implementation, template management, and entity creation.
/// </summary>
public sealed class EntityFactoryTests : IDisposable
{
    private readonly World _world;
    private readonly Mock<ILogger<EntityFactory>> _mockLogger;
    private readonly Mock<IEventBus> _mockEventBus;
    private readonly EntityFactory _factory;

    public EntityFactoryTests()
    {
        _world = World.Create();
        _mockLogger = new Mock<ILogger<EntityFactory>>();
        _mockEventBus = new Mock<IEventBus>();
        _factory = new EntityFactory(_mockLogger.Object, _mockEventBus.Object);
    }

    public void Dispose()
    {
        World.Destroy(_world);
    }

    #region Factory Creation Tests

    [Fact]
    public void Factory_ShouldInitializeSuccessfully()
    {
        // Assert
        Assert.NotNull(_factory);
        Assert.Empty(_factory.GetTemplateNames());
    }

    [Fact]
    public void Factory_ShouldRequireLogger()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new EntityFactory(null!, _mockEventBus.Object));
    }

    [Fact]
    public void Factory_ShouldWorkWithoutEventBus()
    {
        // Arrange & Act
        var factory = new EntityFactory(_mockLogger.Object, null);

        // Assert
        Assert.NotNull(factory);
    }

    #endregion

    #region Entity Creation Tests

    [Fact]
    public void Create_WithValidDefinition_ShouldCreateEntity()
    {
        // Arrange
        var definition = new EntityDefinition(
            "TestEntity",
            new object[] { new Position(10, 20), new GridPosition(5, 10) }
        );

        // Act
        var entity = _factory.Create(_world, definition);

        // Assert
        Assert.True(_world.IsAlive(entity));
        Assert.True(_world.Has<Position>(entity));
        Assert.True(_world.Has<GridPosition>(entity));

        ref var position = ref _world.Get<Position>(entity);
        Assert.Equal(10f, position.X);
        Assert.Equal(20f, position.Y);

        ref var gridPosition = ref _world.Get<GridPosition>(entity);
        Assert.Equal(5, gridPosition.TileX);
        Assert.Equal(10, gridPosition.TileY);
    }

    [Fact]
    public void Create_ShouldPublishEntityCreatedEvent()
    {
        // Arrange
        var definition = new EntityDefinition(
            "TestEntity",
            new object[] { new Position(0, 0) }
        );

        EntityCreatedEvent? publishedEvent = null;
        _mockEventBus.Setup(x => x.Publish(It.IsAny<EntityCreatedEvent>()))
            .Callback<IGameEvent>(e => publishedEvent = e as EntityCreatedEvent);

        // Act
        var entity = _factory.Create(_world, definition);

        // Assert
        _mockEventBus.Verify(x => x.Publish(It.IsAny<EntityCreatedEvent>()), Times.Once);
        Assert.NotNull(publishedEvent);
        Assert.Equal(entity, publishedEvent!.Entity);
        Assert.Equal("TestEntity", publishedEvent.EntityType);
        Assert.Equal("EntityFactory", publishedEvent.FactoryName);
        Assert.Equal(1, publishedEvent.ComponentCount);
    }

    [Fact]
    public void Create_WithNullWorld_ShouldThrowArgumentNullException()
    {
        // Arrange
        var definition = new EntityDefinition("Test", new object[] { new Position() });

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _factory.Create(null!, definition));
    }

    [Fact]
    public void Create_WithNullDefinition_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _factory.Create(_world, null!));
    }

    [Fact]
    public void Create_WithInvalidDefinition_ShouldThrowInvalidOperationException()
    {
        // Arrange - definition with no components
        var definition = new EntityDefinition("Empty", Array.Empty<object>());

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => _factory.Create(_world, definition));
    }

    [Fact]
    public void Create_WithDuplicateComponents_ShouldThrowArgumentException()
    {
        // Arrange - two Position components
        var definition = new EntityDefinition(
            "Duplicate",
            new object[] { new Position(0, 0), new Position(10, 10) }
        );

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            _factory.Create(_world, definition));
        Assert.Contains("Duplicate component types", exception.Message);
    }

    #endregion

    #region Template Management Tests

    [Fact]
    public void RegisterTemplate_WithValidData_ShouldSucceed()
    {
        // Arrange
        var definition = new EntityDefinition(
            "Player",
            new object[] { new Position(), new Health(100) }
        );

        // Act
        _factory.RegisterTemplate("player_basic", definition);

        // Assert
        Assert.True(_factory.HasTemplate("player_basic"));
        Assert.Contains("player_basic", _factory.GetTemplateNames());
    }

    [Fact]
    public void RegisterTemplate_WithEmptyName_ShouldThrowArgumentException()
    {
        // Arrange
        var definition = new EntityDefinition("Test", new object[] { new Position() });

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            _factory.RegisterTemplate("", definition));
    }

    [Fact]
    public void RegisterTemplate_WithDuplicateName_ShouldThrowArgumentException()
    {
        // Arrange
        var definition = new EntityDefinition("Test", new object[] { new Position() });
        _factory.RegisterTemplate("test", definition);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            _factory.RegisterTemplate("test", definition));
        Assert.Contains("already registered", exception.Message);
    }

    [Fact]
    public void RegisterTemplate_IsCaseInsensitive()
    {
        // Arrange
        var definition = new EntityDefinition("Test", new object[] { new Position() });
        _factory.RegisterTemplate("MyTemplate", definition);

        // Act & Assert
        Assert.True(_factory.HasTemplate("mytemplate"));
        Assert.True(_factory.HasTemplate("MYTEMPLATE"));
        Assert.True(_factory.HasTemplate("MyTemplate"));
    }

    [Fact]
    public void UnregisterTemplate_WithExistingTemplate_ShouldReturnTrue()
    {
        // Arrange
        var definition = new EntityDefinition("Test", new object[] { new Position() });
        _factory.RegisterTemplate("test", definition);

        // Act
        var result = _factory.UnregisterTemplate("test");

        // Assert
        Assert.True(result);
        Assert.False(_factory.HasTemplate("test"));
    }

    [Fact]
    public void UnregisterTemplate_WithNonExistentTemplate_ShouldReturnFalse()
    {
        // Act
        var result = _factory.UnregisterTemplate("nonexistent");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void HasTemplate_WithNullOrEmpty_ShouldReturnFalse()
    {
        // Act & Assert
        Assert.False(_factory.HasTemplate(null!));
        Assert.False(_factory.HasTemplate(""));
        Assert.False(_factory.HasTemplate("   "));
    }

    [Fact]
    public void GetTemplateNames_ShouldReturnAllRegisteredNames()
    {
        // Arrange
        var def1 = new EntityDefinition("Test1", new object[] { new Position() });
        var def2 = new EntityDefinition("Test2", new object[] { new GridPosition() });

        _factory.RegisterTemplate("template1", def1);
        _factory.RegisterTemplate("template2", def2);

        // Act
        var names = _factory.GetTemplateNames().ToList();

        // Assert
        Assert.Equal(2, names.Count);
        Assert.Contains("template1", names);
        Assert.Contains("template2", names);
    }

    #endregion

    #region CreateFromTemplate Tests

    [Fact]
    public void CreateFromTemplate_WithExistingTemplate_ShouldCreateEntity()
    {
        // Arrange
        var definition = new EntityDefinition(
            "Enemy",
            new object[] { new Position(50, 50), new Health(50) }
        );
        _factory.RegisterTemplate("enemy_basic", definition);

        // Act
        var entity = _factory.CreateFromTemplate(_world, "enemy_basic");

        // Assert
        Assert.True(_world.IsAlive(entity));
        Assert.True(_world.Has<Position>(entity));
        Assert.True(_world.Has<Health>(entity));

        ref var health = ref _world.Get<Health>(entity);
        Assert.Equal(50, health.Current);
    }

    [Fact]
    public void CreateFromTemplate_WithNonExistentTemplate_ShouldThrowArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            _factory.CreateFromTemplate(_world, "nonexistent"));
        Assert.Contains("not found", exception.Message);
    }

    [Fact]
    public void CreateFromTemplate_WithEmptyName_ShouldThrowArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            _factory.CreateFromTemplate(_world, ""));
    }

    #endregion

    #region EntityDefinition Tests

    [Fact]
    public void EntityDefinition_WithComponents_ShouldInitializeCorrectly()
    {
        // Arrange & Act
        var definition = new EntityDefinition(
            "TestEntity",
            new object[] { new Position(1, 2), new GridPosition(3, 4) },
            new Dictionary<string, object> { ["Key"] = "Value" }
        );

        // Assert
        Assert.Equal("TestEntity", definition.Name);
        Assert.Equal(2, definition.Components.Count);
        Assert.Single(definition.Metadata);
        Assert.True(definition.IsValid());
    }

    [Fact]
    public void EntityDefinition_WithComponents_ShouldBeValid()
    {
        // Arrange
        var definition = new EntityDefinition("Valid", new object[] { new Position() });

        // Act & Assert
        Assert.True(definition.IsValid());
    }

    [Fact]
    public void EntityDefinition_WithoutComponents_ShouldBeInvalid()
    {
        // Arrange
        var definition = new EntityDefinition("Invalid", Array.Empty<object>());

        // Act & Assert
        Assert.False(definition.IsValid());
    }

    [Fact]
    public void EntityDefinition_WithEmptyName_ShouldBeInvalid()
    {
        // Arrange
        var definition = new EntityDefinition("", new object[] { new Position() });

        // Act & Assert
        Assert.False(definition.IsValid());
    }

    [Fact]
    public void EntityDefinition_WithComponents_ShouldCopyComponents()
    {
        // Arrange
        var components = new List<object> { new Position(), new GridPosition() };
        var definition = new EntityDefinition("Test", components);

        // Act - modify original list
        components.Add(new Health(100));

        // Assert - definition should not be affected
        Assert.Equal(2, definition.Components.Count);
    }

    [Fact]
    public void EntityDefinition_WithComponents_ShouldAddComponents()
    {
        // Arrange
        var definition = new EntityDefinition(
            "Base",
            new object[] { new Position() }
        );

        // Act
        var extended = definition.WithComponents(new GridPosition(), new Health(100));

        // Assert
        Assert.Single(definition.Components); // Original unchanged
        Assert.Equal(3, extended.Components.Count); // New has all components
    }

    [Fact]
    public void EntityDefinition_WithMetadata_ShouldAddMetadata()
    {
        // Arrange
        var definition = new EntityDefinition(
            "Base",
            new object[] { new Position() }
        );

        // Act
        var withMetadata = definition.WithMetadata("Level", 5);

        // Assert
        Assert.Empty(definition.Metadata); // Original unchanged
        Assert.Single(withMetadata.Metadata); // New has metadata
        Assert.Equal(5, withMetadata.Metadata["Level"]);
    }

    #endregion
}
