using System.Collections.Generic;

namespace PokeNET.Core.Data;

/// <summary>
/// Complete information about a Pokemon type including its matchups.
/// Loaded from types.json array.
/// </summary>
public class TypeData
{
    /// <summary>
    /// Unique type identifier.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Type name (e.g., "Fire", "Water").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Display color in hex format (e.g., "#F08030").
    /// </summary>
    public string Color { get; set; } = "#FFFFFF";

    /// <summary>
    /// Type effectiveness matchups.
    /// Key: Defender type name
    /// Value: Effectiveness multiplier (0.0, 0.5, 1.0, 2.0)
    /// </summary>
    public Dictionary<string, double> Matchups { get; set; } = new();

    /// <summary>
    /// Optional: Category or description of the type.
    /// </summary>
    public string? Description { get; set; }
}

