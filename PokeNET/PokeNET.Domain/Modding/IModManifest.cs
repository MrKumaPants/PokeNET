namespace PokeNET.Domain.Modding;

/// <summary>
/// Represents the metadata and configuration for a mod, parsed from modinfo.json.
/// </summary>
/// <remarks>
/// This interface provides read-only access to mod metadata. The manifest is
/// loaded and validated before mod initialization.
/// </remarks>
public interface IModManifest
{
    /// <summary>
    /// Unique identifier for the mod (e.g., "com.example.mymod").
    /// </summary>
    /// <remarks>
    /// This should follow reverse domain notation and be lowercase.
    /// It is used for dependency resolution and as the default Harmony instance ID.
    /// </remarks>
    string Id { get; }

    /// <summary>
    /// Human-readable name of the mod.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Mod version following semantic versioning (e.g., "1.2.3").
    /// </summary>
    ModVersion Version { get; }

    /// <summary>
    /// Required PokeNET.ModApi version (supports version ranges like "^1.0.0").
    /// </summary>
    string ApiVersion { get; }

    /// <summary>
    /// Author name or organization.
    /// </summary>
    string? Author { get; }

    /// <summary>
    /// Brief description of mod functionality.
    /// </summary>
    string? Description { get; }

    /// <summary>
    /// URL to mod's homepage or repository.
    /// </summary>
    Uri? Homepage { get; }

    /// <summary>
    /// SPDX license identifier (e.g., "MIT", "GPL-3.0").
    /// </summary>
    string? License { get; }

    /// <summary>
    /// Tags for categorizing the mod (e.g., "creatures", "balance", "ui").
    /// </summary>
    IReadOnlyList<string> Tags { get; }

    /// <summary>
    /// Required mods that must be loaded before this mod.
    /// </summary>
    IReadOnlyList<ModDependency> Dependencies { get; }

    /// <summary>
    /// Mods that should be loaded before this mod (soft dependency).
    /// </summary>
    /// <remarks>
    /// If the specified mods are present, they will be loaded before this mod.
    /// If they are not present, the mod will still load.
    /// </remarks>
    IReadOnlyList<string> LoadAfter { get; }

    /// <summary>
    /// Mods that should be loaded after this mod.
    /// </summary>
    IReadOnlyList<string> LoadBefore { get; }

    /// <summary>
    /// Mods that cannot be loaded together with this mod.
    /// </summary>
    IReadOnlyList<ModIncompatibility> IncompatibleWith { get; }

    /// <summary>
    /// Primary type of mod (data, content, code, or hybrid).
    /// </summary>
    ModType ModType { get; }

    /// <summary>
    /// Fully qualified name of the mod's entry class (for code mods).
    /// </summary>
    /// <remarks>
    /// Must be a class that implements <see cref="IMod"/>.
    /// Example: "MyMod.ModEntry"
    /// </remarks>
    string? EntryPoint { get; }

    /// <summary>
    /// Additional assemblies to load (for code mods).
    /// </summary>
    IReadOnlyList<string> Assemblies { get; }

    /// <summary>
    /// Custom Harmony instance ID (defaults to mod ID if not specified).
    /// </summary>
    string? HarmonyId { get; }

    /// <summary>
    /// Custom asset directory mappings.
    /// </summary>
    AssetPathConfiguration AssetPaths { get; }

    /// <summary>
    /// Assets to preload during mod initialization.
    /// </summary>
    IReadOnlyList<string> Preload { get; }

    /// <summary>
    /// Security trust level for the mod.
    /// </summary>
    ModTrustLevel TrustLevel { get; }

    /// <summary>
    /// SHA256 checksum of mod assembly (for verification).
    /// </summary>
    string? Checksum { get; }

    /// <summary>
    /// Age-appropriate content rating.
    /// </summary>
    ContentRating ContentRating { get; }

    /// <summary>
    /// Localization support information.
    /// </summary>
    LocalizationConfiguration? Localization { get; }

    /// <summary>
    /// Directory path where the mod is located.
    /// </summary>
    string Directory { get; }

    /// <summary>
    /// Additional custom metadata (mod-specific).
    /// </summary>
    IReadOnlyDictionary<string, object> Metadata { get; }
}

/// <summary>
/// Represents a mod dependency with version requirements.
/// </summary>
public record ModDependency
{
    /// <summary>
    /// ID of the required mod.
    /// </summary>
    public required string ModId { get; init; }

    /// <summary>
    /// Required version (supports ranges like "^1.0.0", ">=2.1.0").
    /// </summary>
    public string? Version { get; init; }

