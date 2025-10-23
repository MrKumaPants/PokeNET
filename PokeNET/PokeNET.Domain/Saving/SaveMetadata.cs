namespace PokeNET.Domain.Saving;

/// <summary>
/// Metadata about a save file, stored separately for quick access.
/// Displayed in save/load menus without loading full save data.
/// </summary>
public class SaveMetadata
{
    /// <summary>Save slot identifier.</summary>
    public string SlotId { get; set; } = null!;

    /// <summary>Player name in this save.</summary>
    public string PlayerName { get; set; } = null!;

    /// <summary>Player's current map/location.</summary>
    public string CurrentLocation { get; set; } = null!;

    /// <summary>Total playtime in seconds.</summary>
    public long PlaytimeSeconds { get; set; }

    /// <summary>Formatted playtime for display (e.g., "12:34:56").</summary>
    public string PlaytimeFormatted => TimeSpan.FromSeconds(PlaytimeSeconds).ToString(@"hh\:mm\:ss");

    /// <summary>Number of Pokemon in party.</summary>
    public int PartyCount { get; set; }

    /// <summary>Number of badges earned.</summary>
    public int BadgeCount { get; set; }

    /// <summary>Pokedex caught count.</summary>
    public int PokedexCaught { get; set; }

    /// <summary>Pokedex seen count.</summary>
    public int PokedexSeen { get; set; }

    /// <summary>Save file creation timestamp.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Last modified timestamp.</summary>
    public DateTime LastModified { get; set; }

    /// <summary>Save file version.</summary>
    public Version SaveVersion { get; set; } = null!;

    /// <summary>Optional description for this save.</summary>
    public string? Description { get; set; }

    /// <summary>File size in bytes.</summary>
    public long FileSizeBytes { get; set; }

    /// <summary>Whether this save is corrupted or invalid.</summary>
    public bool IsCorrupted { get; set; }

    /// <summary>Whether this save needs migration to current version.</summary>
    public bool RequiresMigration { get; set; }

    /// <summary>Quick summary for UI display.</summary>
    public override string ToString()
    {
        return $"{PlayerName} - {CurrentLocation} - Badges: {BadgeCount} - Playtime: {PlaytimeFormatted}";
    }
}
