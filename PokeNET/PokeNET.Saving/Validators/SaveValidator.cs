using Microsoft.Extensions.Logging;
using PokeNET.Domain.Saving;

namespace PokeNET.Saving.Validators;

/// <summary>
/// Validates save files for integrity, version compatibility, and data consistency.
/// </summary>
public class SaveValidator : ISaveValidator
{
    private readonly ILogger<SaveValidator> _logger;
    private readonly ISaveSerializer _serializer;
    private readonly Version _currentVersion = new Version(1, 0, 0);
    private readonly Version _minimumVersion = new Version(1, 0, 0);

    public SaveValidator(ILogger<SaveValidator> logger, ISaveSerializer serializer)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
    }

    /// <inheritdoc/>
    public SaveValidationResult Validate(byte[] data)
    {
        var result = new SaveValidationResult
        {
            Exists = data != null && data.Length > 0
        };

        if (!result.Exists)
        {
            result.IsValid = false;
            result.Errors.Add("Save data is null or empty");
            return result;
        }

        try
        {
            // Try to deserialize
            var snapshot = _serializer.Deserialize(data!);

            // Validate checksum if present
            if (!string.IsNullOrWhiteSpace(snapshot.Checksum))
            {
                // Create a copy without checksum for validation
                var snapshotCopy = System.Text.Json.JsonSerializer.Deserialize<GameStateSnapshot>(
                    System.Text.Json.JsonSerializer.Serialize(snapshot));

                if (snapshotCopy != null)
                {
                    snapshotCopy.Checksum = null;
                    var dataWithoutChecksum = _serializer.Serialize(snapshotCopy);
                    result.ChecksumValid = _serializer.ValidateChecksum(dataWithoutChecksum, snapshot.Checksum);

                    if (!result.ChecksumValid)
                    {
                        result.Errors.Add("Checksum validation failed - save file may be corrupted");
                    }
                }
            }
            else
            {
                result.ChecksumValid = true; // No checksum to validate
                result.Warnings.Add("Save file has no checksum - integrity cannot be verified");
            }

            // Validate version
            result.SaveVersion = snapshot.SaveVersion;
            result.VersionCompatible = IsVersionCompatible(snapshot.SaveVersion);

            if (!result.VersionCompatible)
            {
                result.Errors.Add($"Save version {snapshot.SaveVersion} is not compatible with current version {_currentVersion}");
            }

            // Validate snapshot structure
            var snapshotValidation = ValidateSnapshot(snapshot);
            result.Warnings.AddRange(snapshotValidation.Warnings);
            result.Errors.AddRange(snapshotValidation.Errors);

            result.IsValid = result.Errors.Count == 0 && result.ChecksumValid && result.VersionCompatible;

            _logger.LogInformation(
                "Save validation completed: Valid={IsValid}, Errors={ErrorCount}, Warnings={WarningCount}",
                result.IsValid,
                result.Errors.Count,
                result.Warnings.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Save validation failed with exception");

            result.IsValid = false;
            result.Errors.Add($"Validation failed: {ex.Message}");
            return result;
        }
    }

    /// <inheritdoc/>
    public SaveValidationResult ValidateSnapshot(GameStateSnapshot snapshot)
    {
        var result = new SaveValidationResult
        {
            Exists = snapshot != null
        };

        if (snapshot == null)
        {
            result.IsValid = false;
            result.Errors.Add("Snapshot is null");
            return result;
        }

        // Validate player data
        if (snapshot.Player == null)
        {
            result.Errors.Add("Player data is missing");
        }
        else
        {
            if (string.IsNullOrWhiteSpace(snapshot.Player.Name))
                result.Warnings.Add("Player name is empty");

            if (string.IsNullOrWhiteSpace(snapshot.Player.CurrentMap))
                result.Warnings.Add("Player current map is empty");

            if (snapshot.Player.PlaytimeSeconds < 0)
                result.Errors.Add("Player playtime is negative");
        }

        // Validate party
        if (snapshot.Party == null)
        {
            result.Warnings.Add("Party is null - assuming empty party");
        }
        else
        {
            if (snapshot.Party.Count == 0)
                result.Warnings.Add("Party is empty - player has no Pokemon");

            if (snapshot.Party.Count > 6)
                result.Errors.Add($"Party has {snapshot.Party.Count} Pokemon (maximum is 6)");

            // Validate each Pokemon
            for (int i = 0; i < snapshot.Party.Count; i++)
            {
                var pokemon = snapshot.Party[i];
                if (pokemon.CurrentHp < 0)
                    result.Errors.Add($"Party Pokemon {i}: CurrentHP is negative");

                if (pokemon.MaxHp <= 0)
                    result.Errors.Add($"Party Pokemon {i}: MaxHP must be positive");

                if (pokemon.CurrentHp > pokemon.MaxHp)
                    result.Errors.Add($"Party Pokemon {i}: CurrentHP exceeds MaxHP");

                if (pokemon.Level < 1 || pokemon.Level > 100)
                    result.Errors.Add($"Party Pokemon {i}: Level {pokemon.Level} is out of valid range (1-100)");

                if (pokemon.Moves == null || pokemon.Moves.Count == 0)
                    result.Warnings.Add($"Party Pokemon {i}: Has no moves");

                if (pokemon.Moves?.Count > 4)
                    result.Errors.Add($"Party Pokemon {i}: Has {pokemon.Moves.Count} moves (maximum is 4)");
            }
        }

        // Validate inventory
        if (snapshot.Inventory == null)
        {
            result.Warnings.Add("Inventory is null - assuming empty inventory");
        }

        // Validate world data
        if (snapshot.World == null)
        {
            result.Warnings.Add("World data is null");
        }

        // Validate progress data
        if (snapshot.Progress == null)
        {
            result.Warnings.Add("Progress data is null");
        }

        // Validate Pokedex
        if (snapshot.Pokedex == null)
        {
            result.Warnings.Add("Pokedex data is null");
        }
        else
        {
            if (snapshot.Pokedex.Caught.Count > snapshot.Pokedex.Seen.Count)
                result.Errors.Add("Pokedex: More Pokemon caught than seen");
        }

        result.IsValid = result.Errors.Count == 0;
        return result;
    }

    /// <inheritdoc/>
    public bool IsVersionCompatible(Version saveVersion)
    {
        if (saveVersion == null)
            return false;

        // Save version must be >= minimum supported version
        // Save version must be <= current version (we don't support future versions)
        return saveVersion >= _minimumVersion && saveVersion <= _currentVersion;
    }

    /// <inheritdoc/>
    public Version GetMinimumSupportedVersion()
    {
        return _minimumVersion;
    }

    /// <inheritdoc/>
    public Version GetCurrentVersion()
    {
        return _currentVersion;
    }
}
