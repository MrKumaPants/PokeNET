namespace PokeNET.Audio.Reactive
{
    /// <summary>
    /// Types of audio reactions that can be enabled or disabled.
    /// Used for fine-grained control over reactive audio behavior.
    /// </summary>
    public enum AudioReactionType
    {
        /// <summary>
        /// Music changes during battles.
        /// </summary>
        BattleMusic,

        /// <summary>
        /// Ambient sound effects based on weather.
        /// </summary>
        WeatherAmbient,

        /// <summary>
        /// Music ducking during menu navigation.
        /// </summary>
        MenuDucking,

        /// <summary>
        /// Health-based music changes (low health warning).
        /// </summary>
        HealthMusic,

        /// <summary>
        /// Location-based music transitions.
        /// </summary>
        LocationMusic,

        /// <summary>
        /// Battle sound effects.
        /// </summary>
        BattleSoundEffects,

        /// <summary>
        /// Item use sound effects.
        /// </summary>
        ItemSoundEffects,

        /// <summary>
        /// Pokemon interaction sounds.
        /// </summary>
        PokemonSounds,
    }
}
