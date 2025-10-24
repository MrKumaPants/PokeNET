# Component Factory Pattern

## Overview

The Component Factory Pattern enables dynamic component creation from configuration data (JSON, XML, mods, scripts) without modifying existing code. This follows the **Open/Closed Principle** - open for extension, closed for modification.

## Architecture

### Core Components

```
Domain Layer (Interfaces & Contracts)
├── IComponentFactory          - Factory interface
├── ComponentDefinition        - Component data definition
└── ComponentCreationException - Error handling

Core Layer (Implementation)
├── ComponentFactory           - Factory implementation
└── ComponentBuilders          - Standard component builders
```

### Class Diagram

```
┌─────────────────────────────┐
│   IComponentFactory         │
├─────────────────────────────┤
│ + Create<T>()               │
│ + CreateDynamic()           │
│ + RegisterBuilder<T>()      │
│ + CanCreate()               │
│ + GetRegisteredTypes()      │
└─────────────────────────────┘
            △
            │ implements
            │
┌─────────────────────────────┐
│   ComponentFactory          │
├─────────────────────────────┤
│ - _builders: Dictionary     │
│ - _logger: ILogger          │
├─────────────────────────────┤
│ + Create<T>()               │
│ + CreateDynamic()           │
│ - CreateViaReflection<T>()  │
│ - PopulateProperties()      │
│ - ConvertValue()            │
└─────────────────────────────┘
```

## Usage Patterns

### 1. Basic Component Creation

```csharp
var factory = serviceProvider.GetService<IComponentFactory>();

// From definition
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

var position = factory.Create<Position>(definition);
```

### 2. JSON-Based Component Creation

```csharp
// JSON configuration
var json = @"{
  ""TypeName"": ""Sprite"",
  ""Properties"": {
    ""TexturePath"": ""textures/pikachu.png"",
    ""Width"": 64,
    ""Height"": 64,
    ""LayerDepth"": 0.7,
    ""Scale"": 2.0
  }
}";

var definition = JsonSerializer.Deserialize<ComponentDefinition>(json);
var sprite = factory.Create<Sprite>(definition);
```

### 3. Dynamic Type Creation (Runtime)

```csharp
// Type only known at runtime
var componentType = Type.GetType("PokeNET.Domain.ECS.Components.Position");
var component = factory.CreateDynamic(componentType, definition);

// Cast back when needed
if (component is Position position)
{
    Console.WriteLine($"Position: {position}");
}
```

### 4. Custom Builder Registration

```csharp
// Register custom builder for complex initialization
factory.RegisterBuilder<CustomComponent>(def =>
{
    var component = new CustomComponent();

    // Custom initialization logic
    component.Initialize(
        def.GetString("Config"),
        def.GetInt("Level")
    );

    return component;
});
```

### 5. Mod System Integration

```csharp
public class ModLoader
{
    private readonly IComponentFactory _factory;

    public Entity LoadEntityFromMod(string modPath)
    {
        var modData = LoadModJson(modPath);

        var entity = world.CreateEntity();

        foreach (var componentDef in modData.Components)
        {
            var type = Type.GetType(componentDef.TypeName);
            var component = _factory.CreateDynamic(type, componentDef);

            // Add component to entity
            entity.Add(component);
        }

        return entity;
    }
}
```

## JSON Schema Examples

### Position Component

```json
{
  "TypeName": "Position",
  "Properties": {
    "X": 150.5,
    "Y": 200.0,
    "Z": 1.0
  }
}
```

### Sprite Component (Full)

```json
{
  "TypeName": "Sprite",
  "Properties": {
    "TexturePath": "textures/pikachu.png",
    "Width": 64,
    "Height": 64,
    "LayerDepth": 0.7,
    "SourceX": 0,
    "SourceY": 0,
    "SourceWidth": 32,
    "SourceHeight": 32,
    "Scale": 2.0,
    "Rotation": 0.0,
    "IsVisible": true,
    "ColorR": 255,
    "ColorG": 255,
    "ColorB": 255,
    "ColorA": 255
  }
}
```

### Health Component

```json
{
  "TypeName": "Health",
  "Properties": {
    "Current": 75,
    "Maximum": 100
  }
}
```

### Entity Definition (Multiple Components)

