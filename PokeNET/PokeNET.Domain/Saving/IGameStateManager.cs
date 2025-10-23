namespace PokeNET.Domain.Saving;

/// <summary>
/// Manages game state snapshots for save/load operations.
/// Responsible for capturing and restoring complete game state.
/// </summary>
/// <remarks>
/// This interface follows the Memento pattern, capturing the entire game state
/// without exposing internal implementation details.
/// </remarks>
public interface IGameStateManager
{
    /// <summary>
    /// Creates a snapshot of the current game state.
    /// </summary>
    /// <param name="description">Optional description for the snapshot.</param>
    /// <returns>Complete game state snapshot.</returns>
    GameStateSnapshot CreateSnapshot(string? description = null);

    /// <summary>
    /// Restores game state from a snapshot.
    /// </summary>
    /// <param name="snapshot">The snapshot to restore from.</param>
    /// <exception cref="ArgumentNullException">Snapshot is null.</exception>
    /// <exception cref="InvalidOperationException">Snapshot cannot be restored.</exception>
    void RestoreSnapshot(GameStateSnapshot snapshot);

    /// <summary>
    /// Validates that a snapshot can be restored without errors.
    /// </summary>
    /// <param name="snapshot">The snapshot to validate.</param>
    /// <returns>True if snapshot is valid and can be restored.</returns>
    bool ValidateSnapshot(GameStateSnapshot snapshot);

    /// <summary>
    /// Gets the current game state enum value (for audio/UI purposes).
    /// </summary>
    /// <returns>Current game state.</returns>
    Models.GameState GetCurrentState();

    /// <summary>
    /// Sets the current game state enum value.
    /// </summary>
    /// <param name="state">New game state.</param>
    void SetCurrentState(Models.GameState state);
}
