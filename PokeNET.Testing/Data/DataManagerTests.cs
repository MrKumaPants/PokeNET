using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using PokeNET.Core.Data;
using Xunit;

namespace PokeNET.Core.Tests.Data;

/// <summary>
/// Unit tests for DataManager.
/// </summary>
public class DataManagerTests : IDisposable
{
    private readonly string _testDataPath;
    private readonly GameDataContext _context;
    private readonly DataManager _dataManager;

    public DataManagerTests()
    {
        // Create temporary test data directory
        _testDataPath = Path.Combine(Path.GetTempPath(), $"PokeNET_Test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDataPath);

        // Create subdirectories for data files
        Directory.CreateDirectory(Path.Combine(_testDataPath, "species"));
        Directory.CreateDirectory(Path.Combine(_testDataPath, "moves"));
        Directory.CreateDirectory(Path.Combine(_testDataPath, "items"));
        Directory.CreateDirectory(Path.Combine(_testDataPath, "encounters"));
        Directory.CreateDirectory(Path.Combine(_testDataPath, "types"));

        // Create GameDataContext with in-memory database
        var options = new DbContextOptionsBuilder<GameDataContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .EnableSensitiveDataLogging() // Enable detailed entity tracking information for debugging
            .EnableDetailedErrors() // Enable detailed error messages
            .Options;
        _context = new GameDataContext(options);

        _dataManager = new DataManager(_context, NullLogger<DataManager>.Instance, _testDataPath);

        // Create test data files
        CreateTestData();
    }

    private void CreateTestData()
    {
        // Create test species data (one per file)
        var bulbasaur = new SpeciesData
        {
            Id = "bulbasaur",
            NationalDexNumber = 1,
            Name = "Bulbasaur",
            Types = new List<string> { "Grass", "Poison" },
            BaseStats = new BaseStats
            {
                HP = 45,
                Attack = 49,
                Defense = 49,
                SpecialAttack = 65,
                SpecialDefense = 65,
                Speed = 45,
            },
        };

        var charmander = new SpeciesData
        {
            Id = "charmander",
            NationalDexNumber = 4,
            Name = "Charmander",
            Types = new List<string> { "Fire" },
            BaseStats = new BaseStats
            {
                HP = 39,
                Attack = 52,
                Defense = 43,
                SpecialAttack = 60,
                SpecialDefense = 50,
                Speed = 65,
            },
        };

        // Create test move data (one per file)
        var tackle = new MoveData
        {
            Id = "tackle",
            Name = "Tackle",
            Type = "Normal",
            Category = MoveCategory.Physical,
            Power = 40,
            Accuracy = 100,
            PP = 35,
        };

        var thunderbolt = new MoveData
        {
            Id = "thunderbolt",
            Name = "Thunderbolt",
            Type = "Electric",
            Category = MoveCategory.Special,
            Power = 90,
            Accuracy = 100,
            PP = 15,
        };

        // Create test item data (one per file)
        var potion = new ItemData
        {
            Id = "potion",
            Name = "Potion",
            Category = ItemCategory.Medicine,
            BuyPrice = 200,
            SellPrice = 100,
            Consumable = true,
        };

        var pokeball = new ItemData
        {
            Id = "pokeball",
            Name = "Pokeball",
            Category = ItemCategory.Pokeball,
            BuyPrice = 200,
            SellPrice = 100,
            Consumable = true,
        };

        // Create test encounter data
        var route1 = new EncounterTable
        {
            LocationId = "route_1",
            LocationName = "Route 1",
            GrassEncounters = new List<Encounter>
            {
                new()
                {
                    SpeciesId = "bulbasaur",
                    MinLevel = 2,
                    MaxLevel = 4,
                    Rate = 50,
                },
            },
        };

        // Create test type data
        var fire = new TypeData
        {
            Id = "fire",
            Name = "Fire",
            Color = "#F08030",
            Description = "Fire type Pokemon",
            Matchups = new Dictionary<string, double>
            {
                { "Fire", 0.5 },
                { "Water", 0.5 },
                { "Grass", 2.0 },
                { "Ice", 2.0 },
                { "Steel", 2.0 },
            },
        };

        var water = new TypeData
        {
            Id = "water",
            Name = "Water",
            Color = "#6890F0",
            Description = "Water type Pokemon",
            Matchups = new Dictionary<string, double>
            {
                { "Fire", 2.0 },
                { "Water", 0.5 },
                { "Grass", 0.5 },
                { "Ground", 2.0 },
                { "Rock", 2.0 },
            },
        };

        var grass = new TypeData
        {
            Id = "grass",
            Name = "Grass",
            Color = "#78C850",
            Description = "Grass type Pokemon",
            Matchups = new Dictionary<string, double>
            {
                { "Fire", 0.5 },
                { "Water", 2.0 },
                { "Grass", 0.5 },
                { "Ground", 2.0 },
                { "Rock", 2.0 },
            },
        };

        // Write JSON files (one file per entity, as expected by the new directory structure)
        var jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        File.WriteAllText(
            Path.Combine(_testDataPath, "species", "bulbasaur.json"),
            JsonSerializer.Serialize(bulbasaur, jsonOptions)
        );

        File.WriteAllText(
            Path.Combine(_testDataPath, "species", "charmander.json"),
            JsonSerializer.Serialize(charmander, jsonOptions)
        );

        File.WriteAllText(
            Path.Combine(_testDataPath, "moves", "tackle.json"),
            JsonSerializer.Serialize(tackle, jsonOptions)
        );

        File.WriteAllText(
            Path.Combine(_testDataPath, "moves", "thunderbolt.json"),
            JsonSerializer.Serialize(thunderbolt, jsonOptions)
        );

        File.WriteAllText(
            Path.Combine(_testDataPath, "items", "potion.json"),
            JsonSerializer.Serialize(potion, jsonOptions)
        );

        File.WriteAllText(
            Path.Combine(_testDataPath, "items", "pokeball.json"),
            JsonSerializer.Serialize(pokeball, jsonOptions)
        );

        File.WriteAllText(
            Path.Combine(_testDataPath, "encounters", "route_1.json"),
            JsonSerializer.Serialize(route1, jsonOptions)
        );

        File.WriteAllText(
            Path.Combine(_testDataPath, "types", "fire.json"),
            JsonSerializer.Serialize(fire, jsonOptions)
        );

        File.WriteAllText(
            Path.Combine(_testDataPath, "types", "water.json"),
            JsonSerializer.Serialize(water, jsonOptions)
        );

        File.WriteAllText(
            Path.Combine(_testDataPath, "types", "grass.json"),
            JsonSerializer.Serialize(grass, jsonOptions)
        );
    }

    [Fact]
    public async Task GetSpeciesAsync_ReturnsSpeciesById()
    {
        // Act
        var species = await _dataManager.GetSpeciesAsync("bulbasaur");

        // Assert
        Assert.NotNull(species);
        Assert.Equal("bulbasaur", species.Id);
        Assert.Equal("Bulbasaur", species.Name);
        Assert.Contains("Grass", species.Types);
    }

    [Fact]
    public async Task GetSpeciesByNameAsync_ReturnsSpeciesByName()
    {
        // Act
        var species = await _dataManager.GetSpeciesByNameAsync("Charmander");

        // Assert
        Assert.NotNull(species);
        Assert.Equal("charmander", species.Id);
        Assert.Equal("Charmander", species.Name);
    }

    [Fact]
    public async Task GetSpeciesByNameAsync_IsCaseInsensitive()
    {
        // Act
        var species = await _dataManager.GetSpeciesByNameAsync("BULBASAUR");

        // Assert
        Assert.NotNull(species);
        Assert.Equal("Bulbasaur", species.Name);
    }

    [Fact]
    public async Task GetAllSpeciesAsync_ReturnsAllSpecies()
    {
        // Act
        var allSpecies = await _dataManager.GetAllSpeciesAsync();

        // Assert
        Assert.Equal(2, allSpecies.Count);
    }

    [Fact]
    public async Task GetMoveAsync_ReturnsMoveById()
    {
        // Act
        var move = await _dataManager.GetMoveAsync("tackle");

        // Assert
        Assert.NotNull(move);
        Assert.Equal("Tackle", move.Name);
        Assert.Equal(40, move.Power);
    }

    [Fact]
    public async Task GetMovesByTypeAsync_FiltersCorrectly()
    {
        // Act
        var electricMoves = await _dataManager.GetMovesByTypeAsync("Electric");

        // Assert
        Assert.Single(electricMoves);
        Assert.Equal("Thunderbolt", electricMoves[0].Name);
    }

    [Fact]
    public async Task GetItemAsync_ReturnsItemById()
    {
        // Act
        var item = await _dataManager.GetItemAsync("potion");

        // Assert
        Assert.NotNull(item);
        Assert.Equal("Potion", item.Name);
        Assert.Equal(ItemCategory.Medicine, item.Category);
    }

    [Fact]
    public async Task GetItemByNameAsync_ReturnsItemByName()
    {
        // Act
        var item = await _dataManager.GetItemByNameAsync("Pokeball");

        // Assert
        Assert.NotNull(item);
        Assert.Equal("pokeball", item.Id);
    }

    [Fact]
    public async Task GetItemsByCategoryAsync_FiltersCorrectly()
    {
        // Act
        var medicine = await _dataManager.GetItemsByCategoryAsync(ItemCategory.Medicine);

        // Assert
        Assert.Single(medicine);
        Assert.Equal("Potion", medicine[0].Name);
    }

    [Fact]
    public async Task GetEncountersAsync_ReturnsEncounterTable()
    {
        // Act
        var encounters = await _dataManager.GetEncountersAsync("route_1");

        // Assert
        Assert.NotNull(encounters);
        Assert.Equal("Route 1", encounters.LocationName);
        Assert.Single(encounters.GrassEncounters);
    }

    [Fact]
    public async Task ReloadDataAsync_RefreshesCache()
    {
        // Arrange - Load data initially
        await _dataManager.GetSpeciesAsync("bulbasaur");
        Assert.True(_dataManager.IsDataLoaded());

        // Act - Reload
        await _dataManager.ReloadDataAsync();

        // Assert - Data still accessible
        var species = await _dataManager.GetSpeciesAsync("bulbasaur");
        Assert.NotNull(species);
    }

    [Fact]
    public async Task ModDataPaths_OverrideBaseData()
    {
        // Arrange - Create mod directory with modified species
        var modPath = Path.Combine(Path.GetTempPath(), $"PokeNET_Mod_{Guid.NewGuid()}");
        Directory.CreateDirectory(modPath);
        Directory.CreateDirectory(Path.Combine(modPath, "species"));

        var modSpecies = new SpeciesData
        {
            Id = "bulbasaur",
            NationalDexNumber = 1,
            Name = "Bulbasaur",
            Types = new List<string> { "Grass", "Poison", "Dragon" }, // Modified!
            BaseStats = new BaseStats
            {
                HP = 100,
                Attack = 49,
                Defense = 49,
                SpecialAttack = 65,
                SpecialDefense = 65,
                Speed = 45,
            }, // Modified!
        };

        var jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        File.WriteAllText(
            Path.Combine(modPath, "species", "bulbasaur.json"),
            JsonSerializer.Serialize(modSpecies, jsonOptions)
        );

        // Act - Set mod path and reload
        _dataManager.SetModDataPaths(new[] { modPath });
        await _dataManager.ReloadDataAsync();

        // Assert - Mod data is used
        var species = await _dataManager.GetSpeciesAsync("bulbasaur");
        Assert.NotNull(species);
        Assert.Contains("Dragon", species.Types);
        Assert.Equal(100, species.BaseStats.HP);

        // Cleanup
        Directory.Delete(modPath, true);
    }

    [Fact]
    public async Task ThreadSafety_ConcurrentAccess()
    {
        // Arrange - Ensure data is loaded first
        await _dataManager.GetSpeciesAsync("bulbasaur");
        Assert.True(_dataManager.IsDataLoaded());

        // Act - Multiple concurrent read requests (EF Core supports concurrent reads from loaded data)
        var tasks = new List<Task<SpeciesData?>>();
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(_dataManager.GetSpeciesAsync("bulbasaur"));
        }

        var results = await Task.WhenAll(tasks);

        // Assert - All requests succeed
        foreach (var result in results)
        {
            Assert.NotNull(result);
            Assert.Equal("Bulbasaur", result.Name);
        }
    }

