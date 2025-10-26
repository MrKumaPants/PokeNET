using System;
using System.Collections.Generic;
using Arch.Core;
using Microsoft.Extensions.Logging;

namespace PokeNET.Domain.Input;

/// <summary>
/// Manages command history for undo/redo functionality.
/// Implements the Memento pattern for state management.
/// </summary>
public class CommandHistory
{
    private readonly ILogger<CommandHistory> _logger;
    private readonly Stack<ICommand> _undoStack = new();
    private readonly Stack<ICommand> _redoStack = new();
    private readonly int _maxHistorySize;

    /// <summary>
    /// Gets the number of commands that can be undone.
    /// </summary>
    public int UndoCount => _undoStack.Count;

    /// <summary>
    /// Gets the number of commands that can be redone.
    /// </summary>
    public int RedoCount => _redoStack.Count;

    /// <summary>
    /// Gets whether undo is available.
    /// </summary>
    public bool CanUndo => _undoStack.Count > 0;

    /// <summary>
    /// Gets whether redo is available.
    /// </summary>
    public bool CanRedo => _redoStack.Count > 0;

    /// <summary>
    /// Initializes a new command history.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="maxHistorySize">Maximum number of commands to store in history.</param>
    public CommandHistory(ILogger<CommandHistory> logger, int maxHistorySize = 50)
    {
        _logger = logger;
        _maxHistorySize = maxHistorySize;
    }

    /// <summary>
    /// Records a command in the history.
    /// </summary>
    /// <param name="command">The command to record.</param>
    public void Record(ICommand command)
    {
        if (!command.SupportsUndo)
            return;

        if (_undoStack.Count >= _maxHistorySize)
        {
            // Remove oldest command to maintain size limit
            var items = _undoStack.ToArray();
            _undoStack.Clear();
            for (int i = 0; i < items.Length - 1; i++)
            {
                _undoStack.Push(items[i]);
            }
        }

        _undoStack.Push(command);
        _redoStack.Clear(); // Clear redo stack when new command is executed

        _logger.LogTrace(
            "Recorded command in history: {CommandType} (History size: {HistorySize})",
            command.GetType().Name,
            _undoStack.Count
        );
    }

    /// <summary>
    /// Undoes the last command.
    /// </summary>
    /// <param name="world">The ECS world instance.</param>
    /// <returns>True if undo was successful.</returns>
    public bool Undo(World world)
    {
        if (!CanUndo)
        {
            _logger.LogTrace("Cannot undo: history is empty");
            return false;
        }

        var command = _undoStack.Pop();

        try
        {
            if (command.Undo(world))
            {
                _redoStack.Push(command);
                _logger.LogDebug("Undid command: {CommandType}", command.GetType().Name);
                return true;
            }
            else
            {
                _logger.LogWarning("Failed to undo command: {CommandType}", command.GetType().Name);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error undoing command: {CommandType}", command.GetType().Name);
            return false;
        }
    }

    /// <summary>
    /// Redoes the last undone command.
    /// </summary>
    /// <param name="world">The ECS world instance.</param>
    /// <returns>True if redo was successful.</returns>
    public bool Redo(World world)
    {
        if (!CanRedo)
        {
            _logger.LogTrace("Cannot redo: redo stack is empty");
            return false;
        }

        var command = _redoStack.Pop();

        try
        {
            if (command.CanExecute(world))
            {
                command.Execute(world);
                _undoStack.Push(command);
                _logger.LogDebug("Redid command: {CommandType}", command.GetType().Name);
                return true;
            }
            else
            {
                _logger.LogWarning("Cannot redo command: {CommandType}", command.GetType().Name);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error redoing command: {CommandType}", command.GetType().Name);
            return false;
        }
    }

    /// <summary>
    /// Clears all history.
    /// </summary>
    public void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();
        _logger.LogInformation("Cleared command history");
    }
}
