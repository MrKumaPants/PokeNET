using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace PokeNET.Core.Data.Extensions;

/// <summary>
/// Extension methods for querying game data with Entity Framework Core.
/// Provides expression-based querying for flexible data access.
/// </summary>
public static class DataQueryExtensions
{
    /// <summary>
    /// Queries species data with a custom predicate expression.
    /// </summary>
    /// <param name="context">The game data context.</param>
    /// <param name="predicate">Expression to filter species.</param>
    /// <returns>List of species matching the predicate.</returns>
    public static async Task<IReadOnlyList<SpeciesData>> QuerySpeciesAsync(
        this GameDataContext context,
        Expression<Func<SpeciesData, bool>> predicate
    )
    {
        return await context.Species.AsNoTracking().Where(predicate).ToListAsync();
    }

    /// <summary>
    /// Queries move data with a custom predicate expression.
    /// </summary>
    /// <param name="context">The game data context.</param>
    /// <param name="predicate">Expression to filter moves.</param>
    /// <returns>List of moves matching the predicate.</returns>
    public static async Task<IReadOnlyList<MoveData>> QueryMovesAsync(
        this GameDataContext context,
        Expression<Func<MoveData, bool>> predicate
    )
    {
        return await context.Moves.AsNoTracking().Where(predicate).ToListAsync();
    }

    /// <summary>
    /// Queries item data with a custom predicate expression.
    /// </summary>
    /// <param name="context">The game data context.</param>
    /// <param name="predicate">Expression to filter items.</param>
    /// <returns>List of items matching the predicate.</returns>
    public static async Task<IReadOnlyList<ItemData>> QueryItemsAsync(
        this GameDataContext context,
        Expression<Func<ItemData, bool>> predicate
    )
    {
        return await context.Items.AsNoTracking().Where(predicate).ToListAsync();
    }

    /// <summary>
    /// Queries type data with a custom predicate expression.
    /// </summary>
    /// <param name="context">The game data context.</param>
    /// <param name="predicate">Expression to filter types.</param>
    /// <returns>List of types matching the predicate.</returns>
    public static async Task<IReadOnlyList<TypeData>> QueryTypesAsync(
        this GameDataContext context,
        Expression<Func<TypeData, bool>> predicate
    )
    {
        return await context.Types.AsNoTracking().Where(predicate).ToListAsync();
    }

    // ==================== Convenience Query Methods ====================

    /// <summary>
    /// Gets all species of the specified type.
    /// </summary>
    public static async Task<IReadOnlyList<SpeciesData>> GetSpeciesByTypeAsync(
        this GameDataContext context,
        string type
    )
    {
        if (string.IsNullOrWhiteSpace(type))
            return Array.Empty<SpeciesData>().ToList().AsReadOnly();

        // Note: Since Types is stored as JSON, we need to load and filter in memory
        // For better performance with large datasets, consider denormalizing or using a join table
        var allSpecies = await context.Species.AsNoTracking().ToListAsync();

        return allSpecies
            .Where(s => s.Types.Any(t => t.Equals(type, StringComparison.OrdinalIgnoreCase)))
            .ToList()
            .AsReadOnly();
    }

    /// <summary>
    /// Gets species within a stat range for the specified stat.
    /// </summary>
    public static async Task<IReadOnlyList<SpeciesData>> GetSpeciesByStatRangeAsync(
        this GameDataContext context,
        Func<BaseStats, int> statSelector,
        int minValue,
        int maxValue
    )
    {
        var allSpecies = await context.Species.AsNoTracking().ToListAsync();

        return allSpecies
            .Where(s =>
            {
                var statValue = statSelector(s.BaseStats);
                return statValue >= minValue && statValue <= maxValue;
            })
            .ToList()
            .AsReadOnly();
    }

    /// <summary>
    /// Gets moves within a power range.
    /// </summary>
    public static async Task<IReadOnlyList<MoveData>> GetMovesByPowerRangeAsync(
        this GameDataContext context,
        int minPower,
        int maxPower
    )
    {
        return await context
            .Moves.AsNoTracking()
            .Where(m => m.Power >= minPower && m.Power <= maxPower)
            .ToListAsync();
    }

    /// <summary>
    /// Gets moves by category.
    /// </summary>
    public static async Task<IReadOnlyList<MoveData>> GetMovesByCategoryAsync(
        this GameDataContext context,
        MoveCategory category
    )
    {
        return await context.Moves.AsNoTracking().Where(m => m.Category == category).ToListAsync();
    }

    /// <summary>
    /// Gets items within a price range.
    /// </summary>
    public static async Task<IReadOnlyList<ItemData>> GetItemsByPriceRangeAsync(
        this GameDataContext context,
        int minPrice,
        int maxPrice
    )
    {
        return await context
            .Items.AsNoTracking()
            .Where(i => i.BuyPrice >= minPrice && i.BuyPrice <= maxPrice)
            .ToListAsync();
    }
}
