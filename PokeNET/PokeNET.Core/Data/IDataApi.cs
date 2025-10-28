using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace PokeNET.Core.Data;

/// <summary>
/// Interface for accessing static Pokemon game data.
/// Provides read-only access to species, moves, items, and encounters.
/// Follows the Dependency Inversion Principle - domain defines the contract, infrastructure implements it.
/// </summary>
public interface IDataApi
{
    // ==================== Species Data ====================

    /// <summary>
    /// Retrieves species data by identifier.
    /// </summary>
    /// <param name="speciesId">Species identifier.</param>
    /// <returns>Species data, or null if not found.</returns>
    Task<SpeciesData?> GetSpeciesAsync(string speciesId);

    /// <summary>
    /// Retrieves species data by name.
    /// </summary>
    /// <param name="name">Species name (case-insensitive).</param>
    /// <returns>Species data, or null if not found.</returns>
    Task<SpeciesData?> GetSpeciesByNameAsync(string name);

    /// <summary>
    /// Retrieves all species data.
    /// </summary>
    /// <returns>Read-only list of all species.</returns>
    Task<IReadOnlyList<SpeciesData>> GetAllSpeciesAsync();

    /// <summary>
    /// Queries species data with a custom predicate expression.
    /// </summary>
    /// <param name="predicate">Expression to filter species.</param>
    /// <returns>List of species matching the predicate.</returns>
    Task<IReadOnlyList<SpeciesData>> QuerySpeciesAsync(
        Expression<Func<SpeciesData, bool>> predicate
    );

    /// <summary>
    /// Gets all species of the specified type.
    /// </summary>
    /// <param name="type">Pokemon type (e.g., "Fire", "Water").</param>
    /// <returns>List of species with the specified type.</returns>
    Task<IReadOnlyList<SpeciesData>> GetSpeciesByTypeAsync(string type);

    // ==================== Move Data ====================

    /// <summary>
    /// Retrieves move data by identifier.
    /// </summary>
    /// <param name="moveId">Move identifier.</param>
    /// <returns>Move data, or null if not found.</returns>
    Task<MoveData?> GetMoveAsync(string moveId);

    /// <summary>
    /// Retrieves move data by name.
    /// </summary>
    /// <param name="name">Move name (case-insensitive).</param>
    /// <returns>Move data, or null if not found.</returns>
    Task<MoveData?> GetMoveByNameAsync(string name);

    /// <summary>
    /// Retrieves all move data.
    /// </summary>
    /// <returns>Read-only list of all moves.</returns>
    Task<IReadOnlyList<MoveData>> GetAllMovesAsync();

    /// <summary>
    /// Retrieves moves by type.
    /// </summary>
    /// <param name="type">Move type (e.g., "Fire", "Water").</param>
    /// <returns>List of moves matching the type.</returns>
    Task<IReadOnlyList<MoveData>> GetMovesByTypeAsync(string type);

    /// <summary>
    /// Queries move data with a custom predicate expression.
    /// </summary>
    /// <param name="predicate">Expression to filter moves.</param>
    /// <returns>List of moves matching the predicate.</returns>
    Task<IReadOnlyList<MoveData>> QueryMovesAsync(Expression<Func<MoveData, bool>> predicate);

    /// <summary>
    /// Gets moves within a power range.
    /// </summary>
    /// <param name="minPower">Minimum power (inclusive).</param>
    /// <param name="maxPower">Maximum power (inclusive).</param>
    /// <returns>List of moves within the power range.</returns>
    Task<IReadOnlyList<MoveData>> GetMovesByPowerRangeAsync(int minPower, int maxPower);

    /// <summary>
    /// Gets moves by category.
    /// </summary>
    /// <param name="category">Move category.</param>
    /// <returns>List of moves in the category.</returns>
    Task<IReadOnlyList<MoveData>> GetMovesByCategoryAsync(MoveCategory category);

    // ==================== Item Data ====================

    /// <summary>
    /// Retrieves item data by ID.
    /// </summary>
    /// <param name="itemId">Item identifier.</param>
    /// <returns>Item data, or null if not found.</returns>
    Task<ItemData?> GetItemAsync(string itemId);

    /// <summary>
    /// Retrieves item data by name.
    /// </summary>
    /// <param name="name">Item name (case-insensitive).</param>
    /// <returns>Item data, or null if not found.</returns>
    Task<ItemData?> GetItemByNameAsync(string name);

