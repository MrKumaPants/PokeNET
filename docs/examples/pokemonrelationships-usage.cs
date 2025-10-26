using Arch.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PokeNET.Domain.ECS.Components;
using PokeNET.Domain.ECS.Relationships;
using PokeNET.Domain.ECS.Systems;

namespace PokeNET.Examples;

/// <summary>
/// Demonstrates PokemonRelationships integration with PartyManagementSystem and BattleSystem.
/// Shows how to use relationship-based party management instead of the old Party component.
///
/// Key Features Demonstrated:
/// - Adding/removing Pokemon from trainer parties (max 6)
/// - Querying party composition using world.GetParty(trainer)
/// - Managing held items using world.GiveHeldItem(pokemon, item)
/// - Bidirectional queries: GetOwner, GetParty, IsInParty
/// - Battle state management using world.StartBattle(trainer1, trainer2)
/// </summary>
public class PokemonRelationshipsUsageExample
{
    private readonly World _world;
    private readonly PartyManagementSystem _partySystem;
    private readonly BattleSystem _battleSystem;
    private readonly ILogger _logger;

    public PokemonRelationshipsUsageExample(
        World world,
        PartyManagementSystem partySystem,
        BattleSystem battleSystem,
        ILogger<PokemonRelationshipsUsageExample> logger
    )
    {
        _world = world;
        _partySystem = partySystem;
        _battleSystem = battleSystem;
        _logger = logger;
    }

    /// <summary>
    /// Example 1: Basic party management - Add Pokemon to trainer's party
    /// </summary>
    public void Example1_BasicPartyManagement()
    {
        _logger.LogInformation("=== Example 1: Basic Party Management ===");

        // Create a trainer entity
        var trainer = _world.Create(new TrainerData { Name = "Ash Ketchum" });

        // Create 6 Pokemon entities
        var pikachu = _world.Create(
            new PokemonData
            {
                SpeciesId = 25,
                Nickname = "Pikachu",
                Level = 25,
            }
        );
        var charizard = _world.Create(
            new PokemonData
            {
                SpeciesId = 6,
                Nickname = "Charizard",
                Level = 36,
            }
        );
        var blastoise = _world.Create(
            new PokemonData
            {
                SpeciesId = 9,
                Nickname = "Blastoise",
                Level = 42,
            }
        );
        var venusaur = _world.Create(
            new PokemonData
            {
                SpeciesId = 3,
                Nickname = "Venusaur",
                Level = 40,
            }
        );
        var snorlax = _world.Create(
            new PokemonData
            {
                SpeciesId = 143,
                Nickname = "Snorlax",
                Level = 35,
            }
        );
        var lapras = _world.Create(
            new PokemonData
            {
                SpeciesId = 131,
                Nickname = "Lapras",
                Level = 38,
            }
        );

        // Add Pokemon to party using PartyManagementSystem
        _partySystem.AddToParty(trainer, pikachu);
        _partySystem.AddToParty(trainer, charizard);
        _partySystem.AddToParty(trainer, blastoise);
        _partySystem.AddToParty(trainer, venusaur);
        _partySystem.AddToParty(trainer, snorlax);
        _partySystem.AddToParty(trainer, lapras);

        // Check party size
        var partySize = _world.GetPartySize(trainer);
        _logger.LogInformation("Trainer party size: {Size}/6", partySize);

        // Try to add 7th Pokemon (should fail - party is full)
        var mew = _world.Create(
            new PokemonData
            {
                SpeciesId = 151,
                Nickname = "Mew",
                Level = 100,
            }
        );
        var addedMew = _partySystem.AddToParty(trainer, mew);
        _logger.LogInformation("Added Mew to full party: {Success} (expected: False)", addedMew);
    }

    /// <summary>
    /// Example 2: Querying party composition
    /// </summary>
    public void Example2_QueryPartyComposition()
    {
        _logger.LogInformation("=== Example 2: Query Party Composition ===");

        var trainer = _world.Create(new TrainerData { Name = "Gary Oak" });
        var pokemon1 = _world.Create(
            new PokemonData
            {
                SpeciesId = 130,
                Nickname = "Gyarados",
                Level = 45,
            }
        );
        var pokemon2 = _world.Create(
            new PokemonData
            {
                SpeciesId = 65,
                Nickname = "Alakazam",
                Level = 42,
            }
        );
        var pokemon3 = _world.Create(
            new PokemonData
            {
                SpeciesId = 68,
                Nickname = "Machamp",
                Level = 40,
            }
        );

        _partySystem.AddToParty(trainer, pokemon1);
        _partySystem.AddToParty(trainer, pokemon2);
        _partySystem.AddToParty(trainer, pokemon3);

        // Get all Pokemon in party
        var party = _partySystem.GetParty(trainer);
        _logger.LogInformation("Party contains {Count} Pokemon:", party.Count());

        foreach (var pokemon in party)
        {
            var data = _world.Get<PokemonData>(pokemon);
            _logger.LogInformation("  - {Nickname} (Level {Level})", data.Nickname, data.Level);
        }

        // Get lead Pokemon (first in party)
        var leadPokemon = _world.GetLeadPokemon(trainer);
        if (leadPokemon.HasValue)
        {
            var leadData = _world.Get<PokemonData>(leadPokemon.Value);
            _logger.LogInformation("Lead Pokemon: {Nickname}", leadData.Nickname);
        }
    }

