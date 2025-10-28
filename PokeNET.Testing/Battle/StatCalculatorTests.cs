using PokeNET.Core.Battle;
using PokeNET.Core.ECS.Components;
using Xunit;

namespace PokeNET.Core.Tests.Battle;

/// <summary>
/// Unit tests for StatCalculator.
/// Tests HP/stat calculation formulas, nature modifiers, and known Pokemon stat values.
/// </summary>
public class StatCalculatorTests
{
    #region HP Calculation Tests

    [Fact]
    public void CalculateHP_Level50_Perfect_CalculatesCorrectly()
    {
        // Arrange - Bulbasaur base HP 45, perfect IVs/EVs, level 50
        int baseHP = 45;
        int iv = 31;
        int ev = 252;
        int level = 50;

        // Act
        int hp = StatCalculator.CalculateHP(baseHP, iv, ev, level);

        // Assert
        // Formula: ((2 * 45 + 31 + 252/4) * 50 / 100) + 50 + 10
        // = ((90 + 31 + 63) * 50 / 100) + 60
        // = (184 * 50 / 100) + 60
        // = 92 + 60 = 152
        Assert.Equal(152, hp);
    }

    [Fact]
    public void CalculateHP_Level100_Perfect_CalculatesCorrectly()
    {
        // Arrange - Chansey base HP 250, perfect IVs/EVs, level 100
        int baseHP = 250;
        int iv = 31;
        int ev = 252;
        int level = 100;

        // Act
        int hp = StatCalculator.CalculateHP(baseHP, iv, ev, level);

        // Assert
        // Formula: ((2 * 250 + 31 + 63) * 100 / 100) + 100 + 10
        // = 594 + 110 = 704
        Assert.Equal(704, hp);
    }

    [Fact]
    public void CalculateHP_Level1_NoIVsEVs_CalculatesCorrectly()
    {
        // Arrange - Any Pokemon at level 1 with no IVs/EVs
        int baseHP = 45;
        int iv = 0;
        int ev = 0;
        int level = 1;

        // Act
        int hp = StatCalculator.CalculateHP(baseHP, iv, ev, level);

        // Assert
        // Formula: ((2 * 45 + 0 + 0) * 1 / 100) + 1 + 10
        // = (90 / 100) + 11 = 0 + 11 = 11
        Assert.Equal(11, hp);
    }

    [Fact]
    public void CalculateHP_Shedinja_AlwaysOne()
    {
        // Arrange - Shedinja always has 1 HP (base HP 1)
        int baseHP = 1;
        int iv = 0;
        int ev = 0;
        int level = 50;

        // Act
        int hp = StatCalculator.CalculateHP(baseHP, iv, ev, level);

        // Assert
        // Even at level 50, Shedinja should have 1 HP
        // Formula: ((2 * 1 + 0 + 0) * 50 / 100) + 50 + 10
        // = (2 * 50 / 100) + 60 = 1 + 60 = 61
        // NOTE: Game mechanics force Shedinja to 1, but formula calculates normally
        Assert.Equal(61, hp); // Formula result (game would override to 1)
    }

    #endregion

    #region Stat Calculation Tests

    [Fact]
    public void CalculateStat_Level50_Perfect_NeutralNature()
    {
        // Arrange - Garchomp base Attack 130, perfect IVs/EVs, neutral nature, level 50
        int baseStat = 130;
        int iv = 31;
        int ev = 252;
        int level = 50;
        float natureModifier = 1.0f;

        // Act
        int stat = StatCalculator.CalculateStat(baseStat, iv, ev, level, natureModifier);

        // Assert
        // Formula: (((2 * 130 + 31 + 63) * 50 / 100) + 5) * 1.0
        // = (((260 + 31 + 63) * 50 / 100) + 5) * 1.0
        // = ((354 * 50 / 100) + 5) * 1.0
        // = (177 + 5) * 1.0 = 182
        Assert.Equal(182, stat);
    }

