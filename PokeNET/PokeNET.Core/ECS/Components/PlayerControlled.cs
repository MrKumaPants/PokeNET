namespace PokeNET.Core.ECS.Components;

/// <summary>
/// Marks an entity as controlled by the player.
/// </summary>
public struct PlayerControlled
{
    public bool IsInputEnabled { get; set; }
}
