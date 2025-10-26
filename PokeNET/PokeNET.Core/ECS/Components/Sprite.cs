using System.Drawing;
using System.Numerics;
using Microsoft.Xna.Framework;
using Color = Microsoft.Xna.Framework.Color;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using Vector2 = Microsoft.Xna.Framework.Vector2;

namespace PokeNET.Core.ECS.Components;

/// <summary>
/// Represents visual rendering information for an entity.
/// This component follows the Single Responsibility Principle by only handling sprite data.
/// </summary>
public struct Sprite
{
    /// <summary>
    /// The asset path or identifier for the sprite texture.
    /// </summary>
    public string TexturePath { get; set; }

    /// <summary>
    /// The layer depth for rendering order (0.0 = back, 1.0 = front).
    /// </summary>
    public float LayerDepth { get; set; }

    /// <summary>
    /// The source rectangle within the texture (null = entire texture).
    /// </summary>
    public Rectangle? SourceRectangle { get; set; }

    /// <summary>
    /// The color tint to apply (White = no tint).
    /// </summary>
    public Color Color { get; set; }

    /// <summary>
    /// The scale factor for rendering (1.0 = original size).
    /// </summary>
    public float Scale { get; set; }

    /// <summary>
    /// The rotation angle in radians.
    /// </summary>
    public float Rotation { get; set; }

    /// <summary>
    /// The origin point for rotation and scaling (defaults to center).
    /// </summary>
    public Vector2 Origin { get; set; }

    /// <summary>
    /// Whether the sprite is currently visible.
    /// </summary>
    public bool IsVisible { get; set; }

    /// <summary>
    /// The width of the sprite in pixels (from source rectangle or texture).
    /// </summary>
    public int Width { get; set; }

    /// <summary>
    /// The height of the sprite in pixels (from source rectangle or texture).
    /// </summary>
    public int Height { get; set; }

    /// <summary>
    /// Initializes a new sprite component.
    /// </summary>
    /// <param name="texturePath">The path to the texture asset.</param>
    /// <param name="width">The width in pixels.</param>
    /// <param name="height">The height in pixels.</param>
    /// <param name="layerDepth">The rendering layer depth.</param>
    public Sprite(string texturePath, int width, int height, float layerDepth = 0.5f)
    {
        TexturePath = texturePath;
        Width = width;
        Height = height;
        LayerDepth = layerDepth;
        SourceRectangle = null;
        Color = Color.White;
        Scale = 1.0f;
        Rotation = 0f;
        Origin = new Vector2(width / 2f, height / 2f);
        IsVisible = true;
    }
}
