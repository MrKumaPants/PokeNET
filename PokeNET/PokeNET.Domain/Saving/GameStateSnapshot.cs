using PokeNET.Domain.ECS.Components;

namespace PokeNET.Domain.Saving;

/// <summary>
/// Complete snapshot of the game state at a point in time.
/// Contains all data necessary to restore the game to this exact state.
/// </summary>
/// <remarks>
/// This is the root data model for save files. All game data that needs
/// persistence should be included in this snapshot.
/// </remarks>
public class GameStateSnapshot
{
    /// <summary>Version of the save file format (for migration support).</summary>
    public Version SaveVersion { get; set; } = new Version(1, 0, 0);

    /// <summary>Timestamp when this snapshot was created.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Optional description for this save (shown to player).</summary>
    public string? Description { get; set; }

    /// <summary>Current game state (Menu, Overworld, Battle, etc.).</summary>
    public Models.GameState CurrentGameState { get; set; }

    /// <summary>Player data (name, ID, playtime, etc.).</summary>
    public PlayerData Player { get; set; } = null!;

    /// <summary>Party Pokemon data (current team).</summary>
    public List<PokemonData> Party { get; set; } = new();

    /// <summary>Boxed Pokemon storage.</summary>
    public List<BoxData> PokemonBoxes { get; set; } = new();

    /// <summary>Player inventory (items, key items, etc.).</summary>
    public InventoryData Inventory { get; set; } = null!;

    /// <summary>World state (map positions, flags, etc.).</summary>
    public WorldData World { get; set; } = null!;

    /// <summary>Battle state (if currently in battle).</summary>
    public BattleData? CurrentBattle { get; set; }

    /// <summary>Game progress flags and story progression.</summary>
    public ProgressData Progress { get; set; } = null!;

    /// <summary>Pokedex data (seen/caught Pokemon).</summary>
    public PokedexData Pokedex { get; set; } = null!;

    /// <summary>Mod-specific save data (extensibility for mods).</summary>
    public Dictionary<string, object> ModData { get; set; } = new();

    /// <summary>Checksum for data integrity validation (computed during save).</summary>
    public string? Checksum { get; set; }
}

/// <summary>
/// Player information and statistics.
/// </summary>
public class PlayerData
{
    /// <summary>Player's unique ID.</summary>
    public Guid PlayerId { get; set; } = Guid.NewGuid();

    /// <summary>Player's name.</summary>
    public string Name { get; set; } = null!;

    /// <summary>Player's current position in the world.</summary>
    public Position Position { get; set; }

    /// <summary>Current map/location ID.</summary>
    public string CurrentMap { get; set; } = null!;

    /// <summary>Player's money/currency.</summary>
    public int Money { get; set; }

    /// <summary>Total playtime in seconds.</summary>
    public long PlaytimeSeconds { get; set; }

    /// <summary>Player's trainer ID number.</summary>
    public int TrainerId { get; set; }

    /// <summary>Number of badges earned.</summary>
    public int BadgeCount { get; set; }

    /// <summary>Player gender/appearance.</summary>
    public string? Gender { get; set; }
}

/// <summary>
/// Pokemon data for save files.
/// </summary>
public class PokemonData
{
    /// <summary>Unique instance ID for this Pokemon.</summary>
    public Guid InstanceId { get; set; } = Guid.NewGuid();

    /// <summary>Species ID (Pokedex number).</summary>
    public int SpeciesId { get; set; }

    /// <summary>Pokemon nickname (if any).</summary>
    public string? Nickname { get; set; }

    /// <summary>Current level.</summary>
    public int Level { get; set; }

    /// <summary>Current experience points.</summary>
    public int Experience { get; set; }

    /// <summary>Current HP.</summary>
    public int CurrentHp { get; set; }

    /// <summary>Maximum HP.</summary>
    public int MaxHp { get; set; }

    /// <summary>Base stats (Attack, Defense, etc.).</summary>
    public Stats Stats { get; set; }

    /// <summary>Individual Values (IVs) for stat calculation.</summary>
    public StatsIV IndividualValues { get; set; }

    /// <summary>Effort Values (EVs) for stat calculation.</summary>
    public StatsEV EffortValues { get; set; }

    /// <summary>Pokemon nature affecting stat growth.</summary>
    public string Nature { get; set; } = null!;

    /// <summary>Pokemon ability.</summary>
    public string Ability { get; set; } = null!;

    /// <summary>Held item (if any).</summary>
    public string? HeldItem { get; set; }

    /// <summary>Known moves (up to 4).</summary>
    public List<MoveData> Moves { get; set; } = new();

    /// <summary>Current status condition (poisoned, paralyzed, etc.).</summary>
    public string? StatusCondition { get; set; }

    /// <summary>Whether this Pokemon is shiny.</summary>
    public bool IsShiny { get; set; }

    /// <summary>Original trainer name.</summary>
    public string OriginalTrainer { get; set; } = null!;

    /// <summary>Friendship/happiness level.</summary>
    public int Friendship { get; set; }
}

