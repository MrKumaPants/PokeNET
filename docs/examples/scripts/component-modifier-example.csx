// ================================================================
// Component Modifier Example Script (.csx format)
// ================================================================
// This example demonstrates how to modify entity components
// dynamically using the PokeNET scripting API.
//
// Script Type: Component Modification
// File Format: .csx (C# Script)
// API Access: IScriptApi via global 'Api' variable
// ================================================================

using PokeNET.ModApi;
using PokeNET.ModApi.Events;
using System;
using System.Linq;

/// <summary>
/// Dynamic stat modifier that adjusts creature stats based on conditions
///
/// Features:
/// - Weather-based stat modifications
/// - Time-of-day bonuses
/// - Health-based buffs/debuffs
/// - Equipment/item effects
/// - Temporary battle modifiers
///
/// This demonstrates:
/// - Component access and modification
/// - Query system for finding entities
/// - Event-driven modifications
/// - Conditional logic
/// - Safe component manipulation
/// </summary>
public class DynamicStatModifier
{
    private readonly IScriptApi _api;

    // Modifier presets
    private readonly Dictionary<string, StatModifierSet> _modifiers = new()
    {
        ["RainBoost"] = new StatModifierSet
        {
            Speed = 2.0f,  // 2x speed in rain for Water types
            Description = "Rain Dance speed boost"
        },
        ["SunBoost"] = new StatModifierSet
        {
            Attack = 1.5f,
            SpecialAttack = 1.5f,
            Description = "Harsh sunlight power boost"
        },
        ["LowHealthBoost"] = new StatModifierSet
        {
            Attack = 1.5f,
            Description = "Low health desperation boost"
        },
        ["DefensiveStance"] = new StatModifierSet
        {
            Defense = 1.5f,
            SpecialDefense = 1.5f,
            Speed = 0.5f,
            Description = "Defensive stance"
        }
    };

    public DynamicStatModifier(IScriptApi api)
    {
        _api = api;

        // Subscribe to events that trigger stat modifications
        _api.Events.Subscribe<WeatherChangedEvent>(OnWeatherChanged);
        _api.Events.Subscribe<BattleStartEvent>(OnBattleStart);
        _api.Events.Subscribe<HealthChangedEvent>(OnHealthChanged);
        _api.Events.Subscribe<EquipmentChangedEvent>(OnEquipmentChanged);

        _api.Logger.LogInformation("Dynamic Stat Modifier loaded");
    }

    // ============================================================
    // Public Methods - Direct Modification
    // ============================================================

    /// <summary>
    /// Apply a named modifier to a creature
    /// </summary>
    public void ApplyModifier(Entity entity, string modifierName, int duration = -1)
    {
        if (!_modifiers.TryGetValue(modifierName, out var modifier))
        {
            _api.Logger.LogWarning($"Unknown modifier: {modifierName}");
            return;
        }

        _api.Logger.LogInformation(
            $"Applying {modifierName} to {entity.Name}: {modifier.Description}");

        // Store original stats if not already stored
        if (!entity.Has<OriginalStats>())
        {
            StoreOriginalStats(entity);
        }

        // Apply the modifier
        ModifyStats(entity, modifier);

        // Track active modifier
        if (duration > 0)
        {
            entity.Add(new ActiveModifier
            {
                ModifierName = modifierName,
                RemainingTurns = duration,
                AppliedAt = DateTime.UtcNow
            });
        }

        // Recalculate derived stats
        _api.Utilities.RecalculateStats(entity);
    }

    /// <summary>
    /// Remove a specific modifier from a creature
    /// </summary>
    public void RemoveModifier(Entity entity, string modifierName)
    {
        _api.Logger.LogInformation($"Removing {modifierName} from {entity.Name}");

        // Restore original stats
        if (entity.Has<OriginalStats>())
        {
            RestoreOriginalStats(entity);
        }

        // Remove modifier component
        if (entity.Has<ActiveModifier>())
        {
            var modifier = entity.Get<ActiveModifier>();
            if (modifier.ModifierName == modifierName)
            {
                entity.Remove<ActiveModifier>();
            }
        }

        // Recalculate stats
        _api.Utilities.RecalculateStats(entity);
    }

    /// <summary>
    /// Apply custom stat multipliers to a creature
    /// </summary>
    public void ApplyCustomModifier(Entity entity, StatModifierSet modifiers)
    {
        _api.Logger.LogDebug($"Applying custom modifier to {entity.Name}");

        if (!entity.Has<OriginalStats>())
        {
            StoreOriginalStats(entity);
        }

        ModifyStats(entity, modifiers);
        _api.Utilities.RecalculateStats(entity);
    }