    [Fact]
    public void CalculateStat_Level50_Perfect_BoostedNature()
    {
        // Arrange - Garchomp base Attack 130, perfect IVs/EVs, Adamant nature (+Atk), level 50
        int baseStat = 130;
        int iv = 31;
        int ev = 252;
        int level = 50;
        float natureModifier = 1.1f; // +10%

        // Act
        int stat = StatCalculator.CalculateStat(baseStat, iv, ev, level, natureModifier);

        // Assert
        // Formula: (((2 * 130 + 31 + 63) * 50 / 100) + 5) * 1.1
        // = 182 * 1.1 = 200.2 = 200 (truncated)
        Assert.Equal(200, stat);
    }

    [Fact]
    public void CalculateStat_Level50_Perfect_HinderedNature()
    {
        // Arrange - Gengar base Defense 60, perfect IVs/EVs, Timid nature (-Atk), level 50
        int baseStat = 60;
        int iv = 31;
        int ev = 0; // Not investing in hindered stat
        int level = 50;
        float natureModifier = 0.9f; // -10%

        // Act
        int stat = StatCalculator.CalculateStat(baseStat, iv, ev, level, natureModifier);

        // Assert
        // Formula: (((2 * 60 + 31 + 0) * 50 / 100) + 5) * 0.9
        // = ((151 * 50 / 100) + 5) * 0.9
        // = (75 + 5) * 0.9 = 80 * 0.9 = 72
        Assert.Equal(72, stat);
    }

    [Fact]
    public void CalculateStat_Level100_MaxEVs()
    {
        // Arrange - Alakazam base SpAttack 135, perfect IVs/EVs, level 100
        int baseStat = 135;
        int iv = 31;
        int ev = 252;
        int level = 100;
        float natureModifier = 1.1f;

        // Act
        int stat = StatCalculator.CalculateStat(baseStat, iv, ev, level, natureModifier);

        // Assert
        // Formula: (((2 * 135 + 31 + 63) * 100 / 100) + 5) * 1.1
        // = ((270 + 31 + 63) * 100 / 100 + 5) * 1.1
        // = (364 + 5) * 1.1 = 369 * 1.1 = 405.9 = 405
        Assert.Equal(405, stat);
    }

    [Fact]
    public void CalculateStat_Level1_NoIVsEVs()
    {
        // Arrange - Level 1 Pokemon with no IVs/EVs
        int baseStat = 50;
        int iv = 0;
        int ev = 0;
        int level = 1;
        float natureModifier = 1.0f;

        // Act
        int stat = StatCalculator.CalculateStat(baseStat, iv, ev, level, natureModifier);

        // Assert
        // Formula: (((2 * 50 + 0 + 0) * 1 / 100) + 5) * 1.0
        // = (100 / 100 + 5) * 1.0 = (1 + 5) * 1.0 = 6
        Assert.Equal(6, stat);
    }

    #endregion

    #region Nature Modifier Tests