```json
{
  "EntityName": "Pikachu",
  "Components": [
    {
      "TypeName": "Position",
      "Properties": {
        "X": 400,
        "Y": 300,
        "Z": 0
      }
    },
    {
      "TypeName": "Sprite",
      "Properties": {
        "TexturePath": "textures/pikachu.png",
        "Width": 64,
        "Height": 64,
        "LayerDepth": 0.5
      }
    },
    {
      "TypeName": "Health",
      "Properties": {
        "Current": 100,
        "Maximum": 100
      }
    },
    {
      "TypeName": "Stats",
      "Properties": {
        "Level": 25,
        "Attack": 85,
        "Defense": 70,
        "Speed": 95,
        "Special": 80
      }
    },
    {
      "TypeName": "Velocity",
      "Properties": {
        "X": 0,
        "Y": 0
      }
    }
  ]
}
```

## Performance Considerations

### Builder Registry (Fast Path)
- **O(1)** dictionary lookup
- No reflection overhead
- Type-safe creation
- **Use for**: Frequently created components

### Reflection Fallback (Slow Path)
- **O(n)** property iteration
- Reflection overhead
- Dynamic type conversion
- **Use for**: Rare/mod components

### Optimization Tips

```csharp
// 1. Register builders for hot-path components
ComponentBuilders.RegisterAll(factory);

// 2. Cache factory in services
services.AddSingleton<IComponentFactory, ComponentFactory>();

// 3. Reuse definitions when possible
var definition = LoadDefinition();
var component1 = factory.Create<Position>(definition);
var component2 = factory.Create<Position>(definition); // Reuse

// 4. Batch entity creation
var entities = new List<Entity>();
foreach (var def in definitions)
{
    var entity = CreateEntityFromDefinition(def);
    entities.Add(entity);
}
```

## Dependency Injection Setup

```csharp
// Program.cs or Startup.cs
services.AddSingleton<IComponentFactory>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<ComponentFactory>>();
    var factory = new ComponentFactory(logger);

    // Register all standard builders
    ComponentBuilders.RegisterAll(factory);

    return factory;
});
```

## Error Handling

```csharp
try
{
    var component = factory.Create<Position>(definition);
}
catch (ComponentCreationException ex)
{
    Console.WriteLine($"Failed to create {ex.ComponentType?.Name}");
    Console.WriteLine($"Definition: {ex.Definition?.TypeName}");
    Console.WriteLine($"Error: {ex.Message}");

    if (ex.InnerException != null)
    {
        Console.WriteLine($"Inner: {ex.InnerException.Message}");
    }
}
```

## Testing

```csharp
public class ComponentFactoryTests
{
    private readonly IComponentFactory _factory;

    public ComponentFactoryTests()
    {
        var logger = new Mock<ILogger<ComponentFactory>>();
        _factory = new ComponentFactory(logger.Object);
        ComponentBuilders.RegisterAll(_factory);
    }

    [Fact]
    public void Create_Position_Success()
    {
        var definition = new ComponentDefinition
        {
            Properties = new Dictionary<string, object>
            {
                ["X"] = 100f,
                ["Y"] = 200f
            }
        };

        var position = _factory.Create<Position>(definition);

        Assert.Equal(100f, position.X);
        Assert.Equal(200f, position.Y);
    }
}
```

## Supported Components

The factory supports all standard ECS components:

- **Position** - 3D world position (X, Y, Z)
- **Velocity** - Movement velocity (X, Y)
- **Acceleration** - Movement acceleration (X, Y)
- **Sprite** - Visual rendering data
- **Health** - Hit points (Current, Maximum)
- **Stats** - Character stats (Level, Attack, Defense, etc.)
- **Camera** - Camera/viewport settings
- **Friction** - Physics friction coefficient
- **MovementConstraint** - Movement boundaries
- **Renderable** - Rendering flags

## Benefits

1. **Open/Closed Principle**: Add new components without modifying factory
2. **Mod Support**: Mods can define custom components via JSON
3. **Data-Driven**: Entities can be defined in configuration files
4. **Hot-Reloading**: Component definitions can be reloaded at runtime
5. **Type Safety**: Generic methods provide compile-time safety
6. **Performance**: Registered builders avoid reflection overhead
7. **Testability**: Easy to mock and test component creation
8. **Flexibility**: Supports both compile-time and runtime type creation

## Future Enhancements

- Component validation schemas
- Component inheritance support
- Nested component creation
- Component templates
- Component pooling for performance
- Automatic builder generation via source generators
- YAML/XML format support
- Component versioning for save compatibility
