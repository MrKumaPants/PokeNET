using System;
using Arch.Core;

namespace PokeNET.Core.Input;

/// <summary>
/// Base interface for the Command pattern.
/// Commands encapsulate input actions and can be queued, executed, and undone.
/// Follows the Command pattern from Gang of Four design patterns.
/// </summary>
public interface ICommand
{
    /// <summary>
    /// Gets the priority of this command (lower values execute first).
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Gets the timestamp when this command was created.
    /// </summary>
    DateTime Timestamp { get; }

    /// <summary>
    /// Determines if this command can be executed in the current game state.
    /// </summary>
    /// <param name="world">The ECS world instance.</param>
    /// <returns>True if the command can be executed.</returns>
    bool CanExecute(World world);

    /// <summary>
    /// Executes the command's action.
    /// </summary>
    /// <param name="world">The ECS world instance.</param>
    void Execute(World world);

    /// <summary>
    /// Undoes the command's action (if supported).
    /// </summary>
    /// <param name="world">The ECS world instance.</param>
    /// <returns>True if the undo was successful.</returns>
    bool Undo(World world);

    /// <summary>
    /// Gets whether this command supports undo functionality.
    /// </summary>
    bool SupportsUndo { get; }
}
