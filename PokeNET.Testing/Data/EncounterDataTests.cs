using System.Collections.Generic;
using System.Linq;
using PokeNET.Core.Data;
using Xunit;

namespace PokeNET.Core.Tests.Data;

/// <summary>
/// Unit tests for EncounterTable and Encounter models.
/// Tests encounter table loading and rate calculations.
/// </summary>
public class EncounterDataTests
{
    [Fact]
    public void EncounterTable_DefaultValues_AreEmpty()
    {
        // Arrange & Act
        var table = new EncounterTable();

        // Assert
        Assert.NotNull(table.GrassEncounters);
        Assert.Empty(table.GrassEncounters);
        Assert.NotNull(table.WaterEncounters);
        Assert.Empty(table.WaterEncounters);
        Assert.NotNull(table.SpecialEncounters);
        Assert.Empty(table.SpecialEncounters);
    }

    [Fact]
    public void EncounterTable_GrassEncounters_CanContainMultiple()
    {
        // Arrange & Act
        var table = new EncounterTable
        {
            LocationId = "route_1",
            LocationName = "Route 1",
            GrassEncounters = new List<Encounter>
            {
                new()
                {
                    SpeciesId = "pidgey",
                    MinLevel = 2,
                    MaxLevel = 5,
                    Rate = 50,
                }, // Pidgey
                new()
                {
                    SpeciesId = "rattata",
                    MinLevel = 2,
                    MaxLevel = 4,
                    Rate = 50,
                }, // Rattata
            },
        };

        // Assert
        Assert.Equal(2, table.GrassEncounters.Count);
        Assert.Equal(100, table.GrassEncounters.Sum(e => e.Rate)); // Total rate should be 100
    }

    [Fact]
    public void Encounter_LevelRange_IsValid()
    {
        // Arrange & Act
        var encounter = new Encounter
        {
            SpeciesId = "bulbasaur",
            MinLevel = 5,
            MaxLevel = 10,
            Rate = 20,
        };

        // Assert
        Assert.Equal(5, encounter.MinLevel);
        Assert.Equal(10, encounter.MaxLevel);
        Assert.True(encounter.MaxLevel >= encounter.MinLevel);
    }

    [Fact]
    public void Encounter_Rate_IsPercentage()
    {
        // Arrange & Act
        var encounter = new Encounter
        {
            SpeciesId = "pikachu",
            MinLevel = 3,
            MaxLevel = 5,
            Rate = 15,
        };

        // Assert
        Assert.Equal(15, encounter.Rate);
        Assert.InRange(encounter.Rate, 0, 100);
    }

    [Fact]
    public void Encounter_TimeOfDay_DefaultsToAny()
    {
        // Arrange & Act
        var encounter = new Encounter
        {
            SpeciesId = "pikachu",
            MinLevel = 5,
            MaxLevel = 5,
        };

        // Assert
        Assert.Equal("Any", encounter.TimeOfDay);
    }

    [Fact]
    public void Encounter_TimeOfDay_CanBeRestricted()
    {
        // Arrange & Act
        var morningEncounter = new Encounter
        {
            SpeciesId = "pidgey", // Pidgey
            MinLevel = 2,
            MaxLevel = 5,
            Rate = 30,
            TimeOfDay = "Morning",
        };

        var nightEncounter = new Encounter
        {
            SpeciesId = "zubat", // Zubat
            MinLevel = 5,
            MaxLevel = 8,
            Rate = 40,
            TimeOfDay = "Night",
        };

        // Assert
        Assert.Equal("Morning", morningEncounter.TimeOfDay);
        Assert.Equal("Night", nightEncounter.TimeOfDay);
    }

    [Fact]
    public void Encounter_Weather_DefaultsToAny()
    {
        // Arrange & Act
        var encounter = new Encounter();

        // Assert
        Assert.Equal("Any", encounter.Weather);
    }

    [Fact]
    public void Encounter_Weather_CanBeRestricted()
    {
        // Arrange & Act
        var rainyEncounter = new Encounter
        {
            SpeciesId = "psyduck", // Psyduck
            MinLevel = 20,
            MaxLevel = 25,
            Rate = 50,
            Weather = "Rainy",
        };

        // Assert
        Assert.Equal("Rainy", rainyEncounter.Weather);
    }

    [Fact]
    public void EncounterTable_WaterEncounters_Separate()
    {
        // Arrange & Act
        var table = new EncounterTable
        {
            LocationId = "route_24",
            WaterEncounters = new List<Encounter>
            {
                new()
                {
                    SpeciesId = "poliwag",
                    MinLevel = 10,
                    MaxLevel = 25,
                    Rate = 100,
                }, // Poliwag
            },
        };

        // Assert
        Assert.Single(table.WaterEncounters);
        Assert.Empty(table.GrassEncounters);
    }

