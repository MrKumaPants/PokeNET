using System;
using PokeNET.Core.Input;

namespace PokeNET.Core.ECS.Events;

/// <summary>
/// Event published when an input command is created.
/// </summary>
public class InputCommandEvent : IGameEvent
{
    /// <summary>
    /// The command that was created.
    /// </summary>
    public required ICommand Command { get; init; }

    /// <summary>
    /// When the command was created.
    /// </summary>
    public required DateTime Timestamp { get; init; }
}

/// <summary>
/// Event published when input is enabled or disabled.
/// </summary>
public class InputStateChangedEvent : IGameEvent
{
    /// <summary>
    /// Whether input is now enabled.
    /// </summary>
    public required bool IsEnabled { get; init; }

    /// <summary>
    /// When the state changed.
    /// </summary>
    public required DateTime Timestamp { get; init; }
}

/// <summary>
/// Event published when key bindings are changed.
/// </summary>
public class KeyBindingChangedEvent : IGameEvent
{
    /// <summary>
    /// The action that was rebound.
    /// </summary>
    public required string Action { get; init; }

    /// <summary>
    /// The old key binding (if any).
    /// </summary>
    public string? OldBinding { get; init; }

    /// <summary>
    /// The new key binding.
    /// </summary>
    public required string NewBinding { get; init; }

    /// <summary>
    /// When the binding changed.
    /// </summary>
    public required DateTime Timestamp { get; init; }
}
