namespace PokeNET.Core.Scripting;

/// <summary>
/// Interface that move effect scripts must implement.
/// Scripts are loaded from .csx files and compiled via Roslyn.
/// </summary>
public interface IMoveEffect
{
    /// <summary>
    /// Effect name (e.g., "Burn", "Stat Boost").
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Effect description.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Move effect priority (affects execution order).
    /// Higher priority effects execute first.
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Determines if the effect can trigger.
    /// </summary>
    /// <param name="context">Battle context with access to attacker, defender, move data.</param>
    /// <returns>True if the effect should trigger.</returns>
    bool CanTrigger(IBattleContext context);

    /// <summary>
    /// Executes the move's secondary effect.
    /// </summary>
    /// <param name="context">Battle context with access to attacker, defender, move data.</param>
    Task ApplyEffectAsync(IBattleContext context);
}
