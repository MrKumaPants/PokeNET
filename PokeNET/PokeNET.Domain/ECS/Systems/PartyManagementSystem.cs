using System.Collections.Generic;
using System.Linq;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Extensions.Logging;
using PokeNET.Domain.ECS.Components;
using PokeNET.Domain.ECS.Relationships;

namespace PokeNET.Domain.ECS.Systems;

/// <summary>
/// System for managing Pokemon party operations using PokemonRelationships.
/// Replaces the old Party component with relationship-based party management.
///
/// Features:
/// - Add/remove Pokemon from trainer parties (max 6)
/// - Query party composition using world.GetParty(trainer)
/// - Manage held items using world.GiveHeldItem(pokemon, item)
/// - PC box storage management
/// - Bidirectional trainer-Pokemon ownership tracking
///
/// Architecture:
/// - Uses PokemonRelationships extension methods for all operations
/// - No Party component needed - relationships handle everything
/// - Supports efficient queries: GetParty, GetOwner, IsInParty
///
/// Performance:
/// - O(1) party size checks via CountRelationships
/// - O(n) party iteration where n = party size (max 6)
/// - Zero allocation for relationship queries
/// </summary>
public partial class PartyManagementSystem : BaseSystem<World, float>
{
    private readonly ILogger _logger;

    public PartyManagementSystem(World world, ILogger<PartyManagementSystem> logger)
        : base(world)
    {
        _logger = logger;
        _logger.LogInformation("PartyManagementSystem initialized with PokemonRelationships");
    }

    /// <summary>
    /// Update method for system (currently no per-frame logic needed).
    /// Party management is event-driven via direct method calls.
    /// </summary>
    public override void Update(in float deltaTime)
    {
        // Party management is handled via explicit method calls
        // No per-frame processing needed
    }

    #region Party Operations

    /// <summary>
    /// Adds a Pokemon to a trainer's party.
    /// Uses PokemonRelationships.AddToParty for relationship-based management.
    /// </summary>
    /// <param name="trainer">The trainer entity</param>
    /// <param name="pokemon">The Pokemon entity to add</param>
    /// <returns>True if added successfully, false if party is full</returns>
    public bool AddToParty(Entity trainer, Entity pokemon)
    {
        if (!World.IsAlive(trainer) || !World.IsAlive(pokemon))
        {
            _logger.LogWarning("Cannot add to party: Invalid trainer or pokemon entity");
            return false;
        }

        var success = World.AddToParty(trainer, pokemon);

        if (success)
        {
            var partySize = World.GetPartySize(trainer);
            _logger.LogInformation(
                "Added Pokemon {PokemonId} to Trainer {TrainerId} party (now {Size}/6)",
                pokemon.Id,
                trainer.Id,
                partySize
            );
        }
        else
        {
            _logger.LogWarning(
                "Failed to add Pokemon {PokemonId} to Trainer {TrainerId} - party is full",
                pokemon.Id,
                trainer.Id
            );
        }

        return success;
    }

    /// <summary>
    /// Removes a Pokemon from a trainer's party.
    /// Uses PokemonRelationships.RemoveFromParty.
    /// </summary>
    public bool RemoveFromParty(Entity trainer, Entity pokemon)
    {
        if (!World.IsAlive(trainer) || !World.IsAlive(pokemon))
        {
            _logger.LogWarning("Cannot remove from party: Invalid trainer or pokemon entity");
            return false;
        }

        var success = World.RemoveFromParty(trainer, pokemon);

        if (success)
        {
            var partySize = World.GetPartySize(trainer);
            _logger.LogInformation(
                "Removed Pokemon {PokemonId} from Trainer {TrainerId} party (now {Size}/6)",
                pokemon.Id,
                trainer.Id,
                partySize
            );
        }

        return success;
    }

    /// <summary>
    /// Gets all Pokemon in a trainer's party.
    /// Uses PokemonRelationships.GetParty for efficient relationship query.
    /// </summary>
    public IEnumerable<Entity> GetParty(Entity trainer)
    {
        if (!World.IsAlive(trainer))
        {
            _logger.LogWarning("Cannot get party: Invalid trainer entity");
            return Enumerable.Empty<Entity>();
        }

        return World.GetParty(trainer);
    }

