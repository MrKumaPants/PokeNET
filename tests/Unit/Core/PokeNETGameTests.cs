using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PokeNET.Core;

namespace PokeNET.Tests.Unit.Core;

/// <summary>
/// Unit tests for PokeNETGame main game class.
/// Tests initialization, platform detection, and core game lifecycle.
/// </summary>
public class PokeNETGameTests
{
    [Fact]
    public void Constructor_ShouldInitializeGraphicsDeviceManager()
    {
        // Arrange & Act
        using var game = new PokeNETGame();

        // Assert
        game.Services.GetService(typeof(GraphicsDeviceManager)).Should().NotBeNull();
    }

    [Fact]
    public void ContentRootDirectory_ShouldBeSetToContent()
    {
        // Arrange & Act
        using var game = new PokeNETGame();

        // Assert
        game.Content.RootDirectory.Should().Be("Content");
    }

    [Fact]
    public void IsMobile_ShouldReflectPlatform()
    {
        // Arrange & Act
        var isMobile = PokeNETGame.IsMobile;

        // Assert
        isMobile.Should().Be(OperatingSystem.IsAndroid() || OperatingSystem.IsIOS());
    }

    [Fact]
    public void IsDesktop_ShouldReflectPlatform()
    {
        // Arrange & Act
        var isDesktop = PokeNETGame.IsDesktop;

        // Assert
        isDesktop.Should().Be(
            OperatingSystem.IsMacOS() ||
            OperatingSystem.IsLinux() ||
            OperatingSystem.IsWindows()
        );
    }

    [Fact]
    public void PlatformFlags_ShouldBeMutuallyExclusive()
    {
        // Arrange & Act
        var isMobile = PokeNETGame.IsMobile;
        var isDesktop = PokeNETGame.IsDesktop;

        // Assert - On most platforms, should not be both mobile and desktop
        if (isMobile)
        {
            isDesktop.Should().BeFalse();
        }
        else if (isDesktop)
        {
            isMobile.Should().BeFalse();
        }
    }
}
