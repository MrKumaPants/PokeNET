using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace PokeNET.Core.Data.Loaders;

/// <summary>
/// Loader for item data from items.json.
/// Handles deserialization and validation of ItemData models.
/// </summary>
public class ItemDataLoader : JsonArrayLoader<ItemData>
{
    public ItemDataLoader(ILogger<BaseDataLoader<List<ItemData>>> logger) : base(logger)
    {
    }

    /// <inheritdoc/>
    public override bool Validate(List<ItemData> data)
    {
        if (!base.Validate(data))
            return false;

        foreach (var item in data)
        {
            if (!ValidateItem(item))
            {
                Logger.LogWarning("Invalid item data found: {Name}", item.Name ?? "Unknown");
                return false;
            }
        }

        return true;
    }

    private bool ValidateItem(ItemData item)
    {
        // Validate required fields
        if (!ValidateString(item.Id, nameof(item.Id)))
            return false;

        if (!ValidateString(item.Name, nameof(item.Name)))
            return false;

        // Validate category
        var validCategories = new[]
        {
            ItemCategory.Medicine,
            ItemCategory.Pokeball,
            ItemCategory.BattleItem,
            ItemCategory.HeldItem,
            ItemCategory.EvolutionItem,
            ItemCategory.TM,
            ItemCategory.KeyItem,
            ItemCategory.Berry,
            ItemCategory.Miscellaneous,
        };

        if (!validCategories.Contains(item.Category))
        {
            Logger.LogWarning("Item {Name} has invalid category: {Category}", item.Name, item.Category);
            return false;
        }

        // Validate prices
        if (!ValidateRange(item.BuyPrice, nameof(item.BuyPrice), 0, 999999))
            return false;

        if (!ValidateRange(item.SellPrice, nameof(item.SellPrice), 0, 999999))
            return false;

        // Sell price should typically be half of buy price (or 0)
        if (item.BuyPrice > 0 && item.SellPrice > 0)
        {
            var expectedSellPrice = item.BuyPrice / 2;
            if (item.SellPrice != expectedSellPrice && item.SellPrice != 0)
            {
                Logger.LogWarning(
                    "Item {Name} has unusual sell price: {SellPrice} (expected {Expected} or 0)",
                    item.Name,
                    item.SellPrice,
                    expectedSellPrice
                );
            }
        }

        // Validate description
        if (!ValidateString(item.Description, nameof(item.Description)))
            return false;

        // Validate usage flags consistency
        if (!item.UsableInBattle && !item.UsableOutsideBattle && !item.Holdable)
        {
            Logger.LogWarning(
                "Item {Name} has no usage flags set (not usable in battle, outside battle, or holdable)",
                item.Name
            );
        }

        // Key items and TMs should typically not be consumable
        if ((item.Category == ItemCategory.KeyItem || item.Category == ItemCategory.TM) && item.Consumable)
        {
            Logger.LogWarning(
                "Item {Name} is {Category} but marked as consumable",
                item.Name,
                item.Category
            );
        }

        // Validate that Medicine and BattleItems have usage flags
        if (item.Category == ItemCategory.Medicine && !item.UsableInBattle && !item.UsableOutsideBattle)
        {
            Logger.LogWarning(
                "Medicine {Name} is not usable in battle or outside battle",
                item.Name
            );
            return false;
        }

        Logger.LogTrace("Validated item: {Name} (ID: {Id}, Category: {Category})", item.Name, item.Id, item.Category);
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
}
