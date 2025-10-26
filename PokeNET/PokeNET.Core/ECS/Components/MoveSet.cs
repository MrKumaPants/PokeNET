namespace PokeNET.Core.ECS.Components;

/// <summary>
/// Pokemon moveset component managing up to 4 moves with PP (Power Points).
/// Moves are learned through leveling up, TMs/HMs, breeding, or tutors.
/// </summary>
public struct MoveSet
{
    /// <summary>
    /// Array of moves known by the Pokemon.
    /// Maximum of 4 moves can be known at once.
    /// </summary>
    private Move[] _moves;

    /// <summary>
    /// Gets the move at the specified index (0-3).
    /// Returns null if no move exists at that index.
    /// </summary>
    /// <param name="index">Move slot index (0-3)</param>
    /// <returns>Move data or null if slot is empty</returns>
    public Move? GetMove(int index)
    {
        if (index < 0 || index >= 4 || _moves == null || _moves.Length <= index)
            return null;

        return _moves[index].MoveId == 0 ? null : _moves[index];
    }

    /// <summary>
    /// Adds a move to the first available slot.
    /// If all slots are full, the move is not added.
    /// </summary>
    /// <param name="moveId">Move identifier</param>
    /// <param name="maxPP">Maximum PP for this move</param>
    /// <returns>True if move was added, false if all slots are full</returns>
    public bool AddMove(int moveId, int maxPP)
    {
        _moves ??= new Move[4];

        for (int i = 0; i < 4; i++)
        {
            if (_moves[i].MoveId == 0)
            {
                _moves[i] = new Move
                {
                    MoveId = moveId,
                    PP = maxPP,
                    MaxPP = maxPP,
                };
                return true;
            }
        }

        return false; // All slots full
    }

    /// <summary>
    /// Removes a move from the specified slot.
    /// </summary>
    /// <param name="index">Move slot index (0-3)</param>
    /// <returns>True if move was removed, false if index is invalid</returns>
    public bool RemoveMove(int index)
    {
        if (index < 0 || index >= 4 || _moves == null)
            return false;

        _moves[index] = new Move(); // Reset to default (MoveId = 0)
        return true;
    }

    /// <summary>
    /// Checks if a specific move is known by this Pokemon.
    /// </summary>
    /// <param name="moveId">Move identifier to check</param>
    /// <returns>True if the move is known, false otherwise</returns>
    public bool IsMoveKnown(int moveId)
    {
        if (_moves == null)
            return false;

        for (int i = 0; i < 4; i++)
        {
            if (_moves[i].MoveId == moveId)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Gets the number of moves currently known.
    /// </summary>
    /// <returns>Count of known moves (0-4)</returns>
    public int GetMoveCount()
    {
        if (_moves == null)
            return 0;

        int count = 0;
        for (int i = 0; i < 4; i++)
        {
            if (_moves[i].MoveId != 0)
                count++;
        }
        return count;
    }
}

/// <summary>
/// Represents a single move with its identifier and PP values.
/// </summary>
public struct Move
{
    /// <summary>
    /// Move identifier from the move database.
    /// 0 indicates an empty/no move.
    /// </summary>
    public int MoveId { get; set; }

    /// <summary>
    /// Current Power Points remaining for this move.
    /// When PP reaches 0, the move cannot be used until restored.
    /// </summary>
    public int PP { get; set; }

    /// <summary>
    /// Maximum Power Points for this move.
    /// Can be increased with PP Up items (max 1.6x base PP, rounded down).
    /// </summary>
    public int MaxPP { get; set; }
}
