# Arch.Extended Code Examples for PokeNET

This document provides practical, copy-paste-ready examples of Arch.Extended integration in PokeNET.

---

## Table of Contents

1. [Basic System Migration](#basic-system-migration)
2. [Movement System with Queries](#movement-system-with-queries)
3. [Battle System with Events](#battle-system-with-events)
4. [Pokemon Party with Relationships](#pokemon-party-with-relationships)
5. [Save System with Persistence](#save-system-with-persistence)
6. [Audio System with EventBus](#audio-system-with-eventbus)
7. [Entity Factory with Source Generation](#entity-factory-with-source-generation)
8. [Performance-Critical System](#performance-critical-system)

---

## Basic System Migration

### Before: Vanilla Arch

```csharp
using Arch.Core;
using Microsoft.Extensions.Logging;
using PokeNET.Domain.ECS.Systems;

namespace PokeNET.Core.ECS.Systems;

public class InputSystem : SystemBase
{
    private QueryDescription _playerQuery;

    public InputSystem(ILogger<InputSystem> logger) : base(logger)
    {
    }

    protected override void OnInitialize()
    {
        _playerQuery = new QueryDescription()
            .WithAll<PlayerData, GridPosition, MovementState>();
    }

    protected override void OnUpdate(float deltaTime)
    {
        World.Query(in _playerQuery, (Entity entity,
            ref PlayerData player,
            ref GridPosition pos,
            ref MovementState movement) =>
        {
            // Input handling logic
            if (Keyboard.GetState().IsKeyDown(Keys.W))
            {
                movement.IsMoving = true;
                movement.TargetY = pos.Y - 1;
                movement.FacingDirection = Direction.North;
            }
        });
    }
}
```

### After: Arch.Extended

```csharp
using Arch.Core;
using Arch.System;
using Microsoft.Extensions.Logging;

namespace PokeNET.Core.ECS.Systems;

public partial class InputSystem : BaseSystem<World, float>
{
    private readonly ILogger<InputSystem> _logger;

    public InputSystem(World world, ILogger<InputSystem> logger) : base(world)
    {
        _logger = logger;
    }

    public override void Initialize()
    {
        _logger.LogInformation("InputSystem initialized");
    }

    [Query]
    [All<PlayerData, GridPosition, MovementState>]
    public void ProcessPlayerInput(
        ref PlayerData player,
        ref GridPosition pos,
        ref MovementState movement)
    {
        var keyboard = Keyboard.GetState();

        // Movement input
        if (keyboard.IsKeyDown(Keys.W))
        {
            movement.IsMoving = true;
            movement.TargetY = pos.Y - 1;
            movement.FacingDirection = Direction.North;
        }
        else if (keyboard.IsKeyDown(Keys.S))
        {
            movement.IsMoving = true;
            movement.TargetY = pos.Y + 1;
            movement.FacingDirection = Direction.South;
        }
        else if (keyboard.IsKeyDown(Keys.A))
        {
            movement.IsMoving = true;
            movement.TargetX = pos.X - 1;
            movement.FacingDirection = Direction.West;
        }
        else if (keyboard.IsKeyDown(Keys.D))
        {
            movement.IsMoving = true;
            movement.TargetX = pos.X + 1;
            movement.FacingDirection = Direction.East;
        }
    }
}
```

**Changes:**
- ✅ Added `partial` keyword
- ✅ Inherit from `BaseSystem<World, float>`
- ✅ Pass `World` to constructor
- ✅ Removed `QueryDescription` field
- ✅ Removed `OnInitialize` query setup
- ✅ Converted lambda to `[Query]` method
- ✅ Removed `OnUpdate` override

---

## Movement System with Queries

### Complete Movement System

```csharp
using Arch.Core;
using Arch.System;
using Microsoft.Extensions.Logging;
using PokeNET.Domain.ECS.Components;

namespace PokeNET.Core.ECS.Systems;

/// <summary>
/// Handles smooth grid-based movement with interpolation.
/// </summary>
public partial class MovementSystem : BaseSystem<World, float>
{
    private readonly ILogger<MovementSystem> _logger;
    private const float MOVEMENT_SPEED = 4.0f; // Tiles per second

    public MovementSystem(World world, ILogger<MovementSystem> logger)
        : base(world)
    {
        _logger = logger;
    }

    /// <summary>
    /// Process all moving entities.
    /// </summary>
    [Query]
    [All<GridPosition, MovementState, PixelVelocity>]
    [None<Frozen, Disabled>]
    public void ProcessMovement(
        Entity entity,
        ref GridPosition pos,
        ref MovementState movement,
        ref PixelVelocity velocity,
        [Data] float deltaTime)
    {
        if (!movement.IsMoving)
        {
            velocity.VelocityX = 0;
            velocity.VelocityY = 0;
            return;
        }

        // Update movement progress
        movement.MovementProgress += deltaTime * MOVEMENT_SPEED;

        // Interpolate pixel position
        if (movement.TargetX != pos.X)
        {
            velocity.VelocityX = (movement.TargetX - pos.X) * MOVEMENT_SPEED;
        }
        if (movement.TargetY != pos.Y)
        {
            velocity.VelocityY = (movement.TargetY - pos.Y) * MOVEMENT_SPEED;
        }

        // Check if movement complete
        if (movement.MovementProgress >= 1.0f)
        {
            // Snap to grid
            pos.X = movement.TargetX;
            pos.Y = movement.TargetY;
            pos.Z = movement.TargetZ;

            // Reset movement state
            movement.IsMoving = false;
            movement.MovementProgress = 0f;
            velocity.VelocityX = 0;
            velocity.VelocityY = 0;

            _logger.LogDebug($"Entity {entity} completed movement to ({pos.X}, {pos.Y})");
        }
    }

    /// <summary>
    /// Update sprite facing direction based on movement.
    /// </summary>
    [Query]
    [All<MovementState, Sprite>]
    public void UpdateSpriteFacing(ref MovementState movement, ref Sprite sprite)
    {
        // Update sprite frame based on facing direction
        sprite.FrameIndex = movement.FacingDirection switch
        {
            Direction.North => 0,
            Direction.South => 1,
            Direction.West => 2,
            Direction.East => 3,
            _ => sprite.FrameIndex
        };
    }
}
```

---

## Battle System with Events

### Event Definitions

```csharp
namespace PokeNET.Domain.ECS.Events;

public struct BattleStartEvent
{
    public Entity Attacker;
    public Entity Defender;
    public BattleType Type; // Wild, Trainer, etc.
}

public struct MoveExecutedEvent
{
    public Entity Attacker;
    public Entity Defender;
    public int MoveId;
    public int Damage;
    public bool IsCritical;
    public float TypeEffectiveness;
}

public struct PokemonFaintedEvent
{
    public Entity Pokemon;
    public string Nickname;
    public int ExperienceAwarded;
}

public struct LevelUpEvent
{
    public Entity Pokemon;
    public int OldLevel;
    public int NewLevel;
    public int[] StatGains; // HP, Atk, Def, SpAtk, SpDef, Spd
}
```

### EventBus Setup

```csharp
using Arch.EventBus;

namespace PokeNET.Core;

public partial class PokeNETEventBus
{
    [EventBus]
    public static partial void RegisterEvents();
}

// In PokeNETGame.Initialize():
// PokeNETEventBus.RegisterEvents();
```

### Battle System Implementation

```csharp
using Arch.Core;
using Arch.EventBus;
using Arch.System;
using Microsoft.Extensions.Logging;
using PokeNET.Domain.ECS.Components;
using PokeNET.Domain.ECS.Events;

namespace PokeNET.Core.ECS.Systems;

public partial class BattleSystem : BaseSystem<World, float>
{
    private readonly ILogger<BattleSystem> _logger;
    private readonly Random _random = new();

    public BattleSystem(World world, ILogger<BattleSystem> logger)
        : base(world)
    {
        _logger = logger;
    }

    /// <summary>
    /// Process all Pokemon in active battles.
    /// </summary>
    [Query]
    [All<PokemonData, PokemonStats, BattleState, MoveSet>]
    [None<Fainted>]
    public void ProcessBattlers(
        Entity entity,
        ref PokemonData data,
        ref PokemonStats stats,
        ref BattleState battle,
        ref MoveSet moves)
    {
        if (battle.Status != BattleStatus.InBattle)
            return;

        // Apply status effects
        ApplyStatusEffects(entity, ref stats, ref battle);

        // Check if fainted
        if (stats.HP <= 0)
        {
            OnPokemonFainted(entity, ref data, ref stats);
        }
    }

    /// <summary>
    /// Execute a move from attacker to defender.
    /// </summary>
    public void ExecuteMove(Entity attacker, Entity defender, int moveSlot)
    {
        ref var attackerData = ref World.Get<PokemonData>(attacker);
        ref var attackerStats = ref World.Get<PokemonStats>(attacker);
        ref var attackerMoves = ref World.Get<MoveSet>(attacker);

        ref var defenderData = ref World.Get<PokemonData>(defender);
        ref var defenderStats = ref World.Get<PokemonStats>(defender);

        var move = attackerMoves.GetMove(moveSlot);
        if (move == null)
        {
            _logger.LogWarning($"Invalid move slot {moveSlot}");
            return;
        }

        // Calculate damage (Pokemon formula)
        int damage = CalculateDamage(
            attackerStats,
            defenderStats,
            attackerData.Level,
            move.Value,
            out bool isCritical,
            out float effectiveness
        );

        // Apply damage
        defenderStats.HP = Math.Max(0, defenderStats.HP - damage);

        _logger.LogInformation(
            $"{attackerData.Nickname} used {move.Value.Name}! " +
            $"Dealt {damage} damage to {defenderData.Nickname}!"
        );

        // Send event
        EventBus.Send(new MoveExecutedEvent
        {
            Attacker = attacker,
            Defender = defender,
            MoveId = move.Value.Id,
            Damage = damage,
            IsCritical = isCritical,
            TypeEffectiveness = effectiveness
        });
    }

    private int CalculateDamage(
        PokemonStats attackerStats,
        PokemonStats defenderStats,
        int attackerLevel,
        Move move,
        out bool isCritical,
        out float effectiveness)
    {
        // Critical hit (6.25% chance)
        isCritical = _random.NextDouble() < 0.0625;
        float criticalMod = isCritical ? 1.5f : 1.0f;

        // Type effectiveness (simplified)
        effectiveness = 1.0f; // TODO: Implement type chart

        // Official Pokemon damage formula
        int attack = move.Category == MoveCategory.Physical
            ? attackerStats.Attack
            : attackerStats.SpAttack;

        int defense = move.Category == MoveCategory.Physical
            ? defenderStats.Defense
            : defenderStats.SpDefense;

        float damage = ((2f * attackerLevel / 5f + 2f) * move.Power * attack / defense) / 50f + 2f;

        // Apply modifiers
        damage *= criticalMod;
        damage *= effectiveness;
        damage *= (float)(_random.NextDouble() * 0.15 + 0.85); // 85-100% random

        return (int)Math.Max(1, damage);
    }

    private void ApplyStatusEffects(Entity entity, ref PokemonStats stats, ref BattleState battle)
    {
        if (!World.Has<StatusCondition>(entity))
            return;

        ref var status = ref World.Get<StatusCondition>(entity);

        int damage = status.StatusTick(stats.MaxHP);
        if (damage > 0)
        {
            stats.HP = Math.Max(0, stats.HP - damage);
            _logger.LogDebug($"Entity {entity} took {damage} status damage");
        }
    }

    private void OnPokemonFainted(Entity pokemon, ref PokemonData data, ref PokemonStats stats)
    {
        _logger.LogInformation($"{data.Nickname} fainted!");

        // Add Fainted component
        World.Add<Fainted>(pokemon);

        // Send event
        EventBus.Send(new PokemonFaintedEvent
        {
            Pokemon = pokemon,
            Nickname = data.Nickname ?? "Unknown",
            ExperienceAwarded = CalculateExperience(data.Level)
        });
    }

    /// <summary>
    /// Handle battle start event.
    /// </summary>
    [EventHandler]
    public void OnBattleStart(ref BattleStartEvent evt)
    {
        _logger.LogInformation($"Battle started: {evt.Attacker} vs {evt.Defender}");

        // Initialize battle states
        if (World.Has<BattleState>(evt.Attacker))
        {
            ref var state = ref World.Get<BattleState>(evt.Attacker);
            state.Status = BattleStatus.InBattle;
            state.TurnCounter = 0;
        }

        if (World.Has<BattleState>(evt.Defender))
        {
            ref var state = ref World.Get<BattleState>(evt.Defender);
            state.Status = BattleStatus.InBattle;
            state.TurnCounter = 0;
        }
    }

    /// <summary>
    /// Handle Pokemon fainted event.
    /// </summary>
    [EventHandler]
    public void OnPokemonFainted(ref PokemonFaintedEvent evt)
    {
        // Award experience to winner (handled in experience system)
        _logger.LogInformation($"{evt.Nickname} fainted! Awarded {evt.ExperienceAwarded} EXP");
    }

    private int CalculateExperience(int level)
    {
        // Simplified experience formula
        return level * 50;
    }
}
```

---

## Pokemon Party with Relationships

### Relationship Types

```csharp
namespace PokeNET.Domain.ECS.Relationships;

/// <summary>
/// Marks an entity as a child of another (e.g., Pokemon in party).
/// </summary>
public struct ChildOf { }

/// <summary>
/// Marks an entity as owned by another (e.g., Item in inventory).
/// </summary>
public struct OwnedBy { }

/// <summary>
/// Marks an entity as following another (e.g., Partner Pokemon).
/// </summary>
public struct Following { }
```

### Party Management System

```csharp
using Arch.Core;
using Arch.Relationships;
using Arch.System;
using Microsoft.Extensions.Logging;
using PokeNET.Domain.ECS.Components;
using PokeNET.Domain.ECS.Relationships;

namespace PokeNET.Core.ECS.Systems;

public partial class PartySystem : BaseSystem<World, float>
{
    private readonly ILogger<PartySystem> _logger;
    private const int MAX_PARTY_SIZE = 6;

    public PartySystem(World world, ILogger<PartySystem> logger)
        : base(world)
    {
        _logger = logger;
    }

    /// <summary>
    /// Add Pokemon to trainer's party.
    /// </summary>
    public bool AddToParty(Entity trainer, Entity pokemon)
    {
        // Check party size
        var party = World.GetRelationships<ChildOf>(trainer);
        if (party.Count() >= MAX_PARTY_SIZE)
        {
            _logger.LogWarning("Party is full!");
            return false;
        }

        // Add relationship
        World.AddRelationship<ChildOf>(pokemon, trainer);

        ref var pokemonData = ref World.Get<PokemonData>(pokemon);
        _logger.LogInformation($"Added {pokemonData.Nickname} to party");

        return true;
    }

    /// <summary>
    /// Remove Pokemon from party.
    /// </summary>
    public bool RemoveFromParty(Entity trainer, Entity pokemon)
    {
        if (!World.HasRelationship<ChildOf>(pokemon, trainer))
        {
            _logger.LogWarning($"Pokemon {pokemon} not in party");
            return false;
        }

        World.RemoveRelationship<ChildOf>(pokemon, trainer);

        ref var pokemonData = ref World.Get<PokemonData>(pokemon);
        _logger.LogInformation($"Removed {pokemonData.Nickname} from party");

        return true;
    }

    /// <summary>
    /// Get all Pokemon in party.
    /// </summary>
    public IEnumerable<Entity> GetParty(Entity trainer)
    {
        return World.GetRelationships<ChildOf>(trainer);
    }

    /// <summary>
    /// Get first non-fainted Pokemon in party.
    /// </summary>
    public Entity? GetFirstAlivePokemon(Entity trainer)
    {
        var party = GetParty(trainer);

        foreach (var pokemon in party)
        {
            if (!World.Has<Fainted>(pokemon))
            {
                ref var stats = ref World.Get<PokemonStats>(pokemon);
                if (stats.HP > 0)
                    return pokemon;
            }
        }

        return null;
    }

    /// <summary>
    /// Heal all Pokemon in party.
    /// </summary>
    public void HealParty(Entity trainer)
    {
        var party = GetParty(trainer);

        foreach (var pokemon in party)
        {
            ref var stats = ref World.Get<PokemonStats>(pokemon);
            stats.HP = stats.MaxHP;

            // Remove fainted and status conditions
            World.Remove<Fainted>(pokemon);
            World.Remove<StatusCondition>(pokemon);

            ref var data = ref World.Get<PokemonData>(pokemon);
            _logger.LogInformation($"Healed {data.Nickname}");
        }

        _logger.LogInformation("Party fully healed!");
    }

    /// <summary>
    /// Get party statistics.
    /// </summary>
    public (int Total, int Alive, int Fainted) GetPartyStats(Entity trainer)
    {
        var party = GetParty(trainer);
        int total = party.Count();
        int alive = 0;
        int fainted = 0;

        foreach (var pokemon in party)
        {
            if (World.Has<Fainted>(pokemon))
                fainted++;
            else
                alive++;
        }

        return (total, alive, fainted);
    }

    /// <summary>
    /// Update following Pokemon position (e.g., partner following player).
    /// </summary>
    [Query]
    [All<GridPosition>]
    public void UpdateFollowingPokemon(Entity entity, ref GridPosition pos)
    {
        // Find all Pokemon following this entity
        var followers = World.GetRelationships<Following>(entity);

        foreach (var follower in followers)
        {
            if (!World.Has<GridPosition>(follower))
                continue;

            ref var followerPos = ref World.Get<GridPosition>(follower);

            // Simple follow logic (move to entity's previous position)
            followerPos.X = pos.X;
            followerPos.Y = pos.Y;
        }
    }
}
```

---

## Save System with Persistence

```csharp
using Arch.Core;
using Arch.Persistence;
using Arch.Persistence.Json;
using Arch.Persistence.Binary;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Threading.Tasks;

namespace PokeNET.Saving;

public class SaveSystem
{
    private readonly ILogger<SaveSystem> _logger;
    private readonly string _saveDirectory;
    private readonly JsonWorldSerializer _jsonSerializer = new();
    private readonly BinaryWorldSerializer _binarySerializer = new();

    public SaveSystem(ILogger<SaveSystem> logger, string saveDirectory)
    {
        _logger = logger;
        _saveDirectory = saveDirectory;

        // Create save directory if it doesn't exist
        Directory.CreateDirectory(_saveDirectory);
    }

    /// <summary>
    /// Save game (JSON format for debugging).
    /// </summary>
    public async Task SaveGameAsync(World world, string saveName)
    {
        try
        {
            string path = Path.Combine(_saveDirectory, $"{saveName}.json");

            _logger.LogInformation($"Saving game to {path}...");

            // Serialize world
            string json = _jsonSerializer.Serialize(world);

            // Write to file
            await File.WriteAllTextAsync(path, json);

            _logger.LogInformation($"Game saved successfully ({json.Length} bytes)");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save game");
            throw;
        }
    }

    /// <summary>
    /// Load game (JSON format).
    /// </summary>
    public async Task<World> LoadGameAsync(string saveName)
    {
        try
        {
            string path = Path.Combine(_saveDirectory, $"{saveName}.json");

            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"Save file not found: {path}");
            }

            _logger.LogInformation($"Loading game from {path}...");

            // Read file
            string json = await File.ReadAllTextAsync(path);

            // Deserialize world
            var world = _jsonSerializer.Deserialize(json);

            _logger.LogInformation("Game loaded successfully");
            return world;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load game");
            throw;
        }
    }

    /// <summary>
    /// Save game (Binary format for production).
    /// </summary>
    public async Task SaveGameBinaryAsync(World world, string saveName)
    {
        try
        {
            string path = Path.Combine(_saveDirectory, $"{saveName}.sav");

            _logger.LogInformation($"Saving game to {path}...");

            // Serialize world
            byte[] data = _binarySerializer.Serialize(world);

            // Write to file
            await File.WriteAllBytesAsync(path, data);

            _logger.LogInformation($"Game saved successfully ({data.Length} bytes)");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save game");
            throw;
        }
    }

    /// <summary>
    /// Load game (Binary format).
    /// </summary>
    public async Task<World> LoadGameBinaryAsync(string saveName)
    {
        try
        {
            string path = Path.Combine(_saveDirectory, $"{saveName}.sav");

            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"Save file not found: {path}");
            }

            _logger.LogInformation($"Loading game from {path}...");

            // Read file
            byte[] data = await File.ReadAllBytesAsync(path);

            // Deserialize world
            var world = _binarySerializer.Deserialize(data);

            _logger.LogInformation("Game loaded successfully");
            return world;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load game");
            throw;
        }
    }

    /// <summary>
    /// Quick save (save to slot 0).
    /// </summary>
    public Task QuickSaveAsync(World world)
    {
        return SaveGameBinaryAsync(world, "quicksave");
    }

    /// <summary>
    /// Quick load (load from slot 0).
    /// </summary>
    public Task<World> QuickLoadAsync()
    {
        return LoadGameBinaryAsync("quicksave");
    }

    /// <summary>
    /// List all save files.
    /// </summary>
    public IEnumerable<string> ListSaves()
    {
        var jsonFiles = Directory.GetFiles(_saveDirectory, "*.json");
        var savFiles = Directory.GetFiles(_saveDirectory, "*.sav");

        return jsonFiles.Concat(savFiles)
            .Select(Path.GetFileNameWithoutExtension)
            .Distinct()
            .OrderByDescending(File.GetLastWriteTime);
    }

    /// <summary>
    /// Delete a save file.
    /// </summary>
    public void DeleteSave(string saveName)
    {
        try
        {
            string jsonPath = Path.Combine(_saveDirectory, $"{saveName}.json");
            string savPath = Path.Combine(_saveDirectory, $"{saveName}.sav");

            if (File.Exists(jsonPath))
                File.Delete(jsonPath);

            if (File.Exists(savPath))
                File.Delete(savPath);

            _logger.LogInformation($"Deleted save: {saveName}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to delete save: {saveName}");
            throw;
        }
    }
}
```

---

## Audio System with EventBus

```csharp
using Arch.EventBus;
using Arch.System;
using Microsoft.Extensions.Logging;
using PokeNET.Domain.ECS.Events;
using PokeNET.Audio.Abstractions;

namespace PokeNET.Core.ECS.Systems;

/// <summary>
/// Reactive audio system that responds to game events.
/// </summary>
public partial class AudioSystem : BaseSystem<World, float>
{
    private readonly ILogger<AudioSystem> _logger;
    private readonly IAudioManager _audioManager;
    private readonly IMusicPlayer _musicPlayer;
    private readonly ISoundEffectPlayer _sfxPlayer;

    public AudioSystem(
        World world,
        ILogger<AudioSystem> logger,
        IAudioManager audioManager,
        IMusicPlayer musicPlayer,
        ISoundEffectPlayer sfxPlayer)
        : base(world)
    {
        _logger = logger;
        _audioManager = audioManager;
        _musicPlayer = musicPlayer;
        _sfxPlayer = sfxPlayer;
    }

    [EventHandler]
    public void OnBattleStart(ref BattleStartEvent evt)
    {
        _logger.LogInformation("Starting battle music");

        string musicTrack = evt.Type switch
        {
            BattleType.Wild => "music/battle_wild.mid",
            BattleType.Trainer => "music/battle_trainer.mid",
            BattleType.GymLeader => "music/battle_gym.mid",
            BattleType.Champion => "music/battle_champion.mid",
            _ => "music/battle_wild.mid"
        };

        _musicPlayer.Play(musicTrack, loop: true, fadeIn: 1.0f);
    }

    [EventHandler]
    public void OnMoveExecuted(ref MoveExecutedEvent evt)
    {
        // Play attack sound effect
        _sfxPlayer.Play("sfx/attack.wav");

        // Play hit sound
        if (evt.Damage > 0)
        {
            _sfxPlayer.Play(evt.IsCritical ? "sfx/critical_hit.wav" : "sfx/hit.wav");
        }

        // Play effectiveness sound
        if (evt.TypeEffectiveness > 1.0f)
        {
            _sfxPlayer.Play("sfx/super_effective.wav");
        }
        else if (evt.TypeEffectiveness < 1.0f)
        {
            _sfxPlayer.Play("sfx/not_very_effective.wav");
        }
    }

    [EventHandler]
    public void OnPokemonFainted(ref PokemonFaintedEvent evt)
    {
        _logger.LogInformation($"{evt.Nickname} fainted");
        _sfxPlayer.Play("sfx/faint.wav");
    }

    [EventHandler]
    public void OnLevelUp(ref LevelUpEvent evt)
    {
        _logger.LogInformation($"Pokemon leveled up to {evt.NewLevel}");

        // Stop battle music temporarily
        _musicPlayer.Pause();

        // Play level up jingle
        _sfxPlayer.Play("sfx/levelup.wav", onComplete: () =>
        {
            // Resume battle music after jingle
            _musicPlayer.Resume();
        });
    }

    [EventHandler]
    public void OnBattleEnd(ref BattleEndEvent evt)
    {
        _logger.LogInformation("Battle ended");

        // Fade out battle music
        _musicPlayer.Stop(fadeOut: 2.0f);

        if (evt.Victory)
        {
            // Play victory music
            _musicPlayer.Play("music/victory.mid", loop: false, fadeIn: 1.0f);
        }
    }
}
```

---

## Entity Factory with Source Generation

```csharp
using Arch.Core;
using Microsoft.Extensions.Logging;
using PokeNET.Domain.ECS.Components;

namespace PokeNET.Core.ECS.Factories;

/// <summary>
/// Factory for creating Pokemon entities.
/// </summary>
public class PokemonEntityFactory
{
    private readonly World _world;
    private readonly ILogger<PokemonEntityFactory> _logger;

    public PokemonEntityFactory(World world, ILogger<PokemonEntityFactory> logger)
    {
        _world = world;
        _logger = logger;
    }

    /// <summary>
    /// Create a wild Pokemon at specified location.
    /// </summary>
    public Entity CreateWildPokemon(int speciesId, int level, GridPosition position)
    {
        var pokemon = _world.Create(
            new PokemonData
            {
                SpeciesId = speciesId,
                Level = level,
                Nickname = null, // Wild Pokemon have no nickname
                Nature = (Nature)Random.Shared.Next(25),
                Gender = DetermineGender(speciesId),
                IsShiny = Random.Shared.NextDouble() < 0.000122, // 1/8192 chance
                ExperiencePoints = CalculateExperience(level),
                FriendshipLevel = 0
            },
            CalculateStats(speciesId, level),
            new BattleState
            {
                Status = BattleStatus.Ready,
                TurnCounter = 0
            },
            GenerateMoveset(speciesId, level),
            position,
            new Sprite
            {
                TexturePath = $"sprites/pokemon/{speciesId}.png",
                Layer = 1
            }
        );

        _logger.LogInformation($"Created wild Pokemon: Species {speciesId}, Level {level}");
        return pokemon;
    }

    /// <summary>
    /// Create a trainer's Pokemon.
    /// </summary>
    public Entity CreateTrainerPokemon(
        int speciesId,
        int level,
        string nickname,
        Entity trainer)
    {
        var pokemon = _world.Create(
            new PokemonData
            {
                SpeciesId = speciesId,
                Level = level,
                Nickname = nickname,
                Nature = (Nature)Random.Shared.Next(25),
                Gender = DetermineGender(speciesId),
                IsShiny = false,
                ExperiencePoints = CalculateExperience(level),
                OriginalTrainerId = (int)trainer.Id,
                FriendshipLevel = 70 // Starting friendship
            },
            CalculateStats(speciesId, level),
            new BattleState
            {
                Status = BattleStatus.Ready,
                TurnCounter = 0
            },
            GenerateMoveset(speciesId, level)
        );

        // Add to trainer's party
        _world.AddRelationship<ChildOf>(pokemon, trainer);

        _logger.LogInformation($"Created Pokemon '{nickname}' for trainer");
        return pokemon;
    }

    private PokemonStats CalculateStats(int speciesId, int level)
    {
        // TODO: Load base stats from species data
        var baseStats = new { HP = 45, Attack = 49, Defense = 49, SpAtk = 65, SpDef = 65, Speed = 45 };

        // Generate random IVs (0-31)
        var ivs = new
        {
            HP = Random.Shared.Next(32),
            Attack = Random.Shared.Next(32),
            Defense = Random.Shared.Next(32),
            SpAtk = Random.Shared.Next(32),
            SpDef = Random.Shared.Next(32),
            Speed = Random.Shared.Next(32)
        };

        // Calculate stats using Pokemon formula
        int hp = ((2 * baseStats.HP + ivs.HP) * level / 100) + level + 10;
        int attack = ((2 * baseStats.Attack + ivs.Attack) * level / 100) + 5;
        int defense = ((2 * baseStats.Defense + ivs.Defense) * level / 100) + 5;
        int spAttack = ((2 * baseStats.SpAtk + ivs.SpAtk) * level / 100) + 5;
        int spDefense = ((2 * baseStats.SpDef + ivs.SpDef) * level / 100) + 5;
        int speed = ((2 * baseStats.Speed + ivs.Speed) * level / 100) + 5;

        return new PokemonStats
        {
            MaxHP = hp,
            HP = hp,
            Attack = attack,
            Defense = defense,
            SpAttack = spAttack,
            SpDefense = spDefense,
            Speed = speed,
            IV_HP = ivs.HP,
            IV_Attack = ivs.Attack,
            IV_Defense = ivs.Defense,
            IV_SpAttack = ivs.SpAtk,
            IV_SpDefense = ivs.SpDef,
            IV_Speed = ivs.Speed
        };
    }

    private MoveSet GenerateMoveset(int speciesId, int level)
    {
        // TODO: Load moves from species data
        return new MoveSet(); // Simplified
    }

    private int CalculateExperience(int level)
    {
        // Medium Fast growth rate
        return (int)(Math.Pow(level, 3));
    }

    private Gender DetermineGender(int speciesId)
    {
        // TODO: Load gender ratio from species data
        return Random.Shared.Next(2) == 0 ? Gender.Male : Gender.Female;
    }
}
```

---

## Performance-Critical System

```csharp
using Arch.Core;
using Arch.LowLevel;
using Arch.System;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace PokeNET.Core.ECS.Systems;

/// <summary>
/// High-performance collision detection system.
/// Uses UnsafeList for zero-allocation spatial partitioning.
/// </summary>
public partial class CollisionSystem : BaseSystem<World, float>
{
    private readonly ILogger<CollisionSystem> _logger;
    private UnsafeList<(Entity, int, int)> _spatialGrid;
    private const int GRID_SIZE = 32; // 32x32 grid

    public CollisionSystem(World world, ILogger<CollisionSystem> logger)
        : base(world)
    {
        _logger = logger;
        _spatialGrid = new UnsafeList<(Entity, int, int)>(capacity: 10000);
    }

    public override void BeforeUpdate(in float deltaTime)
    {
        // Clear spatial grid (zero allocation)
        _spatialGrid.Clear();
    }

    /// <summary>
    /// Build spatial grid from all entities with collision.
    /// </summary>
    [Query]
    [All<GridPosition, Collider>]
    public void BuildSpatialGrid(Entity entity, ref GridPosition pos, ref Collider collider)
    {
        if (!collider.IsEnabled)
            return;

        // Add to spatial grid
        int gridX = pos.X / GRID_SIZE;
        int gridY = pos.Y / GRID_SIZE;
        _spatialGrid.Add((entity, gridX, gridY));
    }

    public override void AfterUpdate(in float deltaTime)
    {
        var sw = Stopwatch.StartNew();

        // Check collisions within each grid cell
        CheckCollisions();

        sw.Stop();
        if (sw.ElapsedMilliseconds > 5)
        {
            _logger.LogWarning($"Collision detection took {sw.ElapsedMilliseconds}ms");
        }
    }

    private void CheckCollisions()
    {
        // Group entities by grid cell (zero allocation with UnsafeList)
        for (int i = 0; i < _spatialGrid.Length; i++)
        {
            var (entity1, gridX1, gridY1) = _spatialGrid[i];

            // Only check entities in same or adjacent cells
            for (int j = i + 1; j < _spatialGrid.Length; j++)
            {
                var (entity2, gridX2, gridY2) = _spatialGrid[j];

                // Skip if not in same or adjacent cells
                if (Math.Abs(gridX1 - gridX2) > 1 || Math.Abs(gridY1 - gridY2) > 1)
                    continue;

                // Check collision
                if (IsColliding(entity1, entity2))
                {
                    HandleCollision(entity1, entity2);
                }
            }
        }
    }

    private bool IsColliding(Entity entity1, Entity entity2)
    {
        ref var pos1 = ref World.Get<GridPosition>(entity1);
        ref var pos2 = ref World.Get<GridPosition>(entity2);
        ref var col1 = ref World.Get<Collider>(entity1);
        ref var col2 = ref World.Get<Collider>(entity2);

        // Simple AABB collision
        return pos1.X < pos2.X + col2.Width &&
               pos1.X + col1.Width > pos2.X &&
               pos1.Y < pos2.Y + col2.Height &&
               pos1.Y + col1.Height > pos2.Y;
    }

    private void HandleCollision(Entity entity1, Entity entity2)
    {
        // Send collision event
        EventBus.Send(new CollisionEvent
        {
            Entity1 = entity1,
            Entity2 = entity2
        });
    }

    public override void Dispose()
    {
        // REQUIRED: Dispose LowLevel collections
        _spatialGrid.Dispose();
        base.Dispose();
    }
}
```

---

## Complete Game Setup

```csharp
using Arch.Core;
using Arch.System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework;
using PokeNET.Core.ECS.Systems;

namespace PokeNET.Core;

public class PokeNETGame : Game
{
    private World _world;
    private Group<float> _gameplaySystems;
    private Group<float> _renderSystems;
    private IServiceProvider _services;

    protected override void Initialize()
    {
        // Setup dependency injection
        var services = new ServiceCollection();
        ConfigureServices(services);
        _services = services.BuildServiceProvider();

        // Create world
        _world = World.Create();

        // Register events
        PokeNETEventBus.RegisterEvents();

        // Create system groups
        var logger = _services.GetRequiredService<ILoggerFactory>();

        _gameplaySystems = new Group<float>(
            "Gameplay",
            new InputSystem(_world, logger.CreateLogger<InputSystem>()),
            new MovementSystem(_world, logger.CreateLogger<MovementSystem>()),
            new BattleSystem(_world, logger.CreateLogger<BattleSystem>()),
            new PartySystem(_world, logger.CreateLogger<PartySystem>())
        );

        _renderSystems = new Group<float>(
            "Rendering",
            new RenderSystem(_world, logger.CreateLogger<RenderSystem>()),
            new UISystem(_world, logger.CreateLogger<UISystem>())
        );

        _gameplaySystems.Initialize();
        _renderSystems.Initialize();

        base.Initialize();
    }

    protected override void Update(GameTime gameTime)
    {
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

        _gameplaySystems.BeforeUpdate(in deltaTime);
        _gameplaySystems.Update(in deltaTime);
        _gameplaySystems.AfterUpdate(in deltaTime);

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

        _renderSystems.BeforeUpdate(in deltaTime);
        _renderSystems.Update(in deltaTime);
        _renderSystems.AfterUpdate(in deltaTime);

        base.Draw(gameTime);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _gameplaySystems.Dispose();
            _renderSystems.Dispose();
            _world.Dispose();
        }
        base.Dispose(disposing);
    }

    private void ConfigureServices(IServiceCollection services)
    {
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.AddDebug();
        });

        // Add other services...
    }
}
```

---

**Examples Version:** 1.0.0
**Last Updated:** 2025-10-24
**For Integration Guide:** See `arch-extended-integration-guide.md`
