namespace PokeNET.Audio.Models;

/// <summary>
/// Represents a playing sound effect instance.
/// Note: MonoGame SoundEffectInstance will be used at runtime.
/// This is a placeholder for the domain model.
/// </summary>
public sealed class SoundInstance
{
    public int Id { get; set; }
    public string AssetPath { get; set; } = string.Empty;
    public float Volume { get; set; }
    public float Pitch { get; set; }
    public float Pan { get; set; }
    public bool IsLooping { get; set; }
    public int Priority { get; set; }
    public DateTime StartTime { get; set; }
    public bool IsPlaying { get; set; }
    public bool IsPaused { get; set; }

    /// <summary>
    /// Reference to the actual MonoGame SoundEffectInstance.
    /// This will be set at runtime when MonoGame is available.
    /// </summary>
    public object? RuntimeInstance { get; set; }
}
