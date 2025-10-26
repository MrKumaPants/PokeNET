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
    /// Retrieves species data by Pokedex ID.
    /// </summary>
    /// <param name="speciesId">National Pokedex number (1-1025+).</param>
    /// <returns>Species data, or null if not found.</returns>
    Task<SpeciesData?> GetSpeciesAsync(int speciesId);

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
    /// Retrieves move data by name.
    /// </summary>
    /// <param name="moveName">Move name (case-insensitive).</param>
    /// <returns>Move data, or null if not found.</returns>
    Task<MoveData?> GetMoveAsync(string moveName);

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
    Task<ItemData?> GetItemAsync(int itemId);

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
    /// Retrieves encounter table for a location.
    /// </summary>
    /// <param name="locationId">Location identifier.</param>
    /// <returns>Encounter table, or null if not found.</returns>
    Task<EncounterTable?> GetEncountersAsync(string locationId);

    /// <summary>
    /// Retrieves all encounter tables.
    /// </summary>
    /// <returns>Read-only list of all encounter tables.</returns>
    Task<IReadOnlyList<EncounterTable>> GetAllEncountersAsync();

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
