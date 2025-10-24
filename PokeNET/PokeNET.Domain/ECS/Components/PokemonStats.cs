namespace PokeNET.Domain.ECS.Components;

/// <summary>
/// Pokemon battle statistics component containing all stat values, IVs, and EVs.
/// Stats are calculated using the official Pokemon stat formula incorporating base stats, IVs, EVs, nature, and level.
/// </summary>
public struct PokemonStats
{
    /// <summary>
    /// Hit Points - determines how much damage the Pokemon can sustain.
    /// HP does not have battle stage modifiers.
    /// </summary>
    public int HP { get; set; }

    /// <summary>
    /// Maximum HP value based on base stats, IVs, EVs, and level.
    /// </summary>
    public int MaxHP { get; set; }

    /// <summary>
    /// Attack stat - determines physical move damage output.
    /// </summary>
    public int Attack { get; set; }

    /// <summary>
    /// Defense stat - reduces physical move damage received.
    /// </summary>
    public int Defense { get; set; }

    /// <summary>
    /// Special Attack stat - determines special move damage output.
    /// </summary>
    public int SpAttack { get; set; }

    /// <summary>
    /// Special Defense stat - reduces special move damage received.
    /// </summary>
    public int SpDefense { get; set; }

    /// <summary>
    /// Speed stat - determines turn order in battle.
    /// Higher speed typically attacks first.
    /// </summary>
    public int Speed { get; set; }

    /// <summary>
    /// Individual Values for HP (0-31).
    /// IVs are genetic values that add variance to Pokemon of the same species.
    /// </summary>
    public int IV_HP { get; set; }

    /// <summary>
    /// Individual Values for Attack (0-31).
    /// </summary>
    public int IV_Attack { get; set; }

    /// <summary>
    /// Individual Values for Defense (0-31).
    /// </summary>
    public int IV_Defense { get; set; }

    /// <summary>
    /// Individual Values for Special Attack (0-31).
    /// </summary>
    public int IV_SpAttack { get; set; }

    /// <summary>
    /// Individual Values for Special Defense (0-31).
    /// </summary>
    public int IV_SpDefense { get; set; }

    /// <summary>
    /// Individual Values for Speed (0-31).
    /// </summary>
    public int IV_Speed { get; set; }

    /// <summary>
    /// Effort Values for HP (0-252, total across all stats max 510).
    /// EVs are earned through battle and training.
    /// Every 4 EVs = 1 stat point at level 100.
    /// </summary>
    public int EV_HP { get; set; }

    /// <summary>
    /// Effort Values for Attack (0-252).
    /// </summary>
    public int EV_Attack { get; set; }

    /// <summary>
    /// Effort Values for Defense (0-252).
    /// </summary>
    public int EV_Defense { get; set; }

    /// <summary>
    /// Effort Values for Special Attack (0-252).
    /// </summary>
    public int EV_SpAttack { get; set; }

    /// <summary>
    /// Effort Values for Special Defense (0-252).
    /// </summary>
    public int EV_SpDefense { get; set; }

    /// <summary>
    /// Effort Values for Speed (0-252).
    /// </summary>
    public int EV_Speed { get; set; }

    /// <summary>
    /// Calculates HP stat using the official Pokemon formula.
    /// HP Formula: ((2 * BaseStat + IV + (EV / 4)) * Level / 100) + Level + 10
    /// </summary>
    /// <param name="baseHP">Species base HP stat</param>
    /// <param name="level">Pokemon level (1-100)</param>
    /// <returns>Calculated maximum HP</returns>
    public int CalculateHP(int baseHP, int level)
    {
        return ((2 * baseHP + IV_HP + (EV_HP / 4)) * level / 100) + level + 10;
    }

    /// <summary>
    /// Calculates a stat (Attack, Defense, SpAttack, SpDefense, Speed) using the official Pokemon formula.
    /// Stat Formula: (((2 * BaseStat + IV + (EV / 4)) * Level / 100) + 5) * NatureModifier
    /// </summary>
    /// <param name="baseStat">Species base stat value</param>
    /// <param name="iv">Individual value for this stat (0-31)</param>
    /// <param name="ev">Effort value for this stat (0-252)</param>
    /// <param name="level">Pokemon level (1-100)</param>
    /// <param name="natureModifier">Nature modifier (0.9 for hindered, 1.0 for neutral, 1.1 for boosted)</param>
    /// <returns>Calculated stat value</returns>
    public int CalculateStat(int baseStat, int iv, int ev, int level, float natureModifier)
    {
        return (int)((((2 * baseStat + iv + (ev / 4)) * level / 100) + 5) * natureModifier);
    }

    /// <summary>
    /// Creates a new Pokemon stats instance with default IVs and EVs.
    /// IVs default to 0, EVs default to 0.
    /// </summary>
    public PokemonStats()
    {
        HP = 1;
        MaxHP = 1;
        Attack = 1;
        Defense = 1;
        SpAttack = 1;
        SpDefense = 1;
        Speed = 1;

        // Default IVs (0-31, typically randomized on capture/generation)
        IV_HP = 0;
        IV_Attack = 0;
        IV_Defense = 0;
        IV_SpAttack = 0;
        IV_SpDefense = 0;
        IV_Speed = 0;

        // Default EVs (0, earned through training)
        EV_HP = 0;
        EV_Attack = 0;
        EV_Defense = 0;
        EV_SpAttack = 0;
        EV_SpDefense = 0;
        EV_Speed = 0;
    }
}
