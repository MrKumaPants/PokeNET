namespace PokeNET.Core.Data;

/// <summary>
/// Represents the encounter table for a specific location.
/// Defines which Pokemon can be found and their encounter rates.
/// </summary>
public class EncounterTable
{
    /// <summary>
    /// Location identifier (e.g., "route_1", "viridian_forest").
    /// </summary>
    public string LocationId { get; set; } = string.Empty;

    /// <summary>
    /// Display name of the location.
    /// </summary>
    public string LocationName { get; set; } = string.Empty;

    /// <summary>
    /// Wild Pokemon encounters in tall grass.
    /// </summary>
    public List<Encounter> GrassEncounters { get; set; } = new();

    /// <summary>
    /// Wild Pokemon encounters while surfing.
    /// </summary>
    public List<Encounter> WaterEncounters { get; set; } = new();

    /// <summary>
    /// Wild Pokemon encounters while fishing with Old Rod.
    /// </summary>
    public List<Encounter> OldRodEncounters { get; set; } = new();

    /// <summary>
    /// Wild Pokemon encounters while fishing with Good Rod.
    /// </summary>
    public List<Encounter> GoodRodEncounters { get; set; } = new();

    /// <summary>
    /// Wild Pokemon encounters while fishing with Super Rod.
    /// </summary>
    public List<Encounter> SuperRodEncounters { get; set; } = new();

    /// <summary>
    /// Wild Pokemon encounters in caves/rocky areas.
    /// </summary>
    public List<Encounter> CaveEncounters { get; set; } = new();

    /// <summary>
    /// Special/rare encounters (legendaries, events, etc.).
    /// </summary>
    public List<SpecialEncounter> SpecialEncounters { get; set; } = new();
}

/// <summary>
/// Represents a single wild Pokemon encounter.
/// </summary>
public class Encounter
{
    /// <summary>
    /// Species ID of the wild Pokemon.
    /// </summary>
    public string SpeciesId { get; set; } = string.Empty;

    /// <summary>
    /// Minimum level of the encounter.
    /// </summary>
    public int MinLevel { get; set; }

    /// <summary>
    /// Maximum level of the encounter.
    /// </summary>
    public int MaxLevel { get; set; }

    /// <summary>
    /// Encounter rate/chance (0-100).
    /// </summary>
    public int Rate { get; set; }

    /// <summary>
    /// Time of day restriction (Morning, Day, Night, Any).
    /// </summary>
    public string TimeOfDay { get; set; } = "Any";

    /// <summary>
    /// Weather requirement (Sunny, Rainy, Any, etc.).
    /// </summary>
    public string Weather { get; set; } = "Any";
}

/// <summary>
/// Represents a special encounter (legendary, scripted, etc.).
/// </summary>
public class SpecialEncounter
{
    /// <summary>
    /// Unique identifier for this special encounter.
    /// </summary>
    public string EncounterId { get; set; } = string.Empty;

    /// <summary>
    /// Species ID of the special Pokemon.
    /// </summary>
    public string SpeciesId { get; set; } = string.Empty;

    /// <summary>
    /// Fixed level for this encounter.
    /// </summary>
    public int Level { get; set; }

    /// <summary>
    /// Indicates if this is a one-time encounter.
    /// </summary>
    public bool OneTime { get; set; } = true;

    /// <summary>
    /// Conditions required to trigger this encounter.
    /// </summary>
    public Dictionary<string, object> Conditions { get; set; } = new();

    /// <summary>
    /// Optional script to run when encounter triggers.
    /// </summary>
    public string? Script { get; set; }
}
