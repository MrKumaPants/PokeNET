namespace PokeNET.Domain.Data;

/// <summary>
/// Test helper class for item effects.
/// This is a simplified version for testing purposes.
/// </summary>
public class ItemEffect
{
    public string EffectType { get; set; } = string.Empty;
    public int HealAmount { get; set; }
    public float CatchRateMultiplier { get; set; }
}
