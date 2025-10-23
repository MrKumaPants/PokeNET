// ================================================================
// Move Effect Example Script (.csx format)
// ================================================================
// This example demonstrates how to create a custom move effect
// using the PokeNET scripting API.
//
// Script Type: Move Effect
// File Format: .csx (C# Script)
// API Access: IScriptApi via global 'Api' variable
// ================================================================

using PokeNET.ModApi;
using PokeNET.ModApi.Battle;
using PokeNET.ModApi.Events;
using System;

/// <summary>
/// Custom move effect: "Thunder Strike"
///
/// Effect: Deals electric damage with 30% chance to paralyze.
///         Has 10% increased critical hit chance.
///
/// This demonstrates:
/// - Accessing battle context and entities
/// - Modifying components (Health, Status)
/// - Using utility functions for calculations
/// - Publishing battle events
/// - Random chance mechanics
/// - Logging and debugging
/// </summary>
public class ThunderStrikeEffect
{
    private readonly IScriptApi _api;

    // Constants for move behavior
    private const float PARALYZE_CHANCE = 0.30f;     // 30% chance
    private const float CRIT_BONUS = 0.10f;          // +10% crit rate
    private const int BASE_POWER = 80;

    public ThunderStrikeEffect(IScriptApi api)
    {
        _api = api;
        _api.Logger.LogInformation("Thunder Strike move effect loaded");
    }

    /// <summary>
    /// Main execution method called when move is used
    /// </summary>
    public MoveResult Execute(MoveContext context)
    {
        var result = new MoveResult();

        _api.Logger.LogDebug(
            $"Executing Thunder Strike: {context.User.Name} -> {context.Target.Name}");

        // Step 1: Check if move hits
        if (!CheckAccuracy(context))
        {
            result.Missed = true;
            PublishMissMessage(context);
            return result;
        }

        // Step 2: Calculate damage with critical hit consideration
        var damage = CalculateDamage(context, out bool isCritical);
        result.DamageDealt = damage;
        result.WasCritical = isCritical;

        // Step 3: Apply damage to target
        ApplyDamage(context.Target, damage);

        // Step 4: Check for paralysis effect
        if (_api.Utilities.RandomChance(PARALYZE_CHANCE))
        {
            ApplyParalysis(context.Target);
            result.StatusInflicted = "Paralyzed";
        }

        // Step 5: Publish battle message
        PublishBattleMessage(context, result);

        _api.Logger.LogInformation(
            $"Thunder Strike result: {damage} damage, Critical={isCritical}, " +
            $"Paralyzed={result.StatusInflicted != null}");

        return result;
    }

    /// <summary>
    /// Check if the move hits the target
    /// </summary>
    private bool CheckAccuracy(MoveContext context)
    {
        // Thunder Strike has 95% base accuracy
        const float BASE_ACCURACY = 0.95f;

        // Get accuracy and evasion modifiers from battle stats
        ref var userStats = ref context.User.Get<BattleStats>();
        ref var targetStats = ref context.Target.Get<BattleStats>();

        // Calculate final accuracy (simplified formula)
        float accuracy = BASE_ACCURACY *
            (userStats.AccuracyStage / targetStats.EvasionStage);

        return _api.Utilities.RandomChance(accuracy);
    }

    /// <summary>
    /// Calculate damage with critical hit mechanics
    /// </summary>
    private int CalculateDamage(MoveContext context, out bool isCritical)
    {
        // Check for critical hit with bonus rate
        isCritical = CheckCriticalHit(context);

        // Get attacker's special attack stat (Electric moves are usually special)
        ref var attackerStats = ref context.User.Get<CreatureStats>();
        ref var defenderStats = ref context.Target.Get<CreatureStats>();

        // Base damage calculation
        // Damage = ((2 * Level / 5 + 2) * Power * Attack / Defense / 50 + 2) * Modifiers
        var userLevel = context.User.Get<Level>().Current;

        int baseDamage = ((2 * userLevel / 5 + 2) * BASE_POWER *
                         attackerStats.SpecialAttack / defenderStats.SpecialDefense / 50) + 2;

        // Apply critical hit multiplier
        if (isCritical)
        {
            baseDamage = (int)(baseDamage * 1.5f);
        }

        // Apply type effectiveness
        float effectiveness = _api.Utilities.GetTypeEffectiveness(
            "Electric",
            context.Target.Get<CreatureType>());

        baseDamage = (int)(baseDamage * effectiveness);

        // Apply random variance (85% - 100%)
        float variance = _api.Utilities.RandomFloat(0.85f, 1.0f);
        int finalDamage = (int)(baseDamage * variance);

        // Apply STAB (Same Type Attack Bonus) if user is Electric type
        if (HasType(context.User, "Electric"))
        {
            finalDamage = (int)(finalDamage * 1.5f);
        }

        // Ensure minimum damage of 1
        return Math.Max(1, finalDamage);
    }

