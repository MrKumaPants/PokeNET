namespace PokeNET.Core.Data;

/// <summary>
/// Represents static data for an in-game item.
/// Contains all the immutable information about an item (e.g., Potion, Pokeball).
/// </summary>
public class ItemData
{
    /// <summary>
    /// Unique item identifier.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Item name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Item category (Medicine, Pokeball, TM, HeldItem, KeyItem, etc.).
    /// </summary>
    public ItemCategory Category { get; set; }

    /// <summary>
    /// Purchase price at shops (0 if cannot be bought).
    /// </summary>
    public int BuyPrice { get; set; }

    /// <summary>
    /// Sell price at shops (typically half of buy price, 0 if cannot be sold).
    /// </summary>
    public int SellPrice { get; set; }

    /// <summary>
    /// Item description text.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Indicates if the item is consumed on use.
    /// </summary>
    public bool Consumable { get; set; } = true;

    /// <summary>
    /// Indicates if the item can be used in battle.
    /// </summary>
    public bool UsableInBattle { get; set; }

    /// <summary>
    /// Indicates if the item can be used outside of battle.
    /// </summary>
    public bool UsableOutsideBattle { get; set; }

    /// <summary>
    /// Indicates if the item can be held by Pokemon.
    /// </summary>
    public bool Holdable { get; set; }

    /// <summary>
    /// Path to the Roslyn script (.csx) that implements IItemEffect.
    /// Example: "scripts/items/potion.csx"
    /// </summary>
    public string? EffectScript { get; set; }

    /// <summary>
    /// Parameters to pass to the effect script (e.g., healAmount: 20).
    /// </summary>
    public Dictionary<string, object>? EffectParameters { get; set; }

    /// <summary>
    /// Sprite/icon asset path.
    /// </summary>
    public string? SpritePath { get; set; }
}

/// <summary>
/// Item category classification.
/// </summary>
public enum ItemCategory
{
    /// <summary>Healing items (Potion, Full Heal, etc.)</summary>
    Medicine,

    /// <summary>Pokeballs for catching Pokemon</summary>
    Pokeball,

    /// <summary>Battle items (X Attack, Guard Spec, etc.)</summary>
    BattleItem,

    /// <summary>Items held by Pokemon for passive effects</summary>
    HeldItem,

    /// <summary>Evolution stones and similar items</summary>
    EvolutionItem,

    /// <summary>Technical/Hidden Machines</summary>
    TM,

    /// <summary>Key items required for story progression</summary>
    KeyItem,

    /// <summary>Berries with various effects</summary>
    Berry,

    /// <summary>Miscellaneous items</summary>
    Miscellaneous,
}
