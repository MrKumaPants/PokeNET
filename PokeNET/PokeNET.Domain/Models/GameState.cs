namespace PokeNET.Domain.Models;

/// <summary>
/// Represents the current state of the game.
/// Used by the reactive audio system to determine appropriate music and sound effects.
/// </summary>
public enum GameState
{
    /// <summary>Game is in the main menu or title screen.</summary>
    Menu,

    /// <summary>Player is exploring the overworld.</summary>
    Overworld,

    /// <summary>Player is in a Pokemon battle.</summary>
    Battle,

    /// <summary>A cutscene or story event is playing.</summary>
    Cutscene,

    /// <summary>Player is in a Pokemon Center.</summary>
    PokemonCenter,

    /// <summary>Player is in a shop or mart.</summary>
    Shop,

    /// <summary>Player is in a gym.</summary>
    Gym,

    /// <summary>Player is viewing inventory or Pokemon party.</summary>
    Inventory,
}
