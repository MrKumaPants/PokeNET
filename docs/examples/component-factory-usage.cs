using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PokeNET.Core.ECS.Factories;
using PokeNET.Domain.ECS.Components;
using PokeNET.Domain.ECS.Factories;
using System.Text.Json;

// Example 1: Basic Component Creation
void Example1_BasicCreation()
{
    var logger = LoggerFactory.Create(builder => builder.AddConsole())
        .CreateLogger<ComponentFactory>();
    var factory = new ComponentFactory(logger);

    // Register all standard builders
    ComponentBuilders.RegisterAll(factory);

    // Create a Position component from definition
    var positionDef = new ComponentDefinition
    {
        TypeName = "Position",
        Properties = new Dictionary<string, object>
        {
            ["X"] = 100f,
            ["Y"] = 200f,
            ["Z"] = 0.5f
        }
    };

    var position = factory.Create<Position>(positionDef);
    Console.WriteLine($"Created position: {position}");
}

// Example 2: Loading Components from JSON
void Example2_JsonLoading()
{
    var logger = LoggerFactory.Create(builder => builder.AddConsole())
        .CreateLogger<ComponentFactory>();
    var factory = new ComponentFactory(logger);
    ComponentBuilders.RegisterAll(factory);

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
    if (definition != null)
    {
        var sprite = factory.Create<Sprite>(definition);
        Console.WriteLine($"Created sprite: {sprite.TexturePath}");
    }
}

// Example 3: Dynamic Component Creation (Type Known at Runtime)
void Example3_DynamicCreation()
{
    var logger = LoggerFactory.Create(builder => builder.AddConsole())
        .CreateLogger<ComponentFactory>();
    var factory = new ComponentFactory(logger);
    ComponentBuilders.RegisterAll(factory);

    // Type name from configuration
    var typeName = "Position";
    var componentType = Type.GetType($"PokeNET.Domain.ECS.Components.{typeName}");

    if (componentType != null && factory.CanCreate(componentType))
    {
        var definition = new ComponentDefinition
        {
            TypeName = typeName,
            Properties = new Dictionary<string, object>
            {
                ["X"] = 50f,
                ["Y"] = 75f
            }
        };

        var component = factory.CreateDynamic(componentType, definition);
        Console.WriteLine($"Created {component.GetType().Name} dynamically");
    }
}

// Example 4: Custom Builder Registration
void Example4_CustomBuilder()
{
    var logger = LoggerFactory.Create(builder => builder.AddConsole())
        .CreateLogger<ComponentFactory>();
    var factory = new ComponentFactory(logger);

    // Register custom builder with validation
    factory.RegisterBuilder<Health>(def =>
    {
        var max = def.GetInt("Maximum", 100);
        var current = def.GetInt("Current", max);

        // Custom validation
        if (current > max)
        {
            Console.WriteLine("Warning: Current health exceeds maximum, clamping");
            current = max;
        }

        return new Health(current, max);
    });

    var definition = new ComponentDefinition
    {
        Properties = new Dictionary<string, object>
        {
            ["Current"] = 150, // Invalid: exceeds max
            ["Maximum"] = 100
        }
    };

    var health = factory.Create<Health>(definition);
    Console.WriteLine($"Health: {health}"); // Will be clamped to 100/100
}

// Example 5: Entity Creation from JSON Configuration
void Example5_EntityFromJson()
{
    var logger = LoggerFactory.Create(builder => builder.AddConsole())
        .CreateLogger<ComponentFactory>();
    var factory = new ComponentFactory(logger);
    ComponentBuilders.RegisterAll(factory);

    var entityJson = @"{
        ""EntityName"": ""Pikachu"",
        ""Components"": [
            {
                ""TypeName"": ""Position"",
                ""Properties"": { ""X"": 400, ""Y"": 300 }
            },
            {
                ""TypeName"": ""Sprite"",
                ""Properties"": {
                    ""TexturePath"": ""textures/pikachu.png"",
                    ""Width"": 64,
                    ""Height"": 64
                }
            },
            {
                ""TypeName"": ""Health"",
                ""Properties"": { ""Current"": 100, ""Maximum"": 100 }
            },
            {
                ""TypeName"": ""Velocity"",
                ""Properties"": { ""X"": 0, ""Y"": 0 }
            }
        ]
    }";

    var entityDef = JsonSerializer.Deserialize<EntityDefinition>(entityJson);
    if (entityDef != null)
    {
        Console.WriteLine($"Creating entity: {entityDef.EntityName}");

        foreach (var componentDef in entityDef.Components)
        {
            var typeName = $"PokeNET.Domain.ECS.Components.{componentDef.TypeName}";
            var componentType = Type.GetType(typeName);

            if (componentType != null)
            {
                var component = factory.CreateDynamic(componentType, componentDef);
                Console.WriteLine($"  - Added {componentDef.TypeName} component");
            }
        }
    }
}

// Example 6: Mod System Integration
class ModComponentLoader
{
    private readonly IComponentFactory _factory;

    public ModComponentLoader(IComponentFactory factory)
    {
        _factory = factory;
    }

    public List<object> LoadModComponents(string modJsonPath)
    {
        var components = new List<object>();
        var json = File.ReadAllText(modJsonPath);
        var definitions = JsonSerializer.Deserialize<List<ComponentDefinition>>(json);

        if (definitions != null)
        {
            foreach (var def in definitions)
            {
                try
                {
                    var typeName = $"PokeNET.Domain.ECS.Components.{def.TypeName}";
                    var componentType = Type.GetType(typeName);

                    if (componentType != null && _factory.CanCreate(componentType))
                    {
                        var component = _factory.CreateDynamic(componentType, def);
                        components.Add(component);
                    }
                }
                catch (ComponentCreationException ex)
                {
                    Console.WriteLine($"Failed to create component: {ex.Message}");
                }
            }
        }

        return components;
    }
}

// Example 7: Dependency Injection Setup
void Example7_DISetup()
{
    var services = new ServiceCollection();

    // Register the factory
    services.AddSingleton<IComponentFactory>(sp =>
    {
        var logger = sp.GetRequiredService<ILogger<ComponentFactory>>();
        var factory = new ComponentFactory(logger);

        // Register all standard component builders
        ComponentBuilders.RegisterAll(factory);

        return factory;
    });

    services.AddLogging(builder => builder.AddConsole());

    var provider = services.BuildServiceProvider();

    // Use the factory
    var factory = provider.GetRequiredService<IComponentFactory>();
    Console.WriteLine($"Factory registered {factory.GetRegisteredTypes().Count()} component builders");
}

// Supporting types for examples
public record EntityDefinition
{
    public string EntityName { get; init; } = string.Empty;
    public List<ComponentDefinition> Components { get; init; } = new();
}
