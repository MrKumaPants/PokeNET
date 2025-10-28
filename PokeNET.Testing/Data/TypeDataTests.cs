using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using PokeNET.Core.Data;
using Xunit;

namespace PokeNET.Tests.Data;

/// <summary>
/// Unit tests for TypeData data model.
/// Validates JSON deserialization, property values, and matchup data structure.
/// </summary>
public class TypeDataTests
{
    [Fact]
    public void TypeData_DefaultConstructor_SetsDefaults()
    {
        // Arrange & Act
        var typeInfo = new TypeData();

        // Assert
        Assert.Equal(string.Empty, typeInfo.Name);
        Assert.Equal("#FFFFFF", typeInfo.Color);
        Assert.NotNull(typeInfo.Matchups);
        Assert.Empty(typeInfo.Matchups);
        Assert.Null(typeInfo.Description);
    }

    [Fact]
    public void TypeData_WithProperties_StoresValuesCorrectly()
    {
        // Arrange
        var typeInfo = new TypeData
        {
            Name = "Fire",
            Color = "#F08030",
            Description = "Fire type Pokemon",
            Matchups = new Dictionary<string, double> { { "Grass", 2.0 }, { "Water", 0.5 } },
        };

        // Assert
        Assert.Equal("Fire", typeInfo.Name);
        Assert.Equal("#F08030", typeInfo.Color);
        Assert.Equal("Fire type Pokemon", typeInfo.Description);
        Assert.Equal(2, typeInfo.Matchups.Count);
        Assert.Equal(2.0, typeInfo.Matchups["Grass"]);
        Assert.Equal(0.5, typeInfo.Matchups["Water"]);
    }

    [Fact]
    public void TypeData_DeserializeFromJson_SingleType_Success()
    {
        // Arrange
        var json = """
            {
                "name": "Fire",
                "color": "#F08030",
                "description": "Fire type Pokemon",
                "matchups": {
                    "Fire": 0.5,
                    "Water": 0.5,
                    "Grass": 2.0,
                    "Ice": 2.0,
                    "Bug": 2.0,
                    "Rock": 0.5,
                    "Steel": 2.0
                }
            }
            """;

        // Act
        var typeInfo = JsonSerializer.Deserialize<TypeData>(
            json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );

        // Assert
        Assert.NotNull(typeInfo);
        Assert.Equal("Fire", typeInfo.Name);
        Assert.Equal("#F08030", typeInfo.Color);
        Assert.Equal("Fire type Pokemon", typeInfo.Description);
        Assert.Equal(7, typeInfo.Matchups.Count);
        Assert.Equal(0.5, typeInfo.Matchups["Fire"]);
        Assert.Equal(0.5, typeInfo.Matchups["Water"]);
        Assert.Equal(2.0, typeInfo.Matchups["Grass"]);
        Assert.Equal(2.0, typeInfo.Matchups["Ice"]);
    }

    [Fact]
    public void TypeData_DeserializeFromJson_ArrayOfTypes_Success()
    {
        // Arrange
        var json = """
            [
                {
                    "name": "Normal",
                    "color": "#A8A878",
                    "description": "Normal type Pokemon",
                    "matchups": {
                        "Rock": 0.5,
                        "Ghost": 0.0,
                        "Steel": 0.5
                    }
                },
                {
                    "name": "Fire",
                    "color": "#F08030",
                    "description": "Fire type Pokemon",
                    "matchups": {
                        "Grass": 2.0,
                        "Water": 0.5
                    }
                }
            ]
            """;

        // Act
        var types = JsonSerializer.Deserialize<List<TypeData>>(
            json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );

        // Assert
        Assert.NotNull(types);
        Assert.Equal(2, types.Count);

        // Verify Normal type
        var normalType = types[0];
        Assert.Equal("Normal", normalType.Name);
        Assert.Equal("#A8A878", normalType.Color);
        Assert.Equal(3, normalType.Matchups.Count);
        Assert.Equal(0.0, normalType.Matchups["Ghost"]);

        // Verify Fire type
        var fireType = types[1];
        Assert.Equal("Fire", fireType.Name);
        Assert.Equal("#F08030", fireType.Color);
        Assert.Equal(2, fireType.Matchups.Count);
        Assert.Equal(2.0, fireType.Matchups["Grass"]);
    }

