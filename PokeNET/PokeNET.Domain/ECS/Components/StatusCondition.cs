using System;

namespace PokeNET.Domain.ECS.Components;

/// <summary>
/// Pokemon status condition component managing major status ailments.
/// A Pokemon can only have one major status condition at a time (PSN, BRN, PAR, FRZ, SLP).
/// Status conditions persist outside of battle until cured.
/// </summary>
public struct StatusCondition
{
    /// <summary>
    /// The current major status condition affecting the Pokemon.
    /// </summary>
    public StatusType Status { get; set; }

    /// <summary>
    /// Number of turns remaining for sleep or freeze conditions.
    /// Sleep: 1-3 turns (Gen V+)
    /// Freeze: Indefinite until thawed by fire move or thaw chance
    /// </summary>
    public int TurnsRemaining { get; set; }

    /// <summary>
    /// Checks if the Pokemon can act this turn based on status conditions.
    /// </summary>
    /// <returns>True if Pokemon can act, false if unable (frozen, asleep, or paralyzed this turn)</returns>
    public bool CanAct()
    {
        return Status switch
        {
            StatusType.Frozen => false, // Cannot act while frozen
            StatusType.Asleep => TurnsRemaining <= 0, // Can act only if sleep turns expired
            StatusType.Paralyzed => Random.Shared.Next(100) >= 25, // 25% chance to be fully paralyzed
            _ => true, // Other statuses don't prevent action
        };
    }

    /// <summary>
    /// Processes status condition effects at the end of a turn.
    /// Applies damage for Poison/Burn, decrements sleep/freeze counters.
    /// </summary>
    /// <param name="maxHP">Maximum HP for damage calculation</param>
    /// <returns>Damage taken from status condition (0 if no damage)</returns>
    public int StatusTick(int maxHP)
    {
        int damage = 0;

        switch (Status)
        {
            case StatusType.Poisoned:
                // Regular poison: 1/8 max HP damage per turn
                damage = Math.Max(1, maxHP / 8);
                break;

            case StatusType.BadlyPoisoned:
                // Badly poisoned (Toxic): increases by 1/16 max HP each turn
                damage = Math.Max(1, maxHP / 16 * TurnsRemaining);
                TurnsRemaining++; // Counter for toxic damage escalation
                break;

            case StatusType.Burned:
                // Burn: 1/16 max HP damage per turn (1/8 in Gen I-IV)
                damage = Math.Max(1, maxHP / 16);
                break;

            case StatusType.Asleep:
                // Decrement sleep counter
                if (TurnsRemaining > 0)
                    TurnsRemaining--;
                if (TurnsRemaining == 0)
                    Status = StatusType.None; // Wake up
                break;

            case StatusType.Frozen:
                // 20% chance to thaw each turn
                if (Random.Shared.Next(100) < 20)
                {
                    Status = StatusType.None;
                    TurnsRemaining = 0;
                }
                break;

            case StatusType.Paralyzed:
                // Paralysis has no end-of-turn effect (just speed reduction and action chance)
                break;
        }

        return damage;
    }

    /// <summary>
    /// Applies a new status condition to the Pokemon.
    /// </summary>
    /// <param name="newStatus">Status condition to apply</param>
    /// <param name="turns">Number of turns for sleep (1-3) or toxic counter</param>
    /// <returns>True if status was applied, false if Pokemon already has a status</returns>
    public bool ApplyStatus(StatusType newStatus, int turns = 0)
    {
        if (Status != StatusType.None)
            return false; // Already has a status condition

        Status = newStatus;
        TurnsRemaining = turns;
        return true;
    }

    /// <summary>
    /// Cures the current status condition.
    /// </summary>
    public void Cure()
    {
        Status = StatusType.None;
        TurnsRemaining = 0;
    }
}

/// <summary>
/// Major status condition types in Pokemon battles.
/// A Pokemon can only have one major status at a time.
/// </summary>
public enum StatusType
{
    /// <summary>No status condition</summary>
    None,

    /// <summary>
    /// Poisoned - Takes 1/8 max HP damage each turn.
    /// Halves damage from physical moves in some generations.
    /// </summary>
    Poisoned,

    /// <summary>
    /// Badly Poisoned (Toxic) - Takes increasing damage each turn (1/16, 2/16, 3/16, etc.).
    /// Counter resets when switched out.
    /// </summary>
    BadlyPoisoned,

    /// <summary>
    /// Burned - Takes 1/16 max HP damage each turn.
    /// Halves damage from physical moves (Gen III+).
    /// </summary>
    Burned,

    /// <summary>
    /// Paralyzed - Speed reduced to 50% (25% in Gen I-VI).
    /// 25% chance to be fully paralyzed and unable to act each turn.
    /// </summary>
    Paralyzed,

    /// <summary>
    /// Frozen - Cannot act until thawed.
    /// Thaws when hit by fire-type move or 20% chance each turn.
    /// Cannot be frozen in harsh sunlight (Gen III+).
    /// </summary>
    Frozen,

    /// <summary>
    /// Asleep - Cannot act for 1-3 turns (Gen V+), 1-7 turns (earlier gens).
    /// Wakes up when counter reaches 0.
    /// Some moves can be used while asleep (Snore, Sleep Talk).
    /// </summary>
    Asleep,
}
