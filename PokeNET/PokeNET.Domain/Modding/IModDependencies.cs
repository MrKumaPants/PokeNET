namespace PokeNET.Domain.Modding;

/// <summary>
/// Dependency and load order management for mods (ISP-compliant interface).
/// </summary>
/// <remarks>
/// Implement this interface when your component needs to resolve mod load order,
/// validate dependencies, or detect conflicts. This interface is primarily used
/// by the ModLoader during initialization.
/// </remarks>
public interface IModDependencies
{
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
}
