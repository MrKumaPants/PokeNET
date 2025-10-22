using System;
using System.Collections.Generic;
using System.Linq;
using PokeNET.Domain.Modding;

namespace PokeNET.Core.Modding;

/// <summary>
/// Provides access to information about loaded mods.
/// </summary>
internal class ModRegistry : IModRegistry
{
    private readonly ModLoader _modLoader;

    public ModRegistry(ModLoader modLoader)
    {
        _modLoader = modLoader;
    }

    public IReadOnlyList<IModManifest> GetAllMods()
    {
        return _modLoader.LoadedMods;
    }

    public IModManifest? GetMod(string modId)
    {
        return _modLoader.LoadedMods.FirstOrDefault(m => m.Id == modId);
    }

    public bool IsModLoaded(string modId)
    {
        return _modLoader.IsModLoaded(modId);
    }

    public TApi? GetApi<TApi>(string modId) where TApi : class
    {
        var mod = _modLoader.GetMod(modId);
        return mod as TApi;
    }

    public IReadOnlyList<IModManifest> GetDependentMods(string modId)
    {
        return _modLoader.LoadedMods
            .Where(m => m.Dependencies.Any(d => d.ModId == modId))
            .ToList();
    }

    public IReadOnlyList<IModManifest> GetDependencies(string modId)
    {
        var mod = GetMod(modId);
        if (mod == null) return Array.Empty<IModManifest>();

        return mod.Dependencies
            .Select(d => GetMod(d.ModId))
            .Where(m => m != null)
            .Cast<IModManifest>()
            .ToList();
    }
}
