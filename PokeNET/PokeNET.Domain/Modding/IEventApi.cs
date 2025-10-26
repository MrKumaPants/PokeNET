using System;
using System.Collections.Generic;

namespace PokeNET.Domain.Modding;

/// <summary>
/// API for subscribing to game events.
/// </summary>
/// <remarks>
/// <para>
/// The event API provides a decoupled way for mods to react to game events
/// without directly patching game code (though Harmony patches can also be used).
/// </para>
/// <para>
/// Events are categorized by system (gameplay, UI, save, etc.) and follow
/// a consistent naming pattern: On[System][Action].
/// </para>
/// </remarks>
public interface IEventApi
{
    /// <summary>
    /// Gameplay-related events.
    /// </summary>
    IGameplayEvents Gameplay { get; }

    /// <summary>
    /// Battle system events.
    /// </summary>
    IBattleEvents Battle { get; }

    /// <summary>
    /// UI and input events.
    /// </summary>
    IUIEvents UI { get; }

    /// <summary>
    /// Save/load events.
    /// </summary>
    ISaveEvents Save { get; }

    /// <summary>
    /// Mod lifecycle events.
    /// </summary>
    IModEvents Mod { get; }
}

/// <summary>
/// Gameplay-related events.
/// </summary>
public interface IGameplayEvents
{
    /// <summary>
    /// Fired when the game is updated (every frame).
    /// </summary>
    /// <remarks>
    /// Use sparingly! Prefer component systems for per-frame logic.
    /// </remarks>
    event EventHandler<GameUpdateEventArgs>? OnUpdate;

    /// <summary>
    /// Fired when a new game is started.
    /// </summary>
    event EventHandler<NewGameEventArgs>? OnNewGameStarted;

    /// <summary>
    /// Fired when the player moves to a new location.
    /// </summary>
    event EventHandler<LocationChangedEventArgs>? OnLocationChanged;

    /// <summary>
    /// Fired when an item is picked up.
    /// </summary>
    event EventHandler<ItemPickedUpEventArgs>? OnItemPickedUp;

    /// <summary>
    /// Fired when an item is used.
    /// </summary>
    event EventHandler<ItemUsedEventArgs>? OnItemUsed;
}

/// <summary>
/// Battle system events.
/// </summary>
public interface IBattleEvents
{
    /// <summary>
    /// Fired when a battle starts.
    /// </summary>
    event EventHandler<BattleStartEventArgs>? OnBattleStart;

    /// <summary>
    /// Fired when a battle ends.
    /// </summary>
    event EventHandler<BattleEndEventArgs>? OnBattleEnd;

    /// <summary>
    /// Fired when a turn begins.
    /// </summary>
    event EventHandler<TurnStartEventArgs>? OnTurnStart;

    /// <summary>
    /// Fired when a move is used.
    /// </summary>
    event EventHandler<MoveUsedEventArgs>? OnMoveUsed;

    /// <summary>
    /// Fired when damage is calculated (before applying).
    /// </summary>
    /// <remarks>
    /// Event args are mutable, allowing mods to modify damage.
    /// </remarks>
    event EventHandler<DamageCalculatedEventArgs>? OnDamageCalculated;

    /// <summary>
    /// Fired when a creature faints.
    /// </summary>
    event EventHandler<CreatureFaintedEventArgs>? OnCreatureFainted;

    /// <summary>
    /// Fired when a creature is caught.
    /// </summary>
    event EventHandler<CreatureCaughtEventArgs>? OnCreatureCaught;
}

/// <summary>
/// UI and input events.
/// </summary>
public interface IUIEvents
{
    /// <summary>
    /// Fired when a menu is opened.
    /// </summary>
    event EventHandler<MenuOpenedEventArgs>? OnMenuOpened;

    /// <summary>
    /// Fired when a menu is closed.
    /// </summary>
    event EventHandler<MenuClosedEventArgs>? OnMenuClosed;

    /// <summary>
    /// Fired when a dialog is shown.
    /// </summary>
    event EventHandler<DialogShownEventArgs>? OnDialogShown;
}

/// <summary>
/// Save/load events.
/// </summary>
public interface ISaveEvents
{
    /// <summary>
    /// Fired before the game is saved.
    /// </summary>
    /// <remarks>
    /// Mods can use this to serialize their own data.
    /// </remarks>
    event EventHandler<SavingEventArgs>? OnSaving;

