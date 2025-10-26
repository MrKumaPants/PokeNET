using System;
using System.Drawing;
using System.Numerics;
using Microsoft.Xna.Framework;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using Vector2 = Microsoft.Xna.Framework.Vector2;
using Vector3 = Microsoft.Xna.Framework.Vector3;

namespace PokeNET.Core.ECS.Components;

/// <summary>
/// Represents a camera for viewing the game world.
/// Controls viewport transformation and culling.
/// </summary>
public struct Camera
{
    /// <summary>
    /// The position of the camera in world space.
    /// </summary>
    public Vector2 Position { get; set; }

    /// <summary>
    /// The zoom level (1.0 = 100%, 2.0 = 200%, etc.).
    /// </summary>
    public float Zoom { get; set; }

    /// <summary>
    /// The rotation of the camera in radians.
    /// </summary>
    public float Rotation { get; set; }

    /// <summary>
    /// The viewport width in pixels.
    /// </summary>
    public int ViewportWidth { get; set; }

    /// <summary>
    /// The viewport height in pixels.
    /// </summary>
    public int ViewportHeight { get; set; }

    /// <summary>
    /// Whether this camera is the active rendering camera.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Initializes a new camera component.
    /// </summary>
    /// <param name="position">The camera position.</param>
    /// <param name="viewportWidth">The viewport width.</param>
    /// <param name="viewportHeight">The viewport height.</param>
    /// <param name="zoom">The zoom level.</param>
    public Camera(Vector2 position, int viewportWidth, int viewportHeight, float zoom = 1.0f)
    {
        Position = position;
        ViewportWidth = viewportWidth;
        ViewportHeight = viewportHeight;
        Zoom = Math.Max(0.1f, zoom);
        Rotation = 0f;
        IsActive = true;
    }

    /// <summary>
    /// Gets the transformation matrix for this camera.
    /// </summary>
    public readonly Matrix GetTransformMatrix()
    {
        return Matrix.CreateTranslation(new Vector3(-Position.X, -Position.Y, 0))
            * Matrix.CreateRotationZ(Rotation)
            * Matrix.CreateScale(Zoom, Zoom, 1)
            * Matrix.CreateTranslation(new Vector3(ViewportWidth * 0.5f, ViewportHeight * 0.5f, 0));
    }

    /// <summary>
    /// Gets the bounding rectangle of the camera's view in world space.
    /// </summary>
    public readonly Rectangle GetBounds()
    {
        var halfWidth = ViewportWidth / (2f * Zoom);
        var halfHeight = ViewportHeight / (2f * Zoom);
        return new Rectangle(
            (int)(Position.X - halfWidth),
            (int)(Position.Y - halfHeight),
            (int)(ViewportWidth / Zoom),
            (int)(ViewportHeight / Zoom)
        );
    }

    /// <summary>
    /// Converts screen coordinates to world coordinates.
    /// </summary>
    public readonly Vector2 ScreenToWorld(Vector2 screenPosition)
    {
        var transform = GetTransformMatrix();
        Matrix.Invert(ref transform, out var inverted);
        return Vector2.Transform(screenPosition, inverted);
    }

    /// <summary>
    /// Converts world coordinates to screen coordinates.
    /// </summary>
    public readonly Vector2 WorldToScreen(Vector2 worldPosition)
    {
        return Vector2.Transform(worldPosition, GetTransformMatrix());
    }
}
