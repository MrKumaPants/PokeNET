using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.MusicTheory;
using PokeNET.Audio.Models;
using MidiTimeSignature = Melanchall.DryWetMidi.Interaction.TimeSignature;
using MidiNoteName = Melanchall.DryWetMidi.MusicTheory.NoteName;

namespace PokeNET.Audio.Abstractions;

/// <summary>
/// Interface for procedural music generation using DryWetMidi library.
/// SOLID PRINCIPLE: Dependency Inversion - Abstracts MIDI generation implementation.
/// SOLID PRINCIPLE: Open/Closed - Extensible for different music generation algorithms.
/// </summary>
/// <remarks>
/// This interface enables dynamic music generation based on game state, emotions,
/// or other runtime parameters. Implementations use DryWetMidi for MIDI manipulation
/// and can generate adaptive soundtracks that respond to gameplay.
/// </remarks>
public interface IProceduralMusicGenerator
{
    /// <summary>
    /// Gets the currently configured music scale.
    /// </summary>
    Scale CurrentScale { get; }

    /// <summary>
    /// Gets the current tempo in beats per minute.
    /// </summary>
    int Tempo { get; }

    /// <summary>
    /// Gets the current time signature.
    /// </summary>
    MidiTimeSignature TimeSignature { get; }

    /// <summary>
    /// Generates a procedural music track based on parameters.
    /// </summary>
    /// <param name="parameters">Generation parameters defining mood, style, etc.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>A generated audio track ready for playback.</returns>
    Task<AudioTrack> GenerateAsync(ProceduralMusicParameters parameters, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a MIDI file from procedural parameters.
    /// </summary>
    /// <param name="parameters">Generation parameters.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>A MIDI file object.</returns>
    Task<MidiFile> GenerateMidiAsync(ProceduralMusicParameters parameters, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the music scale for generation.
    /// </summary>
    /// <param name="scale">The musical scale to use.</param>
    void SetScale(Scale scale);

    /// <summary>
    /// Sets the tempo for generated music.
    /// </summary>
    /// <param name="bpm">Beats per minute (typically 60-200).</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when BPM is out of valid range.</exception>
    void SetTempo(int bpm);

    /// <summary>
    /// Sets the time signature for generated music.
    /// </summary>
    /// <param name="timeSignature">The time signature to use.</param>
    void SetTimeSignature(MidiTimeSignature timeSignature);

    /// <summary>
    /// Adapts the currently playing track based on new parameters.
    /// </summary>
    /// <param name="adaptationParams">Parameters for real-time adaptation.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>A task representing the adaptation operation.</returns>
    /// <remarks>
    /// This enables dynamic music that responds to gameplay changes, such as
    /// increasing intensity during combat or calming during exploration.
    /// </remarks>
    Task AdaptCurrentTrackAsync(MusicAdaptationParameters adaptationParams, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a music variation of an existing track.
    /// </summary>
    /// <param name="sourceTrack">The source track to create a variation of.</param>
    /// <param name="variationIntensity">How much to vary from the original (0.0 to 1.0).</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>A new track that is a variation of the source.</returns>
    Task<AudioTrack> GenerateVariationAsync(AudioTrack sourceTrack, float variationIntensity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Event raised when procedural generation completes.
    /// </summary>
    event EventHandler<GenerationCompletedEventArgs>? GenerationCompleted;

    /// <summary>
    /// Event raised when track adaptation occurs.
    /// </summary>
    event EventHandler<TrackAdaptedEventArgs>? TrackAdapted;
}

/// <summary>
/// Parameters for procedural music generation.
/// </summary>
public class ProceduralMusicParameters
{
    /// <summary>
    /// Gets or sets the desired mood (e.g., "happy", "tense", "mysterious").
    /// </summary>
    public string Mood { get; set; } = "neutral";

    /// <summary>
    /// Gets or sets the energy level (0.0 = calm, 1.0 = intense).
    /// </summary>
    public float Energy { get; set; } = 0.5f;

    /// <summary>
    /// Gets or sets the complexity level (0.0 = simple, 1.0 = complex).
    /// </summary>
    public float Complexity { get; set; } = 0.5f;

    /// <summary>
    /// Gets or sets the target duration of the generated track.
    /// </summary>
    public TimeSpan Duration { get; set; } = TimeSpan.FromMinutes(2);

    /// <summary>
    /// Gets or sets the musical key preference.
    /// </summary>
    public MidiNoteName? KeyPreference { get; set; }

    /// <summary>
    /// Gets or sets the genre or style (e.g., "orchestral", "electronic", "chiptune").
    /// </summary>
    public string Style { get; set; } = "orchestral";

    /// <summary>
    /// Gets or sets the instruments to use for generation.
    /// </summary>
    public List<string> Instruments { get; set; } = new();

    /// <summary>
    /// Gets or sets custom generation parameters for advanced control.
    /// </summary>
    public Dictionary<string, object> CustomParameters { get; set; } = new();
}

/// <summary>
/// Parameters for adapting currently playing music.
/// </summary>
public class MusicAdaptationParameters
{
    /// <summary>
    /// Gets or sets the target energy level to transition to.
    /// </summary>
    public float? TargetEnergy { get; set; }

    /// <summary>
    /// Gets or sets the target tempo to transition to.
    /// </summary>
    public int? TargetTempo { get; set; }

    /// <summary>
    /// Gets or sets the transition duration.
    /// </summary>
    public TimeSpan TransitionDuration { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Gets or sets the adaptation style (smooth, abrupt, etc.).
    /// </summary>
    public AdaptationStyle Style { get; set; } = AdaptationStyle.Smooth;
}

/// <summary>
/// Styles for music adaptation.
/// </summary>
public enum AdaptationStyle
{
    /// <summary>Gradual, smooth transition.</summary>
    Smooth,

    /// <summary>Immediate, abrupt change.</summary>
    Abrupt,

    /// <summary>Transition at the next musical phrase boundary.</summary>
    NextPhrase,

    /// <summary>Transition at the next measure boundary.</summary>
    NextMeasure
}

/// <summary>
/// Event arguments for generation completion.
/// </summary>
public class GenerationCompletedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the generated track.
    /// </summary>
    public AudioTrack GeneratedTrack { get; init; } = null!;

    /// <summary>
    /// Gets the parameters used for generation.
    /// </summary>
    public ProceduralMusicParameters Parameters { get; init; } = null!;

    /// <summary>
    /// Gets the timestamp when generation completed.
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Event arguments for track adaptation.
/// </summary>
public class TrackAdaptedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the original track state.
    /// </summary>
    public MusicState OriginalState { get; init; } = null!;

    /// <summary>
    /// Gets the new adapted track state.
    /// </summary>
    public MusicState NewState { get; init; } = null!;

    /// <summary>
    /// Gets the adaptation parameters used.
    /// </summary>
    public MusicAdaptationParameters Parameters { get; init; } = null!;

    /// <summary>
    /// Gets the timestamp when adaptation occurred.
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
