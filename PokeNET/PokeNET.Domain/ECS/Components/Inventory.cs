using System;
using System.Collections.Generic;
using System.Linq;

namespace PokeNET.Domain.ECS.Components;

/// <summary>
/// Component representing a trainer's item inventory.
/// Manages item storage, quantities, and category organization.
/// </summary>
public sealed class Inventory
{
    private const int MaxUniqueItems = 999;
    private readonly Dictionary<int, int> _items = new();

    /// <summary>
    /// Gets the current number of unique items in inventory.
    /// </summary>
    public int UniqueItemCount => _items.Count;

    /// <summary>
    /// Indicates whether the inventory has reached its unique item limit.
    /// </summary>
    public bool IsFull => _items.Count >= MaxUniqueItems;

    /// <summary>
    /// Adds an item to the inventory.
    /// </summary>
    /// <param name="itemId">ID of the item to add.</param>
    /// <param name="quantity">Number of items to add (default 1).</param>
    /// <returns>True if successfully added, false if inventory is full.</returns>
    public bool AddItem(int itemId, int quantity = 1)
    {
        if (itemId <= 0)
            throw new ArgumentException("Item ID must be positive", nameof(itemId));
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be positive", nameof(quantity));

        if (_items.ContainsKey(itemId))
        {
            // Item already exists, increase quantity (cap at 999)
            _items[itemId] = Math.Min(_items[itemId] + quantity, 999);
            return true;
        }

        if (IsFull)
            return false;

        _items[itemId] = Math.Min(quantity, 999);
        return true;
    }

    /// <summary>
    /// Removes an item from the inventory.
    /// </summary>
    /// <param name="itemId">ID of the item to remove.</param>
    /// <param name="quantity">Number of items to remove (default 1).</param>
    /// <returns>True if successfully removed, false if insufficient quantity.</returns>
    public bool RemoveItem(int itemId, int quantity = 1)
    {
        if (itemId <= 0)
            throw new ArgumentException("Item ID must be positive", nameof(itemId));
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be positive", nameof(quantity));

        if (!_items.ContainsKey(itemId) || _items[itemId] < quantity)
            return false;

        _items[itemId] -= quantity;
        if (_items[itemId] <= 0)
            _items.Remove(itemId);

        return true;
    }

    /// <summary>
    /// Checks if the inventory contains a specific item.
    /// </summary>
    /// <param name="itemId">ID of the item to check.</param>
    /// <param name="quantity">Minimum quantity required (default 1).</param>
    public bool HasItem(int itemId, int quantity = 1)
    {
        return _items.TryGetValue(itemId, out int count) && count >= quantity;
    }

    /// <summary>
    /// Gets the quantity of a specific item in the inventory.
    /// </summary>
    /// <returns>Quantity of the item, or 0 if not present.</returns>
    public int GetItemCount(int itemId)
    {
        return _items.TryGetValue(itemId, out int count) ? count : 0;
    }

    /// <summary>
    /// Gets all items in the inventory.
    /// </summary>
    /// <returns>Dictionary of item IDs and their quantities.</returns>
    public IReadOnlyDictionary<int, int> GetAllItems()
    {
        return _items;
    }

    /// <summary>
    /// Gets items by category.
    /// </summary>
    /// <param name="category">Item category to filter by.</param>
    /// <returns>Dictionary of matching items and quantities.</returns>
    public IReadOnlyDictionary<int, int> GetItemsByCategory(ItemCategory category)
    {
        // TODO: In full implementation, filter by actual item category from item database
        // For now, returns all items as placeholder
        return _items;
    }

    /// <summary>
    /// Clears all items from the inventory.
    /// </summary>
    public void Clear()
    {
        _items.Clear();
    }

    /// <summary>
    /// Gets the total number of items (sum of all quantities).
    /// </summary>
    public int TotalItemCount => _items.Values.Sum();
}

/// <summary>
/// Categories of items in the Pokémon world.
/// </summary>
public enum ItemCategory
{
    /// <summary>
    /// HP and status restoration items (Potions, Antidotes, etc.).
    /// </summary>
    Medicine,

    /// <summary>
    /// Poké Balls for catching Pokémon.
    /// </summary>
    Pokeball,

    /// <summary>
    /// Items used during battle (X Attack, Guard Spec, etc.).
    /// </summary>
    BattleItem,

    /// <summary>
    /// Important story items that cannot be sold or discarded.
    /// </summary>
    KeyItem,

    /// <summary>
    /// Technical Machines for teaching moves.
    /// </summary>
    TM,

    /// <summary>
    /// Hold items for Pokémon.
    /// </summary>
    HoldItem,

    /// <summary>
    /// Evolution stones and other evolution items.
    /// </summary>
    EvolutionItem,

    /// <summary>
    /// Berries for various effects.
    /// </summary>
    Berry,

    /// <summary>
    /// Miscellaneous items.
    /// </summary>
    Other,
}
