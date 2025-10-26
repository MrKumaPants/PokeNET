using System;
using System.Collections.Generic;
using System.Linq;

namespace PokeNET.Domain.ECS.Components;

/// <summary>
/// Component representing the player's Pokédex progress.
/// Tracks which Pokémon species have been seen and caught.
/// </summary>
public sealed class Pokedex
{
    private readonly HashSet<int> _seenPokemon = new();
    private readonly HashSet<int> _caughtPokemon = new();

    /// <summary>
    /// Total number of Pokémon species in the complete Pokédex.
    /// Can be configured based on generation (151 for Gen 1, 251 for Gen 2, etc.).
    /// </summary>
    public int TotalSpecies { get; init; }

    /// <summary>
    /// Gets the species IDs of Pokémon that have been seen.
    /// </summary>
    public IReadOnlySet<int> SeenPokemon => _seenPokemon;

    /// <summary>
    /// Gets the species IDs of Pokémon that have been caught.
    /// </summary>
    public IReadOnlySet<int> CaughtPokemon => _caughtPokemon;

    /// <summary>
    /// Gets the number of Pokémon species seen.
    /// </summary>
    public int SeenCount => _seenPokemon.Count;

    /// <summary>
    /// Gets the number of Pokémon species caught.
    /// </summary>
    public int CaughtCount => _caughtPokemon.Count;

    /// <summary>
    /// Gets the Pokédex completion percentage based on caught Pokémon.
    /// </summary>
    public double CompletionPercentage =>
        TotalSpecies > 0 ? (double)CaughtCount / TotalSpecies * 100.0 : 0.0;

    /// <summary>
    /// Gets the Pokédex seen percentage.
    /// </summary>
    public double SeenPercentage =>
        TotalSpecies > 0 ? (double)SeenCount / TotalSpecies * 100.0 : 0.0;

    /// <summary>
    /// Initializes a new Pokédex.
    /// </summary>
    /// <param name="totalSpecies">Total number of species (e.g., 151 for Kanto).</param>
    public Pokedex(int totalSpecies = 151)
    {
        if (totalSpecies <= 0)
            throw new ArgumentException("Total species must be positive", nameof(totalSpecies));
        TotalSpecies = totalSpecies;
    }

    /// <summary>
    /// Registers a Pokémon species as seen.
    /// </summary>
    /// <param name="speciesId">National Dex number of the species.</param>
    public void RegisterSeen(int speciesId)
    {
        ValidateSpeciesId(speciesId);
        _seenPokemon.Add(speciesId);
    }

    /// <summary>
    /// Registers a Pokémon species as caught (automatically marks as seen too).
    /// </summary>
    /// <param name="speciesId">National Dex number of the species.</param>
    public void RegisterCaught(int speciesId)
    {
        ValidateSpeciesId(speciesId);
        _seenPokemon.Add(speciesId);
        _caughtPokemon.Add(speciesId);
    }

    /// <summary>
    /// Checks if a Pokémon species has been seen.
    /// </summary>
    public bool HasSeen(int speciesId)
    {
        return _seenPokemon.Contains(speciesId);
    }

    /// <summary>
    /// Checks if a Pokémon species has been caught.
    /// </summary>
    public bool HasCaught(int speciesId)
    {
        return _caughtPokemon.Contains(speciesId);
    }

    /// <summary>
    /// Gets all unseen species IDs (for tracking which Pokémon are missing).
    /// </summary>
    public IEnumerable<int> GetUnseenSpecies()
    {
        return Enumerable.Range(1, TotalSpecies).Except(_seenPokemon).OrderBy(id => id);
    }

    /// <summary>
    /// Gets all uncaught species IDs that have been seen.
    /// </summary>
    public IEnumerable<int> GetUncaughtSeenSpecies()
    {
        return _seenPokemon.Except(_caughtPokemon).OrderBy(id => id);
    }

    /// <summary>
    /// Indicates whether the Pokédex is complete (all species caught).
    /// </summary>
    public bool IsComplete => CaughtCount >= TotalSpecies;

    private void ValidateSpeciesId(int speciesId)
    {
        if (speciesId <= 0 || speciesId > TotalSpecies)
            throw new ArgumentOutOfRangeException(
                nameof(speciesId),
                $"Species ID must be between 1 and {TotalSpecies}"
            );
    }
}
