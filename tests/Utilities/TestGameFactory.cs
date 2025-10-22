using Microsoft.Xna.Framework;
using PokeNET.Core;

namespace PokeNET.Tests.Utilities;

/// <summary>
/// Factory for creating test instances of game components.
/// Provides mocked and configured instances for testing purposes.
/// </summary>
public static class TestGameFactory
{
    /// <summary>
    /// Creates a new PokeNETGame instance for testing.
    /// Note: Requires GraphicsDevice which may not be available in headless test environments.
    /// </summary>
    public static PokeNETGame CreateGame()
    {
        return new PokeNETGame();
    }

    /// <summary>
    /// Creates a mock GameTime for testing update and draw methods.
    /// </summary>
    public static GameTime CreateGameTime(
        TimeSpan totalGameTime = default,
        TimeSpan elapsedGameTime = default)
    {
        if (totalGameTime == default)
            totalGameTime = TimeSpan.FromSeconds(1.0);

        if (elapsedGameTime == default)
            elapsedGameTime = TimeSpan.FromSeconds(1.0 / 60.0); // 60 FPS

        return new GameTime(totalGameTime, elapsedGameTime);
    }

    /// <summary>
    /// Creates a game time representing a single frame at 60 FPS.
    /// </summary>
    public static GameTime CreateSingleFrame(int frameNumber = 1)
    {
        var elapsed = TimeSpan.FromSeconds(1.0 / 60.0);
        var total = TimeSpan.FromSeconds(frameNumber / 60.0);
        return new GameTime(total, elapsed);
    }

    /// <summary>
    /// Creates a game time representing multiple frames.
    /// </summary>
    public static IEnumerable<GameTime> CreateFrameSequence(int frameCount, int fps = 60)
    {
        var frameTime = TimeSpan.FromSeconds(1.0 / fps);

        for (int i = 0; i < frameCount; i++)
        {
            yield return new GameTime(
                TimeSpan.FromSeconds(i / (double)fps),
                frameTime
            );
        }
    }
}