    // ==================== Type Data Tests ====================

    [Fact]
    public async Task GetTypeByNameAsync_ReturnsTypeByName()
    {
        // Act
        var fireType = await _dataManager.GetTypeByNameAsync("Fire");

        // Assert
        Assert.NotNull(fireType);
        Assert.Equal("Fire", fireType.Name);
        Assert.Equal("#F08030", fireType.Color);
        Assert.Equal("Fire type Pokemon", fireType.Description);
        Assert.Equal(5, fireType.Matchups.Count);
    }

    [Fact]
    public async Task GetTypeByNameAsync_IsCaseInsensitive()
    {
        // Act
        var waterType = await _dataManager.GetTypeByNameAsync("WATER");

        // Assert
        Assert.NotNull(waterType);
        Assert.Equal("Water", waterType.Name);
    }

    [Fact]
    public async Task GetTypeByNameAsync_ReturnsNullForInvalidType()
    {
        // Act
        var invalidType = await _dataManager.GetTypeByNameAsync("InvalidType");

        // Assert
        Assert.Null(invalidType);
    }

    [Fact]
    public async Task GetTypeAsync_ReturnsTypeById()
    {
        // Act
        var fireType = await _dataManager.GetTypeAsync("fire");

        // Assert
        Assert.NotNull(fireType);
        Assert.Equal("Fire", fireType.Name);
    }

