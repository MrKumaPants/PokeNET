using Arch.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework;
using PokeNET.Domain.ECS.Components;
using PokeNET.Domain.ECS.Events;
using PokeNET.Domain.ECS.Factories;

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
    /// </summary>
    public Entity CreateWeakEnemy(World world, Vector2 spawnPosition)
    {
        var definition = new EntityDefinition(
            "WeakEnemy",
            new object[]
            {
                new Position(spawnPosition.X, spawnPosition.Y),
                new GridPosition((int)(spawnPosition.X / 16), (int)(spawnPosition.Y / 16), 0),
                new MovementState { Mode = MovementMode.Walking, MovementSpeed = 2f, CanMove = true },
                new Health(30),
                new Sprite("sprites/enemy_weak.png", 24, 24, 0.4f),
                new Renderable(true)
            },
            new Dictionary<string, object>
            {
                ["EnemyType"] = "Weak",
                ["Difficulty"] = 1,
                ["XPReward"] = 10
            }
        );

        return Create(world, definition);
    }

    /// <summary>
    /// Creates a standard enemy with balanced stats.
    /// </summary>
    public Entity CreateStandardEnemy(World world, Vector2 spawnPosition)
    {
        var definition = new EntityDefinition(
            "StandardEnemy",
            new object[]
            {
                new Position(spawnPosition.X, spawnPosition.Y),
                new GridPosition((int)(spawnPosition.X / 16), (int)(spawnPosition.Y / 16), 0),
                new MovementState { Mode = MovementMode.Walking, MovementSpeed = 3f, CanMove = true },
                new Health(60),
                new Sprite("sprites/enemy_standard.png", 32, 32, 0.4f),
                new Renderable(true)
            },
            new Dictionary<string, object>
            {
                ["EnemyType"] = "Standard",
                ["Difficulty"] = 2,
                ["XPReward"] = 25
            }
        );

        return Create(world, definition);
    }

    /// <summary>
    /// Creates an elite enemy with high health and speed.
    /// </summary>
    public Entity CreateEliteEnemy(World world, Vector2 spawnPosition)
    {
        var definition = new EntityDefinition(
            "EliteEnemy",
            new object[]
            {
                new Position(spawnPosition.X, spawnPosition.Y),
                new GridPosition((int)(spawnPosition.X / 16), (int)(spawnPosition.Y / 16), 0),
                new MovementState { Mode = MovementMode.Walking, MovementSpeed = 4f, CanMove = true },
                new Health(150),
                new Sprite("sprites/enemy_elite.png", 48, 48, 0.4f),
                new Renderable(true),
                new Stats(
                    attack: 20,
                    defense: 15,
                    speed: 18,
                    specialAttack: 12,
                    specialDefense: 10
                )
            },
            new Dictionary<string, object>
            {
                ["EnemyType"] = "Elite",
                ["Difficulty"] = 5,
                ["XPReward"] = 100
            }
        );

        return Create(world, definition);
    }

    /// <summary>
    /// Creates a boss enemy with unique abilities.
    /// </summary>
    public Entity CreateBossEnemy(World world, Vector2 spawnPosition, string bossName)
    {
        var definition = new EntityDefinition(
            $"Boss_{bossName}",
            new object[]
            {
                new Position(spawnPosition.X, spawnPosition.Y),
                new GridPosition((int)(spawnPosition.X / 16), (int)(spawnPosition.Y / 16), 0),
                new MovementState { Mode = MovementMode.Walking, MovementSpeed = 2f, CanMove = true },
                new Health(500),
                new Sprite($"sprites/boss_{bossName.ToLower()}.png", 64, 64, 0.4f),
                new Renderable(true),
                new Stats(
                    attack: 35,
                    defense: 30,
                    speed: 10,
                    specialAttack: 40,
                    specialDefense: 35
                )
            },
            new Dictionary<string, object>
            {
                ["EnemyType"] = "Boss",
                ["BossName"] = bossName,
                ["Difficulty"] = 10,
                ["XPReward"] = 500
            }
        );

        return Create(world, definition);
    }

    private void RegisterDefaultTemplates()
    {
        RegisterTemplate("enemy_weak", new EntityDefinition(
            "WeakEnemy",
            new object[]
            {
                new Position(0, 0),
                new GridPosition(0, 0, 0),
                new MovementState { Mode = MovementMode.Walking, MovementSpeed = 2f, CanMove = true },
                new Health(30),
                new Sprite("sprites/enemy_weak.png", 24, 24, 0.4f),
                new Renderable(true)
            }
        ));

        RegisterTemplate("enemy_standard", new EntityDefinition(
            "StandardEnemy",
            new object[]
            {
                new Position(0, 0),
                new GridPosition(0, 0, 0),
                new MovementState { Mode = MovementMode.Walking, MovementSpeed = 3f, CanMove = true },
                new Health(60),
                new Sprite("sprites/enemy_standard.png", 32, 32, 0.4f),
                new Renderable(true)
            }
        ));

        RegisterTemplate("enemy_elite", new EntityDefinition(
            "EliteEnemy",
            new object[]
            {
                new Position(0, 0),
                new GridPosition(0, 0, 0),
                new MovementState { Mode = MovementMode.Walking, MovementSpeed = 4f, CanMove = true },
                new Health(150),
                new Sprite("sprites/enemy_elite.png", 48, 48, 0.4f),
                new Renderable(true)
            }
        ));
    }
}
