namespace PokeNET.Core.Scripting;

/// <summary>
/// Interface that item effect scripts must implement.
/// Scripts are loaded from .csx files and compiled via Roslyn.
/// </summary>
public interface IItemEffect
{
    /// <summary>
    /// Effect name (e.g., "Restore 20 HP", "Cure Burn").
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Effect description shown to the player.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Determines if the item can be used in the current context.
    /// </summary>
    /// <param name="context">Script context with access to game state.</param>
    /// <returns>True if the item can be used, false otherwise.</returns>
    bool CanUse(IScriptContext context);

    /// <summary>
    /// Executes the item's effect.
    /// </summary>
    /// <param name="context">Script context with access to game state.</param>
    /// <returns>True if the item was consumed, false if it should remain in inventory.</returns>
    Task<bool> UseAsync(IScriptContext context);
}