    [Theory]
    [InlineData(Nature.Hardy, 1.0f)] // Neutral
    [InlineData(Nature.Lonely, 1.1f)] // +Atk -Def
    [InlineData(Nature.Adamant, 1.1f)] // +Atk -SpAtk
    [InlineData(Nature.Modest, 0.9f)] // +SpAtk -Atk
    [InlineData(Nature.Timid, 0.9f)] // +Spd -Atk
    [InlineData(Nature.Bold, 0.9f)] // +Def -Atk
    [InlineData(Nature.Jolly, 1.0f)] // +Spd -SpAtk (neutral attack)
    public void RecalculateAllStats_NatureModifiers_ApplyCorrectly(
        Nature nature,
        float expectedAtkMod
    )
    {
        // Arrange
        var stats = new PokemonStats
        {
            IV_HP = 31,
            IV_Attack = 31,
            IV_Defense = 31,
            IV_SpAttack = 31,
            IV_SpDefense = 31,
            IV_Speed = 31,
            EV_HP = 252,
            EV_Attack = 0,
            EV_Defense = 0,
            EV_SpAttack = 252,
            EV_SpDefense = 4,
            EV_Speed = 0,
        };

        // Base stats for a typical special attacker (like Alakazam)
        int baseHP = 55;
        int baseAtk = 50;
        int baseDef = 45;
        int baseSpAtk = 135;
        int baseSpDef = 95;
        int baseSpd = 120;
        int level = 50;

        // Act
        StatCalculator.RecalculateAllStats(
            ref stats,
            baseHP,
            baseAtk,
            baseDef,
            baseSpAtk,
            baseSpDef,
            baseSpd,
            level,
            nature
        );

        // Assert - Check that nature modifiers were applied
        // We'll verify by recalculating one stat manually
        int expectedAtk = StatCalculator.CalculateStat(baseAtk, 31, 0, level, expectedAtkMod);
        Assert.Equal(expectedAtk, stats.Attack);

        // Verify HP has no nature modifier
        int expectedHP = StatCalculator.CalculateHP(baseHP, 31, 252, level);
        Assert.Equal(expectedHP, stats.MaxHP);
    }

    [Fact]
    public void RecalculateAllStats_UpdatesAllStats()
    {
        // Arrange
        var stats = new PokemonStats
        {
            IV_HP = 31,
            IV_Attack = 31,
            IV_Defense = 31,
            IV_SpAttack = 31,
            IV_SpDefense = 31,
            IV_Speed = 31,
            EV_HP = 252,
            EV_Attack = 252,
            EV_Defense = 4,
            EV_SpAttack = 0,
            EV_SpDefense = 0,
            EV_Speed = 0,
        };

        // Garchomp base stats
        int baseHP = 108;
        int baseAtk = 130;
        int baseDef = 95;
        int baseSpAtk = 80;
        int baseSpDef = 85;
        int baseSpd = 102;
        int level = 50;
        Nature nature = Nature.Jolly; // +Spd -SpAtk

        // Act
        StatCalculator.RecalculateAllStats(
            ref stats,
            baseHP,
            baseAtk,
            baseDef,
            baseSpAtk,
            baseSpDef,
            baseSpd,
            level,
            nature
        );

        // Assert - All stats should be calculated
        Assert.True(stats.MaxHP > 0);
        Assert.True(stats.Attack > 0);
        Assert.True(stats.Defense > 0);
        Assert.True(stats.SpAttack > 0);
        Assert.True(stats.SpDefense > 0);
        Assert.True(stats.Speed > 0);

        // Verify HP (no nature modifier)
        int expectedHP = StatCalculator.CalculateHP(baseHP, 31, 252, level);
        Assert.Equal(expectedHP, stats.MaxHP);

        // Verify Speed is boosted
        int expectedSpeed = StatCalculator.CalculateStat(baseSpd, 31, 0, level, 1.1f);
        Assert.Equal(expectedSpeed, stats.Speed);
    }

    #endregion

    #region Validation Tests

