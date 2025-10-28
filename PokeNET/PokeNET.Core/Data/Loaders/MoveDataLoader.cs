using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace PokeNET.Core.Data.Loaders;

/// <summary>
/// Loader for Pokemon move data from moves.json.
/// Handles deserialization and validation of MoveData models.
/// </summary>
public class MoveDataLoader : JsonArrayLoader<MoveData>
{
    public MoveDataLoader(ILogger<BaseDataLoader<List<MoveData>>> logger)
        : base(logger) { }

    /// <inheritdoc/>
    public override bool Validate(List<MoveData> data)
    {
        if (!base.Validate(data))
            return false;

        foreach (var item in data)
        {
            if (!ValidateItem(item))
            {
                Logger.LogWarning("Invalid move data found: {Name}", item.Name ?? "Unknown");
                return false;
            }
        }

        return true;
    }

    private bool ValidateItem(MoveData item)
    {
        // Validate required fields
        if (!ValidateString(item.Name, nameof(item.Name)))
            return false;

        if (!ValidateString(item.Type, nameof(item.Type)))
            return false;

        // Validate move category
        var validCategories = new[]
        {
            MoveCategory.Physical,
            MoveCategory.Special,
            MoveCategory.Status,
        };
        if (!validCategories.Contains(item.Category))
        {
            Logger.LogWarning(
                "Move {Name} has invalid category: {Category}",
                item.Name,
                item.Category
            );
            return false;
        }

        // Validate power (0 for status moves, 1-250 for damage moves)
        if (item.Category != MoveCategory.Status)
        {
            if (!ValidateRange(item.Power, nameof(item.Power), 0, 250))
                return false;

            if (item.Power == 0)
            {
                Logger.LogWarning(
                    "Move {Name} is {Category} but has 0 power",
                    item.Name,
                    item.Category
                );
            }
        }
        else
        {
            // Status moves should have 0 power
            if (item.Power != 0)
            {
                Logger.LogWarning(
                    "Move {Name} is Status but has non-zero power: {Power}",
                    item.Name,
                    item.Power
                );
            }
        }

        // Validate accuracy (0 for never-miss moves, 1-100 for normal moves)
        if (!ValidateRange(item.Accuracy, nameof(item.Accuracy), 0, 100))
            return false;

        // Validate PP
        if (!ValidateRange(item.PP, nameof(item.PP), 1, 40))
            return false;

        // Validate priority
        if (!ValidateRange(item.Priority, nameof(item.Priority), -7, 5))
            return false;

        // Validate effect chance
        if (!ValidateRange(item.EffectChance, nameof(item.EffectChance), 0, 100))
            return false;

        // Validate description
        if (!ValidateString(item.Description, nameof(item.Description)))
            return false;

        // Validate target
        if (!ValidateString(item.Target, nameof(item.Target)))
            return false;

        var validTargets = new[]
        {
            "SingleTarget",
            "AllOpponents",
            "AllAllies",
            "AllOthers",
            "User",
            "RandomOpponent",
            "AllPokemon",
        };

        if (!validTargets.Contains(item.Target))
        {
            Logger.LogWarning("Move {Name} has invalid target: {Target}", item.Name, item.Target);
            return false;
        }

        // Validate flags collection (can be empty)
        if (!ValidateCollection(item.Flags, nameof(item.Flags), allowEmpty: true))
            return false;

        Logger.LogTrace(
            "Validated move: {Name} (Type: {Type}, Category: {Category})",
            item.Name,
            item.Type,
            item.Category
        );
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
