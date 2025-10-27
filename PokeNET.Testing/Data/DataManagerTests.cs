using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using PokeNET.Core.Data;
using PokeNET.Domain.Data;
using Xunit;

namespace PokeNET.Core.Tests.Data;

/// <summary>
/// Unit tests for DataManager.
/// </summary>
public class DataManagerTests : IDisposable
{
    private readonly string _testDataPath;
    private readonly DataManager _dataManager;

    public DataManagerTests()
    {
        // Create temporary test data directory
        _testDataPath = Path.Combine(Path.GetTempPath(), $"PokeNET_Test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDataPath);

        _dataManager = new DataManager(NullLogger<DataManager>.Instance, _testDataPath);

        // Create test data files
        CreateTestData();
    }

    private void CreateTestData()
    {
        // Create test species data
        var species = new List<SpeciesData>
        {
            new()
            {
                Id = 1,
                Name = "Bulbasaur",
                Types = new List<string> { "Grass", "Poison" },
                BaseStats = new BaseStats
                {
                    HP = 45,
                    Attack = 49,
                    Defense = 49,
                    SpecialAttack = 65,
                    SpecialDefense = 65,
                    Speed = 45
                }
            },
            new()
            {
                Id = 4,
                Name = "Charmander",
                Types = new List<string> { "Fire" },
                BaseStats = new BaseStats
                {
                    HP = 39,
                    Attack = 52,
                    Defense = 43,
                    SpecialAttack = 60,
                    SpecialDefense = 50,
                    Speed = 65
                }
            }
        };

        // Create test move data
        var moves = new List<MoveData>
        {
            new()
            {
                Name = "Tackle",
                Type = "Normal",
                Category = MoveCategory.Physical,
                Power = 40,
                Accuracy = 100,
                PP = 35
            },
            new()
            {
                Name = "Thunderbolt",
                Type = "Electric",
                Category = MoveCategory.Special,
                Power = 90,
                Accuracy = 100,
                PP = 15
            }
        };

        // Create test item data
        var items = new List<ItemData>
        {
            new()
            {
                Id = 1,
                Name = "Potion",
                Category = ItemCategory.Medicine,
                BuyPrice = 200,
                SellPrice = 100,
                Consumable = true
            },
            new()
            {
                Id = 4,
                Name = "Pokeball",
                Category = ItemCategory.Pokeball,
                BuyPrice = 200,
                SellPrice = 100,
                Consumable = true
            }
        };

        // Create test encounter data
        var encounters = new List<EncounterTable>
        {
            new()
            {
                LocationId = "route_1",
                LocationName = "Route 1",
                GrassEncounters = new List<Encounter>
                {
                    new() { SpeciesId = 1, MinLevel = 2, MaxLevel = 4, Rate = 50 }
                }
            }
        };

        // Write JSON files
        var options = new JsonSerializerOptions { WriteIndented = true };

        File.WriteAllText(
            Path.Combine(_testDataPath, "species.json"),
            JsonSerializer.Serialize(species, options)
        );

        File.WriteAllText(
            Path.Combine(_testDataPath, "moves.json"),
            JsonSerializer.Serialize(moves, options)
        );

        File.WriteAllText(
            Path.Combine(_testDataPath, "items.json"),
            JsonSerializer.Serialize(items, options)
        );

        File.WriteAllText(
            Path.Combine(_testDataPath, "encounters.json"),
            JsonSerializer.Serialize(encounters, options)
        );

        // Create test type data
        var types = new List<TypeData>
        {
            new()
            {
                Name = "Fire",
                Color = "#F08030",
                Description = "Fire type Pokemon",
                Matchups = new Dictionary<string, double>
                {
                    { "Fire", 0.5 },
                    { "Water", 0.5 },
                    { "Grass", 2.0 },
                    { "Ice", 2.0 },
                    { "Steel", 2.0 }
                }
            },
            new()
            {
                Name = "Water",
                Color = "#6890F0",
                Description = "Water type Pokemon",
                Matchups = new Dictionary<string, double>
                {
                    { "Fire", 2.0 },
                    { "Water", 0.5 },
                    { "Grass", 0.5 },
                    { "Ground", 2.0 },
                    { "Rock", 2.0 }
                }
            },
            new()
            {
                Name = "Grass",
                Color = "#78C850",
                Description = "Grass type Pokemon",
                Matchups = new Dictionary<string, double>
                {
                    { "Fire", 0.5 },
                    { "Water", 2.0 },
                    { "Grass", 0.5 },
                    { "Ground", 2.0 },
                    { "Rock", 2.0 }
                }
            }
        };

        File.WriteAllText(
            Path.Combine(_testDataPath, "types.json"),
            JsonSerializer.Serialize(types, options)
        );
    }

    [Fact]
    public async Task GetSpeciesAsync_ReturnsSpeciesById()
    {
        // Act
        var species = await _dataManager.GetSpeciesAsync(1);

        // Assert
        Assert.NotNull(species);
        Assert.Equal(1, species.Id);
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
        Assert.Equal(4, species.Id);
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
    public async Task GetMoveAsync_ReturnsMoveByName()
    {
        // Act
        var move = await _dataManager.GetMoveAsync("Tackle");

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
        var item = await _dataManager.GetItemAsync(1);

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
        Assert.Equal(4, item.Id);
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
        await _dataManager.GetSpeciesAsync(1);
        Assert.True(_dataManager.IsDataLoaded());

        // Act - Reload
        await _dataManager.ReloadDataAsync();

        // Assert - Data still accessible
        var species = await _dataManager.GetSpeciesAsync(1);
        Assert.NotNull(species);
    }

    [Fact]
    public async Task ModDataPaths_OverrideBaseData()
    {
        // Arrange - Create mod directory with modified species
        var modPath = Path.Combine(Path.GetTempPath(), $"PokeNET_Mod_{Guid.NewGuid()}");
        Directory.CreateDirectory(modPath);

        var modSpecies = new List<SpeciesData>
        {
            new()
            {
                Id = 1,
                Name = "Bulbasaur",
                Types = new List<string> { "Grass", "Poison", "Dragon" }, // Modified!
                BaseStats = new BaseStats { HP = 100 } // Modified!
            }
        };

        File.WriteAllText(
            Path.Combine(modPath, "species.json"),
            JsonSerializer.Serialize(modSpecies)
        );

        // Act - Set mod path and reload
        _dataManager.SetModDataPaths(new[] { modPath });
        await _dataManager.ReloadDataAsync();

        // Assert - Mod data is used
        var species = await _dataManager.GetSpeciesAsync(1);
        Assert.NotNull(species);
        Assert.Contains("Dragon", species.Types);
        Assert.Equal(100, species.BaseStats.HP);

        // Cleanup
        Directory.Delete(modPath, true);
    }

    [Fact]
    public async Task ThreadSafety_ConcurrentAccess()
    {
        // Act - Multiple concurrent requests
        var tasks = new List<Task<SpeciesData?>>();
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(_dataManager.GetSpeciesAsync(1));
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
    public async Task GetTypeAsync_ReturnsTypeByName()
    {
        // Act
        var fireType = await _dataManager.GetTypeAsync("Fire");

        // Assert
        Assert.NotNull(fireType);
        Assert.Equal("Fire", fireType.Name);
        Assert.Equal("#F08030", fireType.Color);
        Assert.Equal("Fire type Pokemon", fireType.Description);
        Assert.Equal(5, fireType.Matchups.Count);
    }

    [Fact]
    public async Task GetTypeAsync_IsCaseInsensitive()
    {
        // Act
        var waterType = await _dataManager.GetTypeAsync("WATER");

        // Assert
        Assert.NotNull(waterType);
        Assert.Equal("Water", waterType.Name);
    }

    [Fact]
    public async Task GetTypeAsync_ReturnsNullForInvalidType()
    {
        // Act
        var invalidType = await _dataManager.GetTypeAsync("InvalidType");

        // Assert
        Assert.Null(invalidType);
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
        var effectiveness = await _dataManager.GetDualTypeEffectivenessAsync("Water", "Fire", "Ground");

        // Assert
        Assert.Equal(4.0, effectiveness); // Super effective against both
    }

    [Fact]
    public async Task GetDualTypeEffectivenessAsync_DualType_Resistance()
    {
        // Act - Fire vs Water/Grass (0.5 × 2.0 = 1.0)
        var effectiveness = await _dataManager.GetDualTypeEffectivenessAsync("Fire", "Water", "Grass");

        // Assert
        Assert.Equal(1.0, effectiveness); // Resisted by Water, super effective on Grass
    }

    [Fact]
    public async Task GetDualTypeEffectivenessAsync_InvalidAttackType_ReturnsNeutral()
    {
        // Act
        var effectiveness = await _dataManager.GetDualTypeEffectivenessAsync("InvalidType", "Fire", "Water");

        // Assert
        Assert.Equal(1.0, effectiveness);
    }

    [Fact]
    public async Task TypeData_LoadsMatchupsCorrectly()
    {
        // Act
        var fireType = await _dataManager.GetTypeAsync("Fire");

        // Assert
        Assert.NotNull(fireType);
        Assert.Equal(0.5, fireType.Matchups["Fire"]);
        Assert.Equal(0.5, fireType.Matchups["Water"]);
        Assert.Equal(2.0, fireType.Matchups["Grass"]);
        Assert.Equal(2.0, fireType.Matchups["Ice"]);
        Assert.Equal(2.0, fireType.Matchups["Steel"]);
    }

    public void Dispose()
    {
        _dataManager.Dispose();

        if (Directory.Exists(_testDataPath))
        {
            Directory.Delete(_testDataPath, true);
        }
    }
}