    /// <summary>
    /// Reset all modifiers and restore original stats
    /// </summary>
    public void ResetModifiers(Entity entity)
    {
        _api.Logger.LogInformation($"Resetting all modifiers for {entity.Name}");

        if (entity.Has<OriginalStats>())
        {
            RestoreOriginalStats(entity);
            entity.Remove<OriginalStats>();
        }

        if (entity.Has<ActiveModifier>())
        {
            entity.Remove<ActiveModifier>();
        }

        _api.Utilities.RecalculateStats(entity);
    }

    // ============================================================
    // Event Handlers - Automatic Modifications
    // ============================================================

    /// <summary>
    /// Handle weather changes and apply appropriate modifiers
    /// </summary>
    private void OnWeatherChanged(WeatherChangedEvent evt)
    {
        _api.Logger.LogDebug($"Weather changed to: {evt.NewWeather}");

        // Get all active battle participants
        var battleCreatures = _api.Entities.Query<InBattle, CreatureStats>();

        foreach (var entity in battleCreatures)
        {
            // Remove previous weather modifiers
            if (entity.Has<ActiveModifier>())
            {
                var modifier = entity.Get<ActiveModifier>();
                if (modifier.ModifierName.EndsWith("Boost"))
                {
                    RemoveModifier(entity, modifier.ModifierName);
                }
            }

            // Apply new weather modifiers
            ApplyWeatherModifier(entity, evt.NewWeather);
        }
    }

    /// <summary>
    /// Handle battle start and apply initial modifiers
    /// </summary>
    private void OnBattleStart(BattleStartEvent evt)
    {
        _api.Logger.LogInformation("Battle started, checking for ability modifiers");

        // Check for abilities that modify stats at battle start
        foreach (var entity in evt.Participants)
        {
            if (entity.Has<Ability>())
            {
                var ability = entity.Get<Ability>();
                ApplyAbilityModifier(entity, ability.AbilityId);
            }
        }
    }

    /// <summary>
    /// Handle health changes and apply low-health boosts
    /// </summary>
    private void OnHealthChanged(HealthChangedEvent evt)
    {
        ref var health = ref evt.Entity.Get<Health>();
        float healthPercent = (float)health.Current / health.Maximum;

        // Apply low health boost when below 25%
        if (healthPercent < 0.25f && !evt.Entity.Has<ActiveModifier>())
        {
            _api.Logger.LogInformation(
                $"{evt.Entity.Name} is low on health, applying desperation boost!");

            ApplyModifier(evt.Entity, "LowHealthBoost");
        }
        // Remove boost when health is restored
        else if (healthPercent >= 0.25f && evt.Entity.Has<ActiveModifier>())
        {
            var modifier = evt.Entity.Get<ActiveModifier>();
            if (modifier.ModifierName == "LowHealthBoost")
            {
                RemoveModifier(evt.Entity, "LowHealthBoost");
            }
        }
    }

    /// <summary>
    /// Handle equipment changes and apply item modifiers
    /// </summary>
    private void OnEquipmentChanged(EquipmentChangedEvent evt)
    {
        _api.Logger.LogDebug($"{evt.Entity.Name} equipment changed");

        // Reset equipment modifiers
        if (evt.Entity.Has<ActiveModifier>())
        {
            var modifier = evt.Entity.Get<ActiveModifier>();
            if (modifier.ModifierName.StartsWith("Item_"))
            {
                RemoveModifier(evt.Entity, modifier.ModifierName);
            }
        }

        // Apply new equipment modifiers
        if (evt.NewItem != null)
        {
            ApplyItemModifier(evt.Entity, evt.NewItem);
        }
    }

    // ============================================================
    // Private Helper Methods
    // ============================================================

    /// <summary>
    /// Store original stats before modification
    /// </summary>
    private void StoreOriginalStats(Entity entity)
    {
        ref var stats = ref entity.Get<CreatureStats>();

        entity.Add(new OriginalStats
        {
            HP = stats.HP,
            Attack = stats.Attack,
            Defense = stats.Defense,
            SpecialAttack = stats.SpecialAttack,
            SpecialDefense = stats.SpecialDefense,
            Speed = stats.Speed
        });

        _api.Logger.LogDebug($"Stored original stats for {entity.Name}");
    }

    /// <summary>
    /// Restore original stats
    /// </summary>
    private void RestoreOriginalStats(Entity entity)
    {
        ref var original = ref entity.Get<OriginalStats>();
        ref var stats = ref entity.Get<CreatureStats>();

        stats.HP = original.HP;
        stats.Attack = original.Attack;
        stats.Defense = original.Defense;
        stats.SpecialAttack = original.SpecialAttack;
        stats.SpecialDefense = original.SpecialDefense;
        stats.Speed = original.Speed;

        _api.Logger.LogDebug($"Restored original stats for {entity.Name}");
    }

