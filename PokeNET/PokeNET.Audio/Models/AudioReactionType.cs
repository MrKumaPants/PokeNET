namespace PokeNET.Audio.Models;

/// <summary>
/// Defines the types of audio reactions for reactive audio system.
/// SOLID PRINCIPLE: Single Responsibility - Defines only audio event categorization.
/// </summary>
/// <remarks>
/// This enum categorizes different game events that trigger audio reactions,
/// enabling the reactive audio system to respond dynamically to gameplay.
/// Used by the ReactiveAudioEngine for event-driven music and sound changes.
/// </remarks>
public enum AudioReactionType
{
    /// <summary>
    /// Battle initiated with wild Pokemon.
    /// Triggers transition to battle music.
    /// </summary>
    BattleStart,

    /// <summary>
    /// Battle concluded (won or fled).
    /// Triggers return to previous music state.
    /// </summary>
    BattleEnd,

    /// <summary>
    /// Boss or legendary Pokemon battle initiated.
    /// Triggers epic battle music with higher intensity.
    /// </summary>
    BossBattle,

    /// <summary>
    /// Player moved to a new location/zone.
    /// Triggers location-specific music change.
    /// </summary>
    LocationChange,

    /// <summary>
    /// Wild Pokemon encounter (before battle).
    /// Triggers encounter sound effect and music swell.
    /// </summary>
    WildEncounter,

    /// <summary>
    /// Player or Pokemon health dropped to critical levels.
    /// Triggers urgent audio cues and music intensity increase.
    /// </summary>
    HealthCritical,

    /// <summary>
    /// Player health restored above critical threshold.
    /// Triggers return to normal music state.
    /// </summary>
    HealthRecovered,

    /// <summary>
    /// Item obtained by player.
    /// Triggers item pickup sound effect with music ducking.
    /// </summary>
    ItemPickup,

    /// <summary>
    /// Important or rare item obtained.
    /// Triggers special item jingle and music pause.
    /// </summary>
    RareItemPickup,

    /// <summary>
    /// Menu opened or UI interaction.
    /// Triggers menu sound effects.
    /// </summary>
    MenuSound,

    /// <summary>
    /// Dialogue or story sequence started.
    /// Triggers music ducking and dialogue ambience.
    /// </summary>
    Dialogue,

    /// <summary>
    /// Dialogue or story sequence ended.
    /// Triggers return to full music volume.
    /// </summary>
    DialogueEnd,

    /// <summary>
    /// Achievement or milestone unlocked.
    /// Triggers achievement fanfare with music pause.
    /// </summary>
    Achievement,

    /// <summary>
    /// Pokemon evolved.
    /// Triggers evolution music and sound effects.
    /// </summary>
    Evolution,

    /// <summary>
    /// Pokemon captured successfully.
    /// Triggers capture success jingle.
    /// </summary>
    PokemonCaptured,

    /// <summary>
    /// Pokemon capture failed (escaped or broke free).
    /// Triggers escape sound effect.
    /// </summary>
    CaptureFailed,

    /// <summary>
    /// Player Pokemon fainted in battle.
    /// Triggers defeat sound and music intensity decrease.
    /// </summary>
    PokemonFainted,

    /// <summary>
    /// Entire party fainted (white out).
    /// Triggers defeat music sequence.
    /// </summary>
    PartyWipeout,

    /// <summary>
    /// Trainer battle initiated (vs NPC trainer).
    /// Triggers trainer battle music.
    /// </summary>
    TrainerBattle,

    /// <summary>
    /// Gym leader battle initiated.
    /// Triggers gym leader battle music.
    /// </summary>
    GymLeaderBattle,

    /// <summary>
    /// Champion or elite four battle initiated.
    /// Triggers epic champion battle music.
    /// </summary>
    ChampionBattle,

    /// <summary>
    /// Victory against trainer or gym leader.
    /// Triggers victory fanfare.
    /// </summary>
    Victory,

    /// <summary>
    /// Weather changed in the game world.
    /// Triggers weather-appropriate music variation.
    /// </summary>
    WeatherChange,

    /// <summary>
    /// Time of day changed (day/night cycle).
    /// Triggers time-appropriate music variation.
    /// </summary>
    TimeOfDayChange,

    /// <summary>
    /// Entered a building or indoor area.
    /// Triggers indoor music with reverb.
    /// </summary>
    EnteredBuilding,

    /// <summary>
    /// Exited a building to outdoor area.
    /// Triggers outdoor music.
    /// </summary>
    ExitedBuilding,

    /// <summary>
    /// Entered a cave or dungeon.
    /// Triggers atmospheric cave music.
    /// </summary>
    EnteredCave,

    /// <summary>
    /// Surfing on water started.
    /// Triggers surfing music variation.
    /// </summary>
    SurfingStart,

    /// <summary>
    /// Surfing ended (reached shore).
    /// Triggers return to location music.
    /// </summary>
    SurfingEnd,

    /// <summary>
    /// Flying or fast travel initiated.
    /// Triggers flight music.
    /// </summary>
    Flying,

    /// <summary>
    /// Puzzle or minigame started.
    /// Triggers puzzle-specific music.
    /// </summary>
    PuzzleStart,

