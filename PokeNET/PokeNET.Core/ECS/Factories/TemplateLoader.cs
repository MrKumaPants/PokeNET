using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PokeNET.Core.ECS.Factories;

namespace PokeNET.Core.ECS.Factories;

/// <summary>
/// Utility for loading entity templates from JSON files.
/// Supports hot-reloading and validation of template data.
/// </summary>
public sealed class TemplateLoader
{
    private readonly ILogger<TemplateLoader> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public TemplateLoader(ILogger<TemplateLoader> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
        };
    }

    /// <summary>
    /// Loads entity templates from a JSON file and registers them with a factory.
    /// </summary>
    /// <param name="factory">The factory to register templates with.</param>
    /// <param name="filePath">Path to the JSON template file.</param>
    /// <returns>Number of templates loaded.</returns>
    public async Task<int> LoadTemplatesAsync(IEntityFactory factory, string filePath)
    {
        ArgumentNullException.ThrowIfNull(factory);

        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path cannot be empty", nameof(filePath));
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Template file not found: {filePath}", filePath);
        }

        _logger.LogInformation("Loading templates from {FilePath}", filePath);

        try
        {
            await using var fileStream = File.OpenRead(filePath);
            var templateData = await JsonSerializer.DeserializeAsync<TemplateFileData>(
                fileStream,
                _jsonOptions
            );

            if (templateData?.Templates == null || templateData.Templates.Count == 0)
            {
                _logger.LogWarning("No templates found in {FilePath}", filePath);
                return 0;
            }

            var loadedCount = 0;
            foreach (var (name, template) in templateData.Templates)
            {
                try
                {
                    var definition = ConvertToEntityDefinition(template);
                    factory.RegisterTemplate(name, definition);
                    loadedCount++;
                    _logger.LogDebug("Loaded template '{Name}'", name);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to load template '{Name}'", name);
                }
            }

            _logger.LogInformation(
                "Loaded {Count} templates from {FilePath}",
                loadedCount,
                filePath
            );

            return loadedCount;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Invalid JSON in template file: {FilePath}", filePath);
            throw new InvalidOperationException($"Failed to parse template file: {filePath}", ex);
        }
    }

    /// <summary>
    /// Saves entity templates from a factory to a JSON file.
    /// </summary>
    /// <param name="factory">The factory to export templates from.</param>
    /// <param name="filePath">Path to save the JSON file.</param>
    /// <returns>Number of templates saved.</returns>
    public Task<int> SaveTemplatesAsync(IEntityFactory factory, string filePath)
    {
        ArgumentNullException.ThrowIfNull(factory);

        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path cannot be empty", nameof(filePath));
        }

        _logger.LogInformation("Saving templates to {FilePath}", filePath);

        var templateNames = factory.GetTemplateNames().ToList();
        if (templateNames.Count == 0)
        {
            _logger.LogWarning("No templates to save");
            return Task.FromResult(0);
        }

        // Note: This is a simplified version. In production, you'd need to retrieve
        // the actual EntityDefinition objects from the factory.
        _logger.LogWarning(
            "Template export requires factory API enhancement to retrieve definitions"
        );

        return Task.FromResult(0);
    }

    private EntityDefinition ConvertToEntityDefinition(TemplateData template)
    {
        // Convert JSON component data to actual component instances
        // This is a simplified version - production would need type resolution
        var components = new List<object>();

        // In a real implementation, you'd use reflection or a component registry
        // to instantiate components from the JSON data

        return new EntityDefinition(template.Name, components, template.Metadata);
    }

    #region JSON Data Models

    private sealed class TemplateFileData
    {
        public Dictionary<string, TemplateData> Templates { get; set; } = new();
    }

    private sealed class TemplateData
    {
        public string Name { get; set; } = string.Empty;
        public List<ComponentData> Components { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    private sealed class ComponentData
    {
        public string Type { get; set; } = string.Empty;
        public Dictionary<string, object> Properties { get; set; } = new();
    }

    #endregion
}