    /// <summary>
    /// Fired after the game is saved.
    /// </summary>
    event EventHandler<SavedEventArgs>? OnSaved;

    /// <summary>
    /// Fired before a save is loaded.
    /// </summary>
    event EventHandler<LoadingEventArgs>? OnLoading;

    /// <summary>
    /// Fired after a save is loaded.
    /// </summary>
    /// <remarks>
    /// Mods can use this to deserialize their own data.
    /// </remarks>
    event EventHandler<LoadedEventArgs>? OnLoaded;
}

/// <summary>
/// Mod lifecycle events.
/// </summary>
public interface IModEvents
{
    /// <summary>
    /// Fired when all mods have finished loading.
    /// </summary>
    event EventHandler<AllModsLoadedEventArgs>? OnAllModsLoaded;

    /// <summary>
    /// Fired when a mod is unloaded (hot reload).
    /// </summary>
    event EventHandler<ModUnloadedEventArgs>? OnModUnloaded;
}

// Event argument classes (examples - full implementation would have more)

public class GameUpdateEventArgs : EventArgs
{
    public required double DeltaTime { get; init; }
    public required double TotalTime { get; init; }
}

public class NewGameEventArgs : EventArgs
{
    public required string SaveName { get; init; }
}

public class LocationChangedEventArgs : EventArgs
{
    public required string OldLocation { get; init; }
    public required string NewLocation { get; init; }
    public required Entity Player { get; init; }
}

public class ItemPickedUpEventArgs : EventArgs
{
    public required Entity Item { get; init; }
    public required Entity Player { get; init; }
}

public class ItemUsedEventArgs : EventArgs
{
    public required Entity Item { get; init; }
    public required Entity User { get; init; }
    public required Entity? Target { get; init; }
}

public class BattleStartEventArgs : EventArgs
{
    public required Entity PlayerTeam { get; init; }
    public required Entity EnemyTeam { get; init; }
    public required bool IsWildBattle { get; init; }
}

public class BattleEndEventArgs : EventArgs
{
    public required Entity Winner { get; init; }
    public required bool PlayerWon { get; init; }
}

public class TurnStartEventArgs : EventArgs
{
    public required int TurnNumber { get; init; }
}

public class MoveUsedEventArgs : EventArgs
{
    public required Entity Attacker { get; init; }
    public required Entity Defender { get; init; }
    public required string MoveName { get; init; }
}

public class DamageCalculatedEventArgs : EventArgs
{
    public required Entity Attacker { get; init; }
    public required Entity Defender { get; init; }
    public required string MoveName { get; init; }

    /// <summary>
    /// Calculated damage (mutable - mods can modify this).
    /// </summary>
    public int Damage { get; set; }

    /// <summary>
    /// Whether the move is a critical hit.
    /// </summary>
    public bool IsCritical { get; init; }
}

public class CreatureFaintedEventArgs : EventArgs
{
    public required Entity Creature { get; init; }
    public required Entity? Attacker { get; init; }
}

public class CreatureCaughtEventArgs : EventArgs
{
    public required Entity Creature { get; init; }
    public required Entity Player { get; init; }
    public required string BallType { get; init; }
}

public class MenuOpenedEventArgs : EventArgs
{
    public required string MenuName { get; init; }
}

public class MenuClosedEventArgs : EventArgs
{
    public required string MenuName { get; init; }
}

public class DialogShownEventArgs : EventArgs
{
    public required string DialogText { get; init; }
    public required Entity? Speaker { get; init; }
}

public class SavingEventArgs : EventArgs
{
    public required string SavePath { get; init; }

    /// <summary>
    /// Dictionary where mods can store their custom data.
    /// </summary>
    public required IDictionary<string, object> ModData { get; init; }
}

public class SavedEventArgs : EventArgs
{
    public required string SavePath { get; init; }
    public required bool Success { get; init; }
}

public class LoadingEventArgs : EventArgs
{
    public required string SavePath { get; init; }
}

public class LoadedEventArgs : EventArgs
{
    public required string SavePath { get; init; }

    /// <summary>
    /// Dictionary containing mod-specific saved data.
    /// </summary>
    public required IReadOnlyDictionary<string, object> ModData { get; init; }
}

public class AllModsLoadedEventArgs : EventArgs
{
    public required int ModCount { get; init; }
}

public class ModUnloadedEventArgs : EventArgs
{
    public required string ModId { get; init; }
}
