using System;

namespace PokeNET.Domain.ECS.Components;

/// <summary>
/// Collision layers for tile-based collision detection.
/// </summary>
/// <remarks>
/// Entities can belong to one or more layers and interact with specific layers.
/// Inspired by Pokemon's collision system for NPCs, objects, and terrain.
/// </remarks>
[Flags]
public enum CollisionLayer
{
    /// <summary>No collision layer.</summary>
    None = 0,

    /// <summary>Player character layer.</summary>
    Player = 1 << 0,

    /// <summary>Non-player character (trainer, wild Pokemon, NPC) layer.</summary>
    NPC = 1 << 1,

    /// <summary>Interactable object (sign, item ball, cuttable tree) layer.</summary>
    Object = 1 << 2,

    /// <summary>Terrain/tile layer (walls, water, grass, etc.).</summary>
    Terrain = 1 << 3,
}

/// <summary>
/// Component for tile-based collision detection and interaction.
/// </summary>
/// <remarks>
/// Determines whether an entity blocks movement and which layers it can interact with.
/// Used by the collision system to prevent invalid movement and trigger interactions.
/// </remarks>
public struct TileCollider
{
    /// <summary>
    /// The collision layer(s) this entity belongs to.
    /// </summary>
    public CollisionLayer Layer { get; set; }

    /// <summary>
    /// Whether this entity blocks movement (solid collision).
    /// </summary>
    /// <remarks>
    /// True for walls, NPCs, solid objects.
    /// False for grass, water (with Surf), items on ground.
    /// </remarks>
    public bool IsSolid { get; set; }

    /// <summary>
    /// The set of collision layers this entity can interact with.
    /// </summary>
    /// <remarks>
    /// For example, a player might interact with NPCs and Objects but not other Players.
    /// </remarks>
    public CollisionLayer InteractsWithLayers { get; set; }

    /// <summary>
    /// Event triggered when this entity collides with another entity.
    /// </summary>
    /// <remarks>
    /// Used for triggering battles, dialogue, item pickup, etc.
    /// The event argument is the entity ID of the colliding entity.
    /// </remarks>
    public event Action<uint>? OnCollision;

    /// <summary>
    /// Initializes a new tile collider.
    /// </summary>
    /// <param name="layer">The collision layer this entity belongs to.</param>
    /// <param name="isSolid">Whether this entity blocks movement (default: true).</param>
    /// <param name="interactsWithLayers">Layers this entity can interact with (default: all layers).</param>
    public TileCollider(
        CollisionLayer layer,
        bool isSolid = true,
        CollisionLayer interactsWithLayers =
            CollisionLayer.Player
            | CollisionLayer.NPC
            | CollisionLayer.Object
            | CollisionLayer.Terrain
    )
    {
        Layer = layer;
        IsSolid = isSolid;
        InteractsWithLayers = interactsWithLayers;
        OnCollision = null;
    }

    /// <summary>
    /// Checks if this collider can interact with the specified layer.
    /// </summary>
    /// <param name="otherLayer">The layer to check interaction with.</param>
    /// <returns>True if interaction is allowed, false otherwise.</returns>
    public readonly bool CanInteractWith(CollisionLayer otherLayer)
    {
        return (InteractsWithLayers & otherLayer) != 0;
    }

    /// <summary>
    /// Invokes the collision event with the specified entity ID.
    /// </summary>
    /// <param name="entityId">The ID of the entity being collided with.</param>
    internal void TriggerCollision(uint entityId)
    {
        OnCollision?.Invoke(entityId);
    }

    public override readonly string ToString() =>
        $"{Layer} {(IsSolid ? "Solid" : "NonSolid")} (Interacts: {InteractsWithLayers})";
}