    /// <summary>
    /// Whether this dependency is optional.
    /// </summary>
    public bool Optional { get; init; }
}

/// <summary>
/// Represents an incompatibility with another mod.
/// </summary>
public record ModIncompatibility
{
    /// <summary>
    /// ID of the incompatible mod.
    /// </summary>
    public required string ModId { get; init; }

    /// <summary>
    /// Explanation of incompatibility.
    /// </summary>
    public string? Reason { get; init; }
}

/// <summary>
/// Semantic version representation for mods.
/// </summary>
public record ModVersion
{
    /// <summary>
    /// Major version number (breaking changes).
    /// </summary>
    public required int Major { get; init; }

    /// <summary>
    /// Minor version number (backward-compatible features).
    /// </summary>
    public required int Minor { get; init; }

    /// <summary>
    /// Patch version number (backward-compatible bug fixes).
    /// </summary>
    public required int Patch { get; init; }

    /// <summary>
    /// Pre-release label (e.g., "beta.1", "rc.2").
    /// </summary>
    public string? PreRelease { get; init; }

    /// <summary>
    /// Build metadata (e.g., "20231015").
    /// </summary>
    public string? BuildMetadata { get; init; }

    /// <summary>
    /// Gets the version as a string (e.g., "1.2.3-beta.1+20231015").
    /// </summary>
    public override string ToString()
    {
        var version = $"{Major}.{Minor}.{Patch}";
        if (!string.IsNullOrEmpty(PreRelease))
            version += $"-{PreRelease}";
        if (!string.IsNullOrEmpty(BuildMetadata))
            version += $"+{BuildMetadata}";
        return version;
    }

    /// <summary>
    /// Parses a semantic version string.
    /// </summary>
    public static ModVersion Parse(string version)
    {
        // Simplified parsing - full implementation would use regex from schema
        var parts = version.Split(['-', '+'], StringSplitOptions.RemoveEmptyEntries);
        var versionParts = parts[0].Split('.');

        return new ModVersion
        {
            Major = int.Parse(versionParts[0]),
            Minor = int.Parse(versionParts[1]),
            Patch = int.Parse(versionParts[2]),
            PreRelease = parts.Length > 1 ? parts[1] : null,
            BuildMetadata = parts.Length > 2 ? parts[2] : null
        };
    }
}

/// <summary>
/// Asset directory path configuration.
/// </summary>
public record AssetPathConfiguration
{
    /// <summary>
    /// Path to texture assets (default: "Assets/Textures").
    /// </summary>
    public string Textures { get; init; } = "Assets/Textures";

    /// <summary>
    /// Path to audio assets (default: "Assets/Audio").
    /// </summary>
    public string Audio { get; init; } = "Assets/Audio";

    /// <summary>
    /// Path to data files (default: "Data").
    /// </summary>
    public string Data { get; init; } = "Data";
}

/// <summary>
/// Localization configuration.
/// </summary>
public record LocalizationConfiguration
{
    /// <summary>
    /// List of supported language codes (e.g., "en", "en-US", "ja").
    /// </summary>
    public required IReadOnlyList<string> SupportedLanguages { get; init; }

    /// <summary>
    /// Default language code (default: "en").
    /// </summary>
    public string DefaultLanguage { get; init; } = "en";
}

/// <summary>
/// Primary mod type classification.
/// </summary>
public enum ModType
{
    /// <summary>
    /// Mod only contains JSON/XML data files.
    /// </summary>
    Data,

    /// <summary>
    /// Mod only contains art, audio, or other media assets.
    /// </summary>
    Content,

    /// <summary>
    /// Mod contains compiled code (.dll) and potentially Harmony patches.
    /// </summary>
    Code,

    /// <summary>
    /// Mod contains a combination of data, content, and code.
    /// </summary>
    Hybrid
}

/// <summary>
/// Security trust level for mods.
/// </summary>
public enum ModTrustLevel
{
    /// <summary>
    /// Data/content only mods (no code execution).
    /// </summary>
    Safe,

    /// <summary>
    /// Code mod from a trusted source (verified signature).
    /// </summary>
    Trusted,

    /// <summary>
    /// Code mod from unknown source (requires user consent).
    /// </summary>
    Untrusted
}

/// <summary>
/// Age-appropriate content rating.
/// </summary>
public enum ContentRating
{
    /// <summary>
    /// Suitable for all ages.
    /// </summary>
    Everyone,

    /// <summary>
    /// Suitable for ages 13+.
    /// </summary>
    Teen,

    /// <summary>
    /// Suitable for ages 17+.
    /// </summary>
    Mature
}
