using System.Collections.Generic;

namespace PokeNET.Core.Data;

/// <summary>
/// Represents static data for a Pokemon species.
/// This contains all the immutable information about a species (e.g., Bulbasaur, Charmander).
/// </summary>
public class SpeciesData
{
    /// <summary>
    /// National Pokedex number (1-1025+).
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Species name (e.g., "Bulbasaur").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Pokemon types (1 or 2).
    /// </summary>
    public List<string> Types { get; set; } = new();

    /// <summary>
    /// Base stats (HP, Attack, Defense, SpAttack, SpDefense, Speed).
    /// </summary>
    public BaseStats BaseStats { get; set; } = new();

    /// <summary>
    /// Possible abilities for this species.
    /// </summary>
    public List<string> Abilities { get; set; } = new();

    /// <summary>
    /// Hidden/Dream World ability.
    /// </summary>
    public string? HiddenAbility { get; set; }

    /// <summary>
    /// Experience growth rate (Fast, Medium Fast, Medium Slow, Slow, Erratic, Fluctuating).
    /// </summary>
    public string GrowthRate { get; set; } = "Medium Fast";

    /// <summary>
    /// Base experience yield when defeated.
    /// </summary>
    public int BaseExperience { get; set; }

    /// <summary>
    /// Gender ratio (-1 for genderless, 0 for male only, 254 for female only, 127 for 50/50).
    /// </summary>
    public int GenderRatio { get; set; } = 127;

    /// <summary>
    /// Catch rate (0-255, higher is easier).
    /// </summary>
    public int CatchRate { get; set; } = 45;

    /// <summary>
    /// Base friendship value when caught.
    /// </summary>
    public int BaseFriendship { get; set; } = 70;

    /// <summary>
    /// Egg groups for breeding.
    /// </summary>
    public List<string> EggGroups { get; set; } = new();

    /// <summary>
    /// Steps required to hatch egg.
    /// </summary>
    public int HatchSteps { get; set; }

    /// <summary>
    /// Height in decimeters (10 = 1.0m).
    /// </summary>
    public int Height { get; set; }

    /// <summary>
    /// Weight in hectograms (10 = 1.0kg).
    /// </summary>
    public int Weight { get; set; }

    /// <summary>
    /// Pokedex entry text.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Moves learned by leveling up.
    /// </summary>
    public List<LevelMove> LevelMoves { get; set; } = new();

    /// <summary>
    /// Moves learned by TM/HM.
    /// </summary>
    public List<string> TmMoves { get; set; } = new();

    /// <summary>
    /// Moves learned from breeding.
    /// </summary>
    public List<string> EggMoves { get; set; } = new();

    /// <summary>
    /// Evolution conditions and target species.
    /// </summary>
    public List<Evolution> Evolutions { get; set; } = new();
}

/// <summary>
/// Base stats for a Pokemon species.
/// </summary>
public class BaseStats
{
    public int HP { get; set; }
    public int Attack { get; set; }
    public int Defense { get; set; }
    public int SpecialAttack { get; set; }
    public int SpecialDefense { get; set; }
    public int Speed { get; set; }

    /// <summary>
    /// Total of all base stats (BST).
    /// </summary>
    public int Total => HP + Attack + Defense + SpecialAttack + SpecialDefense + Speed;
}

/// <summary>
/// Represents a move learned at a specific level.
/// </summary>
public class LevelMove
{
    public int Level { get; set; }
    public string MoveName { get; set; } = string.Empty;
}

/// <summary>
/// Represents an evolution condition.
/// </summary>
public class Evolution
{
    /// <summary>
    /// Target species ID.
    /// </summary>
    public int TargetSpeciesId { get; set; }

    /// <summary>
    /// Method (Level, Stone, Trade, Friendship, etc.).
    /// </summary>
    public string Method { get; set; } = string.Empty;

    /// <summary>
    /// Required level (for level-up evolutions).
    /// </summary>
    public int? RequiredLevel { get; set; }

    /// <summary>
    /// Required item (for item/stone evolutions).
    /// </summary>
    public string? RequiredItem { get; set; }

    /// <summary>
    /// Additional conditions (time of day, location, held item, etc.).
    /// </summary>
    public Dictionary<string, string> Conditions { get; set; } = new();
}
