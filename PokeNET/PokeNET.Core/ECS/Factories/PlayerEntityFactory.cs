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
/// Specialized factory for creating player entities with default configurations.
/// Provides predefined templates for different player types.
/// </summary>
public sealed class PlayerEntityFactory : EntityFactory
{
    protected override string FactoryName => "PlayerFactory";

    public PlayerEntityFactory(ILogger<PlayerEntityFactory> logger, IEventBus? eventBus = null)
        : base(logger, eventBus)
    {
        RegisterDefaultTemplates();
    }

    /// <summary>
    /// Creates a basic player entity with standard components.
    /// Phase 6: Now uses CommandBuffer for safe deferred entity creation.
    /// </summary>
    public CommandBuffer.CreateCommand CreateBasicPlayer(CommandBuffer cmd, Vector2 spawnPosition)
    {
        var definition = new EntityDefinition(
            "BasicPlayer",
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
                new Health(100),
                new Sprite("sprites/player.png", 32, 32, 0.5f),
                new Renderable(true),
            },
            new Dictionary<string, object>
            {
                ["PlayerType"] = "Basic",
                ["SpawnPosition"] = spawnPosition,
            }
        );

        return Create(cmd, definition);
    }

    /// <summary>
    /// Creates a fast player variant with higher speed.
    /// Phase 6: Now uses CommandBuffer for safe deferred entity creation.
    /// </summary>
    public CommandBuffer.CreateCommand CreateFastPlayer(CommandBuffer cmd, Vector2 spawnPosition)
    {
        var definition = new EntityDefinition(
            "FastPlayer",
            new object[]
            {
                new Position(spawnPosition.X, spawnPosition.Y),
                new GridPosition((int)(spawnPosition.X / 16), (int)(spawnPosition.Y / 16), 0),
                new MovementState
                {
                    Mode = MovementMode.Running,
                    MovementSpeed = 8f,
                    CanMove = true,
                    CanRun = true,
                },
                new Health(75), // Less health for balance
                new Sprite("sprites/player_fast.png", 32, 32, 0.5f),
                new Renderable(true),
            },
            new Dictionary<string, object>
            {
                ["PlayerType"] = "Fast",
                ["SpawnPosition"] = spawnPosition,
            }
        );

        return Create(cmd, definition);
    }

    /// <summary>
    /// Creates a tank player variant with high health.
    /// Phase 6: Now uses CommandBuffer for safe deferred entity creation.
    /// </summary>
    public CommandBuffer.CreateCommand CreateTankPlayer(CommandBuffer cmd, Vector2 spawnPosition)
    {
        var definition = new EntityDefinition(
            "TankPlayer",
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
                new Health(200), // High health
                new Sprite("sprites/player_tank.png", 48, 48, 0.5f),
                new Renderable(true),
            },
            new Dictionary<string, object>
            {
                ["PlayerType"] = "Tank",
                ["SpawnPosition"] = spawnPosition,
            }
        );

        return Create(cmd, definition);
    }

    private void RegisterDefaultTemplates()
    {
        // Register template for quick spawning
        RegisterTemplate(
            "player_basic",
            new EntityDefinition(
                "BasicPlayer",
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
                    new Health(100),
                    new Sprite("sprites/player.png", 32, 32, 0.5f),
                    new Renderable(true),
                }
            )
        );

        RegisterTemplate(
            "player_fast",
            new EntityDefinition(
                "FastPlayer",
                new object[]
                {
                    new Position(0, 0),
                    new GridPosition(0, 0, 0),
                    new MovementState
                    {
                        Mode = MovementMode.Running,
                        MovementSpeed = 8f,
                        CanMove = true,
                        CanRun = true,
                    },
                    new Health(75),
                    new Sprite("sprites/player_fast.png", 32, 32, 0.5f),
                    new Renderable(true),
                }
            )
        );

        RegisterTemplate(
            "player_tank",
            new EntityDefinition(
                "TankPlayer",
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
                    new Health(200),
                    new Sprite("sprites/player_tank.png", 48, 48, 0.5f),
                    new Renderable(true),
                }
            )
        );
    }
}
