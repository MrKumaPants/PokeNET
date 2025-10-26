using System.Collections.Generic;

namespace PokeNET.Domain.Modding;

/// <summary>
/// Asset and content management for content-based mods (ISP-compliant interface).
/// </summary>
/// <remarks>
/// Implement this interface when your component needs to load textures,
/// audio, data files, or other game assets. This interface is primarily used
/// by the asset loading system and content mods.
/// </remarks>
public interface IContentMod
{
    /// <summary>
    /// Custom asset directory mappings.
    /// </summary>
    AssetPathConfiguration AssetPaths { get; }

    /// <summary>
    /// Assets to preload during mod initialization.
    /// </summary>
    IReadOnlyList<string> Preload { get; }
}
