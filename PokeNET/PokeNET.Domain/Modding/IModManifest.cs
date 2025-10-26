using System;
using System.Collections.Generic;

namespace PokeNET.Domain.Modding;

/// <summary>
/// Complete mod manifest interface combining all capabilities (ISP-compliant).
/// </summary>
/// <remarks>
/// <para>
/// This interface inherits from all focused interfaces, providing backwards compatibility
/// while following the Interface Segregation Principle (ISP). Components should depend
/// on the specific interfaces they need rather than this combined interface:
/// </para>
/// <list type="bullet">
/// <item><description><see cref="IModManifestCore"/> - Core identity (Id, Name, Version, ApiVersion)</description></item>
/// <item><description><see cref="IModMetadata"/> - Descriptive metadata (Author, Description, Tags, etc.)</description></item>
/// <item><description><see cref="IModDependencies"/> - Dependency and load order management</description></item>
/// <item><description><see cref="ICodeMod"/> - Code execution and assembly loading</description></item>
/// <item><description><see cref="IContentMod"/> - Asset and content management</description></item>
/// <item><description><see cref="IModSecurity"/> - Security and trust verification</description></item>
/// </list>
/// <para>
/// The manifest is loaded and validated before mod initialization.
/// </para>
/// </remarks>
/// <example>
/// Prefer focused interfaces for better ISP compliance:
/// <code>
/// // ❌ Bad: Depends on entire interface when only needing core properties
/// void DisplayModInfo(IModManifest manifest) { ... }
///
/// // ✅ Good: Depends only on what's needed
/// void DisplayModInfo(IModManifestCore core, IModMetadata metadata) { ... }
/// void ResolveLoadOrder(IEnumerable&lt;IModDependencies&gt; mods) { ... }
/// void LoadCodeMod(IModManifestCore core, ICodeMod codeMod) { ... }
/// </code>
/// </example>
public interface IModManifest
    : IModManifestCore,
        IModMetadata,
        IModDependencies,
        ICodeMod,
        IContentMod,
        IModSecurity
{
    /// <summary>
    /// Directory path where the mod is located.
    /// </summary>
    /// <remarks>
    /// This is a runtime property set by the ModLoader and is not part of the
    /// serialized manifest file. It provides the absolute path to the mod's directory
    /// for loading assets and additional files.
    /// </remarks>
    string Directory { get; }
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
            BuildMetadata = parts.Length > 2 ? parts[2] : null,
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
    Hybrid,
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
    Untrusted,
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
    Mature,
}
