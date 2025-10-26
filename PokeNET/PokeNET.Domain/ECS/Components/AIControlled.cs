namespace PokeNET.Domain.ECS.Components;

/// <summary>
/// Marks an entity as controlled by AI.
/// </summary>
public struct AIControlled
{
    public string BehaviorType { get; set; }
    public float DecisionInterval { get; set; }
    public float TimeSinceLastDecision { get; set; }
}
