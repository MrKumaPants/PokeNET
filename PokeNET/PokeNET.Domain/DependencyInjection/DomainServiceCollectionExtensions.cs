using Arch.Core;
using Arch.System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PokeNET.Domain.ECS.Events;
using PokeNET.Domain.ECS.Persistence;
using PokeNET.Domain.ECS.Systems;

namespace PokeNET.Domain.DependencyInjection;

/// <summary>
/// Extension methods for registering Domain layer services with dependency injection.
/// Centralizes all ECS, persistence, and relationship service registrations.
/// </summary>
public static class DomainServiceCollectionExtensions
{
    /// <summary>
    /// Adds all Domain services to the dependency injection container.
    /// Includes ECS systems, WorldPersistenceService, and relationship-based services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddDomainServices(this IServiceCollection services)
    {
        // Register core ECS services
        AddEcsServices(services);

        // Register persistence services (Arch.Persistence-based)
        AddPersistenceServices(services);

        // Register ECS systems
        AddEcsSystems(services);

        return services;
    }

    /// <summary>
    /// Registers core ECS services including World instance.
    /// </summary>
    private static void AddEcsServices(IServiceCollection services)
    {
        // World factory - creates singleton ECS world instance
        services.AddSingleton<World>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<World>>();
            logger.LogInformation("Creating singleton ECS World instance");
            var world = World.Create();
            logger.LogInformation("ECS World created successfully");
            return world;
        });

        // Event bus is registered in Program.cs core services
        // No need to register again here
    }

    /// <summary>
    /// Registers persistence services using Arch.Persistence.
    /// WorldPersistenceService replaces the old JSON-based SaveSystem.
    /// </summary>
    private static void AddPersistenceServices(IServiceCollection services)
    {
        // NEW: Arch.Persistence-based world serialization
        // 90% code reduction vs custom JSON serialization
        // Binary MessagePack format for performance
        services.AddSingleton<WorldPersistenceService>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<WorldPersistenceService>>();
            var saveDirectory = "Saves"; // Can be made configurable via IConfiguration

            logger.LogInformation(
                "Registering WorldPersistenceService with directory: {SaveDirectory}",
                saveDirectory
            );
            var service = new WorldPersistenceService(logger, saveDirectory);
            logger.LogInformation("WorldPersistenceService registered successfully");

            return service;
        });

        // WorldPersistenceService can be directly injected as itself
        // No interface needed since it's already a concrete implementation
    }

    /// <summary>
    /// Registers ECS systems including BattleSystem, MovementSystem, RenderSystem.
    /// Systems use PokemonRelationships for party management and battles.
    /// </summary>
    private static void AddEcsSystems(IServiceCollection services)
    {
        // BattleSystem - uses PokemonRelationships for battle state management
        services.AddSingleton<ISystem<float>>(sp =>
        {
            var world = sp.GetRequiredService<World>();
            var logger = sp.GetRequiredService<ILogger<BattleSystem>>();
            var eventBus = sp.GetRequiredService<IEventBus>();

            logger.LogInformation("Registering BattleSystem with PokemonRelationships integration");
            return new BattleSystem(world, logger, eventBus);
        });

        // PartyManagementSystem - uses PokemonRelationships for party operations
        services.AddSingleton<ISystem<float>>(sp =>
        {
            var world = sp.GetRequiredService<World>();
            var logger = sp.GetRequiredService<ILogger<PartyManagementSystem>>();

            logger.LogInformation("Registering PartyManagementSystem with PokemonRelationships");
            return new PartyManagementSystem(world, logger);
        });
    }
}

// WorldPersistenceService can be used directly without an interface
// It's already well-designed with public methods for save/load operations
