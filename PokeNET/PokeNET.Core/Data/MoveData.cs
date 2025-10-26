namespace PokeNET.Core.Data;

/// <summary>
/// Represents static data for a Pokemon move.
/// Contains all the immutable information about a move (e.g., Tackle, Thunderbolt).
/// </summary>
public class MoveData
{
    /// <summary>
    /// Unique move identifier/name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Move type (Normal, Fire, Water, etc.).
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Category (Physical, Special, Status).
    /// </summary>
    public MoveCategory Category { get; set; }

    /// <summary>
    /// Base power (0 for status moves).
    /// </summary>
    public int Power { get; set; }

    /// <summary>
    /// Accuracy percentage (0-100, 0 for moves that never miss).
    /// </summary>
    public int Accuracy { get; set; } = 100;

    /// <summary>
    /// Power Points - number of times the move can be used.
    /// </summary>
    public int PP { get; set; }

    /// <summary>
    /// Priority (-7 to +5, higher goes first).
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Target specification (SingleTarget, AllOpponents, User, etc.).
    /// </summary>
    public string Target { get; set; } = "SingleTarget";

    /// <summary>
    /// Chance of secondary effect triggering (0-100).
    /// </summary>
    public int EffectChance { get; set; }

    /// <summary>
    /// Move description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Special flags (Contact, Sound, Punch, Bite, etc.).
    /// </summary>
    public List<string> Flags { get; set; } = new();

    /// <summary>
    /// Path to the Roslyn script (.csx) that implements IMoveEffect.
    /// Example: "scripts/moves/burn.csx"
    /// </summary>
    public string? EffectScript { get; set; }

    /// <summary>
    /// Parameters to pass to the effect script (e.g., burnChance: 10).
    /// </summary>
    public Dictionary<string, object>? EffectParameters { get; set; }

    /// <summary>
    /// Indicates if this move makes contact with the target.
    /// </summary>
    public bool MakesContact => Flags.Contains("Contact");
}

/// <summary>
/// Move category classification.
/// </summary>
public enum MoveCategory
{
    /// <summary>Physical damage (uses Attack stat)</summary>
    Physical,

    /// <summary>Special damage (uses Special Attack stat)</summary>
    Special,

    /// <summary>No damage (status effects, stat changes, etc.)</summary>
    Status,
}
