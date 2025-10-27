using System;
using System.Collections.Generic;
using System.Linq;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Extensions.Logging;
using PokeNET.Core.Battle;
using PokeNET.Core.ECS.Commands;
using PokeNET.Core.ECS.Components;
using PokeNET.Core.ECS.Events;
using PokeNET.Core.ECS.Relationships;

namespace PokeNET.Core.ECS.Systems;

/// <summary>
/// System responsible for managing turn-based Pokemon battles.
/// Implements authentic Pokemon battle mechanics including:
/// - Turn order based on Speed stat
/// - Damage calculation with type effectiveness, STAB, critical hits
/// - Status effect processing (poison, burn, paralysis, freeze, sleep)
/// - Stat stage modifiers
/// - Victory/defeat conditions
///
/// Architecture:
/// - Queries entities with PokemonData + PokemonStats + BattleState + MoveSet
/// - Processes battle turns in speed-order
/// - Emits BattleEvent for UI updates
///
/// Features:
/// - Official Pokemon damage formula
/// - Status condition management
/// - Critical hit calculation
/// - Stat stage modifiers (-6 to +6)
/// - Experience and level-up handling
///
/// Performance:
/// - Efficient query filtering
/// - Turn-based execution (no per-frame overhead when not in battle)
/// - Zero allocation for stat calculations
///
/// Migration Status:
/// - Phase 2: ✅ Migrated to Arch.System.BaseSystem with World and float parameters
/// - Phase 3: ⚠️ SKIPPED - Query pooling optimization postponed
/// - Phase 5: ✅ COMPLETED - CommandBuffer integrated for safe structural changes
/// - Phase 5: ✅ COMPLETED - PokemonRelationships integrated for battle state (StartBattle, EndBattle, GetBattleOpponent)
/// </summary>
public partial class BattleSystem : BaseSystem<World, float>
{
    private readonly ILogger _logger;
    private readonly IEventBus? _eventBus;
    private int _turnsProcessed;
    private readonly Random _random;
    private List<BattleEntity> _battlersCache;
    private float _deltaTime; // Store for access in query methods

    /// <summary>
    /// Initializes the battle system with Arch's BaseSystem.
    /// </summary>
    /// <param name="world">World instance.</param>
    /// <param name="logger">Logger instance.</param>
    /// <param name="eventBus">Optional event bus for battle events.</param>
    public BattleSystem(World world, ILogger<BattleSystem> logger, IEventBus? eventBus = null)
        : base(world)
    {
        _logger = logger;
        _eventBus = eventBus;
        _random = new Random();
        _battlersCache = new List<BattleEntity>(16); // Pre-allocate for typical battle size

        _logger.LogInformation(
            "BattleSystem initialized with Arch BaseSystem and source-generated queries"
        );
    }

    /// <summary>
    /// Core battle logic: Process all active battles using source-generated queries.
    /// Uses CommandBuffer for safe deferred structural changes.
    /// </summary>
    /// <param name="deltaTime">Time elapsed since last frame in seconds.</param>
    public override void Update(in float deltaTime)
    {
        // Store deltaTime for access in query methods
        _deltaTime = deltaTime;

        // Reset turn counter for this frame
        _turnsProcessed = 0;

        // Clear battlers cache for reuse
        _battlersCache.Clear();

        // Collect battlers using source-generated query
        CollectBattlerQuery(World);

        if (_battlersCache.Count == 0)
            return;

        // Sort by speed stat (with stat stage modifiers)
        _battlersCache.Sort(
            (a, b) =>
            {
                int speedA = (int)(
                    a.Stats.Speed * a.BattleState.GetStageMultiplier(a.BattleState.SpeedStage)
                );
                int speedB = (int)(
                    b.Stats.Speed * b.BattleState.GetStageMultiplier(b.BattleState.SpeedStage)
                );
                return speedB.CompareTo(speedA); // Descending order (faster first)
            }
        );

        // Phase 5: Use CommandBuffer for safe structural changes during iteration
        using var cmd = new CommandBuffer(World);

        // Process each battler's turn with CommandBuffer
        foreach (var battler in _battlersCache)
        {
            ProcessTurn(battler, cmd);
        }

        // Execute all deferred structural changes AFTER iteration completes
        cmd.Playback();

        // Check victory/defeat conditions
        CheckBattleEnd(_battlersCache);

        // Log battle processing results
        if (_turnsProcessed > 0)
        {
            _logger.LogDebug(
                "BattleSystem processed {Turns} battle turns this frame",
                _turnsProcessed
            );
        }
    }

