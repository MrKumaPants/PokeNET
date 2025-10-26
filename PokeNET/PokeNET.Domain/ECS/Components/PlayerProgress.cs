namespace PokeNET.Domain.ECS.Components;

/// <summary>
/// Tracks player progress and game state.
/// </summary>
public struct PlayerProgress
{
    public int BadgesEarned { get; set; }
    public int PokedexSeen { get; set; }
    public int PokedexCaught { get; set; }
    public float PlayTime { get; set; }
}
