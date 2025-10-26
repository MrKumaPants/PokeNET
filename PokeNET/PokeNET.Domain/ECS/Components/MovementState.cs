namespace PokeNET.Domain.ECS.Components;

/// <summary>
/// Represents the current movement state of an entity in Pokemon-style gameplay.
/// </summary>
/// <remarks>
/// Determines movement speed, animation state, and allowed actions.
/// Different states may have different speeds and interact with terrain differently.
/// </remarks>
public enum MovementMode
{
    /// <summary>Not moving, standing still.</summary>
    Idle = 0,

    /// <summary>Normal walking speed.</summary>
    Walking = 1,

    /// <summary>Faster running speed (typically with Running Shoes item).</summary>
    Running = 2,

    /// <summary>Jumping over ledges or obstacles.</summary>
    Jumping = 3,

    /// <summary>Moving on water with Surf ability.</summary>
    Surfing = 4,

    /// <summary>Moving with a bicycle (fastest ground movement).</summary>
    Cycling = 5,
}

/// <summary>
/// Component that tracks movement state and capabilities.
/// </summary>
public struct MovementState
{
    /// <summary>
    /// The current movement mode (walking, running, surfing, etc.).
    /// </summary>
    public MovementMode Mode { get; set; }

    /// <summary>
    /// Movement speed in tiles per second for the current mode.
    /// </summary>
    /// <remarks>
    /// Typical Pokemon speeds:
    /// - Walking: 4 tiles/sec
    /// - Running: 8 tiles/sec
    /// - Cycling: 12 tiles/sec
    /// - Surfing: 6 tiles/sec
    /// </remarks>
    public float MovementSpeed { get; set; }

    /// <summary>
    /// Whether the entity can currently move.
    /// Set to false during dialogue, battles, or other interruptions.
    /// </summary>
    public bool CanMove { get; set; }

    /// <summary>
    /// Whether the entity has the ability to run (Running Shoes item).
    /// </summary>
    public bool CanRun { get; set; }

    /// <summary>
    /// Initializes a new movement state with default values.
    /// </summary>
    /// <param name="mode">The initial movement mode (default: Walking).</param>
    /// <param name="canRun">Whether running is enabled (default: false).</param>
    public MovementState(MovementMode mode = MovementMode.Walking, bool canRun = false)
    {
        Mode = mode;
        MovementSpeed = GetDefaultSpeed(mode);
        CanMove = true;
        CanRun = canRun;
    }

    /// <summary>
    /// Gets the default movement speed for a given mode.
    /// </summary>
    private static float GetDefaultSpeed(MovementMode mode)
    {
        return mode switch
        {
            MovementMode.Walking => 4.0f,
            MovementMode.Running => 8.0f,
            MovementMode.Cycling => 12.0f,
            MovementMode.Surfing => 6.0f,
            MovementMode.Jumping => 8.0f,
            _ => 0.0f,
        };
    }

    public override readonly string ToString() =>
        $"{Mode} ({MovementSpeed:F1} tiles/s) {(CanMove ? "Ready" : "Blocked")}";
}
