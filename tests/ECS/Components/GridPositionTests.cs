using FluentAssertions;
using PokeNET.Domain.ECS.Components;
using System.Numerics;
using Xunit;

namespace PokeNET.Tests.ECS.Components;

/// <summary>
/// Comprehensive tests for GridPosition component covering initialization, world position calculation,
/// movement state tracking, and interpolation progress.
/// Target: >80% code coverage
/// </summary>
public class GridPositionTests
{
    #region Initialization Tests

    [Fact]
    public void Constructor_WithValidCoordinates_InitializesCorrectly()
    {
        // Arrange & Act
        var gridPos = new GridPosition(5, 10, 2);

        // Assert
        gridPos.TileX.Should().Be(5);
        gridPos.TileY.Should().Be(10);
        gridPos.MapId.Should().Be(2);
        gridPos.InterpolationProgress.Should().Be(1.0f, "should start idle");
        gridPos.TargetTileX.Should().Be(5, "target should match current when idle");
        gridPos.TargetTileY.Should().Be(10, "target should match current when idle");
    }

    [Fact]
    public void Constructor_WithDefaultMapId_InitializesToZero()
    {
        // Arrange & Act
        var gridPos = new GridPosition(3, 7);

        // Assert
        gridPos.TileX.Should().Be(3);
        gridPos.TileY.Should().Be(7);
        gridPos.MapId.Should().Be(0, "default map ID should be 0");
    }

    [Fact]
    public void Constructor_NegativeCoordinates_AllowsNegativeValues()
    {
        // Arrange & Act
        var gridPos = new GridPosition(-5, -10, 1);

        // Assert
        gridPos.TileX.Should().Be(-5, "negative coordinates should be allowed");
        gridPos.TileY.Should().Be(-10);
    }

    #endregion

    #region WorldPosition Tests

    [Fact]
    public void WorldPosition_AtOrigin_ReturnsZeroVector()
    {
        // Arrange
        var gridPos = new GridPosition(0, 0);

        // Act
        var worldPos = gridPos.WorldPosition;

        // Assert
        worldPos.Should().Be(new Vector2(0, 0));
    }

    [Fact]
    public void WorldPosition_PositiveTiles_CalculatesCorrectly()
    {
        // Arrange
        var gridPos = new GridPosition(10, 5);

        // Act
        var worldPos = gridPos.WorldPosition;

        // Assert - Each tile is 16x16 pixels
        worldPos.X.Should().Be(160f, "10 tiles * 16 pixels = 160");
        worldPos.Y.Should().Be(80f, "5 tiles * 16 pixels = 80");
    }

    [Fact]
    public void WorldPosition_NegativeTiles_CalculatesCorrectly()
    {
        // Arrange
        var gridPos = new GridPosition(-3, -2);

        // Act
        var worldPos = gridPos.WorldPosition;

        // Assert
        worldPos.X.Should().Be(-48f, "-3 tiles * 16 pixels = -48");
        worldPos.Y.Should().Be(-32f, "-2 tiles * 16 pixels = -32");
    }

    #endregion

    #region IsMoving Tests

    [Fact]
    public void IsMoving_WhenInterpolationComplete_ReturnsFalse()
    {
        // Arrange
        var gridPos = new GridPosition(5, 5)
        {
            InterpolationProgress = 1.0f
        };

        // Act & Assert
        gridPos.IsMoving.Should().BeFalse("interpolation is complete");
    }

    [Fact]
    public void IsMoving_WhenInterpolationInProgress_ReturnsTrue()
    {
        // Arrange
        var gridPos = new GridPosition(5, 5)
        {
            InterpolationProgress = 0.5f,
            TargetTileX = 6,
            TargetTileY = 5
        };

        // Act & Assert
        gridPos.IsMoving.Should().BeTrue("interpolation is in progress");
    }

    [Fact]
    public void IsMoving_WhenInterpolationAtStart_ReturnsTrue()
    {
        // Arrange
        var gridPos = new GridPosition(5, 5)
        {
            InterpolationProgress = 0.0f,
            TargetTileX = 6,
            TargetTileY = 5
        };

        // Act & Assert
        gridPos.IsMoving.Should().BeTrue("interpolation just started");
    }

