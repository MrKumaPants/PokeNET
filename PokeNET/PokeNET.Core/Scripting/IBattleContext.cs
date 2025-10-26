namespace PokeNET.Core.Scripting;

/// <summary>
/// Battle context provided to move effect scripts.
/// Gives access to battle state during move execution.
/// </summary>
public interface IBattleContext
{
    /// <summary>
    /// The Pokemon using the move.
    /// </summary>
    IPokemonBattleState Attacker { get; }

    /// <summary>
    /// The Pokemon targeted by the move.
    /// </summary>
    IPokemonBattleState Defender { get; }

    /// <summary>
    /// The move being executed.
    /// </summary>
    IMoveInfo Move { get; }

    /// <summary>
    /// Effect parameters from JSON (e.g., {"burnChance": 10}).
    /// </summary>
    IReadOnlyDictionary<string, object> Parameters { get; }

    /// <summary>
    /// Random number generator (seeded for replay consistency).
    /// </summary>
    Random Random { get; }

    /// <summary>
    /// Show a battle message to the player.
    /// </summary>
    Task ShowMessageAsync(string message);

    /// <summary>
    /// Check if a random event occurs (e.g., 30% burn chance).
    /// </summary>
    /// <param name="chance">Probability 0-100.</param>
    bool RandomChance(int chance);

    /// <summary>
    /// Get a parameter value with type conversion.
    /// </summary>
    T GetParameter<T>(string key, T defaultValue = default!);
}

/// <summary>
/// Pokemon battle state accessible from scripts.
/// </summary>
public interface IPokemonBattleState
{
    string Name { get; }
    int CurrentHP { get; set; }
    int MaxHP { get; }
    bool HasStatus { get; }
    string? StatusCondition { get; }
    void ApplyStatus(string statusCondition);
    void RemoveStatus();
    void BoostStat(string stat, int stages);
    int GetStatStage(string stat);
}

/// <summary>
/// Move information accessible from scripts.
/// </summary>
public interface IMoveInfo
{
    string Name { get; }
    string Type { get; }
    int Power { get; }
    int Accuracy { get; }
    bool MakesContact { get; }
}
