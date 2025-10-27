using PokeNET.Core.ECS.Components;
using Nature = PokeNET.Core.ECS.Components.Nature;

namespace PokeNET.Core.Battle;

/// <summary>
/// Utility class for calculating Pokemon statistics using official Gen 3+ formulas.
/// Provides static methods for HP and stat calculations based on base stats, IVs, EVs, level, and nature.
/// </summary>
/// <remarks>
/// Official Pokemon Stat Formulas (Generation 3+):
///
/// HP Formula:
///   HP = floor(((2 * Base + IV + floor(EV/4)) * Level) / 100) + Level + 10
///
/// Other Stats (Attack, Defense, SpAttack, SpDefense, Speed):
///   Stat = floor((floor(((2 * Base + IV + floor(EV/4)) * Level) / 100) + 5) * Nature)
///
/// Nature Modifiers:
///   - Boosted stat: 1.1 (10% increase)
///   - Hindered stat: 0.9 (10% decrease)
///   - Neutral stat: 1.0 (no change)
///
/// Constraints:
///   - Level: 1-100
///   - IVs: 0-31 for each stat
///   - EVs: 0-252 for each stat (max 510 total across all stats)
///   - Base stats: Species-dependent, typically 1-255
/// </remarks>
public static class StatCalculator
{
    /// <summary>
    /// Calculates the HP stat using the official Pokemon formula.
    /// HP = floor(((2 * Base + IV + floor(EV/4)) * Level) / 100) + Level + 10
    /// </summary>
    /// <param name="baseHP">Species base HP stat (typically 1-255)</param>
    /// <param name="iv">Individual Value for HP (0-31)</param>
    /// <param name="ev">Effort Value for HP (0-252)</param>
    /// <param name="level">Pokemon level (1-100)</param>
    /// <returns>Calculated maximum HP value</returns>
    /// <example>
    /// // Calculate HP for level 50 Bulbasaur (base HP 45) with 31 IV, 252 EV
    /// int maxHP = StatCalculator.CalculateHP(45, 31, 252, 50);
    /// // Result: 155 HP
    /// </example>
    public static int CalculateHP(int baseHP, int iv, int ev, int level)
    {
        // HP Formula: ((2 * Base + IV + (EV / 4)) * Level / 100) + Level + 10
        // Note: Integer division in C# automatically floors the result
        return ((2 * baseHP + iv + (ev / 4)) * level / 100) + level + 10;
    }

    /// <summary>
    /// Calculates a non-HP stat (Attack, Defense, SpAttack, SpDefense, Speed) using the official Pokemon formula.
    /// Stat = floor((floor(((2 * Base + IV + floor(EV/4)) * Level) / 100) + 5) * Nature)
    /// </summary>
    /// <param name="baseStat">Species base stat value (typically 1-255)</param>
    /// <param name="iv">Individual Value for this stat (0-31)</param>
    /// <param name="ev">Effort Value for this stat (0-252)</param>
    /// <param name="level">Pokemon level (1-100)</param>
    /// <param name="natureModifier">Nature modifier (0.9 for hindered, 1.0 for neutral, 1.1 for boosted)</param>
    /// <returns>Calculated stat value</returns>
    /// <example>
    /// // Calculate Attack for level 50 Charizard (base 84) with 31 IV, 252 EV, boosted nature
    /// int attack = StatCalculator.CalculateStat(84, 31, 252, 50, 1.1f);
    /// // Result: 150 Attack
    /// </example>
    public static int CalculateStat(int baseStat, int iv, int ev, int level, float natureModifier)
    {
        // Stat Formula: (((2 * Base + IV + (EV / 4)) * Level / 100) + 5) * Nature
        // Note: Integer division in C# automatically floors intermediate results
        int baseValue = ((2 * baseStat + iv + (ev / 4)) * level / 100) + 5;
        return (int)(baseValue * natureModifier);
    }

    /// <summary>
    /// Calculates all stats for a Pokemon and updates the PokemonStats component.
    /// </summary>
    /// <param name="stats">Reference to the PokemonStats component to update</param>
    /// <param name="baseHP">Species base HP</param>
    /// <param name="baseAttack">Species base Attack</param>
    /// <param name="baseDefense">Species base Defense</param>
    /// <param name="baseSpAttack">Species base Special Attack</param>
    /// <param name="baseSpDefense">Species base Special Defense</param>
    /// <param name="baseSpeed">Species base Speed</param>
    /// <param name="level">Pokemon level (1-100)</param>
    /// <param name="nature">Pokemon nature (for stat modifiers)</param>
    /// <remarks>
    /// This method recalculates all six stats based on the IVs and EVs already stored in the PokemonStats component.
    /// It applies nature modifiers to the appropriate stats.
    /// </remarks>
    public static void RecalculateAllStats(
        ref PokemonStats stats,
        int baseHP,
        int baseAttack,
        int baseDefense,
        int baseSpAttack,
        int baseSpDefense,
        int baseSpeed,
        int level,
        Nature nature)
    {
        // Calculate HP (no nature modifier)
        stats.MaxHP = CalculateHP(baseHP, stats.IV_HP, stats.EV_HP, level);

        // Calculate other stats with nature modifiers
        var (attackMod, defenseMod, spAttackMod, spDefenseMod, speedMod) = GetNatureModifiers(nature);

        stats.Attack = CalculateStat(baseAttack, stats.IV_Attack, stats.EV_Attack, level, attackMod);
        stats.Defense = CalculateStat(baseDefense, stats.IV_Defense, stats.EV_Defense, level, defenseMod);
        stats.SpAttack = CalculateStat(baseSpAttack, stats.IV_SpAttack, stats.EV_SpAttack, level, spAttackMod);
        stats.SpDefense = CalculateStat(baseSpDefense, stats.IV_SpDefense, stats.EV_SpDefense, level, spDefenseMod);
        stats.Speed = CalculateStat(baseSpeed, stats.IV_Speed, stats.EV_Speed, level, speedMod);
    }