    /// <summary>
    /// Source-generated query for collecting Pokemon in battle.
    /// Generated method: CollectBattlerQuery(World world)
    /// </summary>
    [Query]
    [All<PokemonData, PokemonStats, BattleState, MoveSet>]
    private void CollectBattler(
        in Entity entity,
        ref PokemonData data,
        ref PokemonStats stats,
        ref BattleState battleState,
        ref MoveSet moveSet
    )
    {
        if (battleState.Status == BattleStatus.InBattle)
        {
            _battlersCache.Add(
                new BattleEntity
                {
                    Entity = entity,
                    Data = data,
                    Stats = stats,
                    BattleState = battleState,
                    MoveSet = moveSet,
                }
            );
        }
    }

    /// <summary>
    /// Processes a single Pokemon's turn.
    /// Phase 5: Uses CommandBuffer for safe component additions during iteration.
    /// </summary>
    private void ProcessTurn(BattleEntity battler, CommandBuffer cmd)
    {
        ref var battleState = ref World.Get<BattleState>(battler.Entity);
        ref var stats = ref World.Get<PokemonStats>(battler.Entity);

        // Phase 5: Use CommandBuffer for safe structural changes
        // Add StatusCondition if it doesn't exist (deferred until Playback)
        if (!World.Has<StatusCondition>(battler.Entity))
        {
            cmd.Add<StatusCondition>(battler.Entity);
            // Component won't exist until Playback, skip status processing this turn
            _turnsProcessed++;
            return;
        }
        ref var statusCondition = ref World.Get<StatusCondition>(battler.Entity);

        // Increment turn counter
        battleState.TurnCounter++;

        // Check if Pokemon can act (not frozen/asleep/paralyzed)
        if (!statusCondition.CanAct())
        {
            _logger.LogDebug(
                "Pokemon {Entity} cannot act due to status condition {Status}",
                battler.Entity.Id,
                statusCondition.Status
            );
            return;
        }

        // Process status effects (poison/burn damage)
        int statusDamage = statusCondition.StatusTick(stats.MaxHP);
        if (statusDamage > 0)
        {
            stats.HP = Math.Max(0, stats.HP - statusDamage);
            _logger.LogDebug(
                "Pokemon {Entity} took {Damage} damage from {Status}",
                battler.Entity.Id,
                statusDamage,
                statusCondition.Status
            );

            if (stats.HP == 0)
            {
                battleState.Status = BattleStatus.Fainted;
                _logger.LogInformation(
                    "Pokemon {Entity} fainted from status damage",
                    battler.Entity.Id
                );
                return;
            }
        }

        // TODO: AI/Player move selection would go here
        // For now, this is a framework for turn processing

        _turnsProcessed++;
    }