/// <summary>
/// Individual Values for Pokemon stats.
/// </summary>
public struct StatsIV
{
    public int HP { get; set; }
    public int Attack { get; set; }
    public int Defense { get; set; }
    public int SpecialAttack { get; set; }
    public int SpecialDefense { get; set; }
    public int Speed { get; set; }
}

/// <summary>
/// Effort Values for Pokemon stats.
/// </summary>
public struct StatsEV
{
    public int HP { get; set; }
    public int Attack { get; set; }
    public int Defense { get; set; }
    public int SpecialAttack { get; set; }
    public int SpecialDefense { get; set; }
    public int Speed { get; set; }
}

/// <summary>
/// Move data for Pokemon.
/// </summary>
public class MoveData
{
    /// <summary>Move ID.</summary>
    public int MoveId { get; set; }

    /// <summary>Move name.</summary>
    public string Name { get; set; } = null!;

    /// <summary>Current PP (Power Points).</summary>
    public int CurrentPP { get; set; }

    /// <summary>Maximum PP.</summary>
    public int MaxPP { get; set; }
}

/// <summary>
/// Pokemon box storage data.
/// </summary>
public class BoxData
{
    /// <summary>Box number/ID.</summary>
    public int BoxNumber { get; set; }

    /// <summary>Box name (customizable by player).</summary>
    public string Name { get; set; } = null!;

    /// <summary>Pokemon stored in this box (null = empty slot).</summary>
    public List<PokemonData?> Pokemon { get; set; } = new();
}

/// <summary>
/// Player inventory data.
/// </summary>
public class InventoryData
{
    /// <summary>Regular items (Potions, Poke Balls, etc.).</summary>
    public Dictionary<string, int> Items { get; set; } = new();

    /// <summary>Key items (cannot be discarded).</summary>
    public List<string> KeyItems { get; set; } = new();

    /// <summary>TMs/HMs.</summary>
    public Dictionary<string, int> TMs { get; set; } = new();

    /// <summary>Berries.</summary>
    public Dictionary<string, int> Berries { get; set; } = new();
}

/// <summary>
/// World state data.
/// </summary>
public class WorldData
{
    /// <summary>Story/event flags that have been set.</summary>
    public HashSet<string> Flags { get; set; } = new();

    /// <summary>Map-specific data (visited locations, items collected, etc.).</summary>
    public Dictionary<string, MapData> Maps { get; set; } = new();

    /// <summary>NPCs that have been interacted with or defeated.</summary>
    public HashSet<string> DefeatedTrainers { get; set; } = new();

    /// <summary>Time of day (for day/night cycle).</summary>
    public TimeSpan TimeOfDay { get; set; }
}

/// <summary>
/// Map-specific save data.
/// </summary>
public class MapData
{
    /// <summary>Map ID.</summary>
    public string MapId { get; set; } = null!;

    /// <summary>Items collected on this map.</summary>
    public HashSet<string> CollectedItems { get; set; } = new();

    /// <summary>Visited areas within the map.</summary>
    public HashSet<string> VisitedAreas { get; set; } = new();
}

/// <summary>
/// Current battle state (if in battle).
/// </summary>
public class BattleData
{
    /// <summary>Battle type (wild, trainer, gym, etc.).</summary>
    public string BattleType { get; set; } = null!;

    /// <summary>Opponent Pokemon.</summary>
    public List<PokemonData> OpponentPokemon { get; set; } = new();

    /// <summary>Current turn number.</summary>
    public int TurnNumber { get; set; }

    /// <summary>Active player Pokemon indices.</summary>
    public List<int> ActivePlayerPokemon { get; set; } = new();

    /// <summary>Active opponent Pokemon indices.</summary>
    public List<int> ActiveOpponentPokemon { get; set; } = new();

    /// <summary>Weather condition (if any).</summary>
    public string? Weather { get; set; }

    /// <summary>Terrain condition (if any).</summary>
    public string? Terrain { get; set; }
}

/// <summary>
/// Game progress and story flags.
/// </summary>
public class ProgressData
{
    /// <summary>Story progression flags.</summary>
    public HashSet<string> StoryFlags { get; set; } = new();

    /// <summary>Badges earned.</summary>
    public List<string> Badges { get; set; } = new();

    /// <summary>Gym leaders defeated.</summary>
    public HashSet<string> DefeatedGymLeaders { get; set; } = new();

    /// <summary>Elite Four/Champion status.</summary>
    public bool EliteFourDefeated { get; set; }

    /// <summary>Legendary Pokemon encountered.</summary>
    public HashSet<string> EncounteredLegendaries { get; set; } = new();
}

/// <summary>
/// Pokedex data.
/// </summary>
public class PokedexData
{
    /// <summary>Pokemon species that have been seen.</summary>
    public HashSet<int> Seen { get; set; } = new();

    /// <summary>Pokemon species that have been caught.</summary>
    public HashSet<int> Caught { get; set; } = new();

    /// <summary>Total seen count.</summary>
    public int TotalSeen => Seen.Count;

    /// <summary>Total caught count.</summary>
    public int TotalCaught => Caught.Count;
}
