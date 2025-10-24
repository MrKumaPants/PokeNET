using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Arch.Core;
using Microsoft.Xna.Framework.Graphics;
using PokeNET.Core;
using PokeNET.Core.Assets;
using PokeNET.Core.Assets.Loaders;
using PokeNET.Core.ECS;
using PokeNET.Core.Modding;
using PokeNET.Domain.Assets;
using PokeNET.Domain.ECS.Events;
using PokeNET.Domain.ECS.Systems;
using PokeNET.Domain.Modding;
using PokeNET.Domain.Saving;
using PokeNET.Saving.Services;
using PokeNET.Saving.Serializers;
using PokeNET.Saving.Providers;
using PokeNET.Saving.Validators;
using PokeNET.Audio.DependencyInjection;
using PokeNET.Audio.Reactive;
using PokeNET.Scripting.Abstractions;
using PokeNET.Scripting.Services;

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

                // Register scripting services (Phase 5)
                RegisterScriptingServices(services, context.Configuration);

                // Register audio services (Day 9: Wire Up All Services)
                RegisterAudioServices(services, context.Configuration);

                // Register save system (Day 9: Wire Up All Services)
                RegisterSaveServices(services);

                // Register the game itself
                services.AddSingleton<PokeNETGame>();

                // Configure systems after all services are registered (Day 9)
                ConfigureSystemManager(services);
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

        // GraphicsDevice - provided by PokeNETGame after initialization (Day 9)
        // Will be set up via a factory that gets it from the Game instance
        services.AddSingleton(sp =>
        {
            var game = sp.GetRequiredService<PokeNETGame>();
            return game.GraphicsDevice;
        });
    }

    /// <summary>
    /// Registers ECS-related services.
    /// </summary>
    private static void RegisterEcsServices(IServiceCollection services)
    {
        // World factory - creates new ECS world instances
        services.AddSingleton<World>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("Creating ECS World");
            return World.Create();
        });

        // Register concrete systems (Day 9: Wire Up All Services)
        services.AddSingleton<ISystem>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<RenderSystem>>();
            var graphics = sp.GetRequiredService<GraphicsDevice>();
            return new RenderSystem(logger, graphics);
        });

        services.AddSingleton<ISystem>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<MovementSystem>>();
            var eventBus = sp.GetRequiredService<IEventBus>();
            return new MovementSystem(logger, eventBus);
        });

        services.AddSingleton<ISystem>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<BattleSystem>>();
            var eventBus = sp.GetRequiredService<IEventBus>();
            return new BattleSystem(logger, eventBus);
        });

        // System manager will be configured in ConfigureSystemManager with all registered systems
    }

    /// <summary>
    /// Registers asset management services.
    /// </summary>
    private static void RegisterAssetServices(IServiceCollection services, IConfiguration configuration)
    {
        // Get the base asset path from configuration or use default
        var basePath = configuration["Assets:BasePath"] ?? "Content";

        // Register asset loaders (Day 9: Wire Up All Services)
        RegisterAssetLoaders(services);

        // Register asset manager with loaders
        services.AddSingleton<IAssetManager>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<AssetManager>>();
            var manager = new AssetManager(logger, basePath);

            // Register texture loader
            var textureLoader = sp.GetRequiredService<TextureAssetLoader>();
            manager.RegisterLoader(textureLoader);

            // Register JSON loader for Pokemon data (example)
            // In real implementation, you'd register loaders for specific types
            // var pokemonDataLoader = sp.GetRequiredService<JsonAssetLoader<PokemonData>>();
            // manager.RegisterLoader(pokemonDataLoader);

            logger.LogInformation("AssetManager initialized with {LoaderCount} loaders", 1);
            return manager;
        });
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

    /// <summary>
    /// Registers scripting services including script engine, loaders, and API.
    /// Phase 5: Roslyn C# Scripting Engine
    /// </summary>
    private static void RegisterScriptingServices(IServiceCollection services, IConfiguration configuration)
    {
        // Get the scripts directory from configuration or use default
        var scriptsDirectory = configuration["Scripts:Directory"] ?? "Scripts";
        var maxCacheSize = configuration.GetValue<int?>("Scripts:MaxCacheSize") ?? 100;

        // Register script loader
        services.AddSingleton<IScriptLoader>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<FileScriptLoader>>();
            return new FileScriptLoader(logger);
        });

        // Register scripting engine with compilation cache
        services.AddSingleton<IScriptingEngine>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<ScriptingEngine>>();
            var cacheLogger = sp.GetRequiredService<ILogger<ScriptCompilationCache>>();
            var scriptLoader = sp.GetRequiredService<IScriptLoader>();

            var engine = new ScriptingEngine(logger, scriptLoader, cacheLogger, maxCacheSize);

            logger.LogInformation(
                "Scripting engine initialized: {EngineName} v{Version}, Cache Size: {CacheSize}, Hot Reload: {HotReload}",
                engine.EngineName,
                engine.EngineVersion,
                maxCacheSize,
                engine.SupportsHotReload);

            return engine;
        });

        // Register script context and API (Day 9: Uncommented)
