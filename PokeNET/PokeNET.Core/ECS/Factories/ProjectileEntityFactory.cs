using System;
using System.Collections.Generic;
using Arch.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework;
using PokeNET.Core.ECS.Commands;
using PokeNET.Core.ECS.Components;
using PokeNET.Core.ECS.Events;

namespace PokeNET.Core.ECS.Factories;

/// <summary>
/// Specialized factory for creating projectile entities (bullets, arrows, spells).
/// Projectiles typically have velocity and limited lifetime.
/// </summary>
public sealed class ProjectileEntityFactory : EntityFactory
{
    protected override string FactoryName => "ProjectileFactory";

    public ProjectileEntityFactory(
        ILogger<ProjectileEntityFactory> logger,
        IEventBus? eventBus = null
    )
        : base(logger, eventBus)
    {
        RegisterDefaultTemplates();
    }

    /// <summary>
    /// Creates a basic bullet projectile.
    /// Phase 6: Now uses CommandBuffer for safe deferred entity creation.
    /// </summary>
    public CommandBuffer.CreateCommand CreateBullet(
        CommandBuffer cmd,
        Vector2 position,
        Vector2 direction,
        float speed = 400f
    )
    {
        var normalizedDir = direction;
        if (normalizedDir.Length() > 0)
        {
            normalizedDir.Normalize();
        }

        var definition = new EntityDefinition(
            "Bullet",
            new object[]
            {
                new Position(position.X, position.Y, 0.6f), // Higher Z for rendering
                new Sprite("sprites/projectiles/bullet.png", 8, 8, 0.6f),
                new Renderable(true),
            },
            new Dictionary<string, object>
            {
                ["ProjectileType"] = "Bullet",
                ["Damage"] = 10,
                ["Lifetime"] = 3f, // seconds
                ["Speed"] = speed,
            }
        );

        return Create(cmd, definition);
    }

    /// <summary>
    /// Creates an arrow projectile.
    /// Phase 6: Now uses CommandBuffer for safe deferred entity creation.
    /// </summary>
    public CommandBuffer.CreateCommand CreateArrow(
        CommandBuffer cmd,
        Vector2 position,
        Vector2 direction,
        float speed = 300f
    )
    {
        var normalizedDir = direction;
        if (normalizedDir.Length() > 0)
        {
            normalizedDir.Normalize();
        }

        var angle = MathF.Atan2(normalizedDir.Y, normalizedDir.X);

        var definition = new EntityDefinition(
            "Arrow",
            new object[]
            {
                new Position(position.X, position.Y, 0.6f),
                new Sprite("sprites/projectiles/arrow.png", 16, 4, 0.6f) { Rotation = angle },
                new Renderable(true),
            },
            new Dictionary<string, object>
            {
                ["ProjectileType"] = "Arrow",
                ["Damage"] = 15,
                ["Lifetime"] = 5f,
            }
        );

        return Create(cmd, definition);
    }

    /// <summary>
    /// Creates a fireball spell projectile.
    /// Phase 6: Now uses CommandBuffer for safe deferred entity creation.
    /// </summary>
    public CommandBuffer.CreateCommand CreateFireball(
        CommandBuffer cmd,
        Vector2 position,
        Vector2 direction,
        float speed = 250f
    )
    {
        var normalizedDir = direction;
        if (normalizedDir.Length() > 0)
        {
            normalizedDir.Normalize();
        }

        var definition = new EntityDefinition(
            "Fireball",
            new object[]
            {
                new Position(position.X, position.Y, 0.6f),
                new Sprite("sprites/projectiles/fireball.png", 24, 24, 0.6f),
                new Renderable(true),
            },
            new Dictionary<string, object>
            {
                ["ProjectileType"] = "Magic",
                ["Element"] = "Fire",
                ["Damage"] = 30,
                ["SplashRadius"] = 32f,
                ["Lifetime"] = 4f,
            }
        );

        return Create(cmd, definition);
    }

    /// <summary>
    /// Creates an ice shard projectile.
    /// Phase 6: Now uses CommandBuffer for safe deferred entity creation.
    /// </summary>
    public CommandBuffer.CreateCommand CreateIceShard(
        CommandBuffer cmd,
        Vector2 position,
        Vector2 direction,
        float speed = 350f
    )
    {
        var normalizedDir = direction;
        if (normalizedDir.Length() > 0)
        {
            normalizedDir.Normalize();
        }

        var definition = new EntityDefinition(
            "IceShard",
            new object[]
            {
                new Position(position.X, position.Y, 0.6f),
                new Sprite("sprites/projectiles/ice_shard.png", 16, 16, 0.6f),
                new Renderable(true),
            },
            new Dictionary<string, object>
            {
                ["ProjectileType"] = "Magic",
                ["Element"] = "Ice",
                ["Damage"] = 20,
                ["SlowEffect"] = 0.5f, // Slows target by 50%
                ["SlowDuration"] = 2f,
                ["Lifetime"] = 3f,
            }
        );

        return Create(cmd, definition);
    }

    /// <summary>
    /// Creates a homing missile that can track targets.
    /// Phase 6: Now uses CommandBuffer for safe deferred entity creation.
    /// </summary>
    public CommandBuffer.CreateCommand CreateHomingMissile(
        CommandBuffer cmd,
        Vector2 position,
        Vector2 initialDirection,
        float speed = 200f
    )
    {
        var normalizedDir = initialDirection;
        if (normalizedDir.Length() > 0)
        {
            normalizedDir.Normalize();
        }

        var definition = new EntityDefinition(
            "HomingMissile",
            new object[]
            {
                new Position(position.X, position.Y, 0.6f),
                new Sprite("sprites/projectiles/missile.png", 20, 8, 0.6f),
                new Renderable(true),
            },
            new Dictionary<string, object>
            {
                ["ProjectileType"] = "Homing",
                ["Damage"] = 40,
                ["TurnRate"] = 3f, // radians per second
                ["Lifetime"] = 6f,
            }
        );

        return Create(cmd, definition);
    }

    private void RegisterDefaultTemplates()
    {
        RegisterTemplate(
            "projectile_bullet",
            new EntityDefinition(
                "Bullet",
                new object[]
                {
                    new Position(0, 0, 0.6f),
                    new Sprite("sprites/projectiles/bullet.png", 8, 8, 0.6f),
                    new Renderable(true),
                }
            )
        );

        RegisterTemplate(
            "projectile_arrow",
            new EntityDefinition(
                "Arrow",
                new object[]
                {
                    new Position(0, 0, 0.6f),
                    new Sprite("sprites/projectiles/arrow.png", 16, 4, 0.6f),
                    new Renderable(true),
                }
            )
        );

        RegisterTemplate(
            "projectile_fireball",
            new EntityDefinition(
                "Fireball",
                new object[]
                {
                    new Position(0, 0, 0.6f),
                    new Sprite("sprites/projectiles/fireball.png", 24, 24, 0.6f),
                    new Renderable(true),
                }
            )
        );
    }
}
