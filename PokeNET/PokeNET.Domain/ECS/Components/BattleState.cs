using System;

namespace PokeNET.Domain.ECS.Components;

/// <summary>
/// Battle state component tracking a Pokemon's current battle status and temporary modifiers.
/// Battle state and stat modifiers reset when the Pokemon switches out or the battle ends.
/// </summary>
public struct BattleState
{
    /// <summary>
    /// Current battle status of the Pokemon.
    /// </summary>
    public BattleStatus Status { get; set; }

    /// <summary>
    /// Amount of damage taken from the last attack.
    /// Used for moves like Counter, Mirror Coat, and Revenge.
    /// </summary>
    public int LastDamageTaken { get; set; }

    /// <summary>
    /// ID of the last move used by this Pokemon.
    /// Used for moves like Encore, Disable, and Torment.
    /// </summary>
    public int LastMoveUsed { get; set; }

    /// <summary>
    /// Number of turns this Pokemon has been in battle.
    /// Resets to 0 when switched out.
    /// </summary>
    public int TurnCounter { get; set; }

    /// <summary>
    /// Attack stat modifier stage (-6 to +6).
    /// Each stage multiplies the stat by specific ratios.
    /// Resets to 0 when switched out.
    /// </summary>
    public int AttackStage { get; set; }

    /// <summary>
    /// Defense stat modifier stage (-6 to +6).
    /// </summary>
    public int DefenseStage { get; set; }

    /// <summary>
    /// Special Attack stat modifier stage (-6 to +6).
    /// </summary>
    public int SpAttackStage { get; set; }

    /// <summary>
    /// Special Defense stat modifier stage (-6 to +6).
    /// </summary>
    public int SpDefenseStage { get; set; }

    /// <summary>
    /// Speed stat modifier stage (-6 to +6).
    /// </summary>
    public int SpeedStage { get; set; }

    /// <summary>
    /// Accuracy stat modifier stage (-6 to +6).
    /// Affects the chance of moves hitting.
    /// </summary>
    public int AccuracyStage { get; set; }

    /// <summary>
    /// Evasion stat modifier stage (-6 to +6).
    /// Affects the chance of opponent's moves missing.
    /// </summary>
    public int EvasionStage { get; set; }

    /// <summary>
    /// Gets the stat multiplier for a given stage.
    /// Stage modifiers: -6 = 2/8, -5 = 2/7, ..., 0 = 1.0, ..., +5 = 7/2, +6 = 8/2 (4.0)
    /// </summary>
    /// <param name="stage">Stat stage (-6 to +6)</param>
    /// <returns>Multiplier value</returns>
    public float GetStageMultiplier(int stage)
    {
        stage = Math.Clamp(stage, -6, 6);

        if (stage >= 0)
            return (2 + stage) / 2.0f;
        else
            return 2.0f / (2 - stage);
    }

    /// <summary>
    /// Resets all stat stages to 0.
    /// Called when Pokemon switches out or by moves like Haze.
    /// </summary>
    public void ResetStatStages()
    {
        AttackStage = 0;
        DefenseStage = 0;
        SpAttackStage = 0;
        SpDefenseStage = 0;
        SpeedStage = 0;
        AccuracyStage = 0;
        EvasionStage = 0;
    }

    /// <summary>
    /// Modifies a stat stage by a given amount, clamped to -6 to +6.
    /// </summary>
    /// <param name="stage">Current stage value</param>
    /// <param name="change">Amount to modify</param>
    /// <returns>New stage value</returns>
    public int ModifyStage(int stage, int change)
    {
        return Math.Clamp(stage + change, -6, 6);
    }
}

/// <summary>
/// Battle status enumeration representing a Pokemon's current state in battle.
/// </summary>
public enum BattleStatus
{
    /// <summary>Not currently in a battle</summary>
    NotInBattle,

    /// <summary>Actively participating in battle</summary>
    InBattle,

    /// <summary>Fainted (HP = 0), unable to battle</summary>
    Fainted,
}