    /// <summary>
    /// Gets the nature modifiers for all stats based on the Pokemon's nature.
    /// </summary>
    /// <param name="nature">Pokemon nature</param>
    /// <returns>Tuple of modifiers: (Attack, Defense, SpAttack, SpDefense, Speed)</returns>
    /// <remarks>
    /// Nature modifiers:
    /// - Boosted stat: 1.1 (10% increase)
    /// - Hindered stat: 0.9 (10% decrease)
    /// - Neutral stat: 1.0 (no change)
    /// - Hardy, Docile, Serious, Bashful, Quirky are neutral natures (all 1.0)
    /// </remarks>
    private static (float attack, float defense, float spAttack, float spDefense, float speed) GetNatureModifiers(Nature nature)
    {
        return nature switch
        {
            // Attack boosted
            Nature.Lonely => (1.1f, 0.9f, 1.0f, 1.0f, 1.0f),   // +Atk -Def
            Nature.Brave => (1.1f, 1.0f, 1.0f, 1.0f, 0.9f),    // +Atk -Spd
            Nature.Adamant => (1.1f, 1.0f, 0.9f, 1.0f, 1.0f),  // +Atk -SpAtk
            Nature.Naughty => (1.1f, 1.0f, 1.0f, 0.9f, 1.0f),  // +Atk -SpDef

            // Defense boosted
            Nature.Bold => (0.9f, 1.1f, 1.0f, 1.0f, 1.0f),     // +Def -Atk
            Nature.Relaxed => (1.0f, 1.1f, 1.0f, 1.0f, 0.9f),  // +Def -Spd
            Nature.Impish => (1.0f, 1.1f, 0.9f, 1.0f, 1.0f),   // +Def -SpAtk
            Nature.Lax => (1.0f, 1.1f, 1.0f, 0.9f, 1.0f),      // +Def -SpDef

            // SpAttack boosted
            Nature.Modest => (0.9f, 1.0f, 1.1f, 1.0f, 1.0f),   // +SpAtk -Atk
            Nature.Mild => (1.0f, 0.9f, 1.1f, 1.0f, 1.0f),     // +SpAtk -Def
            Nature.Quiet => (1.0f, 1.0f, 1.1f, 1.0f, 0.9f),    // +SpAtk -Spd
            Nature.Rash => (1.0f, 1.0f, 1.1f, 0.9f, 1.0f),     // +SpAtk -SpDef

            // SpDefense boosted
            Nature.Calm => (0.9f, 1.0f, 1.0f, 1.1f, 1.0f),     // +SpDef -Atk
            Nature.Gentle => (1.0f, 0.9f, 1.0f, 1.1f, 1.0f),   // +SpDef -Def
            Nature.Sassy => (1.0f, 1.0f, 1.0f, 1.1f, 0.9f),    // +SpDef -Spd
            Nature.Careful => (1.0f, 1.0f, 0.9f, 1.1f, 1.0f),  // +SpDef -SpAtk

            // Speed boosted
            Nature.Timid => (0.9f, 1.0f, 1.0f, 1.0f, 1.1f),    // +Spd -Atk
            Nature.Hasty => (1.0f, 0.9f, 1.0f, 1.0f, 1.1f),    // +Spd -Def
            Nature.Jolly => (1.0f, 1.0f, 0.9f, 1.0f, 1.1f),    // +Spd -SpAtk
            Nature.Naive => (1.0f, 1.0f, 1.0f, 0.9f, 1.1f),    // +Spd -SpDef

            // Neutral natures (no stat changes)
            Nature.Hardy or Nature.Docile or Nature.Serious or Nature.Bashful or Nature.Quirky
                => (1.0f, 1.0f, 1.0f, 1.0f, 1.0f),

            // Default to neutral
            _ => (1.0f, 1.0f, 1.0f, 1.0f, 1.0f),
        };
    }

    /// <summary>
    /// Validates that EV distribution is legal (max 252 per stat, 510 total).
    /// </summary>
    /// <param name="stats">PokemonStats component to validate</param>
    /// <returns>True if EV distribution is legal, false otherwise</returns>
    public static bool ValidateEVs(in PokemonStats stats)
    {
        int totalEVs = stats.EV_HP + stats.EV_Attack + stats.EV_Defense
                     + stats.EV_SpAttack + stats.EV_SpDefense + stats.EV_Speed;

        // Check total EV limit (510)
        if (totalEVs > 510)
            return false;

        // Check individual stat limits (252 each)
        if (stats.EV_HP > 252 || stats.EV_Attack > 252 || stats.EV_Defense > 252
            || stats.EV_SpAttack > 252 || stats.EV_SpDefense > 252 || stats.EV_Speed > 252)
            return false;

        return true;
    }

    /// <summary>
    /// Validates that all IVs are within legal range (0-31).
    /// </summary>
    /// <param name="stats">PokemonStats component to validate</param>
    /// <returns>True if all IVs are legal, false otherwise</returns>
    public static bool ValidateIVs(in PokemonStats stats)
    {
        return stats.IV_HP is >= 0 and <= 31
            && stats.IV_Attack is >= 0 and <= 31
            && stats.IV_Defense is >= 0 and <= 31
            && stats.IV_SpAttack is >= 0 and <= 31
            && stats.IV_SpDefense is >= 0 and <= 31
            && stats.IV_Speed is >= 0 and <= 31;
    }
}
