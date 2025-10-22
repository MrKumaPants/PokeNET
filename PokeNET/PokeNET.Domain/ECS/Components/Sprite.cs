namespace PokeNET.Domain.ECS.Components;

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
    /// The width of the sprite in pixels.
    /// </summary>
    public int Width { get; set; }

    /// <summary>
    /// The height of the sprite in pixels.
    /// </summary>
    public int Height { get; set; }

    /// <summary>
    /// Whether the sprite is currently visible.
    /// </summary>
    public bool IsVisible { get; set; }

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
        IsVisible = true;
    }
}