    /// <summary>
    /// Example 3: Bidirectional relationship queries
    /// </summary>
    public void Example3_BidirectionalQueries()
    {
        _logger.LogInformation("=== Example 3: Bidirectional Relationship Queries ===");

        var trainer = _world.Create(new TrainerData { Name = "Misty" });
        var starmie = _world.Create(
            new PokemonData
            {
                SpeciesId = 121,
                Nickname = "Starmie",
                Level = 32,
            }
        );

        _partySystem.AddToParty(trainer, starmie);

        // Query: "Who owns this Pokemon?" (bidirectional query)
        var owner = _partySystem.GetOwner(starmie);
        if (owner.HasValue)
        {
            var ownerData = _world.Get<TrainerData>(owner.Value);
            _logger.LogInformation("Starmie is owned by: {Name}", ownerData.Name);
        }

        // Query: "Is this Pokemon in the trainer's party?"
        var isInParty = _world.IsInParty(trainer, starmie);
        _logger.LogInformation("Is Starmie in Misty's party? {IsInParty}", isInParty);

        // Remove and query again
        _partySystem.RemoveFromParty(trainer, starmie);
        isInParty = _world.IsInParty(trainer, starmie);
        _logger.LogInformation("After removal, is Starmie in party? {IsInParty}", isInParty);
    }

    /// <summary>
    /// Example 4: Held item management
    /// </summary>
    public void Example4_HeldItemManagement()
    {
        _logger.LogInformation("=== Example 4: Held Item Management ===");

        var trainer = _world.Create(new TrainerData { Name = "Brock" });
        var onix = _world.Create(
            new PokemonData
            {
                SpeciesId = 95,
                Nickname = "Onix",
                Level = 28,
            }
        );
        _partySystem.AddToParty(trainer, onix);

        // Create item entities
        var leftovers = _world.Create(new ItemData { ItemId = 234, Name = "Leftovers" });
        var choiceBand = _world.Create(new ItemData { ItemId = 220, Name = "Choice Band" });

        // Give held item to Pokemon
        var previousItem = _partySystem.GiveHeldItem(onix, leftovers);
        _logger.LogInformation(
            "Gave Leftovers to Onix. Previous item: {HasPrevious}",
            previousItem.HasValue ? "Yes" : "None"
        );

        // Replace held item
        previousItem = _partySystem.GiveHeldItem(onix, choiceBand);
        if (previousItem.HasValue)
        {
            var prevItemData = _world.Get<ItemData>(previousItem.Value);
            _logger.LogInformation("Replaced {OldItem} with Choice Band", prevItemData.Name);
        }

        // Query held item
        var heldItem = _world.GetHeldItem(onix);
        if (heldItem.HasValue)
        {
            var itemData = _world.Get<ItemData>(heldItem.Value);
            _logger.LogInformation("Onix is currently holding: {Item}", itemData.Name);
        }

        // Take held item
        var takenItem = _partySystem.TakeHeldItem(onix);
        if (takenItem.HasValue)
        {
            var takenData = _world.Get<ItemData>(takenItem.Value);
            _logger.LogInformation("Took {Item} from Onix", takenData.Name);
        }
    }

    /// <summary>
    /// Example 5: PC box storage
    /// </summary>
    public void Example5_PCBoxStorage()
    {
        _logger.LogInformation("=== Example 5: PC Box Storage ===");

        var trainer = _world.Create(new TrainerData { Name = "Red" });
        var box1 = _world.Create(new BoxData { BoxNumber = 1, Name = "Box 1" });

        // Create Pokemon
        var moltres = _world.Create(
            new PokemonData
            {
                SpeciesId = 146,
                Nickname = "Moltres",
                Level = 50,
            }
        );
        var articuno = _world.Create(
            new PokemonData
            {
                SpeciesId = 144,
                Nickname = "Articuno",
                Level = 50,
            }
        );

        // Add to party first
        _partySystem.AddToParty(trainer, moltres);
        _logger.LogInformation("Party size before storage: {Size}", _world.GetPartySize(trainer));

        // Store in box (removes from party)
        _partySystem.StoreInBox(moltres, box1);
        _logger.LogInformation("Party size after storage: {Size}", _world.GetPartySize(trainer));

        // Withdraw from box
        var withdrawn = _partySystem.WithdrawFromBox(moltres, trainer);
        _logger.LogInformation(
            "Withdrawn from box: {Success}, Party size: {Size}",
            withdrawn,
            _world.GetPartySize(trainer)
        );
    }

