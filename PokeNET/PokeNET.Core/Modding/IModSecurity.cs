namespace PokeNET.Core.Modding;

/// <summary>
/// Security and trust verification for mods (ISP-compliant interface).
/// </summary>
/// <remarks>
/// Implement this interface when your component needs to verify mod integrity,
/// enforce sandboxing, or validate trust levels. This interface is primarily
/// used by the security system and mod loader validation.
/// </remarks>
public interface IModSecurity
{
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
}