    [Fact]
    public void IsMoving_WhenInterpolationNearlyComplete_ReturnsTrue()
    {
        // Arrange
        var gridPos = new GridPosition(5, 5)
        {
            InterpolationProgress = 0.99f,
            TargetTileX = 6,
            TargetTileY = 5
        };

        // Act & Assert
        gridPos.IsMoving.Should().BeTrue("interpolation not yet complete");
    }

    #endregion

    #region Interpolation Progress Tests

    [Fact]
    public void InterpolationProgress_CanBeSetToZero()
    {
        // Arrange
        var gridPos = new GridPosition(5, 5)
        {
            InterpolationProgress = 0.0f
        };

        // Act & Assert
        gridPos.InterpolationProgress.Should().Be(0.0f);
        gridPos.IsMoving.Should().BeTrue();
    }

    [Fact]
    public void InterpolationProgress_CanBeSetToMidpoint()
    {
        // Arrange
        var gridPos = new GridPosition(5, 5)
        {
            InterpolationProgress = 0.5f
        };

        // Act & Assert
        gridPos.InterpolationProgress.Should().Be(0.5f);
    }

    [Fact]
    public void InterpolationProgress_CanBeSetToOne()
    {
        // Arrange
        var gridPos = new GridPosition(5, 5)
        {
            InterpolationProgress = 1.0f
        };

        // Act & Assert
        gridPos.InterpolationProgress.Should().Be(1.0f);
        gridPos.IsMoving.Should().BeFalse();
    }

    #endregion

    #region Target Tile Tests

    [Fact]
    public void TargetTile_WhenMovingEast_UpdatesCorrectly()
    {
        // Arrange
        var gridPos = new GridPosition(5, 5)
        {
            TargetTileX = 6,
            TargetTileY = 5,
            InterpolationProgress = 0.0f
        };

        // Act & Assert
        gridPos.TargetTileX.Should().Be(6);
        gridPos.TargetTileY.Should().Be(5);
        gridPos.IsMoving.Should().BeTrue();
    }

    [Fact]
    public void TargetTile_WhenMovingDiagonally_UpdatesBothCoordinates()
    {
        // Arrange
        var gridPos = new GridPosition(5, 5)
        {
            TargetTileX = 6,
            TargetTileY = 6,
            InterpolationProgress = 0.3f
        };

        // Act & Assert
        gridPos.TargetTileX.Should().Be(6);
        gridPos.TargetTileY.Should().Be(6);
        gridPos.IsMoving.Should().BeTrue();
    }

    #endregion

    #region ToString Tests

    [Fact]
    public void ToString_WhenIdle_ShowsIdleState()
    {
        // Arrange
        var gridPos = new GridPosition(5, 10, 2);

        // Act
        var result = gridPos.ToString();

        // Assert
        result.Should().Contain("Tile(5, 10)");
        result.Should().Contain("Map:2");
        result.Should().Contain("Idle");
    }

    [Fact]
    public void ToString_WhenMoving_ShowsTargetAndProgress()
    {
        // Arrange
        var gridPos = new GridPosition(5, 10, 2)
        {
            TargetTileX = 6,
            TargetTileY = 10,
            InterpolationProgress = 0.5f
        };

        // Act
        var result = gridPos.ToString();

        // Assert
        result.Should().Contain("Tile(5, 10)");
        result.Should().Contain("Map:2");
        result.Should().Contain("(6, 10)");
        result.Should().Contain("%");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void GridPosition_WithLargeCoordinates_HandlesCorrectly()
    {
        // Arrange & Act
        var gridPos = new GridPosition(1000, 2000, 999);

        // Assert
        gridPos.TileX.Should().Be(1000);
        gridPos.TileY.Should().Be(2000);
        gridPos.MapId.Should().Be(999);
        gridPos.WorldPosition.X.Should().Be(16000f);
        gridPos.WorldPosition.Y.Should().Be(32000f);
    }

    [Fact]
    public void GridPosition_InterpolationProgress_BeyondOne_StillCalculatesWorldPosition()
    {
        // Arrange
        var gridPos = new GridPosition(5, 5)
        {
            InterpolationProgress = 1.5f // Edge case - over 100%
        };

        // Act
        var worldPos = gridPos.WorldPosition;

        // Assert
        worldPos.Should().Be(new Vector2(80f, 80f), "world position should still calculate correctly");
        gridPos.IsMoving.Should().BeFalse("should not be moving when >= 1.0");
    }

    #endregion
}