    [Fact]
    public async Task GetAllTypesAsync_ReturnsAllTypes()
    {
        // Act
        var allTypes = await _dataManager.GetAllTypesAsync();

        // Assert
        Assert.Equal(3, allTypes.Count);
        Assert.Contains(allTypes, t => t.Name == "Fire");
        Assert.Contains(allTypes, t => t.Name == "Water");
        Assert.Contains(allTypes, t => t.Name == "Grass");
    }

    [Fact]
    public async Task GetTypeEffectivenessAsync_SuperEffective_Returns2x()
    {
        // Act - Fire vs Grass
        var effectiveness = await _dataManager.GetTypeEffectivenessAsync("Fire", "Grass");

        // Assert
        Assert.Equal(2.0, effectiveness);
    }

    [Fact]
    public async Task GetTypeEffectivenessAsync_NotVeryEffective_Returns05x()
    {
        // Act - Fire vs Water
        var effectiveness = await _dataManager.GetTypeEffectivenessAsync("Fire", "Water");

        // Assert
        Assert.Equal(0.5, effectiveness);
    }

    [Fact]
    public async Task GetTypeEffectivenessAsync_Neutral_Returns1x()
    {
        // Act - Fire vs type not in matchups (defaults to 1.0)
        var effectiveness = await _dataManager.GetTypeEffectivenessAsync("Fire", "Electric");

        // Assert
        Assert.Equal(1.0, effectiveness);
    }

