namespace PokeNET.Core.ECS.Components;

/// <summary>
/// Tracks the current animation state of an entity.
/// </summary>
public struct AnimationState
{
    public string CurrentAnimation { get; set; }
    public float AnimationTime { get; set; }
    public bool IsLooping { get; set; }
}
