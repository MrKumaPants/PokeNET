using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Arch.Core;
using PokeNET.Core;
using PokeNET.Core.Assets;
using PokeNET.Core.ECS;
using PokeNET.Core.Modding;
using PokeNET.Domain.Assets;
using PokeNET.Domain.ECS.Events;
using PokeNET.Domain.ECS.Systems;
using PokeNET.Domain.Modding;

namespace PokeNET.DesktopGL;

/// <summary>
/// Entry point for the DesktopGL platform.
/// Configures dependency injection, logging, and bootstraps the game.
/// Follows the Dependency Inversion Principle - all dependencies injected via DI container.
/// </summary>
internal class Program
{
    /// <summary>
    /// The main entry point for the application.
    /// This creates the dependency injection host and runs the game.
    /// </summary>
    /// <param name="args">Command-line arguments passed to the application.</param>
    [STAThread]
    private static void Main(string[] args)
    {
        // Build the host with configuration, logging, and DI
        var host = CreateHostBuilder().Build();

        // Create and run the game
        using var game = host.Services.GetRequiredService<PokeNETGame>();
        game.Run();
    }

    /// <summary>
    /// Creates the host builder with all service registrations.
    /// This is the composition root following the Dependency Injection pattern.
    /// </summary>
    private static IHostBuilder CreateHostBuilder()
    {
        return Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((context, config) =>
            {
                // Load configuration from appsettings.json
                config.SetBasePath(AppDomain.CurrentDomain.BaseDirectory);
                config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                config.AddJsonFile(
                    $"appsettings.{context.HostingEnvironment.EnvironmentName}.json",
                    optional: true,
                    reloadOnChange: true);
                config.AddEnvironmentVariables();
            })
            .ConfigureLogging((context, logging) =>
            {
                logging.ClearProviders();
                logging.AddConfiguration(context.Configuration.GetSection("Logging"));

                // Add console logging with structured output
                logging.AddConsole();

                // Add debug output in development
                if (context.HostingEnvironment.IsDevelopment())
                {
                    logging.AddDebug();
                }

                // Set minimum log level based on environment
                logging.SetMinimumLevel(
                    context.HostingEnvironment.IsDevelopment()
                        ? LogLevel.Debug
                        : LogLevel.Information);
            })
            .ConfigureServices((context, services) =>
            {
                // Register core game services
                RegisterCoreServices(services, context.Configuration);

                // Register ECS services
                RegisterEcsServices(services);

                // Register asset management
                RegisterAssetServices(services, context.Configuration);

                // Register modding services
                RegisterModdingServices(services, context.Configuration);

                // Register the game itself
                services.AddSingleton<PokeNETGame>();
            })
            .UseConsoleLifetime();
    }

    /// <summary>
    /// Registers core game services like configuration and event bus.
    /// </summary>
    private static void RegisterCoreServices(IServiceCollection services, IConfiguration configuration)
    {
        // Event bus for system communication
        services.AddSingleton<IEventBus, EventBus>();

        // Configuration can be injected as IConfiguration
        services.AddSingleton(configuration);
    }

    /// <summary>
    /// Registers ECS-related services.
    /// </summary>
    private static void RegisterEcsServices(IServiceCollection services)
    {
        // System manager for coordinating all ECS systems
        services.AddSingleton<ISystemManager, SystemManager>();

        // World factory - creates new ECS world instances
        services.AddSingleton<World>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("Creating ECS World");
            return World.Create();
        });

        // TODO: Register individual systems as they are implemented
        // Example:
        // services.AddSingleton<ISystem, MovementSystem>();
        // services.AddSingleton<ISystem, RenderingSystem>();
    }

    /// <summary>
    /// Registers asset management services.
    /// </summary>
    private static void RegisterAssetServices(IServiceCollection services, IConfiguration configuration)
    {
        // Get the base asset path from configuration or use default
        var basePath = configuration["Assets:BasePath"] ?? "Content";

        // Register asset manager
        services.AddSingleton<IAssetManager>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<AssetManager>>();
            var manager = new AssetManager(logger, basePath);

            // TODO: Register asset loaders as they are implemented
            // Example:
            // manager.RegisterLoader(sp.GetRequiredService<IAssetLoader<Texture2D>>());

            return manager;
        });

        // TODO: Register individual asset loaders
        // Example:
        // services.AddSingleton<IAssetLoader<Texture2D>, TextureLoader>();
    }

    /// <summary>
    /// Registers modding services including mod loader and Harmony patcher.
    /// </summary>
    private static void RegisterModdingServices(IServiceCollection services, IConfiguration configuration)
    {
        // Get the mods directory from configuration or use default
        var modsDirectory = configuration["Mods:Directory"] ?? "Mods";

        // Register Harmony patcher
        services.AddSingleton<HarmonyPatcher>();

        // Register mod loader
        services.AddSingleton<IModLoader>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<ModLoader>>();
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            var loader = new ModLoader(logger, sp, loggerFactory, modsDirectory);

            // Discover and load mods at startup
            try
            {
                var discoveredCount = loader.DiscoverMods();
                if (discoveredCount > 0)
                {
                    logger.LogInformation("Discovered {Count} mods, loading...", discoveredCount);
                    loader.LoadMods();

                    // Update asset manager with mod paths
                    var assetManager = sp.GetRequiredService<IAssetManager>();
                    var modPaths = loader.LoadedMods
                        .Select(m => Path.Combine(modsDirectory, m.Id))
                        .ToList();
                    assetManager.SetModPaths(modPaths);
                }
                else
                {
                    logger.LogInformation("No mods found in {ModsDirectory}", modsDirectory);
                }
            }
            catch (ModLoadException ex)
            {
                logger.LogError(ex, "Failed to load mods. Game will start without mods.");
            }

            return loader;
        });
    }
}