    /// <summary>
    /// Check for critical hit with bonus chance
    /// </summary>
    private bool CheckCriticalHit(MoveContext context)
    {
        // Base crit rate is 1/24 (≈4.17%)
        float baseCritRate = 1f / 24f;

        // Thunder Strike has +10% bonus
        float finalCritRate = baseCritRate + CRIT_BONUS;

        // Check for increased crit rate items/abilities
        if (context.User.Has<FocusEnergy>())
        {
            finalCritRate *= 4f; // Focus Energy quadruples crit rate
        }

        return _api.Utilities.RandomChance(finalCritRate);
    }

    /// <summary>
    /// Apply damage to target creature
    /// </summary>
    private void ApplyDamage(Entity target, int damage)
    {
        ref var health = ref target.Get<Health>();

        int oldHP = health.Current;
        health.Current = Math.Max(0, health.Current - damage);

        _api.Logger.LogDebug(
            $"{target.Name} HP: {oldHP} -> {health.Current} (-{damage})");

        // Check if target fainted
        if (health.Current == 0)
        {
            _api.Events.Publish(new CreatureFaintedEvent
            {
                Victim = target,
                Timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Apply paralysis status to target
    /// </summary>
    private void ApplyParalysis(Entity target)
    {
        // Check if target can be paralyzed
        if (target.Has<StatusCondition>())
        {
            _api.Logger.LogDebug($"{target.Name} already has status, cannot paralyze");
            return;
        }

        // Check if target is immune (Electric-type immunity)
        if (HasType(target, "Electric"))
        {
            _api.Logger.LogDebug($"{target.Name} is Electric-type, immune to paralysis");
            return;
        }

        // Apply paralysis
        target.Add(new StatusCondition
        {
            Type = StatusType.Paralyzed,
            TurnCount = 0
        });

        // Reduce speed by 50%
        ref var stats = ref target.Get<CreatureStats>();
        stats.Speed = (int)(stats.Speed * 0.5f);

        _api.Logger.LogInformation($"{target.Name} was paralyzed!");
    }

    /// <summary>
    /// Check if creature has specific type
    /// </summary>
    private bool HasType(Entity creature, string typeName)
    {
        ref var types = ref creature.Get<CreatureType>();
        return types.PrimaryType == typeName ||
               types.SecondaryType == typeName;
    }

    /// <summary>
    /// Publish battle message for move result
    /// </summary>
    private void PublishBattleMessage(MoveContext context, MoveResult result)
    {
        string message = $"{context.User.Name} used Thunder Strike!";

        if (result.WasCritical)
        {
            message += " A critical hit!";
        }

        if (result.StatusInflicted != null)
        {
            message += $" {context.Target.Name} was paralyzed!";
        }

        _api.Events.Publish(new BattleMessageEvent
        {
            Message = message,
            Priority = MessagePriority.MoveEffect
        });
    }

    /// <summary>
    /// Publish miss message
    /// </summary>
    private void PublishMissMessage(MoveContext context)
    {
        _api.Events.Publish(new BattleMessageEvent
        {
            Message = $"{context.User.Name}'s attack missed!",
            Priority = MessagePriority.MoveEffect
        });

        _api.Logger.LogDebug("Thunder Strike missed");
    }
}

// ================================================================
// Script Entry Point
// ================================================================
// The script must return an instance that can execute the move
// The global 'Api' variable provides access to the scripting API
// ================================================================

return new ThunderStrikeEffect(Api);