    [Fact]
    public async Task GetTypeEffectivenessAsync_IsCaseInsensitive()
    {
        // Act
        var effectiveness = await _dataManager.GetTypeEffectivenessAsync("FIRE", "grass");

        // Assert
        Assert.Equal(2.0, effectiveness);
    }

    [Fact]
    public async Task GetDualTypeEffectivenessAsync_SingleType_CalculatesCorrectly()
    {
        // Act - Fire vs Grass (single type)
        var effectiveness = await _dataManager.GetDualTypeEffectivenessAsync("Fire", "Grass", null);

        // Assert
        Assert.Equal(2.0, effectiveness);
    }

    [Fact]
    public async Task GetDualTypeEffectivenessAsync_DualType_MultipliesEffectiveness()
    {
        // Act - Water vs Fire/Ground (2.0 × 2.0 = 4.0)
        var effectiveness = await _dataManager.GetDualTypeEffectivenessAsync(
            "Water",
            "Fire",
            "Ground"
        );

        // Assert
        Assert.Equal(4.0, effectiveness); // Super effective against both
    }

    [Fact]
    public async Task GetDualTypeEffectivenessAsync_DualType_Resistance()
    {
        // Act - Fire vs Water/Grass (0.5 × 2.0 = 1.0)
        var effectiveness = await _dataManager.GetDualTypeEffectivenessAsync(
            "Fire",
            "Water",
            "Grass"
        );

        // Assert
        Assert.Equal(1.0, effectiveness); // Resisted by Water, super effective on Grass
    }