    /// <summary>
    /// Gets the trainer who owns a specific Pokemon.
    /// Demonstrates bidirectional relationship query.
    /// </summary>
    public Entity? GetOwner(Entity pokemon)
    {
        if (!World.IsAlive(pokemon))
        {
            _logger.LogWarning("Cannot get owner: Invalid pokemon entity");
            return null;
        }

        return World.GetOwner(pokemon);
    }

    /// <summary>
    /// Checks if a trainer's party is full (6 Pokemon).
    /// </summary>
    public bool IsPartyFull(Entity trainer)
    {
        if (!World.IsAlive(trainer))
            return false;

        return World.IsPartyFull(trainer);
    }

    #endregion

    #region Item Management

    /// <summary>
    /// Gives an item to a Pokemon to hold.
    /// Uses PokemonRelationships.GiveHeldItem.
    /// </summary>
    public Entity? GiveHeldItem(Entity pokemon, Entity item)
    {
        if (!World.IsAlive(pokemon) || !World.IsAlive(item))
        {
            _logger.LogWarning("Cannot give held item: Invalid pokemon or item entity");
            return null;
        }

        var previousItem = World.GiveHeldItem(pokemon, item);

        if (previousItem.HasValue)
        {
            _logger.LogInformation(
                "Pokemon {PokemonId} replaced held item {OldItemId} with {NewItemId}",
                pokemon.Id,
                previousItem.Value.Id,
                item.Id
            );
        }
        else
        {
            _logger.LogInformation(
                "Pokemon {PokemonId} now holding item {ItemId}",
                pokemon.Id,
                item.Id
            );
        }

        return previousItem;
    }

    /// <summary>
    /// Takes the held item from a Pokemon.
    /// Uses PokemonRelationships.TakeHeldItem.
    /// </summary>
    public Entity? TakeHeldItem(Entity pokemon)
    {
        if (!World.IsAlive(pokemon))
        {
            _logger.LogWarning("Cannot take held item: Invalid pokemon entity");
            return null;
        }

        var heldItem = World.TakeHeldItem(pokemon);

        if (heldItem.HasValue)
        {
            _logger.LogInformation(
                "Took item {ItemId} from Pokemon {PokemonId}",
                heldItem.Value.Id,
                pokemon.Id
            );
        }

        return heldItem;
    }

    #endregion

    #region PC Storage

    /// <summary>
    /// Stores a Pokemon in a PC box.
    /// Removes from party if currently in party.
    /// </summary>
    public void StoreInBox(Entity pokemon, Entity box)
    {
        if (!World.IsAlive(pokemon) || !World.IsAlive(box))
        {
            _logger.LogWarning("Cannot store in box: Invalid pokemon or box entity");
            return;
        }

        World.StoreInBox(pokemon, box);
        _logger.LogInformation("Stored Pokemon {PokemonId} in box {BoxId}", pokemon.Id, box.Id);
    }

    /// <summary>
    /// Withdraws a Pokemon from a PC box and adds to trainer's party.
    /// </summary>
    public bool WithdrawFromBox(Entity pokemon, Entity trainer)
    {
        if (!World.IsAlive(pokemon) || !World.IsAlive(trainer))
        {
            _logger.LogWarning("Cannot withdraw from box: Invalid pokemon or trainer entity");
            return false;
        }

        var success = World.WithdrawFromBox(pokemon, trainer);

        if (success)
        {
            _logger.LogInformation(
                "Withdrew Pokemon {PokemonId} from box to Trainer {TrainerId} party",
                pokemon.Id,
                trainer.Id
            );
        }
        else
        {
            _logger.LogWarning(
                "Failed to withdraw Pokemon {PokemonId} - Trainer {TrainerId} party is full",
                pokemon.Id,
                trainer.Id
            );
        }

        return success;
    }

    #endregion
}
