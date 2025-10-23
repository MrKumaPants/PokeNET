namespace PokeNET.Domain.Saving;

/// <summary>
/// Base exception for save system errors.
/// </summary>
public class SaveException : Exception
{
    public SaveException() { }
    public SaveException(string message) : base(message) { }
    public SaveException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Exception thrown when a save file is not found.
/// </summary>
public class SaveNotFoundException : SaveException
{
    public string SlotId { get; }

    public SaveNotFoundException(string slotId)
        : base($"Save file not found for slot: {slotId}")
    {
        SlotId = slotId;
    }

    public SaveNotFoundException(string slotId, string message)
        : base(message)
    {
        SlotId = slotId;
    }
}

/// <summary>
/// Exception thrown when a save file is corrupted or invalid.
/// </summary>
public class SaveCorruptedException : SaveException
{
    public string SlotId { get; }
    public string? ChecksumExpected { get; }
    public string? ChecksumActual { get; }

    public SaveCorruptedException(string slotId)
        : base($"Save file is corrupted: {slotId}")
    {
        SlotId = slotId;
    }

    public SaveCorruptedException(string slotId, string message)
        : base(message)
    {
        SlotId = slotId;
    }

    public SaveCorruptedException(string slotId, string checksumExpected, string checksumActual)
        : base($"Save file checksum mismatch for {slotId}. Expected: {checksumExpected}, Actual: {checksumActual}")
    {
        SlotId = slotId;
        ChecksumExpected = checksumExpected;
        ChecksumActual = checksumActual;
    }
}

/// <summary>
/// Exception thrown when save file version is incompatible.
/// </summary>
public class SaveVersionIncompatibleException : SaveException
{
    public Version SaveVersion { get; }
    public Version CurrentVersion { get; }

    public SaveVersionIncompatibleException(Version saveVersion, Version currentVersion)
        : base($"Save version {saveVersion} is incompatible with current version {currentVersion}")
    {
        SaveVersion = saveVersion;
        CurrentVersion = currentVersion;
    }
}

/// <summary>
/// Exception thrown during serialization/deserialization.
/// </summary>
public class SerializationException : SaveException
{
    public SerializationException(string message) : base(message) { }
    public SerializationException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Exception thrown when save data validation fails.
/// </summary>
public class SaveValidationException : SaveException
{
    public SaveValidationResult ValidationResult { get; }

    public SaveValidationException(SaveValidationResult validationResult)
        : base($"Save validation failed with {validationResult.Errors.Count} errors")
    {
        ValidationResult = validationResult;
    }

    public SaveValidationException(string message, SaveValidationResult validationResult)
        : base(message)
    {
        ValidationResult = validationResult;
    }
}
