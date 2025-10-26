namespace PokeNET.Core.Modding;

/// <summary>
/// Core identity properties required by all mods (ISP-compliant interface).
/// </summary>
/// <remarks>
/// This interface represents the minimal required information for any mod.
/// All mods must provide: Id, Name, Version, and ApiVersion.
/// Use more specific interfaces (IModMetadata, IModDependencies, etc.) for additional capabilities.
/// </remarks>
public interface IModManifestCore
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
}
