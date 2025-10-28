using System.Collections.Generic;
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
        string? defenseType2);

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
