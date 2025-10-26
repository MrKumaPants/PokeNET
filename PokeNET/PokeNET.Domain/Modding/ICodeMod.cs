using System.Collections.Generic;

namespace PokeNET.Domain.Modding;

/// <summary>
/// Code execution and assembly loading for code-based mods (ISP-compliant interface).
/// </summary>
/// <remarks>
/// Implement this interface when your component needs to load assemblies,
/// execute mod code, or apply Harmony patches. Data-only and content-only
/// mods do not need to implement this interface.
/// </remarks>
public interface ICodeMod
{
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
}