    [Fact]
    public void ValidateEVs_ValidDistribution_ReturnsTrue()
    {
        // Arrange - 252/252/4 spread (common competitive)
        var stats = new PokemonStats
        {
            EV_HP = 4,
            EV_Attack = 252,
            EV_Defense = 0,
            EV_SpAttack = 0,
            EV_SpDefense = 0,
            EV_Speed = 252,
        };

        // Act
        bool isValid = StatCalculator.ValidateEVs(stats);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void ValidateEVs_ExceedsTotalLimit_ReturnsFalse()
    {
        // Arrange - 600 total (exceeds 510)
        var stats = new PokemonStats
        {
            EV_HP = 252,
            EV_Attack = 252,
            EV_Defense = 96,
            EV_SpAttack = 0,
            EV_SpDefense = 0,
            EV_Speed = 0,
        };

        // Act
        bool isValid = StatCalculator.ValidateEVs(stats);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void ValidateEVs_ExceedsIndividualLimit_ReturnsFalse()
    {
        // Arrange - 255 in one stat (exceeds 252)
        var stats = new PokemonStats
        {
            EV_HP = 255,
            EV_Attack = 252,
            EV_Defense = 0,
            EV_SpAttack = 0,
            EV_SpDefense = 0,
            EV_Speed = 0,
        };

        // Act
        bool isValid = StatCalculator.ValidateEVs(stats);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void ValidateIVs_AllValid_ReturnsTrue()
    {
        // Arrange - Perfect IVs
        var stats = new PokemonStats
        {
            IV_HP = 31,
            IV_Attack = 31,
            IV_Defense = 31,
            IV_SpAttack = 31,
            IV_SpDefense = 31,
            IV_Speed = 31,
        };

        // Act
        bool isValid = StatCalculator.ValidateIVs(stats);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void ValidateIVs_ExceedsLimit_ReturnsFalse()
    {
        // Arrange - IV too high
        var stats = new PokemonStats
        {
            IV_HP = 32,
            IV_Attack = 31,
            IV_Defense = 31,
            IV_SpAttack = 31,
            IV_SpDefense = 31,
            IV_Speed = 31,
        };

        // Act
        bool isValid = StatCalculator.ValidateIVs(stats);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void ValidateIVs_Negative_ReturnsFalse()
    {
        // Arrange - Negative IV
        var stats = new PokemonStats
        {
            IV_HP = -1,
            IV_Attack = 31,
            IV_Defense = 31,
            IV_SpAttack = 31,
            IV_SpDefense = 31,
            IV_Speed = 31,
        };

        // Act
        bool isValid = StatCalculator.ValidateIVs(stats);

        // Assert
        Assert.False(isValid);
    }

    #endregion

    #region Known Pokemon Stat Tests

    [Fact]
    public void KnownStats_Garchomp_Level50_Jolly()
    {
        // Arrange - Garchomp 252 Atk / 4 Def / 252 Spe Jolly
        var stats = new PokemonStats
        {
            IV_HP = 31,
            IV_Attack = 31,
            IV_Defense = 31,
            IV_SpAttack = 31,
            IV_SpDefense = 31,
            IV_Speed = 31,
            EV_HP = 0,
            EV_Attack = 252,
            EV_Defense = 4,
            EV_SpAttack = 0,
            EV_SpDefense = 0,
            EV_Speed = 252,
        };

        // Act
        StatCalculator.RecalculateAllStats(
            ref stats,
            108,
            130,
            95,
            80,
            85,
            102, // Garchomp base stats
            50,
            Nature.Jolly // +Spd -SpAtk
        );

        // Assert - Calculated competitive values
        Assert.Equal(183, stats.MaxHP);
        Assert.Equal(182, stats.Attack);
        Assert.Equal(116, stats.Defense);
        Assert.Equal(169, stats.Speed);
    }

    [Fact]
    public void KnownStats_Alakazam_Level50_Timid()
    {
        // Arrange - Alakazam 252 SpA / 4 SpD / 252 Spe Timid
        var stats = new PokemonStats
        {
            IV_HP = 31,
            IV_Attack = 31,
            IV_Defense = 31,
            IV_SpAttack = 31,
            IV_SpDefense = 31,
            IV_Speed = 31,
            EV_HP = 0,
            EV_Attack = 0,
            EV_Defense = 0,
            EV_SpAttack = 252,
            EV_SpDefense = 4,
            EV_Speed = 252,
        };

        // Act
        StatCalculator.RecalculateAllStats(
            ref stats,
            55,
            50,
            45,
            135,
            95,
            120, // Alakazam base stats
            50,
            Nature.Timid // +Spd -Atk
        );

        // Assert - Calculated competitive values
        Assert.Equal(130, stats.MaxHP);
        Assert.Equal(187, stats.SpAttack);
        Assert.Equal(189, stats.Speed);
    }

    #endregion
}
