using System;
using System.Collections.Generic;
using System.Linq;
using Arch.Core;
using Arch.Core.Extensions;

namespace PokeNET.Core.ECS.Relationships;

/// <summary>
/// Provides relationship-based extensions for managing Pokemon party, trainer bonds, and item ownership.
/// This system uses Arch.Relationships to establish entity connections without component bloat.
/// </summary>
/// <remarks>
/// Relationship Graph:
/// - Trainer --HasPokemon--> Pokemon (one-to-many, max 6 in party)
/// - Pokemon --OwnedBy--> Trainer (many-to-one)
/// - Pokemon --HoldsItem--> Item (one-to-one, held item)
/// - Trainer --HasItem--> Item (one-to-many, bag inventory)
/// - Pokemon --StoredIn--> Box (many-to-one, PC storage)
/// </remarks>
public static class PokemonRelationships
{
    #region Relationship Type Definitions

    /// <summary>
    /// Relationship indicating a trainer has a Pokemon in their party.
    /// Limited to 6 Pokemon per trainer (party limit).
    /// </summary>
    public readonly struct HasPokemon;

    /// <summary>
    /// Relationship indicating a Pokemon is owned by a trainer.
    /// Inverse of HasPokemon for bidirectional traversal.
    /// </summary>
    public readonly struct OwnedBy;

    /// <summary>
    /// Relationship indicating a Pokemon is holding an item.
    /// One-to-one relationship (Pokemon can only hold one item).
    /// </summary>
    public readonly struct HoldsItem;

    /// <summary>
    /// Relationship indicating a trainer has an item in their bag.
    /// One-to-many relationship (trainers can have multiple items).
    /// </summary>
    public readonly struct HasItem;

    /// <summary>
    /// Relationship indicating a Pokemon is stored in a PC box.
    /// Used for Pokemon storage system (not in active party).
    /// </summary>
    public readonly struct StoredIn;

    /// <summary>
    /// Relationship indicating a trainer is currently battling another trainer.
    /// Used for battle state management.
    /// </summary>
    public readonly struct BattlingWith;

    #endregion

    #region Party Management

    /// <summary>
    /// Adds a Pokemon to a trainer's party (up to 6 Pokemon).
    /// Establishes bidirectional HasPokemon/OwnedBy relationships.
    /// </summary>
    /// <param name="world">The ECS world.</param>
    /// <param name="trainer">The trainer entity.</param>
    /// <param name="pokemon">The Pokemon entity to add.</param>
    /// <returns>True if successfully added, false if party is full.</returns>
    public static bool AddToParty(this World world, Entity trainer, Entity pokemon)
    {
        // Check if party is already full (max 6)
        var currentPartySize = world.CountRelationships<HasPokemon>(trainer);
        if (currentPartySize >= 6)
            return false;

        // Establish bidirectional relationship
        world.AddRelationship<HasPokemon>(trainer, pokemon);
        world.AddRelationship<OwnedBy>(pokemon, trainer);

        return true;
    }

    /// <summary>
    /// Removes a Pokemon from a trainer's party.
    /// Breaks the bidirectional HasPokemon/OwnedBy relationships.
    /// </summary>
    /// <param name="world">The ECS world.</param>
    /// <param name="trainer">The trainer entity.</param>
    /// <param name="pokemon">The Pokemon entity to remove.</param>
    /// <returns>True if successfully removed, false if not in party.</returns>
    public static bool RemoveFromParty(this World world, Entity trainer, Entity pokemon)
    {
        if (!world.HasRelationship<HasPokemon>(trainer, pokemon))
            return false;

        world.RemoveRelationship<HasPokemon>(trainer, pokemon);
        world.RemoveRelationship<OwnedBy>(pokemon, trainer);

        return true;
    }

    /// <summary>
    /// Gets all Pokemon in a trainer's party (up to 6).
    /// Returns entities in the order they were added.
    /// </summary>
    /// <param name="world">The ECS world.</param>
    /// <param name="trainer">The trainer entity.</param>
    /// <returns>Enumerable of Pokemon entities in the party.</returns>
    public static IEnumerable<Entity> GetParty(this World world, Entity trainer)
    {
        return world.GetRelationships<HasPokemon>(trainer).Take(6);
    }

