using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PokeNET.Core.Data;
using PokeNET.Core.Modding;
using PokeNET.Scripting.Abstractions;
using PokeNET.Scripting.Services;

namespace PokeNET.CLI.Infrastructure;

/// <summary>
/// Configures services for the CLI application.
/// Provides reusable service registration methods.
/// </summary>
public static class ServiceConfiguration
{
    /// <summary>
    /// Registers data services (DataManager and loaders).
    /// </summary>
    public static IServiceCollection AddDataServices(
        this IServiceCollection services,
        string dataPath
    )
    {
        // Register GameDataContext with in-memory database provider
        services.AddDbContext<GameDataContext>(
            options =>
            {
                options.UseInMemoryDatabase($"PokeNET_CLI_GameData_{System.Guid.NewGuid()}");
                options.EnableSensitiveDataLogging(); // Enable detailed entity tracking information
                options.EnableDetailedErrors(); // Enable detailed error messages
            },
            ServiceLifetime.Singleton
        );

        // Register DataManager as IDataApi
        services.AddSingleton<IDataApi>(sp =>
        {
            var context = sp.GetRequiredService<GameDataContext>();
            var logger = sp.GetRequiredService<ILogger<DataManager>>();
            var manager = new DataManager(context, logger, dataPath);

            // Data will be loaded lazily on first access via EnsureDataLoadedAsync

            return manager;
        });

        return services;
    }

    /// <summary>
    /// Registers modding services (ModLoader).
    /// </summary>
    public static IServiceCollection AddModdingServices(
        this IServiceCollection services,
        string modsDirectory
    )
    {
        services.AddSingleton<IModLoader>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<ModLoader>>();
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            return new ModLoader(logger, sp, loggerFactory, modsDirectory);
        });

        return services;
    }

    /// <summary>
    /// Registers scripting services (ScriptingEngine).
    /// </summary>
    public static IServiceCollection AddScriptingServices(
        this IServiceCollection services,
        int maxCacheSize = 100
    )
    {
        // Register script loader
        services.AddSingleton<IScriptLoader>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<FileScriptLoader>>();
            return new FileScriptLoader(logger);
        });

        // Register scripting engine
        services.AddSingleton<IScriptingEngine>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<ScriptingEngine>>();
            var cacheLogger = sp.GetRequiredService<ILogger<ScriptCompilationCache>>();
            var scriptLoader = sp.GetRequiredService<IScriptLoader>();

            return new ScriptingEngine(logger, scriptLoader, cacheLogger, maxCacheSize);
        });

        return services;
    }

    /// <summary>
    /// Registers the CLI context.
    /// </summary>
    public static IServiceCollection AddCliContext(this IServiceCollection services)
    {
        services.AddSingleton<CliContext>();
        return services;
    }
}
