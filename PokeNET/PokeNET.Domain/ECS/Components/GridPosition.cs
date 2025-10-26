using System.Numerics;

namespace PokeNET.Domain.ECS.Components;

/// <summary>
/// Tile-based position for Pokemon-style grid movement.
/// Replaces physics-based Position + Velocity approach with discrete tile coordinates.
/// </summary>
/// <remarks>
/// This component represents a character's position on a tile-based grid, similar to classic Pokemon games.
/// Each tile is 16x16 pixels. The component supports smooth interpolation between tiles for animation.
/// </remarks>
public struct GridPosition
{
    /// <summary>
    /// The current tile X coordinate on the grid.
    /// </summary>
    public int TileX { get; set; }

    /// <summary>
    /// The current tile Y coordinate on the grid.
    /// </summary>
    public int TileY { get; set; }

    /// <summary>
    /// The map/zone identifier this position belongs to.
    /// Allows for multiple maps, routes, buildings, etc.
    /// </summary>
    public int MapId { get; set; }

    /// <summary>
    /// Progress of movement interpolation between current and target tile.
    /// 0.0 = at current tile, 1.0 = at target tile.
    /// </summary>
    /// <remarks>
    /// Used for smooth animation between discrete tile positions.
    /// Values between 0 and 1 indicate the entity is currently moving.
    /// </remarks>
    public float InterpolationProgress { get; set; }

    /// <summary>
    /// The target tile X coordinate when moving.
    /// Equals TileX when not moving.
    /// </summary>
    public int TargetTileX { get; set; }

    /// <summary>
    /// The target tile Y coordinate when moving.
    /// Equals TileY when not moving.
    /// </summary>
    public int TargetTileY { get; set; }

    /// <summary>
    /// Gets the world position in pixels based on tile coordinates.
    /// Each tile is 16x16 pixels.
    /// </summary>
    public readonly Vector2 WorldPosition => new(TileX * 16f, TileY * 16f);

    /// <summary>
    /// Gets whether the entity is currently moving between tiles.
    /// </summary>
    public readonly bool IsMoving => InterpolationProgress < 1.0f;

    /// <summary>
    /// Initializes a new grid position at the specified tile coordinates.
    /// </summary>
    /// <param name="tileX">The X tile coordinate.</param>
    /// <param name="tileY">The Y tile coordinate.</param>
    /// <param name="mapId">The map identifier (default: 0).</param>
    public GridPosition(int tileX, int tileY, int mapId = 0)
    {
        TileX = tileX;
        TileY = tileY;
        MapId = mapId;
        InterpolationProgress = 1.0f; // Not moving initially
        TargetTileX = tileX;
        TargetTileY = tileY;
    }

    public override readonly string ToString() =>
        $"Tile({TileX}, {TileY}) Map:{MapId} {(IsMoving ? $"→ ({TargetTileX}, {TargetTileY}) {InterpolationProgress:P0}" : "Idle")}";
}