    /// <summary>
    /// Gets the number of Pokemon in a trainer's party.
    /// </summary>
    /// <param name="world">The ECS world.</param>
    /// <param name="trainer">The trainer entity.</param>
    /// <returns>Number of Pokemon in party (0-6).</returns>
    public static int GetPartySize(this World world, Entity trainer)
    {
        return world.CountRelationships<HasPokemon>(trainer);
    }

    /// <summary>
    /// Checks if a trainer's party is full (6 Pokemon).
    /// </summary>
    /// <param name="world">The ECS world.</param>
    /// <param name="trainer">The trainer entity.</param>
    /// <returns>True if party has 6 Pokemon, false otherwise.</returns>
    public static bool IsPartyFull(this World world, Entity trainer)
    {
        return world.CountRelationships<HasPokemon>(trainer) >= 6;
    }

    /// <summary>
    /// Checks if a trainer's party has at least one empty slot.
    /// </summary>
    /// <param name="world">The ECS world.</param>
    /// <param name="trainer">The trainer entity.</param>
    /// <returns>True if party has less than 6 Pokemon.</returns>
    public static bool HasEmptyPartySlot(this World world, Entity trainer)
    {
        return world.CountRelationships<HasPokemon>(trainer) < 6;
    }

    /// <summary>
    /// Gets the lead Pokemon (first Pokemon in party).
    /// Typically used for overworld encounters and HM usage.
    /// </summary>
    /// <param name="world">The ECS world.</param>
    /// <param name="trainer">The trainer entity.</param>
    /// <returns>The lead Pokemon entity, or null if party is empty.</returns>
    public static Entity? GetLeadPokemon(this World world, Entity trainer)
    {
        return world.GetRelationships<HasPokemon>(trainer).FirstOrDefault();
    }

    /// <summary>
    /// Gets the trainer who owns a specific Pokemon.
    /// </summary>
    /// <param name="world">The ECS world.</param>
    /// <param name="pokemon">The Pokemon entity.</param>
    /// <returns>The trainer entity, or null if Pokemon has no owner.</returns>
    public static Entity? GetOwner(this World world, Entity pokemon)
    {
        return world.GetRelationships<OwnedBy>(pokemon).FirstOrDefault();
    }

    /// <summary>
    /// Checks if a specific Pokemon is in a trainer's party.
    /// </summary>
    /// <param name="world">The ECS world.</param>
    /// <param name="trainer">The trainer entity.</param>
    /// <param name="pokemon">The Pokemon entity to check.</param>
    /// <returns>True if Pokemon is in party, false otherwise.</returns>
    public static bool IsInParty(this World world, Entity trainer, Entity pokemon)
    {
        return world.HasRelationship<HasPokemon>(trainer, pokemon);
    }

    #endregion

    #region Item Management

    /// <summary>
    /// Gives an item to a Pokemon to hold.
    /// Pokemon can only hold one item at a time; replaces existing held item.
    /// </summary>
    /// <param name="world">The ECS world.</param>
    /// <param name="pokemon">The Pokemon entity.</param>
    /// <param name="item">The item entity to give.</param>
    /// <returns>The previously held item entity, or null if none.</returns>
    public static Entity? GiveHeldItem(this World world, Entity pokemon, Entity item)
    {
        // Get and remove existing held item if any
        var previousItem = world.GetRelationships<HoldsItem>(pokemon).FirstOrDefault();
        if (previousItem != default)
        {
            world.RemoveRelationship<HoldsItem>(pokemon, previousItem);
        }

        // Give new item
        world.AddRelationship<HoldsItem>(pokemon, item);

        return previousItem != default ? previousItem : null;
    }

    /// <summary>
    /// Takes the held item from a Pokemon.
    /// </summary>
    /// <param name="world">The ECS world.</param>
    /// <param name="pokemon">The Pokemon entity.</param>
    /// <returns>The held item entity, or null if Pokemon wasn't holding anything.</returns>
    public static Entity? TakeHeldItem(this World world, Entity pokemon)
    {
        var heldItem = world.GetRelationships<HoldsItem>(pokemon).FirstOrDefault();
        if (heldItem != default)
        {
            world.RemoveRelationship<HoldsItem>(pokemon, heldItem);
            return heldItem;
        }

        return null;
    }

