using System.Collections.Generic;
using Arch.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework;
using PokeNET.Core.ECS.Commands;
using PokeNET.Core.ECS.Components;
using PokeNET.Core.ECS.Events;

namespace PokeNET.Core.ECS.Factories;

/// <summary>
/// Specialized factory for creating enemy entities with AI patterns.
/// Supports different enemy types and difficulty levels.
/// </summary>
public sealed class EnemyEntityFactory : EntityFactory
{
    protected override string FactoryName => "EnemyFactory";

    public EnemyEntityFactory(ILogger<EnemyEntityFactory> logger, IEventBus? eventBus = null)
        : base(logger, eventBus)
    {
        RegisterDefaultTemplates();
    }

    /// <summary>
    /// Creates a weak enemy suitable for early game.
    /// Phase 6: Now uses CommandBuffer for safe deferred entity creation.
    /// </summary>
    public CommandBuffer.CreateCommand CreateWeakEnemy(CommandBuffer cmd, Vector2 spawnPosition)
    {
        var definition = new EntityDefinition(
            "WeakEnemy",
            new object[]
            {
                new Position(spawnPosition.X, spawnPosition.Y),
                new GridPosition((int)(spawnPosition.X / 16), (int)(spawnPosition.Y / 16), 0),
                new MovementState
                {
                    Mode = MovementMode.Walking,
                    MovementSpeed = 2f,
                    CanMove = true,
                },
                new Health(30),
                new Sprite("sprites/enemy_weak.png", 24, 24, 0.4f),
                new Renderable(true),
            },
            new Dictionary<string, object>
            {
                ["EnemyType"] = "Weak",
                ["Difficulty"] = 1,
                ["XPReward"] = 10,
            }
        );

        return Create(cmd, definition);
    }

    /// <summary>
    /// Creates a standard enemy with balanced stats.
    /// Phase 6: Now uses CommandBuffer for safe deferred entity creation.
    /// </summary>
    public CommandBuffer.CreateCommand CreateStandardEnemy(CommandBuffer cmd, Vector2 spawnPosition)
    {
        var definition = new EntityDefinition(
            "StandardEnemy",
            new object[]
            {
                new Position(spawnPosition.X, spawnPosition.Y),
                new GridPosition((int)(spawnPosition.X / 16), (int)(spawnPosition.Y / 16), 0),
                new MovementState
                {
                    Mode = MovementMode.Walking,
                    MovementSpeed = 3f,
                    CanMove = true,
                },
                new Health(60),
                new Sprite("sprites/enemy_standard.png", 32, 32, 0.4f),
                new Renderable(true),
            },
            new Dictionary<string, object>
            {
                ["EnemyType"] = "Standard",
                ["Difficulty"] = 2,
                ["XPReward"] = 25,
            }
        );

        return Create(cmd, definition);
    }

    /// <summary>
    /// Creates an elite enemy with high health and speed.
    /// Phase 6: Now uses CommandBuffer for safe deferred entity creation.
    /// </summary>
    public CommandBuffer.CreateCommand CreateEliteEnemy(CommandBuffer cmd, Vector2 spawnPosition)
    {
        var definition = new EntityDefinition(
            "EliteEnemy",
            new object[]
            {
                new Position(spawnPosition.X, spawnPosition.Y),
                new GridPosition((int)(spawnPosition.X / 16), (int)(spawnPosition.Y / 16), 0),
                new MovementState
                {
                    Mode = MovementMode.Walking,
                    MovementSpeed = 4f,
                    CanMove = true,
                },
                new Health(150),
                new Sprite("sprites/enemy_elite.png", 48, 48, 0.4f),
                new Renderable(true),
                new PokemonStats
                {
                    Attack = 20,
                    Defense = 15,
                    Speed = 18,
                    SpAttack = 12,
                    SpDefense = 10,
                    HP = 150,
                    MaxHP = 150,
                    // Default IVs and EVs (can be randomized later)
                    IV_Attack = 15,
                    IV_Defense = 15,
                    IV_Speed = 15,
                    IV_SpAttack = 15,
                    IV_SpDefense = 15,
                    IV_HP = 15
                },
            },
            new Dictionary<string, object>
            {
                ["EnemyType"] = "Elite",
                ["Difficulty"] = 5,
                ["XPReward"] = 100,
            }
        );

        return Create(cmd, definition);
    }

    /// <summary>
    /// Creates a boss enemy with unique abilities.
    /// Phase 6: Now uses CommandBuffer for safe deferred entity creation.
    /// </summary>
    public CommandBuffer.CreateCommand CreateBossEnemy(
        CommandBuffer cmd,
        Vector2 spawnPosition,
        string bossName
    )
    {
        var definition = new EntityDefinition(
            $"Boss_{bossName}",
            new object[]
            {
                new Position(spawnPosition.X, spawnPosition.Y),
                new GridPosition((int)(spawnPosition.X / 16), (int)(spawnPosition.Y / 16), 0),
                new MovementState
                {
                    Mode = MovementMode.Walking,
                    MovementSpeed = 2f,
                    CanMove = true,
                },
                new Health(500),
                new Sprite($"sprites/boss_{bossName.ToLower()}.png", 64, 64, 0.4f),
                new Renderable(true),
                new PokemonStats
                {
                    Attack = 35,
                    Defense = 30,
                    Speed = 10,
                    SpAttack = 40,
                    SpDefense = 35,
                    HP = 500,
                    MaxHP = 500,
                    // Boss-level IVs
                    IV_Attack = 31,
                    IV_Defense = 31,
                    IV_Speed = 31,
                    IV_SpAttack = 31,
                    IV_SpDefense = 31,
                    IV_HP = 31
                },
            },
            new Dictionary<string, object>
            {
                ["EnemyType"] = "Boss",
                ["BossName"] = bossName,
                ["Difficulty"] = 10,
                ["XPReward"] = 500,
            }
        );

        return Create(cmd, definition);
    }

    private void RegisterDefaultTemplates()
    {
        RegisterTemplate(
            "enemy_weak",
            new EntityDefinition(
                "WeakEnemy",
                new object[]
                {
                    new Position(0, 0),
                    new GridPosition(0, 0, 0),
                    new MovementState
                    {
                        Mode = MovementMode.Walking,
                        MovementSpeed = 2f,
                        CanMove = true,
                    },
                    new Health(30),
                    new Sprite("sprites/enemy_weak.png", 24, 24, 0.4f),
                    new Renderable(true),
                }
            )
        );

        RegisterTemplate(
            "enemy_standard",
            new EntityDefinition(
                "StandardEnemy",
                new object[]
                {
                    new Position(0, 0),
                    new GridPosition(0, 0, 0),
                    new MovementState
                    {
                        Mode = MovementMode.Walking,
                        MovementSpeed = 3f,
                        CanMove = true,
                    },
                    new Health(60),
                    new Sprite("sprites/enemy_standard.png", 32, 32, 0.4f),
                    new Renderable(true),
                }
            )
        );

        RegisterTemplate(
            "enemy_elite",
            new EntityDefinition(
                "EliteEnemy",
                new object[]
                {
                    new Position(0, 0),
                    new GridPosition(0, 0, 0),
                    new MovementState
                    {
                        Mode = MovementMode.Walking,
                        MovementSpeed = 4f,
                        CanMove = true,
                    },
                    new Health(150),
                    new Sprite("sprites/enemy_elite.png", 48, 48, 0.4f),
                    new Renderable(true),
                }
            )
        );
    }
}
