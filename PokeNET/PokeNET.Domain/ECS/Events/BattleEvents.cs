namespace PokeNET.Domain.ECS.Events
{
    /// <summary>
    /// Event published when a battle starts.
    /// </summary>
    public class BattleStartEvent : BattleEvent
    {
        /// <summary>
        /// Initializes a new instance of the BattleStartEvent class.
        /// </summary>
        public BattleStartEvent() : base(BattleEventType.BattleStart)
        {
        }

        /// <summary>
        /// Gets or sets whether this is a wild Pokemon battle.
        /// </summary>
        public bool IsWildBattle { get; set; }

        /// <summary>
        /// Gets or sets whether this is a gym leader battle.
        /// </summary>
        public bool IsGymLeader { get; set; }
    }

    /// <summary>
    /// Event published when a battle ends.
    /// </summary>
    public class BattleEndEvent : BattleEvent
    {
        /// <summary>
        /// Initializes a new instance of the BattleEndEvent class.
        /// </summary>
        public BattleEndEvent() : base(BattleEventType.BattleEnd)
        {
        }

        /// <summary>
        /// Gets or sets whether the player won the battle.
        /// </summary>
        public bool PlayerWon { get; set; }
    }

    /// <summary>
    /// Event published when a Pokemon faints.
    /// </summary>
    public class PokemonFaintEvent : BattleEvent
    {
        /// <summary>
        /// Initializes a new instance of the PokemonFaintEvent class.
        /// </summary>
        public PokemonFaintEvent() : base(BattleEventType.PokemonFainted)
        {
        }

        /// <summary>
        /// Gets or sets the name of the fainted Pokemon.
        /// </summary>
        public string PokemonName { get; set; } = string.Empty;
    }

    /// <summary>
    /// Event published when an attack is used.
    /// </summary>
    public class AttackEvent : BattleEvent
    {
        /// <summary>
        /// Initializes a new instance of the AttackEvent class.
        /// </summary>
        public AttackEvent() : base(BattleEventType.MoveUsed)
        {
        }

        /// <summary>
        /// Gets or sets the name of the attack.
        /// </summary>
        public string AttackName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the type of the attack (e.g., "Fire", "Water").
        /// </summary>
        public string AttackType { get; set; } = string.Empty;
    }

    /// <summary>
    /// Event published when a critical hit occurs.
    /// </summary>
    public class CriticalHitEvent : BattleEvent
    {
        /// <summary>
        /// Initializes a new instance of the CriticalHitEvent class.
        /// </summary>
        public CriticalHitEvent() : base(BattleEventType.CriticalHit)
        {
        }
    }

    /// <summary>
    /// Event published when health changes.
    /// </summary>
    public class HealthChangedEvent : IGameEvent
    {
        /// <summary>
        /// Gets or sets the current health.
        /// </summary>
        public int CurrentHealth { get; set; }

        /// <summary>
        /// Gets or sets the maximum health.
        /// </summary>
        public int MaxHealth { get; set; }

        /// <summary>
        /// Gets or sets the health percentage (0.0 to 1.0).
        /// </summary>
        public float HealthPercentage { get; set; }

        /// <summary>
        /// Gets the timestamp when the event occurred.
        /// </summary>
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Event published when weather changes.
    /// </summary>
    public class WeatherChangedEvent : IGameEvent
    {
        /// <summary>
        /// Gets or sets the new weather condition.
        /// </summary>
        public Weather NewWeather { get; set; }

        /// <summary>
        /// Gets the timestamp when the event occurred.
        /// </summary>
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Event published when an item is used.
    /// </summary>
    public class ItemUseEvent : IGameEvent
    {
        /// <summary>
        /// Gets or sets the name of the item used.
        /// </summary>
        public string ItemName { get; set; } = string.Empty;

        /// <summary>
        /// Gets the timestamp when the event occurred.
        /// </summary>
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Event published when a Pokemon is caught.
    /// </summary>
    public class PokemonCaughtEvent : IGameEvent
    {
        /// <summary>
        /// Gets or sets the name of the caught Pokemon.
        /// </summary>
        public string PokemonName { get; set; } = string.Empty;

        /// <summary>
        /// Gets the timestamp when the event occurred.
        /// </summary>
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Event published when a Pokemon levels up.
    /// </summary>
    public class LevelUpEvent : IGameEvent
    {
        /// <summary>
        /// Gets or sets the new level.
        /// </summary>
        public int NewLevel { get; set; }

        /// <summary>
        /// Gets the timestamp when the event occurred.
        /// </summary>
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Weather conditions in the game.
    /// </summary>
    public enum Weather
    {
        /// <summary>
        /// Clear weather (no effects).
        /// </summary>
        Clear,

        /// <summary>
        /// Rainy weather.
        /// </summary>
        Rain,

        /// <summary>
        /// Snowy weather.
        /// </summary>
        Snow,

        /// <summary>
        /// Sandstorm weather.
        /// </summary>
        Sandstorm,

        /// <summary>
        /// Foggy weather.
        /// </summary>
        Fog
    }
}
