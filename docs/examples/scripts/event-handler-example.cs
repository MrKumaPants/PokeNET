// ================================================================
// Event Handler Example Script (.cs format)
// ================================================================
// This example demonstrates comprehensive event handling using
// the PokeNET scripting API.
//
// Script Type: Event System
// File Format: .cs (C# Class)
// API Access: IScriptApi via constructor injection
// ================================================================

using PokeNET.ModApi;
using PokeNET.ModApi.Events;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PokeNET.Examples.Scripts
{
    /// <summary>
    /// Comprehensive event handler demonstrating the event system
    ///
    /// Features:
    /// - Battle event tracking
    /// - Achievement system integration
    /// - Statistics collection
    /// - Custom event publishing
    /// - Event filtering and priority
    /// - Proper cleanup and unsubscription
    ///
    /// This demonstrates:
    /// - Event subscription patterns
    /// - Event filtering and handling
    /// - State management across events
    /// - Publishing custom events
    /// - Performance considerations
    /// - Memory management
    /// </summary>
    public class ComprehensiveEventHandler
    {
        private readonly IScriptApi _api;

        // Event handlers (stored for cleanup)
        private readonly List<Action> _unsubscribeActions = new();

        // Statistics tracking
        private readonly BattleStatistics _stats = new();

        // Achievement tracking
        private readonly HashSet<string> _unlockedAchievements = new();

        public ComprehensiveEventHandler(IScriptApi api)
        {
            _api = api ?? throw new ArgumentNullException(nameof(api));

            _api.Logger.LogInformation("Comprehensive Event Handler initialized");

            // Subscribe to all relevant events
            SubscribeToEvents();
        }

        /// <summary>
        /// Subscribe to all game events
        /// </summary>
        private void SubscribeToEvents()
        {
            // Battle Events
            Subscribe<BattleStartEvent>(OnBattleStart);
            Subscribe<BattleEndEvent>(OnBattleEnd);
            Subscribe<TurnStartEvent>(OnTurnStart);
            Subscribe<TurnEndEvent>(OnTurnEnd);

            // Move Events
            Subscribe<MoveUsedEvent>(OnMoveUsed);
            Subscribe<MoveMissedEvent>(OnMoveMissed);
            Subscribe<CriticalHitEvent>(OnCriticalHit);
            Subscribe<SuperEffectiveEvent>(OnSuperEffective);

            // Damage Events
            Subscribe<DamageDealtEvent>(OnDamageDealt);
            Subscribe<HealingEvent>(OnHealing);

            // Status Events
            Subscribe<StatusInflictedEvent>(OnStatusInflicted);
            Subscribe<StatusCuredEvent>(OnStatusCured);

            // Creature Events
            Subscribe<CreatureFaintedEvent>(OnCreatureFainted);
            Subscribe<CreatureCaughtEvent>(OnCreatureCaught);
            Subscribe<CreatureEvolvedEvent>(OnCreatureEvolved);
            Subscribe<LevelUpEvent>(OnLevelUp);

            // Item Events
            Subscribe<ItemUsedEvent>(OnItemUsed);
            Subscribe<ItemObtainedEvent>(OnItemObtained);

            // Custom Events
            Subscribe<PlayerActionEvent>(OnPlayerAction);
            Subscribe<QuestCompletedEvent>(OnQuestCompleted);

            _api.Logger.LogInformation($"Subscribed to {_unsubscribeActions.Count} events");
        }

        /// <summary>
        /// Helper method to subscribe to events with automatic cleanup tracking
        /// </summary>
        private void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : class
        {
            _api.Events.Subscribe(handler);

            // Store unsubscribe action for cleanup
            _unsubscribeActions.Add(() => _api.Events.Unsubscribe(handler));
        }

        /// <summary>
        /// Cleanup method - unsubscribe from all events
        /// </summary>
        public void Cleanup()
        {
            _api.Logger.LogInformation("Cleaning up event subscriptions");

            foreach (var unsubscribe in _unsubscribeActions)
            {
                unsubscribe();
            }

            _unsubscribeActions.Clear();

            // Save statistics before cleanup
            SaveStatistics();
        }

        // ============================================================
        // Battle Event Handlers
        // ============================================================

        private void OnBattleStart(BattleStartEvent evt)
        {
            _api.Logger.LogInformation(
                $"Battle started: {evt.BattleType} with {evt.Participants.Count} participants");

            _stats.TotalBattles++;
            _stats.CurrentBattleStartTime = DateTime.UtcNow;

            // Publish custom battle start notification
            _api.Events.Publish(new BattleMessageEvent
            {
                Message = "The battle begins!",
                Priority = MessagePriority.BattleStart
            });

            // Track battle type statistics
            if (!_stats.BattlesByType.ContainsKey(evt.BattleType))
            {
                _stats.BattlesByType[evt.BattleType] = 0;
            }
            _stats.BattlesByType[evt.BattleType]++;
        }

        private void OnBattleEnd(BattleEndEvent evt)
        {
            var duration = DateTime.UtcNow - _stats.CurrentBattleStartTime;

            _api.Logger.LogInformation(
                $"Battle ended: Winner={evt.Winner}, Duration={duration.TotalSeconds:F2}s");

            // Update statistics
            if (evt.Winner == BattleWinner.Player)
            {
                _stats.Victories++;
                CheckVictoryAchievements();
            }
            else if (evt.Winner == BattleWinner.Opponent)
            {
                _stats.Defeats++;
            }

            _stats.TotalBattleDuration += duration;

            // Track longest battle
            if (duration > _stats.LongestBattle)
            {
                _stats.LongestBattle = duration;
                _api.Logger.LogInformation($"New longest battle: {duration.TotalSeconds:F2}s");
            }
        }

        private void OnTurnStart(TurnStartEvent evt)
        {
            _stats.TotalTurns++;

            _api.Logger.LogDebug($"Turn {evt.TurnNumber} started");

            // Check for turn-based achievements
            if (evt.TurnNumber == 100)
            {
                UnlockAchievement("long_battle", "Survived 100 turns in a single battle");
            }
        }

        private void OnTurnEnd(TurnEndEvent evt)
        {
            _api.Logger.LogDebug($"Turn {evt.TurnNumber} ended");

            // Update turn statistics
            _stats.AverageTurnsPerBattle =
                (float)_stats.TotalTurns / _stats.TotalBattles;
        }

        // ============================================================
        // Move Event Handlers
        // ============================================================

        private void OnMoveUsed(MoveUsedEvent evt)
        {
            _stats.TotalMovesUsed++;

            // Track most used moves
            if (!_stats.MoveUsageCount.ContainsKey(evt.MoveId))
            {
                _stats.MoveUsageCount[evt.MoveId] = 0;
            }
            _stats.MoveUsageCount[evt.MoveId]++;

            _api.Logger.LogDebug(
                $"{evt.User.Name} used {evt.MoveId} on {evt.Target.Name}");

            // Track PP usage for efficiency achievements
            var moveset = evt.User.Get<Moveset>();
            var move = moveset.Moves[evt.MoveIndex];

            if (move.CurrentPP == 1)  // Last PP used
            {
                _api.Events.Publish(new BattleMessageEvent
                {
                    Message = $"{evt.MoveId} has only 1 PP left!",
                    Priority = MessagePriority.Warning
                });
            }
        }

        private void OnMoveMissed(MoveMissedEvent evt)
        {
            _stats.TotalMisses++;

            _api.Logger.LogDebug($"{evt.MoveId} missed!");

            // Track consecutive misses
            _stats.ConsecutiveMisses++;

            if (_stats.ConsecutiveMisses >= 3)
            {
                _api.Events.Publish(new BattleMessageEvent
                {
                    Message = "The attacks keep missing!",
                    Priority = MessagePriority.MoveEffect
                });
            }
        }

        private void OnCriticalHit(CriticalHitEvent evt)
        {
            _stats.TotalCriticalHits++;

            if (evt.Attacker.Has<PlayerControlled>())
            {
                _stats.PlayerCriticalHits++;

                // Check for critical hit achievements
                if (_stats.PlayerCriticalHits == 50)
                {
                    UnlockAchievement("critical_master",
                        "Land 50 critical hits");
                }
            }

            _api.Logger.LogInformation("Critical hit!");

            // Reset consecutive misses on successful crit
            _stats.ConsecutiveMisses = 0;
        }

        private void OnSuperEffective(SuperEffectiveEvent evt)
        {
            _stats.TotalSuperEffectiveHits++;

            _api.Logger.LogInformation(
                $"Super effective! {evt.Effectiveness}x damage");

            // Track type matchup knowledge
            string matchup = $"{evt.AttackType}->{evt.DefenderType}";
            if (!_stats.TypeMatchupUsage.ContainsKey(matchup))
            {
                _stats.TypeMatchupUsage[matchup] = 0;
            }
            _stats.TypeMatchupUsage[matchup]++;

            // Achievement for type mastery
            if (_stats.TypeMatchupUsage.Count >= 50)
            {
                UnlockAchievement("type_master",
                    "Use 50 different type matchups");
            }
        }

        // ============================================================
        // Damage Event Handlers
        // ============================================================

        private void OnDamageDealt(DamageDealtEvent evt)
        {
            _stats.TotalDamageDealt += evt.Damage;

            _api.Logger.LogDebug($"{evt.Damage} damage dealt");

            // Track highest single hit
            if (evt.Damage > _stats.HighestDamage)
            {
                _stats.HighestDamage = evt.Damage;
                _api.Logger.LogInformation($"New damage record: {evt.Damage}");

                if (evt.Damage >= 500)
                {
                    UnlockAchievement("heavy_hitter",
                        "Deal 500+ damage in one hit");
                }
            }

            // Track overkill damage
            ref var health = ref evt.Target.Get<Health>();
            if (evt.Damage > health.Current)
            {
                _stats.OverkillDamage += evt.Damage - health.Current;
            }
        }

        private void OnHealing(HealingEvent evt)
        {
            _stats.TotalHealingDone += evt.Amount;

            _api.Logger.LogDebug($"{evt.Amount} HP restored to {evt.Target.Name}");

            // Track healing efficiency
            ref var health = ref evt.Target.Get<Health>();
            int maxPossibleHealing = health.Maximum - health.Current;

            if (evt.Amount == maxPossibleHealing && maxPossibleHealing > 0)
            {
                _stats.PerfectHeals++;
            }
        }

        // ============================================================
        // Status Event Handlers
        // ============================================================

        private void OnStatusInflicted(StatusInflictedEvent evt)
        {
            _api.Logger.LogInformation(
                $"{evt.Target.Name} was {evt.StatusType}!");

            // Track status usage
            if (!_stats.StatusInflictionCount.ContainsKey(evt.StatusType))
            {
                _stats.StatusInflictionCount[evt.StatusType] = 0;
            }
            _stats.StatusInflictionCount[evt.StatusType]++;
        }

        private void OnStatusCured(StatusCuredEvent evt)
        {
            _api.Logger.LogInformation(
                $"{evt.Target.Name} was cured of {evt.StatusType}!");

            _stats.TotalStatusCures++;
        }

        // ============================================================
        // Creature Event Handlers
        // ============================================================

        private void OnCreatureFainted(CreatureFaintedEvent evt)
        {
            _api.Logger.LogInformation($"{evt.Victim.Name} fainted!");

            if (evt.Victim.Has<PlayerControlled>())
            {
                _stats.PlayerCreaturesFainted++;
            }
            else
            {
                _stats.OpponentCreaturesFainted++;
            }

            // Check for flawless victory
            CheckFlawlessVictory(evt);
        }

        private void OnCreatureCaught(CreatureCaughtEvent evt)
        {
            _stats.CreaturesCaught++;

            _api.Logger.LogInformation(
                $"Caught {evt.Creature.Name}! Total caught: {_stats.CreaturesCaught}");

            // Milestone achievements
            int[] milestones = { 10, 50, 100, 250, 500 };
            if (milestones.Contains(_stats.CreaturesCaught))
            {
                UnlockAchievement($"caught_{_stats.CreaturesCaught}",
                    $"Catch {_stats.CreaturesCaught} creatures");
            }

            // Check if this is a shiny
            if (evt.Creature.Has<ShinyVariant>())
            {
                _stats.ShiniesCaught++;
                UnlockAchievement("shiny_hunter", "Catch your first shiny!");

                _api.Events.Publish(new BattleMessageEvent
                {
                    Message = $"✨ Caught a shiny {evt.Creature.Name}! ✨",
                    Priority = MessagePriority.Critical
                });
            }
        }

        private void OnCreatureEvolved(CreatureEvolvedEvent evt)
        {
            _stats.TotalEvolutions++;

            _api.Logger.LogInformation(
                $"{evt.OldForm} evolved into {evt.NewForm}!");

            _api.Events.Publish(new BattleMessageEvent
            {
                Message = $"Congratulations! {evt.OldForm} evolved into {evt.NewForm}!",
                Priority = MessagePriority.Evolution
            });
        }

        private void OnLevelUp(LevelUpEvent evt)
        {
            var newLevel = evt.Entity.Get<Level>().Current;

            _api.Logger.LogInformation($"{evt.Entity.Name} reached level {newLevel}!");

            // Track highest level
            if (newLevel > _stats.HighestLevel)
            {
                _stats.HighestLevel = newLevel;
            }

            // Level milestone achievements
            if (newLevel == 100)
            {
                UnlockAchievement("max_level", "Reach level 100");
            }
        }

        // ============================================================
        // Item Event Handlers
        // ============================================================

        private void OnItemUsed(ItemUsedEvent evt)
        {
            _stats.TotalItemsUsed++;

            _api.Logger.LogDebug($"Used {evt.ItemId}");

            // Track item usage
            if (!_stats.ItemUsageCount.ContainsKey(evt.ItemId))
            {
                _stats.ItemUsageCount[evt.ItemId] = 0;
            }
            _stats.ItemUsageCount[evt.ItemId]++;
        }

        private void OnItemObtained(ItemObtainedEvent evt)
        {
            _stats.TotalItemsObtained++;

            _api.Logger.LogInformation(
                $"Obtained {evt.Quantity}x {evt.ItemId}");
        }

        // ============================================================
        // Custom Event Handlers
        // ============================================================

        private void OnPlayerAction(PlayerActionEvent evt)
        {
            _api.Logger.LogDebug($"Player action: {evt.ActionType}");

            // Track player actions for behavioral analysis
            if (!_stats.PlayerActions.ContainsKey(evt.ActionType))
            {
                _stats.PlayerActions[evt.ActionType] = 0;
            }
            _stats.PlayerActions[evt.ActionType]++;
        }

        private void OnQuestCompleted(QuestCompletedEvent evt)
        {
            _stats.QuestsCompleted++;

            _api.Logger.LogInformation($"Quest completed: {evt.QuestId}");

            _api.Events.Publish(new BattleMessageEvent
            {
                Message = $"Quest '{evt.QuestName}' completed!",
                Priority = MessagePriority.Quest
            });
        }

        // ============================================================
        // Achievement System
        // ============================================================

        private void UnlockAchievement(string achievementId, string description)
        {
            if (_unlockedAchievements.Contains(achievementId))
            {
                return; // Already unlocked
            }

            _unlockedAchievements.Add(achievementId);

            _api.Logger.LogInformation($"🏆 Achievement unlocked: {description}");

            _api.Events.Publish(new AchievementUnlockedEvent
            {
                AchievementId = achievementId,
                Description = description,
                UnlockedAt = DateTime.UtcNow
            });

            _api.Events.Publish(new BattleMessageEvent
            {
                Message = $"🏆 Achievement: {description}",
                Priority = MessagePriority.Achievement
            });
        }

        private void CheckVictoryAchievements()
        {
            if (_stats.Victories == 10)
            {
                UnlockAchievement("first_ten_wins", "Win 10 battles");
            }
            else if (_stats.Victories == 100)
            {
                UnlockAchievement("century_club", "Win 100 battles");
            }
            else if (_stats.Victories == 1000)
            {
                UnlockAchievement("battle_master", "Win 1000 battles");
            }

            // Win streak tracking
            if (_stats.Victories > 0 && _stats.Defeats == 0)
            {
                if (_stats.Victories >= 10)
                {
                    UnlockAchievement("perfect_streak", "Win 10 battles without losing");
                }
            }
        }

        private void CheckFlawlessVictory(CreatureFaintedEvent evt)
        {
            // Check if opponent fainted without player taking damage
            if (evt.Victim.Has<EnemyControlled>())
            {
                var playerCreatures = _api.Entities.Query<PlayerControlled, Health>();

                bool flawless = playerCreatures.All(creature =>
                {
                    ref var health = ref creature.Get<Health>();
                    return health.Current == health.Maximum;
                });

                if (flawless)
                {
                    UnlockAchievement("flawless_victory",
                        "Win a battle without taking damage");
                }
            }
        }

        // ============================================================
        // Statistics Management
        // ============================================================

        /// <summary>
        /// Save statistics to persistent storage
        /// </summary>
        private void SaveStatistics()
        {
            _api.Logger.LogInformation("Saving battle statistics");

            // Serialize statistics
            var statsJson = System.Text.Json.JsonSerializer.Serialize(_stats);

            // Save to file (via asset API or configuration)
            _api.Assets.SaveData("battle_statistics.json", statsJson);

            _api.Logger.LogInformation($"Statistics saved: {_stats.TotalBattles} battles");
        }

        /// <summary>
        /// Get current statistics
        /// </summary>
        public BattleStatistics GetStatistics() => _stats;

        /// <summary>
        /// Print statistics summary
        /// </summary>
        public void PrintStatistics()
        {
            _api.Logger.LogInformation("=== Battle Statistics ===");
            _api.Logger.LogInformation($"Total Battles: {_stats.TotalBattles}");
            _api.Logger.LogInformation($"Victories: {_stats.Victories}");
            _api.Logger.LogInformation($"Defeats: {_stats.Defeats}");
            _api.Logger.LogInformation(
                $"Win Rate: {(_stats.Victories / (float)_stats.TotalBattles * 100):F1}%");
            _api.Logger.LogInformation($"Total Damage: {_stats.TotalDamageDealt}");
            _api.Logger.LogInformation($"Highest Hit: {_stats.HighestDamage}");
            _api.Logger.LogInformation($"Critical Hits: {_stats.TotalCriticalHits}");
            _api.Logger.LogInformation($"Creatures Caught: {_stats.CreaturesCaught}");
            _api.Logger.LogInformation($"Achievements: {_unlockedAchievements.Count}");
        }
    }

    // ============================================================
    // Statistics Class
    // ============================================================

    public class BattleStatistics
    {
        // Battle tracking
        public int TotalBattles { get; set; }
        public int Victories { get; set; }
        public int Defeats { get; set; }
        public Dictionary<string, int> BattlesByType { get; set; } = new();
        public DateTime CurrentBattleStartTime { get; set; }
        public TimeSpan TotalBattleDuration { get; set; }
        public TimeSpan LongestBattle { get; set; }

        // Turn tracking
        public int TotalTurns { get; set; }
        public float AverageTurnsPerBattle { get; set; }

        // Move tracking
        public int TotalMovesUsed { get; set; }
        public Dictionary<string, int> MoveUsageCount { get; set; } = new();
        public int TotalMisses { get; set; }
        public int ConsecutiveMisses { get; set; }

        // Damage tracking
        public int TotalDamageDealt { get; set; }
        public int HighestDamage { get; set; }
        public int OverkillDamage { get; set; }
        public int TotalHealingDone { get; set; }
        public int PerfectHeals { get; set; }

        // Critical hits
        public int TotalCriticalHits { get; set; }
        public int PlayerCriticalHits { get; set; }

        // Type effectiveness
        public int TotalSuperEffectiveHits { get; set; }
        public Dictionary<string, int> TypeMatchupUsage { get; set; } = new();

        // Status effects
        public Dictionary<string, int> StatusInflictionCount { get; set; } = new();
        public int TotalStatusCures { get; set; }

        // Creature tracking
        public int PlayerCreaturesFainted { get; set; }
        public int OpponentCreaturesFainted { get; set; }
        public int CreaturesCaught { get; set; }
        public int ShiniesCaught { get; set; }
        public int TotalEvolutions { get; set; }
        public int HighestLevel { get; set; }

        // Item tracking
        public int TotalItemsUsed { get; set; }
        public Dictionary<string, int> ItemUsageCount { get; set; } = new();
        public int TotalItemsObtained { get; set; }

        // Player behavior
        public Dictionary<string, int> PlayerActions { get; set; } = new();
        public int QuestsCompleted { get; set; }
    }
}

// ================================================================
// Script Entry Point
// ================================================================
// Return the event handler instance
// ================================================================

return new PokeNET.Examples.Scripts.ComprehensiveEventHandler(Api);