    /// <summary>
    /// Apply stat modifiers to entity
    /// </summary>
    private void ModifyStats(Entity entity, StatModifierSet modifiers)
    {
        ref var stats = ref entity.Get<CreatureStats>();

        if (modifiers.HP != 1.0f)
            stats.HP = (int)(stats.HP * modifiers.HP);
        if (modifiers.Attack != 1.0f)
            stats.Attack = (int)(stats.Attack * modifiers.Attack);
        if (modifiers.Defense != 1.0f)
            stats.Defense = (int)(stats.Defense * modifiers.Defense);
        if (modifiers.SpecialAttack != 1.0f)
            stats.SpecialAttack = (int)(stats.SpecialAttack * modifiers.SpecialAttack);
        if (modifiers.SpecialDefense != 1.0f)
            stats.SpecialDefense = (int)(stats.SpecialDefense * modifiers.SpecialDefense);
        if (modifiers.Speed != 1.0f)
            stats.Speed = (int)(stats.Speed * modifiers.Speed);

        _api.Logger.LogDebug(
            $"Modified stats - Atk:{stats.Attack} Def:{stats.Defense} " +
            $"SpA:{stats.SpecialAttack} SpD:{stats.SpecialDefense} Spe:{stats.Speed}");
    }

    /// <summary>
    /// Apply weather-specific modifiers
    /// </summary>
    private void ApplyWeatherModifier(Entity entity, string weather)
    {
        ref var types = ref entity.Get<CreatureType>();

        switch (weather)
        {
            case "Rain":
                if (types.PrimaryType == "Water" || types.SecondaryType == "Water")
                {
                    ApplyModifier(entity, "RainBoost");
                }
                break;

            case "HarshSunlight":
                if (types.PrimaryType == "Fire" || types.SecondaryType == "Fire")
                {
                    ApplyModifier(entity, "SunBoost");
                }
                break;

            case "Sandstorm":
                if (types.PrimaryType == "Rock" || types.SecondaryType == "Rock" ||
                    types.PrimaryType == "Ground" || types.SecondaryType == "Ground" ||
                    types.PrimaryType == "Steel" || types.SecondaryType == "Steel")
                {
                    // Sandstorm grants immunity and SpD boost
                    var sandstormBoost = new StatModifierSet
                    {
                        SpecialDefense = 1.5f,
                        Description = "Sandstorm SpD boost"
                    };
                    ApplyCustomModifier(entity, sandstormBoost);
                }
                break;
        }
    }

    /// <summary>
    /// Apply ability-based stat modifiers
    /// </summary>
    private void ApplyAbilityModifier(Entity entity, string abilityId)
    {
        // Example: Intimidate lowers opponent's Attack
        if (abilityId == "intimidate")
        {
            _api.Logger.LogInformation($"{entity.Name}'s Intimidate activated!");

            // Get opponent creatures
            var opponents = _api.Entities.Query<InBattle, EnemyControlled>();

            foreach (var opponent in opponents)
            {
                var intimidateDebuff = new StatModifierSet
                {
                    Attack = 0.75f,  // -25% Attack
                    Description = "Intimidate debuff"
                };

                ApplyCustomModifier(opponent, intimidateDebuff);

                _api.Events.Publish(new BattleMessageEvent
                {
                    Message = $"{opponent.Name}'s Attack fell!",
                    Priority = MessagePriority.Ability
                });
            }
        }
    }

    /// <summary>
    /// Apply item-based stat modifiers
    /// </summary>
    private void ApplyItemModifier(Entity entity, string itemId)
    {
        var itemData = _api.Data.GetItem(itemId);

        if (itemData.StatModifiers != null)
        {
            _api.Logger.LogInformation(
                $"Applying item modifiers from {itemData.Name}");

            ApplyCustomModifier(entity, itemData.StatModifiers);

            entity.Add(new ActiveModifier
            {
                ModifierName = $"Item_{itemId}",
                RemainingTurns = -1,  // Permanent while equipped
                AppliedAt = DateTime.UtcNow
            });
        }
    }
}

// ============================================================
// Supporting Types
// ============================================================

/// <summary>
/// Defines a set of stat multipliers
/// </summary>
public class StatModifierSet
{
    public float HP { get; set; } = 1.0f;
    public float Attack { get; set; } = 1.0f;
    public float Defense { get; set; } = 1.0f;
    public float SpecialAttack { get; set; } = 1.0f;
    public float SpecialDefense { get; set; } = 1.0f;
    public float Speed { get; set; } = 1.0f;
    public string Description { get; set; }
}

// ================================================================
// Script Entry Point
// ================================================================
// Return an instance that handles component modifications
// ================================================================

return new DynamicStatModifier(Api);
