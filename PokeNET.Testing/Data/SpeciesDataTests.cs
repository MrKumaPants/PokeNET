using System.Collections.Generic;
using PokeNET.Core.Data;
using Xunit;

namespace PokeNET.Core.Tests.Data;

/// <summary>
/// Unit tests for SpeciesData model.
/// Tests evolution chains, learnsets, and data validation.
/// </summary>
public class SpeciesDataTests
{
    [Fact]
    public void SpeciesData_DefaultValues_AreValid()
    {
        // Arrange & Act
        var species = new SpeciesData();

        // Assert
        Assert.NotNull(species.Types);
        Assert.NotNull(species.Abilities);
        Assert.NotNull(species.LevelMoves);
        Assert.NotNull(species.TmMoves);
        Assert.NotNull(species.EggMoves);
        Assert.NotNull(species.Evolutions);
        Assert.Equal("Medium Fast", species.GrowthRate);
        Assert.Equal(127, species.GenderRatio); // 50/50
    }

    [Fact]
    public void BaseStats_Total_CalculatesCorrectly()
    {
        // Arrange
        var baseStats = new BaseStats
        {
            HP = 45,
            Attack = 49,
            Defense = 49,
            SpecialAttack = 65,
            SpecialDefense = 65,
            Speed = 45,
        };

        // Act
        var total = baseStats.Total;

        // Assert
        Assert.Equal(318, total); // Bulbasaur's BST
    }

    [Fact]
    public void Evolution_RequiredLevel_IsOptional()
    {
        // Arrange
        var evolution = new Evolution
        {
            TargetSpeciesId = "ivysaur",
            Method = "Level",
            RequiredLevel = 16,
        };

        // Assert
        Assert.Equal(16, evolution.RequiredLevel);
        Assert.Null(evolution.RequiredItem);
    }

    [Fact]
    public void Evolution_StoneEvolution_RequiresItem()
    {
        // Arrange
        var evolution = new Evolution
        {
            TargetSpeciesId = "venusaur",
            Method = "Stone",
            RequiredItem = "Fire Stone",
        };

        // Assert
        Assert.Equal("Fire Stone", evolution.RequiredItem);
        Assert.Null(evolution.RequiredLevel);
    }

    [Fact]
    public void LevelMove_StoresCorrectData()
    {
        // Arrange
        var levelMove = new LevelMove { Level = 7, MoveName = "Vine Whip" };

        // Assert
        Assert.Equal(7, levelMove.Level);
        Assert.Equal("Vine Whip", levelMove.MoveName);
    }

    [Fact]
    public void SpeciesData_EvolutionChain_CanHaveMultipleEvolutions()
    {
        // Arrange - Eevee with multiple evolution paths
        var species = new SpeciesData
        {
            Id = "eevee",
            Name = "Eevee",
            Evolutions = new List<Evolution>
            {
                new()
                {
                    TargetSpeciesId = "vaporeon",
                    Method = "Stone",
                    RequiredItem = "Water Stone",
                }, // Vaporeon
                new()
                {
                    TargetSpeciesId = "jolteon",
                    Method = "Stone",
                    RequiredItem = "Thunder Stone",
                }, // Jolteon
                new()
                {
                    TargetSpeciesId = "flareon",
                    Method = "Stone",
                    RequiredItem = "Fire Stone",
                }, // Flareon
                new()
                {
                    TargetSpeciesId = "espeon",
                    Method = "Friendship",
                    Conditions = new() { { "TimeOfDay", "Day" } },
                }, // Espeon
                new()
                {
                    TargetSpeciesId = "umbreon",
                    Method = "Friendship",
                    Conditions = new() { { "TimeOfDay", "Night" } },
                }, // Umbreon
            },
        };

        // Assert
        Assert.Equal(5, species.Evolutions.Count);
        Assert.All(
            species.Evolutions,
            evo => Assert.False(string.IsNullOrEmpty(evo.TargetSpeciesId))
        );
    }

    [Fact]
    public void SpeciesData_Learnset_ContainsAllMoveTypes()
    {
        // Arrange
        var species = new SpeciesData
        {
            Id = "charizard",
            Name = "Charizard",
            LevelMoves = new List<LevelMove>
            {
                new() { Level = 1, MoveName = "Scratch" },
                new() { Level = 7, MoveName = "Ember" },
                new() { Level = 36, MoveName = "Flamethrower" },
            },
            TmMoves = new List<string> { "Solar Beam", "Earthquake", "Dragon Claw" },
            EggMoves = new List<string> { "Dragon Dance", "Belly Drum" },
        };

        // Assert
        Assert.Equal(3, species.LevelMoves.Count);
        Assert.Equal(3, species.TmMoves.Count);
        Assert.Equal(2, species.EggMoves.Count);
        Assert.Contains(species.LevelMoves, lm => lm.MoveName == "Flamethrower");
        Assert.Contains("Earthquake", species.TmMoves);
        Assert.Contains("Dragon Dance", species.EggMoves);
    }

    [Fact]
    public void SpeciesData_DualType_HasTwoTypes()
    {
        // Arrange
        var species = new SpeciesData
        {
            Id = "bulbasaur",
            Name = "Bulbasaur",
            Types = new List<string> { "Grass", "Poison" },
        };

        // Assert
        Assert.Equal(2, species.Types.Count);
        Assert.Contains("Grass", species.Types);
        Assert.Contains("Poison", species.Types);
    }

    [Fact]
    public void SpeciesData_SingleType_HasOneType()
    {
        // Arrange
        var species = new SpeciesData
        {
            Id = "pikachu",
            Name = "Pikachu",
            Types = new List<string> { "Electric" },
        };

        // Assert
        Assert.Single(species.Types);
        Assert.Equal("Electric", species.Types[0]);
    }

    [Fact]
    public void SpeciesData_HiddenAbility_IsOptional()
    {
        // Arrange - Pokemon without hidden ability
        var withoutHidden = new SpeciesData
        {
            Abilities = new List<string> { "Overgrow" },
            HiddenAbility = null,
        };

        // Pokemon with hidden ability
        var withHidden = new SpeciesData
        {
            Abilities = new List<string> { "Overgrow" },
            HiddenAbility = "Chlorophyll",
        };

        // Assert
        Assert.Null(withoutHidden.HiddenAbility);
        Assert.Equal("Chlorophyll", withHidden.HiddenAbility);
    }
}
