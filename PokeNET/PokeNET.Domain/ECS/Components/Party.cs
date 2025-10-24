using System;
using System.Collections.Generic;
using System.Linq;

namespace PokeNET.Domain.ECS.Components;

/// <summary>
/// Component representing a trainer's Pokémon party (up to 6 Pokémon).
/// Manages party composition, ordering, and provides convenient access methods.
/// </summary>
public sealed class Party
{
    private const int MaxPartySize = 6;
    private readonly Guid?[] _pokemonSlots = new Guid?[MaxPartySize];

    /// <summary>
    /// Gets the entity IDs of Pokémon in the party (null for empty slots).
    /// </summary>
    public IReadOnlyList<Guid?> PokemonSlots => Array.AsReadOnly(_pokemonSlots);

    /// <summary>
    /// Gets the current number of Pokémon in the party.
    /// </summary>
    public int PartySize => _pokemonSlots.Count(slot => slot.HasValue);

    /// <summary>
    /// Indicates whether the party has at least one empty slot.
    /// </summary>
    public bool HasEmptySlot => PartySize < MaxPartySize;

    /// <summary>
    /// Indicates whether the party is full (6 Pokémon).
    /// </summary>
    public bool IsFull => PartySize >= MaxPartySize;

    /// <summary>
    /// Adds a Pokémon to the party.
    /// </summary>
    /// <param name="pokemonEntityId">Entity ID of the Pokémon to add.</param>
    /// <returns>True if successfully added, false if party is full.</returns>
    public bool AddPokemon(Guid pokemonEntityId)
    {
        if (pokemonEntityId == Guid.Empty)
            throw new ArgumentException("Pokemon entity ID cannot be empty", nameof(pokemonEntityId));

        for (int i = 0; i < MaxPartySize; i++)
        {
            if (!_pokemonSlots[i].HasValue)
            {
                _pokemonSlots[i] = pokemonEntityId;
                return true;
            }
        }

        return false; // Party is full
    }

    /// <summary>
    /// Removes a Pokémon from the party by entity ID.
    /// </summary>
    /// <param name="pokemonEntityId">Entity ID of the Pokémon to remove.</param>
    /// <returns>True if successfully removed, false if not found.</returns>
    public bool RemovePokemon(Guid pokemonEntityId)
    {
        for (int i = 0; i < MaxPartySize; i++)
        {
            if (_pokemonSlots[i] == pokemonEntityId)
            {
                // Shift remaining Pokémon forward
                for (int j = i; j < MaxPartySize - 1; j++)
                {
                    _pokemonSlots[j] = _pokemonSlots[j + 1];
                }
                _pokemonSlots[MaxPartySize - 1] = null;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Gets the entity ID of the lead Pokémon (first non-fainted Pokémon in party).
    /// In a real implementation, this would check the fainted status.
    /// </summary>
    /// <returns>Entity ID of lead Pokémon, or null if all fainted/party empty.</returns>
    public Guid? GetLeadPokemon()
    {
        // Returns first Pokémon for now
        // TODO: In full implementation, check Pokemon.CurrentHP > 0
        return _pokemonSlots[0];
    }

    /// <summary>
    /// Swaps the positions of two Pokémon in the party.
    /// </summary>
    /// <param name="index1">First position (0-5).</param>
    /// <param name="index2">Second position (0-5).</param>
    public void SwapPokemon(int index1, int index2)
    {
        if (index1 < 0 || index1 >= MaxPartySize)
            throw new ArgumentOutOfRangeException(nameof(index1), "Index must be 0-5");
        if (index2 < 0 || index2 >= MaxPartySize)
            throw new ArgumentOutOfRangeException(nameof(index2), "Index must be 0-5");

        (_pokemonSlots[index1], _pokemonSlots[index2]) = (_pokemonSlots[index2], _pokemonSlots[index1]);
    }

    /// <summary>
    /// Gets the position index of a Pokémon in the party.
    /// </summary>
    /// <returns>Index (0-5) or -1 if not found.</returns>
    public int GetPokemonIndex(Guid pokemonEntityId)
    {
        for (int i = 0; i < MaxPartySize; i++)
        {
            if (_pokemonSlots[i] == pokemonEntityId)
                return i;
        }
        return -1;
    }

    /// <summary>
    /// Gets all non-null Pokémon entity IDs in the party.
    /// </summary>
    public IEnumerable<Guid> GetAllPokemon()
    {
        return _pokemonSlots.Where(slot => slot.HasValue).Select(slot => slot!.Value);
    }

    /// <summary>
    /// Clears all Pokémon from the party.
    /// </summary>
    public void Clear()
    {
        Array.Clear(_pokemonSlots, 0, MaxPartySize);
    }
}
