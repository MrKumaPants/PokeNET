using Arch.Core;
using Arch.System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PokeNET.Core.ECS.Events;
using PokeNET.Core.ECS.Systems;

// RenderSystem moved to PokeNET.Core.ECS.Systems (C-1: Architecture fix)
// WorldPersistenceService moved to PokeNET.Core.ECS.Persistence (C-2: Architecture fix)
// WorldPersistenceService registration moved to Core layer DI (Domain cannot reference Core)

namespace PokeNET.Core.DependencyInjection;

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

        // NOTE: Persistence services (WorldPersistenceService) moved to Core layer
        // Register in CoreServiceCollectionExtensions or Program.cs instead

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
