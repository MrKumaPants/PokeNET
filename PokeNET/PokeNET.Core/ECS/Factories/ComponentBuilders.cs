using Microsoft.Xna.Framework;
using PokeNET.Domain.ECS.Components;
using PokeNET.Domain.ECS.Factories;

namespace PokeNET.Core.ECS.Factories;

/// <summary>
/// Static class containing builder functions for all standard ECS components.
/// </summary>
/// <remarks>
/// These builders provide optimized, type-safe component creation from definitions.
/// They are registered with the ComponentFactory during application startup.
/// </remarks>
public static class ComponentBuilders
{
    /// <summary>
    /// Registers all standard component builders with the factory.
    /// </summary>
    /// <param name="factory">The component factory.</param>
    public static void RegisterAll(IComponentFactory factory)
    {
        factory.RegisterBuilder<Position>(BuildPosition);
        // NOTE: Acceleration, Friction, MovementConstraint, PixelVelocity builders removed
        // These physics components are inappropriate for Pokemon-style tile-based movement
        factory.RegisterBuilder<Sprite>(BuildSprite);
        factory.RegisterBuilder<Health>(BuildHealth);
        factory.RegisterBuilder<Stats>(BuildStats);
        factory.RegisterBuilder<Camera>(BuildCamera);
        factory.RegisterBuilder<Renderable>(BuildRenderable);
    }

    /// <summary>
    /// Builds a Position component from a definition.
    /// </summary>
    /// <remarks>
    /// Expected properties:
    /// - X (float): X coordinate (default: 0)
    /// - Y (float): Y coordinate (default: 0)
    /// - Z (float): Z coordinate for layer depth (default: 0)
    /// </remarks>
    public static Position BuildPosition(ComponentDefinition definition)
    {
        return new Position(
            x: definition.GetFloat("X", 0f),
            y: definition.GetFloat("Y", 0f),
            z: definition.GetFloat("Z", 0f)
        );
    }

    // NOTE: BuildAcceleration, BuildFriction, BuildMovementConstraint, BuildPixelVelocity removed.
    // These physics components are inappropriate for Pokemon-style tile-based movement.
    // Use GridMovementSystem for proper tile-based movement logic.

    /// <summary>
    /// Builds a Sprite component from a definition.
    /// </summary>
    /// <remarks>
    /// Expected properties:
    /// - TexturePath (string): Asset path to texture (required)
    /// - Width (int): Sprite width in pixels (required)
    /// - Height (int): Sprite height in pixels (required)
    /// - LayerDepth (float): Rendering layer (default: 0.5)
    /// - SourceX (int): Source rectangle X (optional)
    /// - SourceY (int): Source rectangle Y (optional)
    /// - SourceWidth (int): Source rectangle width (optional)
    /// - SourceHeight (int): Source rectangle height (optional)
    /// - Scale (float): Scale factor (default: 1.0)
    /// - Rotation (float): Rotation in radians (default: 0)
    /// - IsVisible (bool): Visibility flag (default: true)
    /// - ColorR (byte): Red color component (default: 255)
    /// - ColorG (byte): Green color component (default: 255)
    /// - ColorB (byte): Blue color component (default: 255)
    /// - ColorA (byte): Alpha component (default: 255)
    /// </remarks>
    public static Sprite BuildSprite(ComponentDefinition definition)
    {
        var texturePath = definition.GetString("TexturePath", "");
        var width = definition.GetInt("Width", 32);
        var height = definition.GetInt("Height", 32);
        var layerDepth = definition.GetFloat("LayerDepth", 0.5f);

        var sprite = new Sprite(texturePath, width, height, layerDepth);

        // Optional source rectangle
        if (definition.HasProperty("SourceX") || definition.HasProperty("SourceY"))
        {
            sprite.SourceRectangle = new Rectangle(
                definition.GetInt("SourceX", 0),
                definition.GetInt("SourceY", 0),
                definition.GetInt("SourceWidth", width),
                definition.GetInt("SourceHeight", height)
            );
        }

        // Optional properties
        sprite.Scale = definition.GetFloat("Scale", 1.0f);
        sprite.Rotation = definition.GetFloat("Rotation", 0f);
        sprite.IsVisible = definition.GetBool("IsVisible", true);

        // Optional color tint
        if (
            definition.HasProperty("ColorR")
            || definition.HasProperty("ColorG")
            || definition.HasProperty("ColorB")
        )
        {
            sprite.Color = new Color(
                definition.GetInt("ColorR", 255),
                definition.GetInt("ColorG", 255),
                definition.GetInt("ColorB", 255),
                definition.GetInt("ColorA", 255)
            );
        }

        return sprite;
    }