//         services.AddScoped<IScriptContext, ScriptContext>();
//         services.AddScoped<IScriptApi, ScriptApi>();
    }

    /// <summary>
    /// Registers asset loaders for different file types.
    /// Day 9: Wire Up All Services
    /// </summary>
    private static void RegisterAssetLoaders(IServiceCollection services)
    {
        // Texture loader for PNG, JPG, BMP assets
        services.AddSingleton<TextureAssetLoader>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<TextureAssetLoader>>();
            var graphics = sp.GetRequiredService<GraphicsDevice>();
            return new TextureAssetLoader(logger, graphics);
        });

        // JSON loader - generic for any data type
        // Register specific instances as needed
        // Example: services.AddSingleton<JsonAssetLoader<PokemonData>>();
    }

    /// <summary>
    /// Registers audio services including reactive audio engine.
    /// Day 9: Wire Up All Services
    /// NOTE: Audio reactions (IAudioReaction) will be implemented in Day 6-7.
    /// </summary>
    private static void RegisterAudioServices(IServiceCollection services, IConfiguration configuration)
    {
        // Use extension method to register core audio services
        services.AddAudioServices();

        // Register reactive audio engine
        services.AddSingleton<ReactiveAudioEngine>();

        // TODO (Day 6-7): Register audio reactions when strategy pattern is implemented
        // services.AddSingleton<IAudioReaction, BattleStartReaction>();
        // services.AddSingleton<IAudioReaction, BattleEndReaction>();
        // services.AddSingleton<IAudioReaction, LowHealthReaction>();
        // services.AddSingleton<IAudioReaction, PokemonCaughtReaction>();
        // services.AddSingleton<IAudioReaction, LevelUpReaction>();
        // services.AddSingleton<IAudioReaction, WeatherReaction>();
    }

    /// <summary>
    /// Registers save system services.
    /// Day 9: Wire Up All Services
    /// </summary>
    private static void RegisterSaveServices(IServiceCollection services)
    {
        services.AddSingleton<ISaveSystem, SaveSystem>();
        services.AddSingleton<ISaveSerializer, JsonSaveSerializer>();
        services.AddSingleton<ISaveFileProvider, FileSystemSaveFileProvider>();
        services.AddSingleton<ISaveValidator, SaveValidator>();
        services.AddSingleton<IGameStateManager, GameStateManager>();
    }

    /// <summary>
    /// Configures the SystemManager with all registered systems.
    /// Day 9: Wire Up All Services
    /// This is called as a post-configuration step after the service provider is built.
    /// </summary>
    private static void ConfigureSystemManager(IServiceCollection services)
    {
        // Use BuildServiceProvider to get access to services and configure SystemManager
        // Note: This creates a temporary service provider for configuration
        services.AddSingleton<ISystemManager>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<SystemManager>>();
            var world = sp.GetRequiredService<World>();
            var systemManager = new SystemManager(logger);

            // Initialize with world
            systemManager.InitializeSystems(world);

            // Register all ISystem instances
            var systems = sp.GetServices<ISystem>();
            foreach (var system in systems)
            {
                systemManager.RegisterSystem(system);
            }

            logger.LogInformation("SystemManager configured with {Count} systems", systems.Count());
            return systemManager;
        });
    }
}