    /// <summary>
    /// Retrieves all item data.
    /// </summary>
    /// <returns>Read-only list of all items.</returns>
    Task<IReadOnlyList<ItemData>> GetAllItemsAsync();

    /// <summary>
    /// Retrieves items by category.
    /// </summary>
    /// <param name="category">Item category.</param>
    /// <returns>List of items in the category.</returns>
    Task<IReadOnlyList<ItemData>> GetItemsByCategoryAsync(ItemCategory category);

    /// <summary>
    /// Queries item data with a custom predicate expression.
    /// </summary>
    /// <param name="predicate">Expression to filter items.</param>
    /// <returns>List of items matching the predicate.</returns>
    Task<IReadOnlyList<ItemData>> QueryItemsAsync(Expression<Func<ItemData, bool>> predicate);

    /// <summary>
    /// Gets items within a price range.
    /// </summary>
    /// <param name="minPrice">Minimum price (inclusive).</param>
    /// <param name="maxPrice">Maximum price (inclusive).</param>
    /// <returns>List of items within the price range.</returns>
    Task<IReadOnlyList<ItemData>> GetItemsByPriceRangeAsync(int minPrice, int maxPrice);

    // ==================== Encounter Data ====================

    /// <summary>
    /// Retrieves encounter table for a location by identifier.
    /// </summary>
    /// <param name="locationId">Location identifier.</param>
    /// <returns>Encounter table, or null if not found.</returns>
    Task<EncounterTable?> GetEncountersAsync(string locationId);

    /// <summary>
    /// Retrieves encounter table for a location by name.
    /// </summary>
    /// <param name="name">Location name (case-insensitive).</param>
    /// <returns>Encounter table, or null if not found.</returns>
    Task<EncounterTable?> GetEncountersByNameAsync(string name);

    /// <summary>
    /// Retrieves all encounter tables.
    /// </summary>
    /// <returns>Read-only list of all encounter tables.</returns>
    Task<IReadOnlyList<EncounterTable>> GetAllEncountersAsync();

    // ==================== Type Data ====================

    /// <summary>
    /// Retrieves type information by identifier.
    /// </summary>
    /// <param name="typeId">Type identifier (e.g., "fire", "water").</param>
    /// <returns>Type information, or null if not found.</returns>
    Task<TypeData?> GetTypeAsync(string typeId);

    /// <summary>
    /// Retrieves type information by name.
    /// </summary>
    /// <param name="name">Name of the type (e.g., "Fire", "Water") (case-insensitive).</param>
    /// <returns>Type information, or null if not found.</returns>
    Task<TypeData?> GetTypeByNameAsync(string name);

    /// <summary>
    /// Retrieves all loaded types.
    /// </summary>
    /// <returns>Read-only list of all type information.</returns>
    Task<IReadOnlyList<TypeData>> GetAllTypesAsync();

    // ==================== Type Effectiveness ====================

    /// <summary>
    /// Retrieves type effectiveness multiplier for a single-type matchup.
    /// </summary>
    /// <param name="attackType">Name of the attacking move's type.</param>
    /// <param name="defenseType">Name of the defending Pokemon's type.</param>
    /// <returns>Effectiveness multiplier (0, 0.5, 1, 2).</returns>
    Task<double> GetTypeEffectivenessAsync(string attackType, string defenseType);

    /// <summary>
    /// Retrieves type effectiveness multiplier for dual-type Pokemon.
    /// Multiplies both type matchups together (e.g., 2.0 × 2.0 = 4.0).
    /// </summary>
    /// <param name="attackType">Name of the attacking move's type.</param>
    /// <param name="defenseType1">Name of the defending Pokemon's first type.</param>
    /// <param name="defenseType2">Name of the defending Pokemon's second type (null if single-type).</param>
    /// <returns>Combined effectiveness multiplier (0, 0.25, 0.5, 1, 2, 4).</returns>
    Task<double> GetDualTypeEffectivenessAsync(
        string attackType,
        string defenseType1,
        string? defenseType2
    );

    // ==================== Cache Management ====================

    /// <summary>
    /// Reloads all data from storage (clears cache).
    /// Useful when mods are loaded/unloaded.
    /// </summary>
    Task ReloadDataAsync();

    /// <summary>
    /// Checks if data is currently loaded.
    /// </summary>
    /// <returns>True if data is loaded and ready.</returns>
    bool IsDataLoaded();
}