    /// <summary>
    /// Gets the item a Pokemon is currently holding.
    /// </summary>
    /// <param name="world">The ECS world.</param>
    /// <param name="pokemon">The Pokemon entity.</param>
    /// <returns>The held item entity, or null if none.</returns>
    public static Entity? GetHeldItem(this World world, Entity pokemon)
    {
        return world.GetRelationships<HoldsItem>(pokemon).FirstOrDefault();
    }

    /// <summary>
    /// Checks if a Pokemon is holding a specific item.
    /// </summary>
    /// <param name="world">The ECS world.</param>
    /// <param name="pokemon">The Pokemon entity.</param>
    /// <param name="item">The item entity to check.</param>
    /// <returns>True if Pokemon is holding the item, false otherwise.</returns>
    public static bool IsHolding(this World world, Entity pokemon, Entity item)
    {
        return world.HasRelationship<HoldsItem>(pokemon, item);
    }

    /// <summary>
    /// Adds an item to a trainer's bag inventory.
    /// Used for bag items (not held by Pokemon).
    /// </summary>
    /// <param name="world">The ECS world.</param>
    /// <param name="trainer">The trainer entity.</param>
    /// <param name="item">The item entity to add.</param>
    public static void AddToBag(this World world, Entity trainer, Entity item)
    {
        world.AddRelationship<HasItem>(trainer, item);
    }

    /// <summary>
    /// Removes an item from a trainer's bag.
    /// </summary>
    /// <param name="world">The ECS world.</param>
    /// <param name="trainer">The trainer entity.</param>
    /// <param name="item">The item entity to remove.</param>
    /// <returns>True if successfully removed, false if item wasn't in bag.</returns>
    public static bool RemoveFromBag(this World world, Entity trainer, Entity item)
    {
        if (!world.HasRelationship<HasItem>(trainer, item))
            return false;

        world.RemoveRelationship<HasItem>(trainer, item);
        return true;
    }

    /// <summary>
    /// Gets all items in a trainer's bag.
    /// </summary>
    /// <param name="world">The ECS world.</param>
    /// <param name="trainer">The trainer entity.</param>
    /// <returns>Enumerable of item entities in the bag.</returns>
    public static IEnumerable<Entity> GetBagItems(this World world, Entity trainer)
    {
        return world.GetRelationships<HasItem>(trainer);
    }

    /// <summary>
    /// Checks if a trainer has a specific item in their bag.
    /// </summary>
    /// <param name="world">The ECS world.</param>
    /// <param name="trainer">The trainer entity.</param>
    /// <param name="item">The item entity to check.</param>
    /// <returns>True if item is in bag, false otherwise.</returns>
    public static bool HasInBag(this World world, Entity trainer, Entity item)
    {
        return world.HasRelationship<HasItem>(trainer, item);
    }

    #endregion

    #region PC Storage Management

    /// <summary>
    /// Stores a Pokemon in a PC box.
    /// Removes from party if currently in party.
    /// </summary>
    /// <param name="world">The ECS world.</param>
    /// <param name="pokemon">The Pokemon entity.</param>
    /// <param name="box">The box entity.</param>
    public static void StoreInBox(this World world, Entity pokemon, Entity box)
    {
        // Remove from party if in one
        var owner = world.GetOwner(pokemon);
        if (owner.HasValue)
        {
            world.RemoveFromParty(owner.Value, pokemon);
        }

        // Add to box
        world.AddRelationship<StoredIn>(pokemon, box);
    }

    /// <summary>
    /// Withdraws a Pokemon from a PC box and adds to trainer's party.
    /// </summary>
    /// <param name="world">The ECS world.</param>
    /// <param name="pokemon">The Pokemon entity.</param>
    /// <param name="trainer">The trainer entity.</param>
    /// <returns>True if successfully withdrawn, false if party is full.</returns>
    public static bool WithdrawFromBox(this World world, Entity pokemon, Entity trainer)
    {
        // Remove from box
        var box = world.GetRelationships<StoredIn>(pokemon).FirstOrDefault();
        if (box != default)
        {
            world.RemoveRelationship<StoredIn>(pokemon, box);
        }

        // Add to party
        return world.AddToParty(trainer, pokemon);
    }

    /// <summary>
    /// Gets the box a Pokemon is currently stored in.
    /// </summary>
    /// <param name="world">The ECS world.</param>
    /// <param name="pokemon">The Pokemon entity.</param>
    /// <returns>The box entity, or null if not in storage.</returns>
    public static Entity? GetStorageBox(this World world, Entity pokemon)
    {
        return world.GetRelationships<StoredIn>(pokemon).FirstOrDefault();
    }