    [Fact]
    public void EncounterTable_FishingEncounters_Tiered()
    {
        // Arrange & Act
        var table = new EncounterTable
        {
            LocationId = "vermilion_port",
            OldRodEncounters = new List<Encounter>
            {
                new()
                {
                    SpeciesId = "magikarp",
                    MinLevel = 5,
                    MaxLevel = 5,
                    Rate = 100,
                }, // Magikarp
            },
            GoodRodEncounters = new List<Encounter>
            {
                new()
                {
                    SpeciesId = "poliwag",
                    MinLevel = 10,
                    MaxLevel = 20,
                    Rate = 60,
                }, // Poliwag
                new()
                {
                    SpeciesId = "tentacool",
                    MinLevel = 10,
                    MaxLevel = 20,
                    Rate = 40,
                }, // Tentacool
            },
            SuperRodEncounters = new List<Encounter>
            {
                new()
                {
                    SpeciesId = "gyarados",
                    MinLevel = 20,
                    MaxLevel = 30,
                    Rate = 100,
                }, // Gyarados
            },
        };

        // Assert
        Assert.Single(table.OldRodEncounters);
        Assert.Equal(2, table.GoodRodEncounters.Count);
        Assert.Single(table.SuperRodEncounters);
    }

    [Fact]
    public void SpecialEncounter_IsOneTime()
    {
        // Arrange & Act
        var legendary = new SpecialEncounter
        {
            EncounterId = "mewtwo_cerulean_cave",
            SpeciesId = "mewtwo", // Mewtwo
            Level = 70,
            OneTime = true,
        };

        // Assert
        Assert.True(legendary.OneTime);
        Assert.Equal(70, legendary.Level);
    }

    [Fact]
    public void SpecialEncounter_CanHaveConditions()
    {
        // Arrange & Act
        var special = new SpecialEncounter
        {
            EncounterId = "special_eevee",
            SpeciesId = "eevee",
            Level = 25,
            Conditions = new Dictionary<string, object>
            {
                { "RequiredItem", "Special Key" },
                { "MinBadges", 3 },
                { "TimeOfDay", "Morning" },
            },
        };

        // Assert
        Assert.Equal(3, special.Conditions.Count);
        Assert.Equal("Special Key", special.Conditions["RequiredItem"]);
        Assert.Equal(3, special.Conditions["MinBadges"]);
    }

    [Fact]
    public void SpecialEncounter_Script_IsOptional()
    {
        // Arrange & Act
        var withScript = new SpecialEncounter
        {
            EncounterId = "scripted_event",
            SpeciesId = "mew",
            Level = 5,
            Script = "scripts/encounters/mew_event.csx",
        };

        var withoutScript = new SpecialEncounter
        {
            EncounterId = "simple_legendary",
            SpeciesId = "articuno",
            Level = 50,
        };

        // Assert
        Assert.NotNull(withScript.Script);
        Assert.Null(withoutScript.Script);
    }

    [Fact]
    public void EncounterTable_CaveEncounters_Separate()
    {
        // Arrange & Act
        var table = new EncounterTable
        {
            LocationId = "mt_moon",
            CaveEncounters = new List<Encounter>
            {
                new()
                {
                    SpeciesId = "zubat",
                    MinLevel = 7,
                    MaxLevel = 11,
                    Rate = 49,
                }, // Zubat
                new()
                {
                    SpeciesId = "geodude",
                    MinLevel = 7,
                    MaxLevel = 9,
                    Rate = 25,
                }, // Geodude
                new()
                {
                    SpeciesId = "paras",
                    MinLevel = 8,
                    MaxLevel = 10,
                    Rate = 26,
                }, // Paras
            },
        };

        // Assert
        Assert.Equal(3, table.CaveEncounters.Count);
        Assert.Equal(100, table.CaveEncounters.Sum(e => e.Rate));
    }

    [Fact]
    public void EncounterTable_MultipleEncounterTypes_CanCoexist()
    {
        // Arrange & Act
        var table = new EncounterTable
        {
            LocationId = "safari_zone",
            GrassEncounters = new List<Encounter>
            {
                new()
                {
                    SpeciesId = "nidoran-f",
                    MinLevel = 22,
                    MaxLevel = 24,
                    Rate = 30,
                },
            },
            WaterEncounters = new List<Encounter>
            {
                new()
                {
                    SpeciesId = "goldeen",
                    MinLevel = 10,
                    MaxLevel = 15,
                    Rate = 100,
                },
            },
            SpecialEncounters = new List<SpecialEncounter>
            {
                new()
                {
                    EncounterId = "rare_chansey",
                    SpeciesId = "chansey",
                    Level = 25,
                    OneTime = false,
                },
            },
        };

        // Assert
        Assert.NotEmpty(table.GrassEncounters);
        Assert.NotEmpty(table.WaterEncounters);
        Assert.NotEmpty(table.SpecialEncounters);
    }
}
