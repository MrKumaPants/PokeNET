using System.Text.Json.Serialization;

namespace PokeNET.ModAPI.DTOs;

/// <summary>
/// Represents metadata for a mod package.
/// </summary>
/// <remarks>
/// This information is typically loaded from a mod.json file in the mod directory.
/// All mods must provide valid metadata for proper loading and dependency resolution.
/// </remarks>
/// <example>
/// <code>
/// // mod.json
/// {
///   "id": "mymod",
///   "name": "My Awesome Mod",
///   "version": "1.0.0",
///   "author": "ModAuthor",
///   "description": "Adds cool features to PokeNET",
///   "dependencies": ["pokenet.core@^0.1.0"]
/// }
/// </code>
/// </example>
[Serializable]
public class ModMetadata
{
    /// <summary>
    /// Gets or sets the unique mod identifier.
    /// </summary>
    /// <remarks>
    /// Must be lowercase, alphanumeric with underscores only (e.g., "my_mod").
    /// Used for asset namespacing and dependency resolution.
    /// </remarks>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the human-readable mod name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the semantic version (e.g., "1.0.0").
    /// </summary>
    /// <remarks>
    /// Must follow semantic versioning: MAJOR.MINOR.PATCH
    /// </remarks>
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the mod author name.
    /// </summary>
    [JsonPropertyName("author")]
    public string Author { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the mod description.
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of mod dependencies.
    /// </summary>
    /// <remarks>
    /// Format: "modid@version" where version can use semver ranges (^, ~, >=, etc.)
    /// Example: ["pokenet.core@^0.1.0", "other_mod@>=1.2.0"]
    /// </remarks>
    [JsonPropertyName("dependencies")]
    public List<string> Dependencies { get; set; } = new();
}