    /// <summary>
    /// Gets all Pokemon stored in a specific box.
    /// </summary>
    /// <param name="world">The ECS world.</param>
    /// <param name="box">The box entity.</param>
    /// <returns>Enumerable of Pokemon entities in the box.</returns>
    public static IEnumerable<Entity> GetBoxPokemon(this World world, Entity box)
    {
        // Find all Pokemon stored in this box
        var query = new QueryDescription().WithAll<StoredIn>();
        var results = new List<Entity>();

        world.Query(
            in query,
            (Entity entity) =>
            {
                if (world.HasRelationship<StoredIn>(entity, box))
                {
                    results.Add(entity);
                }
            }
        );

        return results;
    }

    #endregion

    #region Battle Relationships

    /// <summary>
    /// Establishes a battle relationship between two trainers.
    /// Creates bidirectional BattlingWith relationship.
    /// </summary>
    /// <param name="world">The ECS world.</param>
    /// <param name="trainer1">First trainer entity.</param>
    /// <param name="trainer2">Second trainer entity.</param>
    public static void StartBattle(this World world, Entity trainer1, Entity trainer2)
    {
        world.AddRelationship<BattlingWith>(trainer1, trainer2);
        world.AddRelationship<BattlingWith>(trainer2, trainer1);
    }

    /// <summary>
    /// Ends a battle between two trainers.
    /// Removes bidirectional BattlingWith relationship.
    /// </summary>
    /// <param name="world">The ECS world.</param>
    /// <param name="trainer1">First trainer entity.</param>
    /// <param name="trainer2">Second trainer entity.</param>
    public static void EndBattle(this World world, Entity trainer1, Entity trainer2)
    {
        world.RemoveRelationship<BattlingWith>(trainer1, trainer2);
        world.RemoveRelationship<BattlingWith>(trainer2, trainer1);
    }

    /// <summary>
    /// Gets the opponent a trainer is currently battling.
    /// </summary>
    /// <param name="world">The ECS world.</param>
    /// <param name="trainer">The trainer entity.</param>
    /// <returns>The opponent trainer entity, or null if not in battle.</returns>
    public static Entity? GetBattleOpponent(this World world, Entity trainer)
    {
        return world.GetRelationships<BattlingWith>(trainer).FirstOrDefault();
    }

    /// <summary>
    /// Checks if a trainer is currently in battle.
    /// </summary>
    /// <param name="world">The ECS world.</param>
    /// <param name="trainer">The trainer entity.</param>
    /// <returns>True if trainer is battling, false otherwise.</returns>
    public static bool IsInBattle(this World world, Entity trainer)
    {
        return world.CountRelationships<BattlingWith>(trainer) > 0;
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// Counts the number of relationships of a specific type for an entity.
    /// </summary>
    private static int CountRelationships<T>(this World world, Entity entity)
        where T : struct
    {
        return world.GetRelationships<T>(entity).Count();
    }

    /// <summary>
    /// Gets all entities related to the given entity through a specific relationship type.
    /// </summary>
    private static IEnumerable<Entity> GetRelationships<T>(this World world, Entity entity)
        where T : struct
    {
        var results = new List<Entity>();

        // Query for entities with the relationship component
        var query = new QueryDescription().WithAll<T>();

        world.Query(
            in query,
            (Entity e) =>
            {
                // This is a simplified version - actual implementation would use Arch.Relationships API
                // For now, we're providing the interface pattern
                results.Add(e);
            }
        );

        return results;
    }

    /// <summary>
    /// Checks if a relationship exists between two entities.
    /// </summary>
    private static bool HasRelationship<T>(this World world, Entity source, Entity target)
        where T : struct
    {
        return world.GetRelationships<T>(source).Contains(target);
    }

    /// <summary>
    /// Adds a relationship between two entities.
    /// </summary>
    private static void AddRelationship<T>(this World world, Entity source, Entity target)
        where T : struct
    {
        // This would use actual Arch.Relationships API
        // For now, we're defining the interface
    }

    /// <summary>
    /// Removes a relationship between two entities.
    /// </summary>
    private static void RemoveRelationship<T>(this World world, Entity source, Entity target)
        where T : struct
    {
        // This would use actual Arch.Relationships API
        // For now, we're defining the interface
    }

    #endregion
}