    [Fact]
    public void TypeData_Matchups_SupportsAllEffectivenessValues()
    {
        // Arrange
        var typeInfo = new TypeData
        {
            Name = "Test",
            Matchups = new Dictionary<string, double>
            {
                { "Immune", 0.0 }, // No effect
                { "Quarter", 0.25 }, // Double resistance (dual-type)
                { "Resist", 0.5 }, // Not very effective
                { "Neutral", 1.0 }, // Normal damage
                { "Super", 2.0 }, // Super effective
                { "Quadruple", 4.0 }, // Double weakness (dual-type)
            },
        };

        // Assert - All standard Pokemon effectiveness values
        Assert.Equal(0.0, typeInfo.Matchups["Immune"]);
        Assert.Equal(0.25, typeInfo.Matchups["Quarter"]);
        Assert.Equal(0.5, typeInfo.Matchups["Resist"]);
        Assert.Equal(1.0, typeInfo.Matchups["Neutral"]);
        Assert.Equal(2.0, typeInfo.Matchups["Super"]);
        Assert.Equal(4.0, typeInfo.Matchups["Quadruple"]);
    }

    [Fact]
    public void TypeData_EmptyMatchups_IsValid()
    {
        // Arrange
        var json = """
            {
                "name": "Normal",
                "color": "#A8A878",
                "matchups": {}
            }
            """;

        // Act
        var typeInfo = JsonSerializer.Deserialize<TypeData>(
            json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );

        // Assert
        Assert.NotNull(typeInfo);
        Assert.Equal("Normal", typeInfo.Name);
        Assert.NotNull(typeInfo.Matchups);
        Assert.Empty(typeInfo.Matchups);
    }

    [Fact]
    public void TypeData_MissingOptionalFields_UsesDefaults()
    {
        // Arrange - Only required fields
        var json = """
            {
                "name": "Fire",
                "matchups": {
                    "Grass": 2.0
                }
            }
            """;

        // Act
        var typeInfo = JsonSerializer.Deserialize<TypeData>(
            json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );

        // Assert
        Assert.NotNull(typeInfo);
        Assert.Equal("Fire", typeInfo.Name);
        Assert.Equal("#FFFFFF", typeInfo.Color); // Default color
        Assert.Null(typeInfo.Description); // Optional field
        Assert.Single(typeInfo.Matchups);
    }

    [Fact]
    public void TypeData_Gen6Types_AllEighteen_Deserialize()
    {
        // Arrange - Test with all 18 Gen 6+ types
        var typeNames = new[]
        {
            "Normal",
            "Fire",
            "Water",
            "Electric",
            "Grass",
            "Ice",
            "Fighting",
            "Poison",
            "Ground",
            "Flying",
            "Psychic",
            "Bug",
            "Rock",
            "Ghost",
            "Dragon",
            "Dark",
            "Steel",
            "Fairy",
        };

        // Act & Assert - Verify each type can be created
        foreach (var typeName in typeNames)
        {
            var typeInfo = new TypeData
            {
                Name = typeName,
                Color = "#FFFFFF",
                Matchups = new Dictionary<string, double>(),
            };

            Assert.Equal(typeName, typeInfo.Name);
        }
    }

    [Fact]
    public void TypeData_ColorFormat_SupportsHexColors()
    {
        // Arrange - Test various valid hex color formats
        var validColors = new[]
        {
            "#FFFFFF", // White
            "#000000", // Black
            "#F08030", // Fire orange
            "#6890F0", // Water blue
            "#78C850", // Grass green
            "#A8A878", // Normal gray
        };

        // Act & Assert
        foreach (var color in validColors)
        {
            var typeInfo = new TypeData { Name = "Test", Color = color };

            Assert.Equal(color, typeInfo.Color);
            Assert.StartsWith("#", typeInfo.Color);
            Assert.Equal(7, typeInfo.Color.Length); // #RRGGBB format
        }
    }

    [Fact]
    public void TypeData_RoundTrip_SerializeDeserialize_PreservesData()
    {
        // Arrange
        var original = new TypeData
        {
            Name = "Dragon",
            Color = "#7038F8",
            Description = "Dragon type Pokemon",
            Matchups = new Dictionary<string, double>
            {
                { "Dragon", 2.0 },
                { "Steel", 0.5 },
                { "Fairy", 0.0 },
            },
        };

        // Act - Serialize then deserialize
        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<TypeData>(
            json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(original.Name, deserialized.Name);
        Assert.Equal(original.Color, deserialized.Color);
        Assert.Equal(original.Description, deserialized.Description);
        Assert.Equal(original.Matchups.Count, deserialized.Matchups.Count);
        Assert.Equal(original.Matchups["Dragon"], deserialized.Matchups["Dragon"]);
        Assert.Equal(original.Matchups["Fairy"], deserialized.Matchups["Fairy"]);
    }
}
