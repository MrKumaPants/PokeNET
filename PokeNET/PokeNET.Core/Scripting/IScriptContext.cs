namespace PokeNET.Core.Scripting;

/// <summary>
/// Script context provided to item effect scripts.
/// Gives access to game state and effect parameters.
/// </summary>
public interface IScriptContext
{
    /// <summary>
    /// The Pokemon selected for item use.
    /// </summary>
    IPokemonBattleState SelectedPokemon { get; }

    /// <summary>
    /// The player's party (for multi-target items).
    /// </summary>
    IReadOnlyList<IPokemonBattleState> Party { get; }

    /// <summary>
    /// Effect parameters from JSON (e.g., {"healAmount": 20}).
    /// </summary>
    IReadOnlyDictionary<string, object> Parameters { get; }

    /// <summary>
    /// Random number generator (seeded for replay consistency).
    /// </summary>
    Random Random { get; }

    /// <summary>
    /// Show a message to the player.
    /// </summary>
    Task ShowMessageAsync(string message);

    /// <summary>
    /// Get a parameter value with type conversion.
    /// </summary>
    T GetParameter<T>(string key, T defaultValue = default!);
}