    /// <summary>
    /// Builds a Health component from a definition.
    /// </summary>
    /// <remarks>
    /// Expected properties:
    /// - Maximum (int): Maximum health (required)
    /// - Current (int): Current health (default: Maximum)
    /// </remarks>
    public static Health BuildHealth(ComponentDefinition definition)
    {
        var maximum = definition.GetInt("Maximum", 100);
        var current = definition.GetInt("Current", maximum);

        return new Health(current, maximum);
    }

    /// <summary>
    /// Builds a Stats component from a definition.
    /// </summary>
    /// <remarks>
    /// Expected properties:
    /// - Level (int): Entity level (default: 1)
    /// - Attack (int): Attack stat (default: 10)
    /// - Defense (int): Defense stat (default: 10)
    /// - SpecialAttack (int): Special attack stat (default: 10)
    /// - SpecialDefense (int): Special defense stat (default: 10)
    /// - Speed (int): Speed stat (default: 10)
    /// </remarks>
    public static Stats BuildStats(ComponentDefinition definition)
    {
        return new Stats
        {
            Level = definition.GetInt("Level", 1),
            Attack = definition.GetInt("Attack", 10),
            Defense = definition.GetInt("Defense", 10),
            SpecialAttack = definition.GetInt("SpecialAttack", 10),
            SpecialDefense = definition.GetInt("SpecialDefense", 10),
            Speed = definition.GetInt("Speed", 10),
        };
    }

    /// <summary>
    /// Builds a Camera component from a definition.
    /// </summary>
    /// <remarks>
    /// Expected properties:
    /// - X (float): Camera X position (default: 0)
    /// - Y (float): Camera Y position (default: 0)
    /// - ViewportWidth (int): Viewport width (default: 800)
    /// - ViewportHeight (int): Viewport height (default: 600)
    /// - Zoom (float): Zoom factor (default: 1.0)
    /// - Rotation (float): Camera rotation in radians (default: 0)
    /// - IsActive (bool): Whether this is the active camera (default: true)
    /// </remarks>
    public static Camera BuildCamera(ComponentDefinition definition)
    {
        var x = definition.GetFloat("X", 0f);
        var y = definition.GetFloat("Y", 0f);
        var width = definition.GetInt("ViewportWidth", 800);
        var height = definition.GetInt("ViewportHeight", 600);
        var zoom = definition.GetFloat("Zoom", 1.0f);

        var camera = new Camera(new Vector2(x, y), width, height, zoom);
        camera.Rotation = definition.GetFloat("Rotation", 0f);
        camera.IsActive = definition.GetBool("IsActive", true);

        return camera;
    }

    // NOTE: BuildFriction and BuildMovementConstraint methods removed.
    // These physics components have been deleted as they're inappropriate
    // for Pokemon-style tile-based movement. Use GridMovementSystem instead.

    /// <summary>
    /// Builds a Renderable component from a definition.
    /// </summary>
    /// <remarks>
    /// Expected properties:
    /// - IsVisible (bool): Visibility flag (default: true)
    /// - ShowDebug (bool): Show debug rendering (default: false)
    /// - Alpha (float): Alpha transparency 0.0-1.0 (default: 1.0)
    /// </remarks>
    public static Renderable BuildRenderable(ComponentDefinition definition)
    {
        return new Renderable(
            isVisible: definition.GetBool("IsVisible", true),
            showDebug: definition.GetBool("ShowDebug", false),
            alpha: definition.GetFloat("Alpha", 1.0f)
        );
    }
}
