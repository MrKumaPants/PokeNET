using System.Text.Json.Serialization;

namespace PokeNET.Audio.Configuration
{
    /// <summary>
    /// Root configuration class for audio reactions loaded from JSON.
    /// </summary>
    public class AudioReactionConfig
    {
        /// <summary>
        /// Gets or sets the list of reaction definitions.
        /// </summary>
        [JsonPropertyName("reactions")]
        public List<ReactionDefinition> Reactions { get; set; } = new();
    }

    /// <summary>
    /// Defines a single audio reaction with conditions and actions.
    /// </summary>
    public class ReactionDefinition
    {
        /// <summary>
        /// Gets or sets the unique name of this reaction.
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the type of reaction (e.g., "MusicTransition", "SoundEffect").
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the event type this reaction responds to.
        /// </summary>
        [JsonPropertyName("eventType")]
        public string EventType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether this reaction is enabled.
        /// </summary>
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the conditions that must be met for this reaction to trigger.
        /// </summary>
        [JsonPropertyName("conditions")]
        public List<ConditionDefinition> Conditions { get; set; } = new();

        /// <summary>
        /// Gets or sets the actions to execute when conditions are met.
        /// </summary>
        [JsonPropertyName("actions")]
        public List<ActionDefinition> Actions { get; set; } = new();
    }

    /// <summary>
    /// Defines a condition that must be met for a reaction to trigger.
    /// </summary>
    public class ConditionDefinition
    {
        /// <summary>
        /// Gets or sets the property name to evaluate (e.g., "IsWildBattle", "HealthPercentage").
        /// </summary>
        [JsonPropertyName("property")]
        public string Property { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the comparison operator (equals, notEquals, lessThan, greaterThan, contains).
        /// </summary>
        [JsonPropertyName("operator")]
        public string Operator { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the value to compare against.
        /// </summary>
        [JsonPropertyName("value")]
        public object? Value { get; set; }
    }

    /// <summary>
    /// Defines an action to execute when reaction conditions are met.
    /// </summary>
    public class ActionDefinition
    {
        /// <summary>
        /// Gets or sets the action type (PlayMusic, PlaySound, FadeIn, FadeOut, SetVolume, StopAll).
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the audio channel (Music, SoundEffect, Ambient).
        /// </summary>
        [JsonPropertyName("channel")]
        public string? Channel { get; set; }

        /// <summary>
        /// Gets or sets the file path for audio playback.
        /// </summary>
        [JsonPropertyName("path")]
        public string? Path { get; set; }

        /// <summary>
        /// Gets or sets the volume level (0.0 to 1.0).
        /// </summary>
        [JsonPropertyName("volume")]
        public float Volume { get; set; } = 1.0f;

        /// <summary>
        /// Gets or sets the fade-in duration in seconds.
        /// </summary>
        [JsonPropertyName("fadeIn")]
        public float FadeIn { get; set; }

        /// <summary>
        /// Gets or sets the fade-out duration in seconds.
        /// </summary>
        [JsonPropertyName("fadeOut")]
        public float FadeOut { get; set; }

        /// <summary>
        /// Gets or sets the duration for time-based actions in seconds.
        /// </summary>
        [JsonPropertyName("duration")]
        public float Duration { get; set; }

        /// <summary>
        /// Gets or sets whether audio should loop.
        /// </summary>
        [JsonPropertyName("loop")]
        public bool Loop { get; set; }
    }
}