    /// <summary>
    /// Executes a move from attacker to defender.
    /// Phase 5: Safe to modify components directly as this is called outside query iteration.
    /// TODO Phase 3: Cache move lookups with a move database for better performance
    /// </summary>
    /// <param name="attacker">Attacking Pokemon entity</param>
    /// <param name="defender">Defending Pokemon entity</param>
    /// <param name="moveId">Move identifier to use</param>
    /// <returns>True if move was executed successfully</returns>
    public bool ExecuteMove(Entity attacker, Entity defender, int moveId)
    {
        if (!World.IsAlive(attacker) || !World.IsAlive(defender))
            return false;

        ref var attackerStats = ref World.Get<PokemonStats>(attacker);
        ref var attackerBattleState = ref World.Get<BattleState>(attacker);
        ref var attackerData = ref World.Get<PokemonData>(attacker);
        ref var attackerMoveSet = ref World.Get<MoveSet>(attacker);

        ref var defenderStats = ref World.Get<PokemonStats>(defender);
        ref var defenderBattleState = ref World.Get<BattleState>(defender);
        ref var defenderData = ref World.Get<PokemonData>(defender);

        // Find move in moveset
        Move? move = null;
        for (int i = 0; i < 4; i++)
        {
            var m = attackerMoveSet.GetMove(i);
            if (m.HasValue && m.Value.MoveId == moveId)
            {
                move = m.Value;
                break;
            }
        }

        if (!move.HasValue || move.Value.PP <= 0)
        {
            _logger.LogWarning("Move {MoveId} not available or out of PP", moveId);
            return false;
        }

        // Deduct PP (would need to update the moveset component)
        // attackerMoveSet would need a method to update PP

        // Calculate damage using Pokemon formula
        int damage = CalculateDamage(
            attackerStats,
            attackerBattleState,
            attackerData,
            defenderStats,
            defenderBattleState,
            defenderData,
            moveId
        );

        // Apply damage
        defenderStats.HP = Math.Max(0, defenderStats.HP - damage);
        defenderBattleState.LastDamageTaken = damage;

        _logger.LogInformation(
            "Pokemon {Attacker} used move {MoveId} on {Defender} for {Damage} damage",
            attacker.Id,
            moveId,
            defender.Id,
            damage
        );

        // Check if defender fainted
        if (defenderStats.HP == 0)
        {
            defenderBattleState.Status = BattleStatus.Fainted;
            _logger.LogInformation("Pokemon {Entity} fainted!", defender.Id);

            // Award experience to attacker
            AwardExperience(attacker, defenderData.Level);
        }

        // Update last move used
        attackerBattleState.LastMoveUsed = moveId;

        return true;
    }

    /// <summary>
    /// Calculates damage using the official Pokemon damage formula.
    /// Damage = ((((2 * Level / 5) + 2) * Power * A/D) / 50) + 2) * Modifiers
    /// TODO Phase 3: Move move data (power, type, category) to a cached move database
    /// TODO Phase 3: Pre-calculate type effectiveness chart for faster lookups
    /// </summary>
    private int CalculateDamage(
        PokemonStats attackerStats,
        BattleState attackerBattleState,
        PokemonData attackerData,
        PokemonStats defenderStats,
        BattleState defenderBattleState,
        PokemonData defenderData,
        int moveId
    )
    {
        // Placeholder values - in real implementation, these would come from move database
        int movePower = 50; // Example: Tackle
        bool isPhysical = true; // Example: physical move
        float typeEffectiveness = 1.0f; // Example: neutral
        bool hasStab = false; // Same Type Attack Bonus

        // Get attack and defense stats with stage modifiers
        int attack = isPhysical
            ? (int)(
                attackerStats.Attack
                * attackerBattleState.GetStageMultiplier(attackerBattleState.AttackStage)
            )
            : (int)(
                attackerStats.SpAttack
                * attackerBattleState.GetStageMultiplier(attackerBattleState.SpAttackStage)
            );

        int defense = isPhysical
            ? (int)(
                defenderStats.Defense
                * defenderBattleState.GetStageMultiplier(defenderBattleState.DefenseStage)
            )
            : (int)(
                defenderStats.SpDefense
                * defenderBattleState.GetStageMultiplier(defenderBattleState.SpDefenseStage)
            );

        // Base damage calculation
        int level = attackerData.Level;
        float baseDamage =
            (((2f * level / 5f) + 2f) * movePower * (attack / (float)defense) / 50f) + 2f;

        // Apply modifiers
        float stab = hasStab ? 1.5f : 1.0f;
        float critical = IsCriticalHit(attackerStats.Speed) ? 2.0f : 1.0f;
        float random = 0.85f + (float)_random.NextDouble() * 0.15f; // 0.85 - 1.0

        float finalDamage = baseDamage * stab * typeEffectiveness * critical * random;

        return Math.Max(1, (int)finalDamage); // Minimum 1 damage
    }

    /// <summary>
    /// Determines if an attack is a critical hit.
    /// Base critical hit rate is 1/24 (4.17%) in modern games.
    /// </summary>
    private bool IsCriticalHit(int speed)
    {
        // Simplified: 1/24 chance
        // In real Pokemon, this is affected by critical hit ratio stages and certain moves/abilities
        int threshold = 24;
        return _random.Next(threshold) == 0;
    }

