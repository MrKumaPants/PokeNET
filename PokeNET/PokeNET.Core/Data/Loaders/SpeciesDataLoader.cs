using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace PokeNET.Core.Data.Loaders;

/// <summary>
/// Loader for Pokemon species data from species.json.
/// Handles deserialization and validation of SpeciesData models.
/// </summary>
public class SpeciesDataLoader : JsonArrayLoader<SpeciesData>
{
    public SpeciesDataLoader(ILogger<BaseDataLoader<List<SpeciesData>>> logger)
        : base(logger) { }

    /// <inheritdoc/>
    public override bool Validate(List<SpeciesData> data)
    {
        if (!base.Validate(data))
            return false;

        foreach (var item in data)
        {
            if (!ValidateItem(item))
            {
                Logger.LogWarning("Invalid species data found: {Name}", item.Name ?? "Unknown");
                return false;
            }
        }

        return true;
    }

    private bool ValidateItem(SpeciesData item)
    {
        // Validate required fields
        if (!ValidateString(item.Id, nameof(item.Id)))
            return false;

        if (!ValidateString(item.Name, nameof(item.Name)))
            return false;

        if (!ValidateCollection(item.Types, nameof(item.Types)))
            return false;

        if (item.Types.Count > 2)
        {
            Logger.LogWarning("Species {Name} has more than 2 types", item.Name);
            return false;
        }

        // Validate base stats
        if (item.BaseStats == null)
        {
            Logger.LogWarning("Species {Name} has null BaseStats", item.Name);
            return false;
        }

        if (!ValidateBaseStats(item.BaseStats, item.Name))
            return false;

        // Validate abilities
        if (!ValidateCollection(item.Abilities, nameof(item.Abilities)))
            return false;

        // Validate growth rate
        if (!ValidateString(item.GrowthRate, nameof(item.GrowthRate)))
            return false;

        var validGrowthRates = new[]
        {
            "Fast",
            "Medium Fast",
            "Medium Slow",
            "Slow",
            "Erratic",
            "Fluctuating",
        };
        if (!validGrowthRates.Contains(item.GrowthRate))
        {
            Logger.LogWarning(
                "Species {Name} has invalid GrowthRate: {GrowthRate}",
                item.Name,
                item.GrowthRate
            );
            return false;
        }

        // Validate catch rate
        if (!ValidateRange(item.CatchRate, nameof(item.CatchRate), 0, 255))
            return false;

        // Validate gender ratio
        if (!ValidateRange(item.GenderRatio, nameof(item.GenderRatio), -1, 254))
            return false;

        // Validate base experience
        if (!ValidateRange(item.BaseExperience, nameof(item.BaseExperience), 0, 1000))
            return false;

        // Validate evolutions if present
        if (item.Evolutions != null && item.Evolutions.Count > 0)
        {
            foreach (var evolution in item.Evolutions)
            {
                if (!ValidateEvolution(evolution, item.Name))
                    return false;
            }
        }

        Logger.LogTrace("Validated species: {Name} (ID: {Id})", item.Name, item.Id);
        return true;
    }

    private bool ValidateBaseStats(BaseStats stats, string speciesName)
    {
        if (!ValidateRange(stats.HP, "HP", 1, 255))
            return false;

        if (!ValidateRange(stats.Attack, "Attack", 1, 255))
            return false;

        if (!ValidateRange(stats.Defense, "Defense", 1, 255))
            return false;

        if (!ValidateRange(stats.SpecialAttack, "SpecialAttack", 1, 255))
            return false;

        if (!ValidateRange(stats.SpecialDefense, "SpecialDefense", 1, 255))
            return false;

        if (!ValidateRange(stats.Speed, "Speed", 1, 255))
            return false;

        // Total should be reasonable (typically 180-780)
        if (stats.Total < 180 || stats.Total > 800)
        {
            Logger.LogWarning(
                "Species {Name} has unusual base stat total: {Total}",
                speciesName,
                stats.Total
            );
        }

        return true;
    }

    private bool ValidateEvolution(Evolution evolution, string speciesName)
    {
        if (!ValidateString(evolution.TargetSpeciesId, nameof(evolution.TargetSpeciesId)))
            return false;

        if (!ValidateString(evolution.Method, nameof(evolution.Method)))
            return false;

        var validMethods = new[] { "Level", "Stone", "Trade", "Friendship", "Special" };
        if (!validMethods.Contains(evolution.Method))
        {
            Logger.LogWarning(
                "Species {Name} has invalid evolution method: {Method}",
                speciesName,
                evolution.Method
            );
            return false;
        }

        // Validate level requirement for level-up evolutions
        if (evolution.Method == "Level" && evolution.RequiredLevel.HasValue)
        {
            if (!ValidateRange(evolution.RequiredLevel.Value, "RequiredLevel", 1, 100))
                return false;
        }

        return true;
    }

    // Helper validation methods
    private bool ValidateString(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            Logger.LogWarning("Invalid {FieldName}: cannot be null or empty", fieldName);
            return false;
        }
        return true;
    }

    private bool ValidateRange(int value, string fieldName, int min, int max)
    {
        if (value < min || value > max)
        {
            Logger.LogWarning(
                "Invalid {FieldName}: {Value} is outside range [{Min}, {Max}]",
                fieldName,
                value,
                min,
                max
            );
            return false;
        }
        return true;
    }

    private bool ValidateCollection<TItem>(
        List<TItem>? collection,
        string fieldName,
        bool allowEmpty = false
    )
    {
        if (collection == null)
        {
            Logger.LogWarning("Invalid {FieldName}: collection is null", fieldName);
            return false;
        }

        if (!allowEmpty && collection.Count == 0)
        {
            Logger.LogWarning("Invalid {FieldName}: collection is empty", fieldName);
            return false;
        }

        return true;
    }
}
