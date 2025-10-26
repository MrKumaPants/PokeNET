using System;

namespace PokeNET.Domain.ECS.Components;

/// <summary>
/// Marker component indicating an entity should be rendered.
/// Provides additional rendering control flags.
/// </summary>
public struct Renderable
{
    /// <summary>
    /// Whether the entity is currently visible and should be rendered.
    /// </summary>
    public bool IsVisible { get; set; }

    /// <summary>
    /// Whether to render debug information (bounding boxes, etc.).
    /// </summary>
    public bool ShowDebug { get; set; }

    /// <summary>
    /// Alpha transparency value (0.0 = fully transparent, 1.0 = fully opaque).
    /// </summary>
    public float Alpha { get; set; }

    /// <summary>
    /// Initializes a new renderable component.
    /// </summary>
    /// <param name="isVisible">Whether the entity is visible.</param>
    /// <param name="showDebug">Whether to show debug rendering.</param>
    /// <param name="alpha">The alpha transparency value.</param>
    public Renderable(bool isVisible = true, bool showDebug = false, float alpha = 1.0f)
    {
        IsVisible = isVisible;
        ShowDebug = showDebug;
        Alpha = Math.Clamp(alpha, 0f, 1f);
    }

    /// <summary>
    /// Creates a renderable that is initially hidden.
    /// </summary>
    public static Renderable Hidden() => new(false);

    /// <summary>
    /// Creates a renderable with debug rendering enabled.
    /// </summary>
    public static Renderable WithDebug() => new(true, true);
}
