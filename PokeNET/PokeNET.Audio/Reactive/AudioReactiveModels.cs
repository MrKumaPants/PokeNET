using PokeNET.Core.ECS.Events;
using PokeNET.Core.Models;

namespace PokeNET.Audio.Reactive
{
    /// <summary>
    /// Represents the comprehensive reactive audio state for audio decision-making.
    /// </summary>
    public class ReactiveAudioState
    {
        /// <summary>
        /// Gets or sets whether the player is currently in a battle.
        /// </summary>
        public bool InBattle { get; set; }

        /// <summary>
        /// Gets or sets the current battle type.
        /// </summary>
        public BattleType BattleType { get; set; }

        /// <summary>
        /// Gets or sets whether this is a trainer battle.
        /// </summary>
        public bool IsTrainerBattle { get; set; }

        /// <summary>
        /// Gets or sets the battle intensity (0.0 = calm, 1.0 = intense).
        /// </summary>
        public float BattleIntensity { get; set; }

        /// <summary>
        /// Gets or sets whether the player is indoors.
        /// </summary>
        public bool IsIndoors { get; set; }

        /// <summary>
        /// Gets or sets the current location type.
        /// </summary>
        public LocationType LocationType { get; set; }

        /// <summary>
        /// Gets or sets the current location name.
        /// </summary>
        public string CurrentLocation { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the current time of day.
        /// </summary>
        public TimeOfDay TimeOfDay { get; set; }

        /// <summary>
        /// Gets or sets the current weather condition.
        /// </summary>
        public Weather Weather { get; set; }

        /// <summary>
        /// Gets or sets the player's current health percentage (0.0 to 1.0).
        /// </summary>
        public float HealthPercentage { get; set; } = 1.0f;

        /// <summary>
        /// Gets or sets whether the player is in a critical health state.
        /// </summary>
        public bool IsCriticalHealth { get; set; }

        /// <summary>
        /// Gets or sets the current story event or cutscene name.
        /// </summary>
        public string? CurrentStoryEvent { get; set; }

        /// <summary>
        /// Gets or sets whether music is paused.
        /// </summary>
        public bool IsMusicPaused { get; set; }

        /// <summary>
        /// Gets or sets whether the player is in a menu.
        /// </summary>
        public bool IsInMenu { get; set; }
    }

    /// <summary>
    /// Battle types for music selection.
    /// </summary>
    public enum BattleType
    {
        /// <summary>
        /// Wild Pokemon battle.
        /// </summary>
        Wild,

        /// <summary>
        /// Trainer battle.
        /// </summary>
        Trainer,

        /// <summary>
        /// Gym leader battle.
        /// </summary>
        Gym,

        /// <summary>
        /// Boss or legendary battle.
        /// </summary>
        Boss,

        /// <summary>
        /// Elite Four or Champion battle.
        /// </summary>
        EliteFour,
    }

    /// <summary>
    /// Location types for environment-based music.
    /// </summary>
    public enum LocationType
    {
        /// <summary>
        /// Route or path between towns.
        /// </summary>
        Route,

        /// <summary>
        /// Small town.
        /// </summary>
        Town,

        /// <summary>
        /// Large city.
        /// </summary>
        City,

        /// <summary>
        /// Cave or underground area.
        /// </summary>
        Cave,

        /// <summary>
        /// Forest area.
        /// </summary>
        Forest,

        /// <summary>
        /// Water or beach area.
        /// </summary>
        Water,

        /// <summary>
        /// Mountain area.
        /// </summary>
        Mountain,

        /// <summary>
        /// Indoor building.
        /// </summary>
        Indoor,

        /// <summary>
        /// Special or story location.
        /// </summary>
        Special,
    }

    /// <summary>
    /// Time of day for ambient music variation.
    /// </summary>
    public enum TimeOfDay
    {
        /// <summary>
        /// Morning (sunrise to mid-morning).
        /// </summary>
        Morning,

        /// <summary>
        /// Day (mid-morning to afternoon).
        /// </summary>
        Day,

        /// <summary>
        /// Evening (afternoon to sunset).
        /// </summary>
        Evening,

        /// <summary>
        /// Night (sunset to sunrise).
        /// </summary>
        Night,
    }
}