    [Fact]
    public async Task GetDualTypeEffectivenessAsync_InvalidAttackType_ReturnsNeutral()
    {
        // Act
        var effectiveness = await _dataManager.GetDualTypeEffectivenessAsync(
            "InvalidType",
            "Fire",
            "Water"
        );

        // Assert
        Assert.Equal(1.0, effectiveness);
    }

    [Fact]
    public async Task TypeData_LoadsMatchupsCorrectly()
    {
        // Act
        var fireType = await _dataManager.GetTypeByNameAsync("Fire");

        // Assert
        Assert.NotNull(fireType);
        Assert.Equal(0.5, fireType.Matchups["Fire"]);
        Assert.Equal(0.5, fireType.Matchups["Water"]);
        Assert.Equal(2.0, fireType.Matchups["Grass"]);
        Assert.Equal(2.0, fireType.Matchups["Ice"]);
        Assert.Equal(2.0, fireType.Matchups["Steel"]);
    }

    [Fact]
    public async Task QuerySpeciesAsync_WithPredicate_ReturnsFilteredResults()
    {
        // Act
        var fireTypes = await _dataManager.QuerySpeciesAsync(s => s.Types.Contains("Fire"));

        // Assert
        Assert.Single(fireTypes);
        Assert.Equal("Charmander", fireTypes[0].Name);
    }

    [Fact]
    public async Task GetSpeciesByTypeAsync_ReturnsCorrectSpecies()
    {
        // Act
        var grassTypes = await _dataManager.GetSpeciesByTypeAsync("Grass");

        // Assert
        Assert.Single(grassTypes);
        Assert.Equal("Bulbasaur", grassTypes[0].Name);
    }

    [Fact]
    public async Task QueryMovesAsync_WithPowerRange_ReturnsFilteredResults()
    {
        // Act
        var powerfulMoves = await _dataManager.QueryMovesAsync(m => m.Power >= 80);

        // Assert
        Assert.Single(powerfulMoves);
        Assert.Equal("Thunderbolt", powerfulMoves[0].Name);
    }

    [Fact]
    public async Task GetMovesByPowerRangeAsync_ReturnsCorrectMoves()
    {
        // Act
        var mediumPowerMoves = await _dataManager.GetMovesByPowerRangeAsync(50, 100);

        // Assert
        Assert.Single(mediumPowerMoves);
        Assert.Equal("Thunderbolt", mediumPowerMoves[0].Name);
    }

    [Fact]
    public async Task GetMovesByCategoryAsync_ReturnsCorrectMoves()
    {
        // Act
        var physicalMoves = await _dataManager.GetMovesByCategoryAsync(MoveCategory.Physical);

        // Assert
        Assert.Single(physicalMoves);
        Assert.Equal("Tackle", physicalMoves[0].Name);
    }

    [Fact]
    public async Task QueryItemsAsync_WithPriceRange_ReturnsFilteredResults()
    {
        // Act
        var affordableItems = await _dataManager.QueryItemsAsync(i => i.BuyPrice <= 200);

        // Assert
        Assert.Equal(2, affordableItems.Count);
    }

    [Fact]
    public async Task GetItemsByPriceRangeAsync_ReturnsCorrectItems()
    {
        // Act
        var cheapItems = await _dataManager.GetItemsByPriceRangeAsync(0, 200);

        // Assert
        Assert.Equal(2, cheapItems.Count);
    }

    public void Dispose()
    {
        _dataManager.Dispose();
        _context.Dispose();

        if (Directory.Exists(_testDataPath))
        {
            Directory.Delete(_testDataPath, true);
        }
    }
}
