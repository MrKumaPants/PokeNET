using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PokeNET.CLI.Infrastructure;
using PokeNET.CLI.Interactive;
using Spectre.Console.Cli;

namespace PokeNET.CLI;

/// <summary>
/// Entry point for the PokeNET CLI application.
/// Supports both command-line mode and interactive menu mode.
/// </summary>
internal class Program
{
    private static async Task<int> Main(string[] args)
    {
        // Build configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables()
            .Build();

        // Build service collection
        var services = new ServiceCollection();
        ConfigureServices(services, configuration);

        // Build service provider
        var serviceProvider = services.BuildServiceProvider();

        try
        {
            // If no arguments provided, run interactive mode
            if (args.Length == 0)
            {
                var menu = serviceProvider.GetRequiredService<InteractiveMenu>();
                await menu.RunAsync();
                return 0;
            }

            // Otherwise, run command mode
            var registrar = new TypeRegistrar(services);
            var app = new CommandApp(registrar);
            ConfigureCommands(app);

            return await app.RunAsync(args);
        }
        catch (Exception ex)
        {
            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "Unhandled exception");
            Console.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Add logging
        services.AddLogging(builder =>
        {
            builder.AddConfiguration(configuration.GetSection("Logging"));
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Warning); // Reduce noise in CLI
        });

        // Add configuration
        services.AddSingleton(configuration);

        // Get paths from configuration
        var dataPath = configuration["Data:Path"] ?? "Data";
        var modsDirectory = configuration["Mods:Directory"] ?? "Mods";
        var scriptCacheSize = configuration.GetValue<int?>("Scripts:MaxCacheSize") ?? 100;

        // Register PokeNET services
        services.AddDataServices(dataPath);
        services.AddModdingServices(modsDirectory);
        services.AddScriptingServices(scriptCacheSize);
        services.AddCliContext();

        // Register interactive menu
        services.AddSingleton<InteractiveMenu>();
    }

    private static void ConfigureCommands(CommandApp app)
    {
        app.Configure(config =>
        {
            config.SetApplicationName("pokenet-cli");
            config.PropagateExceptions();

            // Data commands
            config.AddBranch(
                "data",
                data =>
                {
                    data.SetDescription("Browse and inspect game data");

                    data.AddCommand<Commands.Data.ListSpeciesCommand>("list-species")
                        .WithDescription("List all species with optional filters");
                    data.AddCommand<Commands.Data.ListMovesCommand>("list-moves")
                        .WithDescription("List all moves with optional filters");
                    data.AddCommand<Commands.Data.ListItemsCommand>("list-items")
                        .WithDescription("List all items with optional filters");

                    data.AddBranch(
                        "show",
                        show =>
                        {
                            show.SetDescription("Show detailed information");

                            show.AddCommand<Commands.Data.ShowSpeciesCommand>("species")
                                .WithDescription("Show species details");

                            show.AddCommand<Commands.Data.ShowMoveCommand>("move")
                                .WithDescription("Show move details");

                            show.AddCommand<Commands.Data.ShowItemCommand>("item")
                                .WithDescription("Show item details");

                            show.AddCommand<Commands.Data.ShowTypeCommand>("type")
                                .WithDescription("Show type effectiveness chart");
                        }
                    );
                }
            );

            // Battle commands
            config.AddBranch(
                "battle",
                battle =>
                {
                    battle.SetDescription("Battle simulation and calculations");

                    battle
                        .AddCommand<Commands.Battle.CalculateStatsCommand>("stats")
                        .WithDescription("Calculate Pokemon stats with IVs/EVs/Nature");

                    battle
                        .AddCommand<Commands.Battle.TypeEffectivenessCommand>("effectiveness")
                        .WithDescription("Calculate type effectiveness multiplier");
                }
            );

            // System commands
            config.AddBranch(
                "system",
                system =>
                {
                    system.SetDescription("System tests and benchmarks");

                    system.AddBranch(
                        "test",
                        test =>
                        {
                            test.SetDescription("Run system tests");

                            test.AddCommand<Commands.System.TestDataCommand>("data")
                                .WithDescription("Test data loading and validation");
                        }
                    );
                }
            );

            // Mod commands
            config.AddBranch(
                "mod",
                mod =>
                {
                    mod.SetDescription("Mod management and validation");

                    mod.AddCommand<Commands.Mod.ListModsCommand>("list")
                        .WithDescription("List all discovered mods");

                    mod.AddCommand<Commands.Mod.ValidateModCommand>("validate")
                        .WithDescription("Validate mod manifests and dependencies");
                }
            );
        });
    }
}

/// <summary>
/// Type registrar for Spectre.Console.Cli that integrates with Microsoft.Extensions.DependencyInjection.
/// </summary>
internal sealed class TypeRegistrar : ITypeRegistrar
{
    private readonly IServiceCollection _services;

    public TypeRegistrar(IServiceCollection services)
    {
        _services = services;
    }

    public ITypeResolver Build()
    {
        return new TypeResolver(_services.BuildServiceProvider());
    }

    public void Register(Type service, Type implementation)
    {
        _services.AddSingleton(service, implementation);
    }

    public void RegisterInstance(Type service, object implementation)
    {
        _services.AddSingleton(service, implementation);
    }

    public void RegisterLazy(Type service, Func<object> factory)
    {
        _services.AddSingleton(service, _ => factory());
    }
}

/// <summary>
/// Type resolver for Spectre.Console.Cli that resolves types from the service provider.
/// </summary>
internal sealed class TypeResolver : ITypeResolver
{
    private readonly IServiceProvider _provider;

    public TypeResolver(IServiceProvider provider)
    {
        _provider = provider;
    }

    public object? Resolve(Type? type)
    {
        return type == null ? null : _provider.GetService(type);
    }
}
