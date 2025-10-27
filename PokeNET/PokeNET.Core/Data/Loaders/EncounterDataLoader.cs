using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace PokeNET.Core.Data.Loaders;

/// <summary>
/// Loader for wild encounter data from encounters.json.
/// Handles deserialization and validation of EncounterTable models.
/// </summary>
public class EncounterDataLoader : JsonArrayLoader<EncounterTable>
{
    public EncounterDataLoader(ILogger<BaseDataLoader<List<EncounterTable>>> logger) : base(logger)
    {
    }

    /// <inheritdoc/>
    public override bool Validate(List<EncounterTable> data)
    {
        if (!base.Validate(data))
            return false;

        foreach (var item in data)
        {
            if (!ValidateItem(item))
            {
                Logger.LogWarning("Invalid encounter data found: {LocationId}", item.LocationId ?? "Unknown");
                return false;
            }
        }

        return true;
    }

    private bool ValidateItem(EncounterTable item)
    {
        // Validate required fields
        if (!ValidateString(item.LocationId, nameof(item.LocationId)))
            return false;

        if (!ValidateString(item.LocationName, nameof(item.LocationName)))
            return false;

        // At least one encounter type should have data
        var hasEncounters =
            (item.GrassEncounters?.Count ?? 0) > 0
            || (item.WaterEncounters?.Count ?? 0) > 0
            || (item.OldRodEncounters?.Count ?? 0) > 0
            || (item.GoodRodEncounters?.Count ?? 0) > 0
            || (item.SuperRodEncounters?.Count ?? 0) > 0
            || (item.CaveEncounters?.Count ?? 0) > 0
            || (item.SpecialEncounters?.Count ?? 0) > 0;

        if (!hasEncounters)
        {
            Logger.LogWarning(
                "Location {LocationId} has no encounters defined",
                item.LocationId
            );
            return false;
        }

        // Validate each encounter type
        if (item.GrassEncounters != null)
        {
            foreach (var encounter in item.GrassEncounters)
            {
                if (!ValidateEncounter(encounter, item.LocationId, "Grass"))
                    return false;
            }

            if (!ValidateEncounterRates(item.GrassEncounters, item.LocationId, "Grass"))
                return false;
        }

        if (item.WaterEncounters != null)
        {
            foreach (var encounter in item.WaterEncounters)
            {
                if (!ValidateEncounter(encounter, item.LocationId, "Water"))
                    return false;
            }

            if (!ValidateEncounterRates(item.WaterEncounters, item.LocationId, "Water"))
                return false;
        }

        if (item.CaveEncounters != null)
        {
            foreach (var encounter in item.CaveEncounters)
            {
                if (!ValidateEncounter(encounter, item.LocationId, "Cave"))
                    return false;
            }

            if (!ValidateEncounterRates(item.CaveEncounters, item.LocationId, "Cave"))
                return false;
        }

        // Validate fishing encounters
        if (item.OldRodEncounters != null)
        {
            foreach (var encounter in item.OldRodEncounters)
            {
                if (!ValidateEncounter(encounter, item.LocationId, "OldRod"))
                    return false;
            }
        }

        if (item.GoodRodEncounters != null)
        {
            foreach (var encounter in item.GoodRodEncounters)
            {
                if (!ValidateEncounter(encounter, item.LocationId, "GoodRod"))
                    return false;
            }
        }

        if (item.SuperRodEncounters != null)
        {
            foreach (var encounter in item.SuperRodEncounters)
            {
                if (!ValidateEncounter(encounter, item.LocationId, "SuperRod"))
                    return false;
            }
        }

        // Validate special encounters
        if (item.SpecialEncounters != null)
        {
            foreach (var encounter in item.SpecialEncounters)
            {
                if (!ValidateSpecialEncounter(encounter, item.LocationId))
                    return false;
            }
        }

        Logger.LogTrace("Validated encounter table: {LocationId}", item.LocationId);
        return true;
    }

    private bool ValidateEncounter(Encounter encounter, string locationId, string encounterType)
    {
        if (!ValidateRange(encounter.SpeciesId, nameof(encounter.SpeciesId), 1, 1025))
            return false;

        if (!ValidateRange(encounter.MinLevel, nameof(encounter.MinLevel), 1, 100))
            return false;

        if (!ValidateRange(encounter.MaxLevel, nameof(encounter.MaxLevel), 1, 100))
            return false;

        if (encounter.MinLevel > encounter.MaxLevel)
        {
            Logger.LogWarning(
                "Location {LocationId} {Type}: MinLevel ({Min}) > MaxLevel ({Max})",
                locationId,
                encounterType,
                encounter.MinLevel,
                encounter.MaxLevel
            );
            return false;
        }

        if (!ValidateRange(encounter.Rate, nameof(encounter.Rate), 0, 100))
            return false;

        // Validate time of day
        var validTimeOfDay = new[] { "Morning", "Day", "Night", "Any" };
        if (!validTimeOfDay.Contains(encounter.TimeOfDay))
        {
            Logger.LogWarning(
                "Location {LocationId} has invalid TimeOfDay: {TimeOfDay}",
                locationId,
                encounter.TimeOfDay
            );
            return false;
        }

        // Validate weather
        var validWeather = new[] { "Sunny", "Rainy", "Snowy", "Foggy", "Sandstorm", "Any" };
        if (!validWeather.Contains(encounter.Weather))
        {
            Logger.LogWarning(
                "Location {LocationId} has invalid Weather: {Weather}",
                locationId,
                encounter.Weather
            );
            return false;
        }

        return true;
    }

    private bool ValidateEncounterRates(System.Collections.Generic.List<Encounter> encounters, string locationId, string encounterType)
    {
        // Check that encounter rates sum to approximately 100 (allowing some tolerance)
        var totalRate = encounters.Sum(e => e.Rate);

        if (totalRate < 95 || totalRate > 105)
        {
            Logger.LogWarning(
                "Location {LocationId} {Type}: Encounter rates sum to {Total}% (expected ~100%)",
                locationId,
                encounterType,
                totalRate
            );
        }

        return true;
    }

    private bool ValidateSpecialEncounter(SpecialEncounter encounter, string locationId)
    {
        if (!ValidateString(encounter.EncounterId, nameof(encounter.EncounterId)))
            return false;

        if (!ValidateRange(encounter.SpeciesId, nameof(encounter.SpeciesId), 1, 1025))
            return false;

        if (!ValidateRange(encounter.Level, nameof(encounter.Level), 1, 100))
            return false;

        // Conditions can be empty but not null
        if (encounter.Conditions == null)
        {
            Logger.LogWarning(
                "Special encounter {EncounterId} has null Conditions dictionary",
                encounter.EncounterId
            );
            return false;
        }

        return true;
    }

    // Helper validation methods
    private bool ValidateString(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            Logger.LogWarning("Invalid {FieldName}: cannot be null or empty", fieldName);
            return false;
        }
        return true;
    }

    private bool ValidateRange(int value, string fieldName, int min, int max)
    {
        if (value < min || value > max)
        {
            Logger.LogWarning(
                "Invalid {FieldName}: {Value} is outside range [{Min}, {Max}]",
                fieldName,
                value,
                min,
                max
            );
            return false;
        }
        return true;
    }
}
