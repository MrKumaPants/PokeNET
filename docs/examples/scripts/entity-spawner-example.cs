// ================================================================
// Entity Spawner Example Script (.cs format)
// ================================================================
// This example demonstrates how to create and configure entities
// using the PokeNET scripting API.
//
// Script Type: Entity Creation
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
    /// Advanced entity spawner for creating wild creatures with customization
    ///
    /// Features:
    /// - Procedural creature generation
    /// - Level-based stat calculation
    /// - Random IV/EV generation
    /// - Moveset generation from learnset
    /// - Nature and ability assignment
    /// - Shiny chance calculation
    /// - Location-based encounter rules
    ///
    /// This demonstrates:
    /// - Entity creation and component manipulation
    /// - Data API usage for definitions
    /// - Utility functions for calculations
    /// - Event publishing
    /// - Proper error handling
    /// - Logging best practices
    /// </summary>
    public class AdvancedEntitySpawner
    {
        private readonly IScriptApi _api;
        private readonly Random _random;

        // Configuration constants
        private const float SHINY_BASE_CHANCE = 1f / 4096f;  // 1/4096 (Gen 6+)
        private const int MAX_IVS = 31;
        private const int MAX_EVS_TOTAL = 510;
        private const int MAX_EVS_PER_STAT = 252;

        public AdvancedEntitySpawner(IScriptApi api)
        {
            _api = api ?? throw new ArgumentNullException(nameof(api));
            _random = new Random();

            _api.Logger.LogInformation("Advanced Entity Spawner initialized");
        }

        /// <summary>
        /// Spawn a wild creature based on location and level
        /// </summary>
        public Entity SpawnWildCreature(string location, int level)
        {
            try
            {
                _api.Logger.LogInformation(
                    $"Spawning wild creature at {location}, level {level}");

                // Step 1: Determine which creature to spawn based on location
                var creatureId = DetermineEncounter(location, level);
                if (string.IsNullOrEmpty(creatureId))
                {
                    _api.Logger.LogWarning($"No encounters defined for {location}");
                    return null;
                }

                // Step 2: Create base entity from definition
                var entity = _api.Entities.CreateEntity(creatureId);
                if (entity == null)
                {
                    _api.Logger.LogError($"Failed to create entity for {creatureId}");
                    return null;
                }

                // Step 3: Configure the creature
                ConfigureWildCreature(entity, level);

                // Step 4: Publish spawn event
                PublishSpawnEvent(entity, location);

                _api.Logger.LogInformation(
                    $"Successfully spawned {entity.Name} (Lv.{level}) at {location}");

                return entity;
            }
            catch (Exception ex)
            {
                _api.Logger.LogError(ex,
                    $"Error spawning creature at {location} (level {level})");
                return null;
            }
        }

        /// <summary>
        /// Spawn a specific creature with custom configuration
        /// </summary>
        public Entity SpawnCustomCreature(string creatureId, CreatureConfig config)
        {
            try
            {
                _api.Logger.LogDebug($"Spawning custom creature: {creatureId}");

                // Create entity
                var entity = _api.Entities.CreateEntity(creatureId);
                if (entity == null)
                {
                    _api.Logger.LogError($"Failed to create entity for {creatureId}");
                    return null;
                }

                // Apply custom configuration
                ApplyConfiguration(entity, config);

                return entity;
            }
            catch (Exception ex)
            {
                _api.Logger.LogError(ex, $"Error creating custom creature {creatureId}");
                return null;
            }
        }

        /// <summary>
        /// Spawn multiple creatures in bulk (for trainer battles, etc.)
        /// </summary>
        public List<Entity> SpawnParty(List<PartyMemberConfig> partyConfig)
        {
            var party = new List<Entity>();

            foreach (var config in partyConfig)
            {
                var entity = SpawnCustomCreature(config.CreatureId, config.Config);
                if (entity != null)
                {
                    // Add party member component
                    entity.Add(new PartyMember
                    {
                        Position = party.Count,
                        IsAbleToFight = true
                    });

                    party.Add(entity);
                }
            }

            _api.Logger.LogInformation($"Spawned party of {party.Count} creatures");
            return party;
        }

        // ============================================================
        // Private Helper Methods
        // ============================================================

        /// <summary>
        /// Determine which creature to encounter based on location
        /// </summary>
        private string DetermineEncounter(string location, int level)
        {
            // Get encounter table for location
            var encounterTable = _api.Data.GetEncounterTable(location);
            if (encounterTable == null || !encounterTable.Encounters.Any())
            {
                _api.Logger.LogWarning($"No encounter table for location: {location}");
                return null;
            }

            // Filter by level range
            var validEncounters = encounterTable.Encounters
                .Where(e => level >= e.MinLevel && level <= e.MaxLevel)
                .ToList();

            if (!validEncounters.Any())
            {
                // Fallback to any encounter
                validEncounters = encounterTable.Encounters.ToList();
            }

            // Weighted random selection
            int totalWeight = validEncounters.Sum(e => e.Weight);
            int randomValue = _random.Next(totalWeight);

            int currentWeight = 0;
            foreach (var encounter in validEncounters)
            {
                currentWeight += encounter.Weight;
                if (randomValue < currentWeight)
                {
                    _api.Logger.LogDebug(
                        $"Selected encounter: {encounter.CreatureId} (weight: {encounter.Weight})");
                    return encounter.CreatureId;
                }
            }

            // Fallback to first encounter
            return validEncounters.First().CreatureId;
        }

        /// <summary>
        /// Configure a wild creature with level-appropriate stats
        /// </summary>
        private void ConfigureWildCreature(Entity entity, int level)
        {
            // Set level
            entity.Set(new Level { Current = level });

            // Generate random IVs
            GenerateIVs(entity);

            // Wild creatures have no EVs
            InitializeEVs(entity);

            // Assign random nature
            AssignNature(entity);

            // Assign ability
            AssignAbility(entity);

            // Generate moveset
            GenerateMoveset(entity, level);

            // Calculate final stats
            _api.Utilities.RecalculateStats(entity);

            // Set health to maximum
            ref var health = ref entity.Get<Health>();
            health.Current = health.Maximum;

            // Check for shiny
            DetermineShiny(entity);

            // Mark as wild creature
            entity.Add(new WildCreature());

            // Generate personality value (for gender, nature, etc.)
            entity.Add(new PersonalityValue
            {
                Value = (uint)_random.Next()
            });
        }

        /// <summary>
        /// Apply custom configuration to entity
        /// </summary>
        private void ApplyConfiguration(Entity entity, CreatureConfig config)
        {
            // Set level
            entity.Set(new Level { Current = config.Level });

            // Set IVs
            if (config.IVs != null)
            {
                ref var stats = ref entity.Get<CreatureStats>();
                stats.IVHP = (byte)config.IVs.HP;
                stats.IVAttack = (byte)config.IVs.Attack;
                stats.IVDefense = (byte)config.IVs.Defense;
                stats.IVSpAttack = (byte)config.IVs.SpecialAttack;
                stats.IVSpDefense = (byte)config.IVs.SpecialDefense;
                stats.IVSpeed = (byte)config.IVs.Speed;
            }
            else
            {
                GenerateIVs(entity);
            }

            // Set EVs
            if (config.EVs != null)
            {
                ref var stats = ref entity.Get<CreatureStats>();
                stats.EVHP = (byte)config.EVs.HP;
                stats.EVAttack = (byte)config.EVs.Attack;
                stats.EVDefense = (byte)config.EVs.Defense;
                stats.EVSpAttack = (byte)config.EVs.SpecialAttack;
                stats.EVSpDefense = (byte)config.EVs.SpecialDefense;
                stats.EVSpeed = (byte)config.EVs.Speed;
            }
            else
            {
                InitializeEVs(entity);
            }

            // Set nature
            if (!string.IsNullOrEmpty(config.Nature))
            {
                entity.Set(new Nature { Name = config.Nature });
            }
            else
            {
                AssignNature(entity);
            }

            // Set ability
            if (!string.IsNullOrEmpty(config.Ability))
            {
                entity.Set(new Ability { AbilityId = config.Ability });
            }
            else
            {
                AssignAbility(entity);
            }

            // Set moves
            if (config.Moves != null && config.Moves.Any())
            {
                SetMoves(entity, config.Moves);
            }
            else
            {
                GenerateMoveset(entity, config.Level);
            }

            // Recalculate stats
            _api.Utilities.RecalculateStats(entity);

            // Set health
            ref var health = ref entity.Get<Health>();
            health.Current = config.HealthPercent > 0
                ? (int)(health.Maximum * config.HealthPercent)
                : health.Maximum;

            // Set shiny
            if (config.IsShiny)
            {
                entity.Add(new ShinyVariant());
            }
        }

        /// <summary>
        /// Generate random IVs for creature
        /// </summary>
        private void GenerateIVs(Entity entity)
        {
            ref var stats = ref entity.Get<CreatureStats>();

            stats.IVHP = (byte)_random.Next(MAX_IVS + 1);
            stats.IVAttack = (byte)_random.Next(MAX_IVS + 1);
            stats.IVDefense = (byte)_random.Next(MAX_IVS + 1);
            stats.IVSpAttack = (byte)_random.Next(MAX_IVS + 1);
            stats.IVSpDefense = (byte)_random.Next(MAX_IVS + 1);
            stats.IVSpeed = (byte)_random.Next(MAX_IVS + 1);

            _api.Logger.LogDebug(
                $"Generated IVs - HP:{stats.IVHP} Atk:{stats.IVAttack} " +
                $"Def:{stats.IVDefense} SpA:{stats.IVSpAttack} " +
                $"SpD:{stats.IVSpDefense} Spe:{stats.IVSpeed}");
        }

        /// <summary>
        /// Initialize EVs to zero
        /// </summary>
        private void InitializeEVs(Entity entity)
        {
            ref var stats = ref entity.Get<CreatureStats>();

            stats.EVHP = 0;
            stats.EVAttack = 0;
            stats.EVDefense = 0;
            stats.EVSpAttack = 0;
            stats.EVSpDefense = 0;
            stats.EVSpeed = 0;
        }

        /// <summary>
        /// Assign random nature to creature
        /// </summary>
        private void AssignNature(Entity entity)
        {
            var natures = _api.Data.GetAllNatures();
            var randomNature = natures.ElementAt(_random.Next(natures.Count()));

            entity.Set(new Nature { Name = randomNature.Name });

            _api.Logger.LogDebug($"Assigned nature: {randomNature.Name}");
        }

        /// <summary>
        /// Assign ability to creature
        /// </summary>
        private void AssignAbility(Entity entity)
        {
            var definition = GetCreatureDefinition(entity);

            // Choose random ability from possible abilities
            var abilities = new List<string>();
            if (!string.IsNullOrEmpty(definition.Ability1))
                abilities.Add(definition.Ability1);
            if (!string.IsNullOrEmpty(definition.Ability2))
                abilities.Add(definition.Ability2);

            if (abilities.Any())
            {
                var chosenAbility = abilities[_random.Next(abilities.Count)];
                entity.Set(new Ability { AbilityId = chosenAbility });

                _api.Logger.LogDebug($"Assigned ability: {chosenAbility}");
            }
        }

        /// <summary>
        /// Generate moveset based on level and learnset
        /// </summary>
        private void GenerateMoveset(Entity entity, int level)
        {
            var definition = GetCreatureDefinition(entity);

            // Get all moves learnable at or before this level
            var learnableMoves = definition.Learnset
                .Where(m => m.Level <= level)
                .OrderByDescending(m => m.Level)
                .Take(4)  // Maximum 4 moves
                .ToList();

            SetMoves(entity, learnableMoves.Select(m => m.MoveId).ToList());
        }

        /// <summary>
        /// Set specific moves on creature
        /// </summary>
        private void SetMoves(Entity entity, List<string> moveIds)
        {
            var moveset = new Moveset { Count = Math.Min(4, moveIds.Count) };

            for (int i = 0; i < moveset.Count; i++)
            {
                var moveData = _api.Data.GetMove(moveIds[i]);
                moveset.Moves[i] = new Move
                {
                    MoveId = moveData.Id,
                    CurrentPP = moveData.PP,
                    MaxPP = moveData.PP
                };
            }

            entity.Set(moveset);

            _api.Logger.LogDebug(
                $"Set moveset: {string.Join(", ", moveIds.Take(moveset.Count))}");
        }

        /// <summary>
        /// Determine if creature should be shiny
        /// </summary>
        private void DetermineShiny(Entity entity)
        {
            if (_api.Utilities.RandomChance(SHINY_BASE_CHANCE))
            {
                entity.Add(new ShinyVariant());
                _api.Logger.LogInformation($"✨ Shiny {entity.Name} spawned!");
            }
        }

        /// <summary>
        /// Get creature definition from entity
        /// </summary>
        private CreatureDefinition GetCreatureDefinition(Entity entity)
        {
            var defId = entity.Get<DefinitionReference>().DefinitionId;
            return _api.Data.GetCreature(defId);
        }

        /// <summary>
        /// Publish creature spawn event
        /// </summary>
        private void PublishSpawnEvent(Entity entity, string location)
        {
            _api.Events.Publish(new CreatureSpawnedEvent
            {
                Entity = entity,
                Location = location,
                Timestamp = DateTime.UtcNow
            });
        }
    }

    // ============================================================
    // Configuration Classes
    // ============================================================

    public class CreatureConfig
    {
        public int Level { get; set; } = 5;
        public StatValues IVs { get; set; }
        public StatValues EVs { get; set; }
        public string Nature { get; set; }
        public string Ability { get; set; }
        public List<string> Moves { get; set; }
        public bool IsShiny { get; set; }
        public float HealthPercent { get; set; } = 1.0f;
    }

    public class StatValues
    {
        public int HP { get; set; }
        public int Attack { get; set; }
        public int Defense { get; set; }
        public int SpecialAttack { get; set; }
        public int SpecialDefense { get; set; }
        public int Speed { get; set; }
    }

    public class PartyMemberConfig
    {
        public string CreatureId { get; set; }
        public CreatureConfig Config { get; set; }
    }
}

// ================================================================
// Script Entry Point
// ================================================================
// Return an instance of the spawner that can be used by the game
// ================================================================

return new PokeNET.Examples.Scripts.AdvancedEntitySpawner(Api);