    /// <summary>
    /// Puzzle completed successfully.
    /// Triggers puzzle completion jingle.
    /// </summary>
    PuzzleComplete,

    /// <summary>
    /// Shop or Pokemon Center entered.
    /// Triggers shop/center music.
    /// </summary>
    ShopEntered,

    /// <summary>
    /// Critical game event or cutscene.
    /// Triggers cinematic music sequence.
    /// </summary>
    CinematicEvent,

    /// <summary>
    /// Danger or threat nearby (proximity-based).
    /// Triggers tension music increase.
    /// </summary>
    DangerNearby,

    /// <summary>
    /// Safe area reached (Pokemon Center, home, etc.).
    /// Triggers calm, safe music.
    /// </summary>
    SafeArea,

    /// <summary>
    /// Custom event defined by game logic.
    /// Allows for extensible event handling.
    /// </summary>
    Custom,
}

/// <summary>
/// Extension methods for AudioReactionType enum.
/// </summary>
public static class AudioReactionTypeExtensions
{
    /// <summary>
    /// Determines if the reaction type requires immediate audio response.
    /// </summary>
    /// <param name="reactionType">The reaction type to check.</param>
    /// <returns>True if reaction requires immediate handling.</returns>
    public static bool IsUrgent(this AudioReactionType reactionType)
    {
        return reactionType switch
        {
            AudioReactionType.BossBattle => true,
            AudioReactionType.HealthCritical => true,
            AudioReactionType.WildEncounter => true,
            AudioReactionType.PartyWipeout => true,
            AudioReactionType.DangerNearby => true,
            _ => false,
        };
    }

    /// <summary>
    /// Determines if the reaction type requires music ducking.
    /// </summary>
    /// <param name="reactionType">The reaction type to check.</param>
    /// <returns>True if music should be ducked for this reaction.</returns>
    public static bool RequiresMusicDucking(this AudioReactionType reactionType)
    {
        return reactionType switch
        {
            AudioReactionType.Dialogue => true,
            AudioReactionType.ItemPickup => true,
            AudioReactionType.RareItemPickup => true,
            AudioReactionType.Achievement => true,
            AudioReactionType.PokemonCaptured => true,
            AudioReactionType.Victory => true,
            AudioReactionType.MenuSound => false,
            _ => false,
        };
    }

    /// <summary>
    /// Gets the suggested priority level for the reaction type.
    /// </summary>
    /// <param name="reactionType">The reaction type to check.</param>
    /// <returns>Priority level (0-3, where 3 is highest).</returns>
    public static int GetPriority(this AudioReactionType reactionType)
    {
        return reactionType switch
        {
            // Critical priority (3)
            AudioReactionType.BossBattle => 3,
            AudioReactionType.HealthCritical => 3,
            AudioReactionType.PartyWipeout => 3,
            AudioReactionType.ChampionBattle => 3,

            // High priority (2)
            AudioReactionType.BattleStart => 2,
            AudioReactionType.WildEncounter => 2,
            AudioReactionType.TrainerBattle => 2,
            AudioReactionType.GymLeaderBattle => 2,
            AudioReactionType.CinematicEvent => 2,

            // Medium priority (1)
            AudioReactionType.LocationChange => 1,
            AudioReactionType.ItemPickup => 1,
            AudioReactionType.Achievement => 1,
            AudioReactionType.Evolution => 1,
            AudioReactionType.PokemonCaptured => 1,

            // Low priority (0)
            _ => 0,
        };
    }

    /// <summary>
    /// Determines if the reaction type represents a battle event.
    /// </summary>
    /// <param name="reactionType">The reaction type to check.</param>
    /// <returns>True if this is a battle-related event.</returns>
    public static bool IsBattleEvent(this AudioReactionType reactionType)
    {
        return reactionType switch
        {
            AudioReactionType.BattleStart => true,
            AudioReactionType.BattleEnd => true,
            AudioReactionType.BossBattle => true,
            AudioReactionType.TrainerBattle => true,
            AudioReactionType.GymLeaderBattle => true,
            AudioReactionType.ChampionBattle => true,
            AudioReactionType.WildEncounter => true,
            AudioReactionType.PokemonFainted => true,
            AudioReactionType.PartyWipeout => true,
            AudioReactionType.Victory => true,
            _ => false,
        };
    }

    /// <summary>
    /// Determines if the reaction type represents a location/environment change.
    /// </summary>
    /// <param name="reactionType">The reaction type to check.</param>
    /// <returns>True if this is a location/environment event.</returns>
    public static bool IsEnvironmentEvent(this AudioReactionType reactionType)
    {
        return reactionType switch
        {
            AudioReactionType.LocationChange => true,
            AudioReactionType.WeatherChange => true,
            AudioReactionType.TimeOfDayChange => true,
            AudioReactionType.EnteredBuilding => true,
            AudioReactionType.ExitedBuilding => true,
            AudioReactionType.EnteredCave => true,
            AudioReactionType.SurfingStart => true,
            AudioReactionType.SurfingEnd => true,
            AudioReactionType.SafeArea => true,
            _ => false,
        };
    }
}
