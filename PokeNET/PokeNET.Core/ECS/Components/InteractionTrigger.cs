namespace PokeNET.Core.ECS.Components;

/// <summary>
/// Marks an entity as having an interaction trigger zone.
/// </summary>
public struct InteractionTrigger
{
    public float TriggerRadius { get; set; }
    public bool IsEnabled { get; set; }
    public string InteractionType { get; set; }
}
