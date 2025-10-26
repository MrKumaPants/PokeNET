using System;

namespace PokeNET.Domain.ECS.Events;

/// <summary>
/// Event published during Pokemon battles.
/// Used by the reactive audio system to trigger battle-specific sound effects and music changes.
/// </summary>
public class BattleEvent : IGameEvent
{
    /// <summary>
    /// The type of battle event.
    /// </summary>
    public BattleEventType EventType { get; init; }

    /// <summary>
    /// Gets the timestamp when the event occurred.
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Optional battle context data.
    /// </summary>
    public object? Data { get; init; }

    /// <summary>
    /// Initializes a new instance of the BattleEvent class.
    /// </summary>
    /// <param name="eventType">The type of battle event.</param>
    /// <param name="data">Optional event data.</param>
    public BattleEvent(BattleEventType eventType, object? data = null)
    {
        EventType = eventType;
        Data = data;
    }
}

/// <summary>
/// Types of battle events that can occur.
/// </summary>
public enum BattleEventType
{
    /// <summary>Battle has started.</summary>
    BattleStart,

    /// <summary>Battle has ended.</summary>
    BattleEnd,

    /// <summary>Pokemon took damage.</summary>
    PokemonDamaged,

    /// <summary>Pokemon fainted.</summary>
    PokemonFainted,

    /// <summary>Pokemon caught.</summary>
    PokemonCaught,

    /// <summary>Player won the battle.</summary>
    Victory,

    /// <summary>Player lost the battle.</summary>
    Defeat,

    /// <summary>Move was used.</summary>
    MoveUsed,

    /// <summary>Critical hit occurred.</summary>
    CriticalHit,

    /// <summary>Super effective hit.</summary>
    SuperEffective,

    /// <summary>Not very effective hit.</summary>
    NotVeryEffective,
}