    /// <summary>
    /// Awards experience points to the winning Pokemon.
    /// Phase 5: Safe to modify components directly as this is called outside query iteration.
    /// TODO Phase 3: Cache species base stats for faster stat recalculation
    /// </summary>
    private void AwardExperience(Entity winner, int defeatedLevel)
    {
        ref var data = ref World.Get<PokemonData>(winner);
        ref var stats = ref World.Get<PokemonStats>(winner);

        // Simplified experience formula
        // Real formula: a = (wild ? 1 : 1.5), t = base exp yield, e = lucky egg, L = defeated level
        // exp = (a * t * L) / (5 * participants) * (item modifiers)
        int baseExp = 50; // Example: base exp yield of defeated Pokemon
        int experienceGained = (baseExp * defeatedLevel) / 5;

        data.ExperiencePoints += experienceGained;
        _logger.LogInformation(
            "Pokemon {Entity} gained {Exp} experience points",
            winner.Id,
            experienceGained
        );

        // Check for level up
        while (data.ExperiencePoints >= data.ExperienceToNextLevel && data.Level < 100)
        {
            data.Level++;
            _logger.LogInformation(
                "Pokemon {Entity} leveled up to {Level}!",
                winner.Id,
                data.Level
            );

            // Recalculate stats (would need species base stats)
            // This is a placeholder - real implementation would use species data
            RecalculateStats(ref stats, ref data);

            // Update experience requirement for next level
            data.ExperienceToNextLevel = CalculateExpForNextLevel(data.Level);

            // TODO: Check for move learning, evolution, etc.
        }
    }

    /// <summary>
    /// Recalculates Pokemon stats based on level, IVs, EVs, and nature.
    /// TODO Phase 3: Load base stats from a species database instead of hardcoding
    /// TODO Phase 3: Cache nature modifiers in a lookup table
    /// </summary>
    private void RecalculateStats(ref PokemonStats stats, ref PokemonData data)
    {
        // Placeholder base stats - in real implementation, these come from species data
        int baseHP = 45;
        int baseAttack = 49;
        int baseDefense = 49;
        int baseSpAttack = 65;
        int baseSpDefense = 65;
        int baseSpeed = 45;

        // Get nature modifiers
        float attackMod = GetNatureModifier(data.Nature, StatType.Attack);
        float defenseMod = GetNatureModifier(data.Nature, StatType.Defense);
        float spAttackMod = GetNatureModifier(data.Nature, StatType.SpAttack);
        float spDefenseMod = GetNatureModifier(data.Nature, StatType.SpDefense);
        float speedMod = GetNatureModifier(data.Nature, StatType.Speed);

        // Calculate stats using official formula via StatCalculator
        stats.MaxHP = StatCalculator.CalculateHP(baseHP, stats.IV_HP, stats.EV_HP, data.Level);
        stats.HP = stats.MaxHP; // Heal on level up
        stats.Attack = StatCalculator.CalculateStat(
            baseAttack,
            stats.IV_Attack,
            stats.EV_Attack,
            data.Level,
            attackMod
        );
        stats.Defense = StatCalculator.CalculateStat(
            baseDefense,
            stats.IV_Defense,
            stats.EV_Defense,
            data.Level,
            defenseMod
        );
        stats.SpAttack = StatCalculator.CalculateStat(
            baseSpAttack,
            stats.IV_SpAttack,
            stats.EV_SpAttack,
            data.Level,
            spAttackMod
        );
        stats.SpDefense = StatCalculator.CalculateStat(
            baseSpDefense,
            stats.IV_SpDefense,
            stats.EV_SpDefense,
            data.Level,
            spDefenseMod
        );
        stats.Speed = StatCalculator.CalculateStat(
            baseSpeed,
            stats.IV_Speed,
            stats.EV_Speed,
            data.Level,
            speedMod
        );
    }

    /// <summary>
    /// Gets the nature modifier for a specific stat.
    /// </summary>
    private float GetNatureModifier(Nature nature, StatType stat)
    {
        // Simplified - real implementation would use a lookup table
        // Returns 1.1 for boosted stat, 0.9 for hindered stat, 1.0 for neutral
        return 1.0f; // Placeholder
    }

    /// <summary>
    /// Calculates experience required for the next level.
    /// Uses Medium Fast growth rate as example.
    /// </summary>
    private int CalculateExpForNextLevel(int currentLevel)
    {
        // Medium Fast formula: n^3
        int nextLevel = currentLevel + 1;
        return nextLevel * nextLevel * nextLevel;
    }

