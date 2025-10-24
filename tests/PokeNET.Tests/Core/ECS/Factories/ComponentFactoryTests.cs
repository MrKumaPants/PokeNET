using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework;
using Moq;
using PokeNET.Core.ECS.Factories;
using PokeNET.Domain.ECS.Components;
using PokeNET.Domain.ECS.Factories;

namespace PokeNET.Tests.Core.ECS.Factories;

public class ComponentFactoryTests
{
    private readonly ComponentFactory _factory;
    private readonly Mock<ILogger<ComponentFactory>> _loggerMock;

    public ComponentFactoryTests()
    {
        _loggerMock = new Mock<ILogger<ComponentFactory>>();
        _factory = new ComponentFactory(_loggerMock.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ComponentFactory(null!));
    }

    [Fact]
    public void Constructor_WithValidLogger_Succeeds()
    {
        // Act
        var factory = new ComponentFactory(_loggerMock.Object);

        // Assert
        Assert.NotNull(factory);
    }

    #endregion

    #region Create<T> Tests

    [Fact]
    public void Create_WithNullDefinition_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _factory.Create<Position>(null!));
    }

    [Fact]
    public void Create_Position_WithValidDefinition_CreatesComponent()
    {
        // Arrange
        var definition = new ComponentDefinition
        {
            TypeName = "Position",
            Properties = new Dictionary<string, object>
            {
                ["X"] = 100f,
                ["Y"] = 200f,
                ["Z"] = 0.5f
            }
        };

        // Act
        var position = _factory.Create<Position>(definition);

        // Assert
        Assert.Equal(100f, position.X);
        Assert.Equal(200f, position.Y);
        Assert.Equal(0.5f, position.Z);
    }

    [Fact]
    public void Create_GridPosition_WithValidDefinition_CreatesComponent()
    {
        // Arrange
        var definition = new ComponentDefinition
        {
            TypeName = "GridPosition",
            Properties = new Dictionary<string, object>
            {
                ["X"] = 5,
                ["Y"] = 10
            }
        };

        // Act
        var gridPosition = _factory.Create<GridPosition>(definition);

        // Assert
        Assert.Equal(5, gridPosition.TileX);
        Assert.Equal(10, gridPosition.TileY);
    }

    [Fact]
    public void Create_Health_WithValidDefinition_CreatesComponent()
    {
        // Arrange
        var definition = new ComponentDefinition
        {
            TypeName = "Health",
            Properties = new Dictionary<string, object>
            {
                ["Current"] = 75,
                ["Maximum"] = 100
            }
        };

        // Act
        var health = _factory.Create<Health>(definition);

        // Assert
        Assert.Equal(75, health.Current);
        Assert.Equal(100, health.Maximum);
        Assert.True(health.IsAlive);
    }

    [Fact]
    public void Create_WithRegisteredBuilder_UsesBuilder()
    {
        // Arrange
        var builderCalled = false;
        _factory.RegisterBuilder<Position>(def =>
        {
            builderCalled = true;
            return new Position(999f, 888f, 777f);
        });

        var definition = new ComponentDefinition
        {
            TypeName = "Position",
            Properties = new Dictionary<string, object>
            {
                ["X"] = 1f,
                ["Y"] = 2f,
                ["Z"] = 3f
            }
        };

        // Act
        var position = _factory.Create<Position>(definition);

        // Assert
        Assert.True(builderCalled);
        Assert.Equal(999f, position.X);
        Assert.Equal(888f, position.Y);
        Assert.Equal(777f, position.Z);
    }

    [Fact]
    public void Create_WithThrowingBuilder_ThrowsComponentCreationException()
    {
        // Arrange
        _factory.RegisterBuilder<Position>(def => throw new InvalidOperationException("Builder failed"));

        var definition = new ComponentDefinition
        {
            TypeName = "Position",
            Properties = new Dictionary<string, object>()
        };

        // Act & Assert
        var exception = Assert.Throws<ComponentCreationException>(() => _factory.Create<Position>(definition));
        Assert.Contains("Registered builder", exception.Message);
        Assert.NotNull(exception.InnerException);
    }

    [Fact]
    public void Create_WithMissingProperties_UsesDefaults()
    {
        // Arrange
        var definition = new ComponentDefinition
        {
            TypeName = "Position",
            Properties = new Dictionary<string, object>()
        };

        // Act
        var position = _factory.Create<Position>(definition);

        // Assert
        Assert.Equal(0f, position.X);
        Assert.Equal(0f, position.Y);
        Assert.Equal(0f, position.Z);
    }

    #endregion

    #region CreateDynamic Tests

    [Fact]
    public void CreateDynamic_WithNullType_ThrowsArgumentNullException()
    {
        // Arrange
        var definition = new ComponentDefinition();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _factory.CreateDynamic(null!, definition));
    }

    [Fact]
    public void CreateDynamic_WithNullDefinition_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _factory.CreateDynamic(typeof(Position), null!));
    }

    [Fact]
    public void CreateDynamic_WithNonStructType_ThrowsArgumentException()
    {
        // Arrange
        var definition = new ComponentDefinition();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _factory.CreateDynamic(typeof(string), definition));
    }

    [Fact]
    public void CreateDynamic_WithValidType_CreatesComponent()
    {
        // Arrange
        var definition = new ComponentDefinition
        {
            TypeName = "Position",
            Properties = new Dictionary<string, object>
            {
                ["X"] = 150f,
                ["Y"] = 250f,
                ["Z"] = 1.5f
            }
        };

        // Act
        var component = _factory.CreateDynamic(typeof(Position), definition);

        // Assert
        Assert.IsType<Position>(component);
        var position = (Position)component;
        Assert.Equal(150f, position.X);
        Assert.Equal(250f, position.Y);
        Assert.Equal(1.5f, position.Z);
    }

    [Fact]
    public void CreateDynamic_WithRegisteredBuilder_UsesBuilder()
    {
        // Arrange
        _factory.RegisterBuilder<MovementState>(def => new MovementState
        {
            Mode = MovementMode.Running,
            MovementSpeed = 8.0f,
            CanMove = true,
            CanRun = true
        });

        var definition = new ComponentDefinition
        {
            TypeName = "MovementState"
        };

        // Act
        var component = _factory.CreateDynamic(typeof(MovementState), definition);

        // Assert
        Assert.IsType<MovementState>(component);
        var movementState = (MovementState)component;
        Assert.Equal(MovementMode.Running, movementState.Mode);
        Assert.Equal(8.0f, movementState.MovementSpeed);
        Assert.True(movementState.CanMove);
        Assert.True(movementState.CanRun);
    }

    #endregion

    #region RegisterBuilder Tests

    [Fact]
    public void RegisterBuilder_WithNullBuilder_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _factory.RegisterBuilder<Position>(null!));
    }

    [Fact]
    public void RegisterBuilder_WithValidBuilder_Succeeds()
    {
        // Act
        _factory.RegisterBuilder<Position>(def => new Position());

        // Assert
        var registeredTypes = _factory.GetRegisteredTypes().ToList();
        Assert.Contains(typeof(Position), registeredTypes);
    }

    [Fact]
    public void RegisterBuilder_SameTypeTwice_ReplacesBuilder()
    {
        // Arrange
        _factory.RegisterBuilder<Position>(def => new Position(1f, 1f, 1f));
        _factory.RegisterBuilder<Position>(def => new Position(2f, 2f, 2f));

        var definition = new ComponentDefinition();

        // Act
        var position = _factory.Create<Position>(definition);

        // Assert
        Assert.Equal(2f, position.X);
        Assert.Equal(2f, position.Y);
        Assert.Equal(2f, position.Z);
    }

    #endregion

    #region CanCreate Tests

    [Fact]
    public void CanCreate_WithNullType_ReturnsFalse()
    {
        // Act
        var result = _factory.CanCreate(null!);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void CanCreate_WithNonStructType_ReturnsFalse()
    {
        // Act
        var result = _factory.CanCreate(typeof(string));

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void CanCreate_WithRegisteredType_ReturnsTrue()
    {
        // Arrange
        _factory.RegisterBuilder<Position>(def => new Position());

        // Act
        var result = _factory.CanCreate(typeof(Position));

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void CanCreate_WithUnregisteredStructType_ReturnsTrue()
    {
        // Act
        var result = _factory.CanCreate(typeof(Position));

        // Assert
        Assert.True(result);
    }

    #endregion

    #region GetRegisteredTypes Tests

    [Fact]
    public void GetRegisteredTypes_WithNoRegistrations_ReturnsEmpty()
    {
        // Act
        var types = _factory.GetRegisteredTypes().ToList();

        // Assert
        Assert.Empty(types);
    }

    [Fact]
    public void GetRegisteredTypes_WithMultipleRegistrations_ReturnsAll()
    {
        // Arrange
        _factory.RegisterBuilder<Position>(def => new Position());
        _factory.RegisterBuilder<GridPosition>(def => new GridPosition());
        _factory.RegisterBuilder<Health>(def => new Health());

        // Act
        var types = _factory.GetRegisteredTypes().ToList();

        // Assert
        Assert.Equal(3, types.Count);
        Assert.Contains(typeof(Position), types);
        Assert.Contains(typeof(GridPosition), types);
        Assert.Contains(typeof(Health), types);
    }

    #endregion

    #region UnregisterBuilder Tests

    [Fact]
    public void UnregisterBuilder_WithRegisteredType_RemovesBuilder()
    {
        // Arrange
        _factory.RegisterBuilder<Position>(def => new Position());

        // Act
        var result = _factory.UnregisterBuilder<Position>();

        // Assert
        Assert.True(result);
        var types = _factory.GetRegisteredTypes().ToList();
        Assert.DoesNotContain(typeof(Position), types);
    }

    [Fact]
    public void UnregisterBuilder_WithUnregisteredType_ReturnsFalse()
    {
        // Act
        var result = _factory.UnregisterBuilder<Position>();

        // Assert
        Assert.False(result);
    }

    #endregion

    #region ComponentBuilders Integration Tests

    [Fact]
    public void ComponentBuilders_RegisterAll_RegistersAllBuilders()
    {
        // Act
        ComponentBuilders.RegisterAll(_factory);

        // Assert
        var types = _factory.GetRegisteredTypes().ToList();
        Assert.Contains(typeof(Position), types);
        Assert.Contains(typeof(Sprite), types);
        Assert.Contains(typeof(Health), types);
        Assert.Contains(typeof(Stats), types);
        Assert.Contains(typeof(Camera), types);
        Assert.Contains(typeof(Renderable), types);
    }

    [Fact]
    public void BuildPosition_WithValidDefinition_CreatesCorrectComponent()
    {
        // Arrange
        var definition = new ComponentDefinition
        {
            Properties = new Dictionary<string, object>
            {
                ["X"] = 42f,
                ["Y"] = 84f,
                ["Z"] = 2.5f
            }
        };

        // Act
        var position = ComponentBuilders.BuildPosition(definition);

        // Assert
        Assert.Equal(42f, position.X);
        Assert.Equal(84f, position.Y);
        Assert.Equal(2.5f, position.Z);
    }

    [Fact]
    public void BuildSprite_WithFullDefinition_CreatesCorrectComponent()
    {
        // Arrange
        var definition = new ComponentDefinition
        {
            Properties = new Dictionary<string, object>
            {
                ["TexturePath"] = "textures/pikachu.png",
                ["Width"] = 64,
                ["Height"] = 64,
                ["LayerDepth"] = 0.7f,
                ["SourceX"] = 0,
                ["SourceY"] = 0,
                ["SourceWidth"] = 32,
                ["SourceHeight"] = 32,
                ["Scale"] = 2.0f,
                ["Rotation"] = 1.57f,
                ["IsVisible"] = true,
                ["ColorR"] = 255,
                ["ColorG"] = 200,
                ["ColorB"] = 100,
                ["ColorA"] = 255
            }
        };

        // Act
        var sprite = ComponentBuilders.BuildSprite(definition);

        // Assert
        Assert.Equal("textures/pikachu.png", sprite.TexturePath);
        Assert.Equal(64, sprite.Width);
        Assert.Equal(64, sprite.Height);
        Assert.Equal(0.7f, sprite.LayerDepth);
        Assert.NotNull(sprite.SourceRectangle);
        Assert.Equal(0, sprite.SourceRectangle.Value.X);
        Assert.Equal(0, sprite.SourceRectangle.Value.Y);
        Assert.Equal(32, sprite.SourceRectangle.Value.Width);
        Assert.Equal(32, sprite.SourceRectangle.Value.Height);
        Assert.Equal(2.0f, sprite.Scale);
        Assert.Equal(1.57f, sprite.Rotation);
        Assert.True(sprite.IsVisible);
        Assert.Equal(255, sprite.Color.R);
        Assert.Equal(200, sprite.Color.G);
        Assert.Equal(100, sprite.Color.B);
    }

    [Fact]
    public void BuildHealth_WithCurrentAndMaximum_CreatesCorrectComponent()
    {
        // Arrange
        var definition = new ComponentDefinition
        {
            Properties = new Dictionary<string, object>
            {
                ["Current"] = 50,
                ["Maximum"] = 150
            }
        };

        // Act
        var health = ComponentBuilders.BuildHealth(definition);

        // Assert
        Assert.Equal(50, health.Current);
        Assert.Equal(150, health.Maximum);
        Assert.True(health.IsAlive);
        Assert.Equal(50f / 150f, health.Percentage);
    }

    [Fact]
    public void BuildStats_WithAllProperties_CreatesCorrectComponent()
    {
        // Arrange
        var definition = new ComponentDefinition
        {
            Properties = new Dictionary<string, object>
            {
                ["Level"] = 25,
                ["Attack"] = 85,
                ["Defense"] = 70,
                ["SpecialAttack"] = 90,
                ["SpecialDefense"] = 75,
                ["Speed"] = 95
            }
        };

        // Act
        var stats = ComponentBuilders.BuildStats(definition);

        // Assert
        Assert.Equal(25, stats.Level);
        Assert.Equal(85, stats.Attack);
        Assert.Equal(70, stats.Defense);
        Assert.Equal(90, stats.SpecialAttack);
        Assert.Equal(75, stats.SpecialDefense);
        Assert.Equal(95, stats.Speed);
    }


    #endregion

    #region ComponentDefinition Tests

    [Fact]
    public void ComponentDefinition_GetProperty_WithExistingProperty_ReturnsValue()
    {
        // Arrange
        var definition = new ComponentDefinition
        {
            Properties = new Dictionary<string, object>
            {
                ["TestInt"] = 42,
                ["TestString"] = "hello"
            }
        };

        // Act
        var intValue = definition.GetProperty<int>("TestInt");
        var stringValue = definition.GetProperty<string>("TestString");

        // Assert
        Assert.Equal(42, intValue);
        Assert.Equal("hello", stringValue);
    }

    [Fact]
    public void ComponentDefinition_GetProperty_WithMissingProperty_ReturnsDefault()
    {
        // Arrange
        var definition = new ComponentDefinition();

        // Act
        var value = definition.GetProperty<int>("NonExistent", 99);

        // Assert
        Assert.Equal(99, value);
    }

    [Fact]
    public void ComponentDefinition_GetFloat_ReturnsCorrectValue()
    {
        // Arrange
        var definition = new ComponentDefinition
        {
            Properties = new Dictionary<string, object>
            {
                ["Value"] = 3.14f
            }
        };

        // Act
        var value = definition.GetFloat("Value");

        // Assert
        Assert.Equal(3.14f, value);
    }

    [Fact]
    public void ComponentDefinition_HasProperty_WithExistingProperty_ReturnsTrue()
    {
        // Arrange
        var definition = new ComponentDefinition
        {
            Properties = new Dictionary<string, object>
            {
                ["Test"] = 123
            }
        };

        // Act
        var result = definition.HasProperty("Test");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ComponentDefinition_HasProperty_WithMissingProperty_ReturnsFalse()
    {
        // Arrange
        var definition = new ComponentDefinition();

        // Act
        var result = definition.HasProperty("NonExistent");

        // Assert
        Assert.False(result);
    }

    #endregion
}
