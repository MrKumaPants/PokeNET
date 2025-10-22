using PokeNET.Core;
using PokeNET.Core.Localization;
using PokeNET.Tests.Utilities;

namespace PokeNET.Tests.Integration;

/// <summary>
/// Integration tests for game initialization and startup sequence.
/// Tests the coordination of multiple systems during game bootstrap.
/// </summary>
public class GameInitializationTests : IDisposable
{
    private PokeNETGame? _game;

    [Fact]
    public void Game_ShouldInitializeWithDefaultSettings()
    {
        // Arrange & Act
        _game = new PokeNETGame();

        // Assert
        _game.Should().NotBeNull();
        _game.Content.RootDirectory.Should().Be("Content");
        _game.Services.Should().NotBeNull();
    }

    [Fact]
    public void Game_ShouldRegisterGraphicsDeviceManagerAsService()
    {
        // Arrange & Act
        _game = new PokeNETGame();
        var graphicsService = _game.Services.GetService(typeof(Microsoft.Xna.Framework.GraphicsDeviceManager));

        // Assert
        graphicsService.Should().NotBeNull();
    }

    [Fact]
    public void LocalizationManager_ShouldBeAvailableAfterInitialization()
    {
        // Arrange
        _game = new PokeNETGame();

        // Act
        var cultures = LocalizationManager.GetSupportedCultures();

        // Assert
        cultures.Should().NotBeNull();
        cultures.Should().NotBeEmpty();
    }

    [Fact]
    public void Game_ShouldSupportMultipleInstantiations()
    {
        // Arrange
        using var game1 = new PokeNETGame();
        using var game2 = new PokeNETGame();

        // Act & Assert
        game1.Should().NotBeSameAs(game2);
        game1.Services.Should().NotBeSameAs(game2.Services);
    }

    public void Dispose()
    {
        _game?.Dispose();
    }
}