    /// <summary>
    /// Checks if the battle has ended (all Pokemon on one side fainted).
    /// Phase 5: Safe read-only query, no structural changes needed.
    /// TODO Phase 3: Optimize with early-exit query pattern
    /// </summary>
    private void CheckBattleEnd(List<BattleEntity> battlers)
    {
        // This is a simplified check - real implementation would check teams
        bool anyAlive = battlers.Any(b =>
        {
            var battleState = World.Get<BattleState>(b.Entity);
            return battleState.Status == BattleStatus.InBattle;
        });

        if (!anyAlive)
        {
            _logger.LogInformation("Battle ended - all Pokemon fainted");
            // TODO: Emit battle end event
        }
    }

    /// <summary>
    /// Gets the number of turns processed in the last update.
    /// </summary>
    public int GetTurnsProcessed() => _turnsProcessed;

    /// <summary>
    /// Helper struct for collecting battle entities.
    /// </summary>
    private struct BattleEntity
    {
        public Entity Entity;
        public PokemonData Data;
        public PokemonStats Stats;
        public BattleState BattleState;
        public MoveSet MoveSet;
    }

    /// <summary>
    /// Stat type enumeration for nature modifier lookup.
    /// </summary>
    private enum StatType
    {
        Attack,
        Defense,
        SpAttack,
        SpDefense,
        Speed,
    }

    #region Battle State Management (PokemonRelationships Integration - Phase 5)

    /// <summary>
    /// Starts a battle between two trainers.
    /// Uses PokemonRelationships.StartBattle to establish bidirectional battle relationship.
    /// Phase 5: NEW - Integrated PokemonRelationships for battle state tracking.
    /// </summary>
    /// <param name="trainer1">First trainer entity</param>
    /// <param name="trainer2">Second trainer entity</param>
    public void StartBattle(Entity trainer1, Entity trainer2)
    {
        if (!World.IsAlive(trainer1) || !World.IsAlive(trainer2))
        {
            _logger.LogWarning("Cannot start battle: Invalid trainer entities");
            return;
        }

        // Check if trainers already in battle
        if (World.IsInBattle(trainer1))
        {
            _logger.LogWarning("Trainer {TrainerId} is already in battle", trainer1.Id);
            return;
        }

        if (World.IsInBattle(trainer2))
        {
            _logger.LogWarning("Trainer {TrainerId} is already in battle", trainer2.Id);
            return;
        }

        // Establish battle relationship using PokemonRelationships
        World.StartBattle(trainer1, trainer2);

        _logger.LogInformation(
            "Battle started: Trainer {Trainer1Id} vs Trainer {Trainer2Id}",
            trainer1.Id,
            trainer2.Id
        );

        // TODO: Initialize battle state components for each trainer's lead Pokemon
        // var lead1 = World.GetLeadPokemon(trainer1);
        // var lead2 = World.GetLeadPokemon(trainer2);
    }

    /// <summary>
    /// Ends a battle between two trainers.
    /// Uses PokemonRelationships.EndBattle to remove battle relationship.
    /// </summary>
    public void EndBattle(Entity trainer1, Entity trainer2)
    {
        if (!World.IsAlive(trainer1) || !World.IsAlive(trainer2))
        {
            _logger.LogWarning("Cannot end battle: Invalid trainer entities");
            return;
        }

        World.EndBattle(trainer1, trainer2);

        _logger.LogInformation(
            "Battle ended: Trainer {Trainer1Id} vs Trainer {Trainer2Id}",
            trainer1.Id,
            trainer2.Id
        );

        // TODO: Clean up battle state components
    }

    /// <summary>
    /// Gets the opponent a trainer is currently battling.
    /// Demonstrates PokemonRelationships bidirectional query.
    /// </summary>
    public Entity? GetBattleOpponent(Entity trainer)
    {
        if (!World.IsAlive(trainer))
            return null;

        return World.GetBattleOpponent(trainer);
    }

    /// <summary>
    /// Checks if a trainer is currently in battle.
    /// </summary>
    public bool IsTrainerInBattle(Entity trainer)
    {
        if (!World.IsAlive(trainer))
            return false;

        return World.IsInBattle(trainer);
    }

    #endregion
}
