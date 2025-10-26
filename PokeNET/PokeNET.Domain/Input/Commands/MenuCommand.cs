using System;
using Arch.Core;
using PokeNET.Domain.ECS.Events;

namespace PokeNET.Domain.Input.Commands;

/// <summary>
/// Command for menu-related actions (open inventory, map, etc.).
/// </summary>
public class MenuCommand : CommandBase
{
    private readonly MenuAction _action;
    private readonly IEventBus? _eventBus;

    /// <inheritdoc/>
    public override int Priority => 30; // Menu actions have medium priority

    /// <summary>
    /// Initializes a new menu command.
    /// </summary>
    /// <param name="action">The menu action to perform.</param>
    /// <param name="eventBus">Optional event bus for publishing menu events.</param>
    public MenuCommand(MenuAction action, IEventBus? eventBus = null)
    {
        _action = action;
        _eventBus = eventBus;
    }

    /// <inheritdoc/>
    public override void Execute(World world)
    {
        _eventBus?.Publish(new MenuActionEvent { Action = _action, Timestamp = DateTime.UtcNow });

        // TODO: Implement menu action logic
        // - Open/close specific menus
        // - Navigate menu systems
        // - Handle menu selection
    }
}

/// <summary>
/// Types of menu actions.
/// </summary>
public enum MenuAction
{
    OpenInventory,
    OpenMap,
    OpenSettings,
    CloseMenu,
    NavigateUp,
    NavigateDown,
    NavigateLeft,
    NavigateRight,
    Select,
    Cancel,
}

/// <summary>
/// Event published when a menu action is performed.
/// </summary>
public class MenuActionEvent : IGameEvent
{
    public MenuAction Action { get; init; }
    public DateTime Timestamp { get; init; }
}
