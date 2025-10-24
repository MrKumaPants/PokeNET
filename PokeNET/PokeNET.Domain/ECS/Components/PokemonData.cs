namespace PokeNET.Domain.ECS.Components;

/// <summary>
/// Core Pokemon data component containing species information, trainer data, and personality traits.
/// This component stores the fundamental identity and ownership information for a Pokemon.
/// </summary>
public struct PokemonData
{
    /// <summary>
    /// The species identifier (Pokedex number).
    /// Valid range: 1-1025 (as of Generation IX).
    /// </summary>
    public int SpeciesId { get; set; }

    /// <summary>
    /// The Pokemon's custom nickname.
    /// If null or empty, the species name should be used.
    /// Maximum length: 12 characters (Gen VI+).
    /// </summary>
    public string? Nickname { get; set; }

    /// <summary>
    /// Current level of the Pokemon.
    /// Valid range: 1-100.
    /// </summary>
    public int Level { get; set; }

    /// <summary>
    /// The Pokemon's nature, affecting stat growth.
    /// Natures provide a 10% boost to one stat and 10% reduction to another.
    /// Examples: Hardy, Lonely, Brave, Adamant, Naughty, etc.
    /// </summary>
    public Nature Nature { get; set; }

    /// <summary>
    /// The Pokemon's biological gender.
    /// Some species are gender-locked (e.g., Nidoran) or genderless (e.g., Magnemite).
    /// </summary>
    public Gender Gender { get; set; }

    /// <summary>
    /// Indicates whether this Pokemon has alternate shiny coloration.
    /// Shiny rate is typically 1/4096 in modern games.
    /// </summary>
    public bool IsShiny { get; set; }

    /// <summary>
    /// Current experience points accumulated.
    /// Experience determines level and is gained through battles.
    /// </summary>
    public int ExperiencePoints { get; set; }

    /// <summary>
    /// Experience points required to reach the next level.
    /// This value depends on the species' growth rate (Fast, Medium Fast, Medium Slow, Slow, Erratic, Fluctuating).
    /// </summary>
    public int ExperienceToNextLevel { get; set; }

    /// <summary>
    /// Trainer ID of the Pokemon's original trainer.
    /// Used for obedience checks and traded Pokemon mechanics.
    /// </summary>
    public int OriginalTrainerId { get; set; }

    /// <summary>
    /// Friendship/Happiness level with the trainer.
    /// Valid range: 0-255.
    /// Affects evolution (e.g., Eevee to Espeon/Umbreon), move power (Return/Frustration), and obedience.
    /// </summary>
    public int FriendshipLevel { get; set; }

    /// <summary>
    /// Creates a new Pokemon data instance with default values.
    /// </summary>
    public PokemonData()
    {
        Level = 1;
        Nature = Nature.Hardy;
        Gender = Gender.Unknown;
        IsShiny = false;
        ExperiencePoints = 0;
        ExperienceToNextLevel = 0;
        FriendshipLevel = 70; // Base friendship for most Pokemon
    }
}

/// <summary>
/// Pokemon nature affecting stat growth.
/// Each nature (except neutral ones) increases one stat by 10% and decreases another by 10%.
/// </summary>
public enum Nature
{
    /// <summary>Neutral nature (no stat modifiers)</summary>
    Hardy,
    /// <summary>+Attack, -Defense</summary>
    Lonely,
    /// <summary>+Attack, -Speed</summary>
    Brave,
    /// <summary>+Attack, -SpAttack</summary>
    Adamant,
    /// <summary>+Attack, -SpDefense</summary>
    Naughty,
    /// <summary>+Defense, -Attack</summary>
    Bold,
    /// <summary>Neutral nature (no stat modifiers)</summary>
    Docile,
    /// <summary>+Defense, -Speed</summary>
    Relaxed,
    /// <summary>+Defense, -SpAttack</summary>
    Impish,
    /// <summary>+Defense, -SpDefense</summary>
    Lax,
    /// <summary>+Speed, -Attack</summary>
    Timid,
    /// <summary>+Speed, -Defense</summary>
    Hasty,
    /// <summary>Neutral nature (no stat modifiers)</summary>
    Serious,
    /// <summary>+Speed, -SpAttack</summary>
    Jolly,
    /// <summary>+Speed, -SpDefense</summary>
    Naive,
    /// <summary>+SpAttack, -Attack</summary>
    Modest,
    /// <summary>+SpAttack, -Defense</summary>
    Mild,
    /// <summary>+SpAttack, -Speed</summary>
    Quiet,
    /// <summary>Neutral nature (no stat modifiers)</summary>
    Bashful,
    /// <summary>+SpAttack, -SpDefense</summary>
    Rash,
    /// <summary>+SpDefense, -Attack</summary>
    Calm,
    /// <summary>+SpDefense, -Defense</summary>
    Gentle,
    /// <summary>+SpDefense, -Speed</summary>
    Sassy,
    /// <summary>+SpDefense, -SpAttack</summary>
    Careful,
    /// <summary>Neutral nature (no stat modifiers)</summary>
    Quirky
}

/// <summary>
/// Pokemon gender classification.
/// Gender ratios vary by species (e.g., starters are 87.5% male, Chansey is always female).
/// </summary>
public enum Gender
{
    /// <summary>Male Pokemon</summary>
    Male,
    /// <summary>Female Pokemon</summary>
    Female,
    /// <summary>Genderless Pokemon (e.g., Magnemite, Porygon, Legendary Pokemon)</summary>
    Unknown
}
