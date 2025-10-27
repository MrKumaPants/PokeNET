using System.Collections.Generic;
using PokeNET.Core.Data;
using Xunit;

namespace PokeNET.Core.Tests.Data;

/// <summary>
/// Unit tests for MoveData model.
/// Tests move data loading, power/accuracy/PP parsing, and effect handling.
/// </summary>
public class MoveDataTests
{
    [Fact]
    public void MoveData_PhysicalMove_HasCorrectCategory()
    {
        // Arrange & Act
        var move = new MoveData
        {
            Name = "Tackle",
            Type = "Normal",
            Category = MoveCategory.Physical,
            Power = 40,
            Accuracy = 100,
            PP = 35
        };

        // Assert
        Assert.Equal(MoveCategory.Physical, move.Category);
        Assert.Equal(40, move.Power);
        Assert.Equal(100, move.Accuracy);
        Assert.Equal(35, move.PP);
    }

    [Fact]
    public void MoveData_SpecialMove_HasCorrectCategory()
    {
        // Arrange & Act
        var move = new MoveData
        {
            Name = "Thunderbolt",
            Type = "Electric",
            Category = MoveCategory.Special,
            Power = 90,
            Accuracy = 100,
            PP = 15
        };

        // Assert
        Assert.Equal(MoveCategory.Special, move.Category);
        Assert.Equal(90, move.Power);
    }

    [Fact]
    public void MoveData_StatusMove_HasZeroPower()
    {
        // Arrange & Act
        var move = new MoveData
        {
            Name = "Thunder Wave",
            Type = "Electric",
            Category = MoveCategory.Status,
            Power = 0,
            Accuracy = 90,
            PP = 20
        };

        // Assert
        Assert.Equal(MoveCategory.Status, move.Category);
        Assert.Equal(0, move.Power);
    }

    [Fact]
    public void MoveData_NeverMissMove_HasZeroAccuracy()
    {
        // Arrange & Act
        var move = new MoveData
        {
            Name = "Swift",
            Type = "Normal",
            Category = MoveCategory.Special,
            Power = 60,
            Accuracy = 0, // 0 means never miss
            PP = 20
        };

        // Assert
        Assert.Equal(0, move.Accuracy);
    }

    [Fact]
    public void MoveData_Priority_DefaultsToZero()
    {
        // Arrange & Act
        var move = new MoveData
        {
            Name = "Tackle"
        };

        // Assert
        Assert.Equal(0, move.Priority);
    }

    [Fact]
    public void MoveData_PriorityMove_HasPositivePriority()
    {
        // Arrange & Act
        var move = new MoveData
        {
            Name = "Quick Attack",
            Priority = 1
        };

        // Assert
        Assert.Equal(1, move.Priority);
    }

    [Fact]
    public void MoveData_Target_DefaultsToSingleTarget()
    {
        // Arrange & Act
        var move = new MoveData
        {
            Name = "Tackle"
        };

        // Assert
        Assert.Equal("SingleTarget", move.Target);
    }

    [Fact]
    public void MoveData_MultiTargetMove_HasCorrectTarget()
    {
        // Arrange & Act
        var move = new MoveData
        {
            Name = "Earthquake",
            Target = "AllOpponents"
        };

        // Assert
        Assert.Equal("AllOpponents", move.Target);
    }

    [Fact]
    public void MoveData_EffectChance_CanBeSpecified()
    {
        // Arrange & Act - Thunderbolt has 10% chance to paralyze
        var move = new MoveData
        {
            Name = "Thunderbolt",
            Type = "Electric",
            EffectChance = 10
        };

        // Assert
        Assert.Equal(10, move.EffectChance);
    }

    [Fact]
    public void MoveData_Flags_CanBeEmpty()
    {
        // Arrange & Act
        var move = new MoveData
        {
            Name = "Teleport"
        };

        // Assert
        Assert.NotNull(move.Flags);
        Assert.Empty(move.Flags);
    }

    [Fact]
    public void MoveData_Flags_CanContainMultiple()
    {
        // Arrange & Act
        var move = new MoveData
        {
            Name = "Drain Punch",
            Flags = new List<string> { "Contact", "Punch", "Healing" }
        };

        // Assert
        Assert.Equal(3, move.Flags.Count);
        Assert.Contains("Contact", move.Flags);
        Assert.Contains("Punch", move.Flags);
        Assert.Contains("Healing", move.Flags);
    }

    [Fact]
    public void MoveData_MakesContact_ReturnsTrueWhenContactFlag()
    {
        // Arrange & Act
        var contactMove = new MoveData
        {
            Name = "Tackle",
            Flags = new List<string> { "Contact" }
        };

        var nonContactMove = new MoveData
        {
            Name = "Thunderbolt",
            Flags = new List<string>()
        };

        // Assert
        Assert.True(contactMove.MakesContact);
        Assert.False(nonContactMove.MakesContact);
    }

    [Fact]
    public void MoveData_EffectScript_IsOptional()
    {
        // Arrange & Act
        var moveWithScript = new MoveData
        {
            Name = "Fire Blast",
            EffectScript = "scripts/moves/burn.csx",
            EffectParameters = new Dictionary<string, object> { { "burnChance", 10 } }
        };

        var moveWithoutScript = new MoveData
        {
            Name = "Tackle"
        };

        // Assert
        Assert.NotNull(moveWithScript.EffectScript);
        Assert.Equal("scripts/moves/burn.csx", moveWithScript.EffectScript);
        Assert.NotNull(moveWithScript.EffectParameters);
        Assert.Equal(10, moveWithScript.EffectParameters["burnChance"]);

        Assert.Null(moveWithoutScript.EffectScript);
        Assert.Null(moveWithoutScript.EffectParameters);
    }

    [Fact]
    public void MoveData_Description_CanBeEmpty()
    {
        // Arrange & Act
        var move = new MoveData
        {
            Name = "New Move"
        };

        // Assert
        Assert.NotNull(move.Description);
        Assert.Empty(move.Description);
    }

    [Theory]
    [InlineData(MoveCategory.Physical)]
    [InlineData(MoveCategory.Special)]
    [InlineData(MoveCategory.Status)]
    public void MoveData_AllCategories_AreValid(MoveCategory category)
    {
        // Arrange & Act
        var move = new MoveData
        {
            Name = "Test Move",
            Category = category
        };

        // Assert
        Assert.Equal(category, move.Category);
    }

    [Fact]
    public void MoveData_HighPowerMove_CanExceed100()
    {
        // Arrange & Act - Explosion has base power 250
        var move = new MoveData
        {
            Name = "Explosion",
            Power = 250
        };

        // Assert
        Assert.Equal(250, move.Power);
    }

    [Fact]
    public void MoveData_LowAccuracyMove_IsValid()
    {
        // Arrange & Act - Thunder has 70% accuracy
        var move = new MoveData
        {
            Name = "Thunder",
            Accuracy = 70
        };

        // Assert
        Assert.Equal(70, move.Accuracy);
    }
}