    /// <summary>
    /// Example 6: Battle state management using PokemonRelationships
    /// </summary>
    public void Example6_BattleStateManagement()
    {
        _logger.LogInformation("=== Example 6: Battle State Management ===");

        var trainer1 = _world.Create(new TrainerData { Name = "Red" });
        var trainer2 = _world.Create(new TrainerData { Name = "Blue" });

        // Start battle using BattleSystem (which uses PokemonRelationships.StartBattle)
        _battleSystem.StartBattle(trainer1, trainer2);

        // Check if trainers are in battle
        var isInBattle1 = _battleSystem.IsInBattle(trainer1);
        var isInBattle2 = _battleSystem.IsInBattle(trainer2);
        _logger.LogInformation(
            "Trainer 1 in battle: {InBattle1}, Trainer 2 in battle: {InBattle2}",
            isInBattle1,
            isInBattle2
        );

        // Get battle opponent (bidirectional query)
        var opponent1 = _battleSystem.GetBattleOpponent(trainer1);
        if (opponent1.HasValue)
        {
            var opponentData = _world.Get<TrainerData>(opponent1.Value);
            _logger.LogInformation("Trainer 1's opponent: {Name}", opponentData.Name);
        }

        // End battle
        _battleSystem.EndBattle(trainer1, trainer2);
        _logger.LogInformation(
            "Battle ended. Trainer 1 in battle: {InBattle}",
            _battleSystem.IsInBattle(trainer1)
        );
    }

    /// <summary>
    /// Example 7: Complex party operations - Switching lead Pokemon
    /// </summary>
    public void Example7_ComplexPartyOperations()
    {
        _logger.LogInformation("=== Example 7: Complex Party Operations ===");

        var trainer = _world.Create(new TrainerData { Name = "Lance" });
        var dragonite1 = _world.Create(
            new PokemonData
            {
                SpeciesId = 149,
                Nickname = "Dragonite",
                Level = 62,
            }
        );
        var dragonite2 = _world.Create(
            new PokemonData
            {
                SpeciesId = 149,
                Nickname = "Dragonite",
                Level = 60,
            }
        );
        var dragonite3 = _world.Create(
            new PokemonData
            {
                SpeciesId = 149,
                Nickname = "Dragonite",
                Level = 58,
            }
        );

        _partySystem.AddToParty(trainer, dragonite1);
        _partySystem.AddToParty(trainer, dragonite2);
        _partySystem.AddToParty(trainer, dragonite3);

        // Get lead Pokemon
        var leadPokemon = _world.GetLeadPokemon(trainer);
        _logger.LogInformation("Lead Pokemon ID: {Id}", leadPokemon?.Id);

        // Simulate switching: Remove lead, then re-add to end
        if (leadPokemon.HasValue)
        {
            _partySystem.RemoveFromParty(trainer, leadPokemon.Value);
            _partySystem.AddToParty(trainer, leadPokemon.Value);

            var newLead = _world.GetLeadPokemon(trainer);
            _logger.LogInformation("New lead Pokemon ID: {Id} (should be different)", newLead?.Id);
        }
    }

    /// <summary>
    /// Runs all examples in sequence.
    /// </summary>
    public void RunAllExamples()
    {
        _logger.LogInformation("========================================");
        _logger.LogInformation("PokemonRelationships Integration Examples");
        _logger.LogInformation("========================================\n");

        Example1_BasicPartyManagement();
        Console.WriteLine();

        Example2_QueryPartyComposition();
        Console.WriteLine();

        Example3_BidirectionalQueries();
        Console.WriteLine();

        Example4_HeldItemManagement();
        Console.WriteLine();

        Example5_PCBoxStorage();
        Console.WriteLine();

        Example6_BattleStateManagement();
        Console.WriteLine();

        Example7_ComplexPartyOperations();

        _logger.LogInformation("\n========================================");
        _logger.LogInformation("All examples completed successfully!");
        _logger.LogInformation("========================================");
    }
}

/// <summary>
/// Placeholder components for example code.
/// In production, these would be defined in PokeNET.Domain.ECS.Components.
/// </summary>
public record struct TrainerData
{
    public string Name { get; init; }
}

public record struct ItemData
{
    public int ItemId { get; init; }
    public string Name { get; init; }
}

public record struct BoxData
{
    public int BoxNumber { get; init; }
    public string Name { get; init; }
}
