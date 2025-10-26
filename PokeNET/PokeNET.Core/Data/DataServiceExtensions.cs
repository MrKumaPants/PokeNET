using System;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using PokeNET.Core.Data;

namespace PokeNET.Core.Data;

/// <summary>
/// Dependency injection extensions for data services.
/// </summary>
public static class DataServiceExtensions
{
    /// <summary>
    /// Registers the DataManager and IDataApi services.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="dataPath">Path to game data JSON files (defaults to "Content/Data").</param>
    /// <returns>Service collection for chaining.</returns>
    public static IServiceCollection AddDataServices(
        this IServiceCollection services,
        string? dataPath = null
    )
    {
        // Use default path if not provided
        dataPath ??= Path.Combine("Content", "Data");

        // Register DataManager as singleton (data is shared across application)
        services.AddSingleton<IDataApi>(serviceProvider =>
        {
            var logger =
                serviceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<DataManager>>();
            return new DataManager(logger, dataPath);
        });

        return services;
    }

    /// <summary>
    /// Configures mod data paths for the data manager.
    /// Call this after AddDataServices() to enable mod support.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="modDataPaths">Array of mod data directory paths.</param>
    /// <returns>Service collection for chaining.</returns>
    public static IServiceCollection ConfigureModDataPaths(
        this IServiceCollection services,
        params string[] modDataPaths
    )
    {
        // This will be called during service provider build
        services.AddSingleton<IModDataPathConfigurator>(new ModDataPathConfigurator(modDataPaths));

        return services;
    }
}

/// <summary>
/// Internal helper for configuring mod paths after DI container is built.
/// </summary>
internal interface IModDataPathConfigurator
{
    void Configure(IDataApi dataApi);
}

internal class ModDataPathConfigurator : IModDataPathConfigurator
{
    private readonly string[] _modDataPaths;

    public ModDataPathConfigurator(string[] modDataPaths)
    {
        _modDataPaths = modDataPaths;
    }

    public void Configure(IDataApi dataApi)
    {
        if (dataApi is DataManager dataManager)
        {
            dataManager.SetModDataPaths(_modDataPaths);
        }
    }
}
