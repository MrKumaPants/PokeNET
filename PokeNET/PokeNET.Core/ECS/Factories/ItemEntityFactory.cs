using System.Collections.Generic;
using Arch.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework;
using PokeNET.Domain.ECS.Commands;
using PokeNET.Domain.ECS.Components;
using PokeNET.Domain.ECS.Events;
using PokeNET.Domain.ECS.Factories;

namespace PokeNET.Core.ECS.Factories;

/// <summary>
/// Specialized factory for creating item entities (collectibles, powerups, etc).
/// Items are typically non-moving entities with collision detection.
/// </summary>
public sealed class ItemEntityFactory : EntityFactory
{
    protected override string FactoryName => "ItemFactory";

    public ItemEntityFactory(ILogger<ItemEntityFactory> logger, IEventBus? eventBus = null)
        : base(logger, eventBus)
    {
        RegisterDefaultTemplates();
    }

    /// <summary>
    /// Creates a health potion item.
    /// Phase 6: Now uses CommandBuffer for safe deferred entity creation.
    /// </summary>
    public CommandBuffer.CreateCommand CreateHealthPotion(
        CommandBuffer cmd,
        Vector2 position,
        int healAmount = 50
    )
    {
        var definition = new EntityDefinition(
            "HealthPotion",
            new object[]
            {
                new Position(position.X, position.Y),
                new Sprite("sprites/items/health_potion.png", 16, 16, 0.3f),
                new Renderable(true),
            },
            new Dictionary<string, object>
            {
                ["ItemType"] = "Consumable",
                ["HealAmount"] = healAmount,
                ["Rarity"] = "Common",
            }
        );

        return Create(cmd, definition);
    }

    /// <summary>
    /// Creates a coin/currency item.
    /// Phase 6: Now uses CommandBuffer for safe deferred entity creation.
    /// </summary>
    public CommandBuffer.CreateCommand CreateCoin(
        CommandBuffer cmd,
        Vector2 position,
        int value = 1
    )
    {
        var definition = new EntityDefinition(
            "Coin",
            new object[]
            {
                new Position(position.X, position.Y),
                new Sprite("sprites/items/coin.png", 12, 12, 0.3f),
                new Renderable(true),
            },
            new Dictionary<string, object> { ["ItemType"] = "Currency", ["Value"] = value }
        );

        return Create(cmd, definition);
    }

    /// <summary>
    /// Creates a speed boost powerup.
    /// Phase 6: Now uses CommandBuffer for safe deferred entity creation.
    /// </summary>
    public CommandBuffer.CreateCommand CreateSpeedBoost(
        CommandBuffer cmd,
        Vector2 position,
        float duration = 5f
    )
    {
        var definition = new EntityDefinition(
            "SpeedBoost",
            new object[]
            {
                new Position(position.X, position.Y),
                new Sprite("sprites/items/speed_boost.png", 16, 16, 0.3f),
                new Renderable(true),
            },
            new Dictionary<string, object>
            {
                ["ItemType"] = "PowerUp",
                ["Duration"] = duration,
                ["SpeedMultiplier"] = 1.5f,
                ["Rarity"] = "Uncommon",
            }
        );

        return Create(cmd, definition);
    }

    /// <summary>
    /// Creates a shield powerup.
    /// Phase 6: Now uses CommandBuffer for safe deferred entity creation.
    /// </summary>
    public CommandBuffer.CreateCommand CreateShield(
        CommandBuffer cmd,
        Vector2 position,
        float duration = 10f
    )
    {
        var definition = new EntityDefinition(
            "Shield",
            new object[]
            {
                new Position(position.X, position.Y),
                new Sprite("sprites/items/shield.png", 16, 16, 0.3f),
                new Renderable(true),
            },
            new Dictionary<string, object>
            {
                ["ItemType"] = "PowerUp",
                ["Duration"] = duration,
                ["DamageReduction"] = 0.75f,
                ["Rarity"] = "Rare",
            }
        );

        return Create(cmd, definition);
    }

    /// <summary>
    /// Creates a key item for unlocking areas.
    /// Phase 6: Now uses CommandBuffer for safe deferred entity creation.
    /// </summary>
    public CommandBuffer.CreateCommand CreateKey(CommandBuffer cmd, Vector2 position, string keyId)
    {
        var definition = new EntityDefinition(
            $"Key_{keyId}",
            new object[]
            {
                new Position(position.X, position.Y),
                new Sprite("sprites/items/key.png", 12, 16, 0.3f),
                new Renderable(true),
            },
            new Dictionary<string, object>
            {
                ["ItemType"] = "Key",
                ["KeyId"] = keyId,
                ["Rarity"] = "Special",
            }
        );

        return Create(cmd, definition);
    }

    private void RegisterDefaultTemplates()
    {
        RegisterTemplate(
            "item_health_potion",
            new EntityDefinition(
                "HealthPotion",
                new object[]
                {
                    new Position(0, 0),
                    new Sprite("sprites/items/health_potion.png", 16, 16, 0.3f),
                    new Renderable(true),
                }
            )
        );

        RegisterTemplate(
            "item_coin",
            new EntityDefinition(
                "Coin",
                new object[]
                {
                    new Position(0, 0),
                    new Sprite("sprites/items/coin.png", 12, 12, 0.3f),
                    new Renderable(true),
                }
            )
        );

        RegisterTemplate(
            "item_speed_boost",
            new EntityDefinition(
                "SpeedBoost",
                new object[]
                {
                    new Position(0, 0),
                    new Sprite("sprites/items/speed_boost.png", 16, 16, 0.3f),
                    new Renderable(true),
                }
            )
        );

        RegisterTemplate(
            "item_shield",
            new EntityDefinition(
                "Shield",
                new object[]
                {
                    new Position(0, 0),
                    new Sprite("sprites/items/shield.png", 16, 16, 0.3f),
                    new Renderable(true),
                }
            )
        );
    }
}
