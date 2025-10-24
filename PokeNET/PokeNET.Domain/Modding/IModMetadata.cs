namespace PokeNET.Domain.Modding;

/// <summary>
/// Descriptive metadata for mod discovery and display (ISP-compliant interface).
/// </summary>
/// <remarks>
/// Implement this interface when your component needs to display mod information
/// in UI, mod browsers, or documentation. This interface is primarily for
/// presentation and does not affect mod loading or execution.
/// </remarks>
public interface IModMetadata
{
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
    /// Localization support information.
    /// </summary>
    LocalizationConfiguration? Localization { get; }

    /// <summary>
    /// Additional custom metadata (mod-specific).
    /// </summary>
    IReadOnlyDictionary<string, object> Metadata { get; }
}
