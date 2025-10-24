using System;
using System.Collections.Generic;

namespace PokeNET.Domain.ECS.Components;

/// <summary>
/// Component tracking player's game progress and exploration.
/// Stores play time, location, story flags, and visited areas.
/// </summary>
public sealed class PlayerProgress
{
    /// <summary>
    /// Total time spent playing.
    /// </summary>
    public TimeSpan PlayTime { get; private set; }

    /// <summary>
    /// Current map/location of the player.
    /// </summary>
    public LocationData CurrentLocation { get; private set; }

    /// <summary>
    /// Story progression flags (e.g., "delivered_parcel", "beat_elite_four").
    /// </summary>
    private readonly Dictionary<string, bool> _storyFlags = new();

    /// <summary>
    /// Set of defeated trainer IDs to prevent re-battling.
    /// </summary>
    private readonly HashSet<Guid> _defeatedTrainers = new();

    /// <summary>
    /// Set of visited map IDs for tracking exploration.
    /// </summary>
    private readonly HashSet<int> _visitedMaps = new();

    /// <summary>
    /// Gets the story flags as a read-only dictionary.
    /// </summary>
    public IReadOnlyDictionary<string, bool> StoryFlags => _storyFlags;

    /// <summary>
    /// Gets the defeated trainers as a read-only set.
    /// </summary>
    public IReadOnlySet<Guid> DefeatedTrainers => _defeatedTrainers;

    /// <summary>
    /// Gets the visited maps as a read-only set.
    /// </summary>
    public IReadOnlySet<int> VisitedMaps => _visitedMaps;

    /// <summary>
    /// Initializes player progress with starting location.
    /// </summary>
    public PlayerProgress(LocationData startingLocation)
    {
        CurrentLocation = startingLocation ?? throw new ArgumentNullException(nameof(startingLocation));
        PlayTime = TimeSpan.Zero;
        _visitedMaps.Add(startingLocation.MapId);
    }

    /// <summary>
    /// Adds time to the total play time.
    /// </summary>
    public void AddPlayTime(TimeSpan duration)
    {
        if (duration < TimeSpan.Zero)
            throw new ArgumentException("Duration must be positive", nameof(duration));
        PlayTime += duration;
    }

    /// <summary>
    /// Updates the player's current location and marks the map as visited.
    /// </summary>
    public void UpdateLocation(LocationData newLocation)
    {
        CurrentLocation = newLocation ?? throw new ArgumentNullException(nameof(newLocation));
        _visitedMaps.Add(newLocation.MapId);
    }

    /// <summary>
    /// Sets a story flag to the specified value.
    /// </summary>
    public void SetStoryFlag(string flagName, bool value = true)
    {
        if (string.IsNullOrWhiteSpace(flagName))
            throw new ArgumentException("Flag name cannot be empty", nameof(flagName));
        _storyFlags[flagName] = value;
    }

    /// <summary>
    /// Checks if a story flag is set to true.
    /// </summary>
    public bool HasStoryFlag(string flagName)
    {
        return _storyFlags.TryGetValue(flagName, out bool value) && value;
    }

    /// <summary>
    /// Records a trainer as defeated.
    /// </summary>
    public void DefeatTrainer(Guid trainerId)
    {
        if (trainerId == Guid.Empty)
            throw new ArgumentException("Trainer ID cannot be empty", nameof(trainerId));
        _defeatedTrainers.Add(trainerId);
    }

    /// <summary>
    /// Checks if a trainer has been defeated.
    /// </summary>
    public bool HasDefeatedTrainer(Guid trainerId)
    {
        return _defeatedTrainers.Contains(trainerId);
    }

    /// <summary>
    /// Checks if a map has been visited.
    /// </summary>
    public bool HasVisitedMap(int mapId)
    {
        return _visitedMaps.Contains(mapId);
    }

    /// <summary>
    /// Gets the number of unique maps visited.
    /// </summary>
    public int TotalMapsVisited => _visitedMaps.Count;

    /// <summary>
    /// Gets the number of trainers defeated.
    /// </summary>
    public int TotalTrainersDefeated => _defeatedTrainers.Count;
}

/// <summary>
/// Represents a location in the game world.
/// </summary>
public sealed class LocationData
{
    /// <summary>
    /// ID of the current map.
    /// </summary>
    public int MapId { get; init; }

    /// <summary>
    /// X coordinate on the map (tile position).
    /// </summary>
    public int TileX { get; set; }

    /// <summary>
    /// Y coordinate on the map (tile position).
    /// </summary>
    public int TileY { get; set; }

    /// <summary>
    /// Creates a new location data instance.
    /// </summary>
    public LocationData(int mapId, int tileX, int tileY)
    {
        MapId = mapId;
        TileX = tileX;
        TileY = tileY;
    }
}
