using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using PokeNET.Domain.Modding;

namespace PokeNET.Core.Modding;

/// <summary>
/// Implementation of IModManifest for deserializing modinfo.json files.
/// </summary>
/// <remarks>
/// Properties are organized by interface responsibility following ISP:
/// - IModManifestCore: Core identity
/// - IModMetadata: Descriptive metadata
/// - IModDependencies: Load order management
/// - ICodeMod: Code execution
/// - IContentMod: Asset loading
/// - IModSecurity: Trust and verification
/// </remarks>
public sealed class ModManifest : IModManifest
{
    // ============================================================================
    // IModManifestCore: Core Identity Properties
    // ============================================================================

    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("version")]
    [JsonConverter(typeof(ModVersionJsonConverter))]
    public required ModVersion Version { get; init; }

    [JsonPropertyName("apiVersion")]
    public string ApiVersion { get; init; } = "1.0.0";

    // ============================================================================
    // IModMetadata: Descriptive Metadata
    // ============================================================================

    [JsonPropertyName("author")]
    public string? Author { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("homepage")]
    public Uri? Homepage { get; init; }

    [JsonPropertyName("license")]
    public string? License { get; init; }

    [JsonPropertyName("tags")]
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();

    [JsonPropertyName("localization")]
    public LocalizationConfiguration? Localization { get; init; }

    [JsonPropertyName("metadata")]
    public IReadOnlyDictionary<string, object> Metadata { get; init; } =
        new Dictionary<string, object>();

    // ============================================================================
    // IModDependencies: Load Order Management
    // ============================================================================

    [JsonPropertyName("dependencies")]
    public IReadOnlyList<ModDependency> Dependencies { get; init; } = Array.Empty<ModDependency>();

    [JsonPropertyName("loadAfter")]
    public IReadOnlyList<string> LoadAfter { get; init; } = Array.Empty<string>();

    [JsonPropertyName("loadBefore")]
    public IReadOnlyList<string> LoadBefore { get; init; } = Array.Empty<string>();

    [JsonPropertyName("incompatibleWith")]
    public IReadOnlyList<ModIncompatibility> IncompatibleWith { get; init; } =
        Array.Empty<ModIncompatibility>();

    // ============================================================================
    // ICodeMod: Code Execution
    // ============================================================================

    [JsonPropertyName("modType")]
    public ModType ModType { get; init; } = ModType.Code;

    [JsonPropertyName("entryPoint")]
    public string? EntryPoint { get; init; }

    [JsonPropertyName("assemblies")]
    public IReadOnlyList<string> Assemblies { get; init; } = Array.Empty<string>();

    [JsonPropertyName("harmonyId")]
    public string? HarmonyId { get; init; }

    /// <summary>
    /// Internal property for assembly name (not part of public interfaces).
    /// </summary>
    [JsonPropertyName("assemblyName")]
    internal string? AssemblyName { get; init; }

    // ============================================================================
    // IContentMod: Asset Loading
    // ============================================================================

    [JsonPropertyName("assetPaths")]
    public AssetPathConfiguration AssetPaths { get; init; } = new();

    [JsonPropertyName("preload")]
    public IReadOnlyList<string> Preload { get; init; } = Array.Empty<string>();

    // ============================================================================
    // IModSecurity: Trust and Verification
    // ============================================================================

    [JsonPropertyName("trustLevel")]
    public ModTrustLevel TrustLevel { get; init; } = ModTrustLevel.Untrusted;

    [JsonPropertyName("checksum")]
    public string? Checksum { get; init; }

    [JsonPropertyName("contentRating")]
    public ContentRating ContentRating { get; init; } = ContentRating.Everyone;

    // ============================================================================
    // IModManifest: Runtime Properties
    // ============================================================================

    [JsonPropertyName("directory")]
    public string Directory { get; set; } = string.Empty;

    // ============================================================================
    // Helper Methods
    // ============================================================================

    /// <summary>
    /// Gets the assembly file name to load for this mod.
    /// Defaults to "{Id}.dll" if not specified.
    /// </summary>
    public string GetAssemblyFileName() => AssemblyName ?? $"{Id}.dll";
}

/// <summary>
/// JSON converter for ModVersion type.
/// </summary>
internal sealed class ModVersionJsonConverter : JsonConverter<ModVersion>
{
    public override ModVersion Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        var versionString = reader.GetString();
        if (string.IsNullOrEmpty(versionString))
            throw new JsonException("Version string cannot be null or empty");

        return ModVersion.Parse(versionString);
    }

    public override void Write(
        Utf8JsonWriter writer,
        ModVersion value,
        JsonSerializerOptions options
    )
    {
        writer.WriteStringValue(value.ToString());
    }
}
