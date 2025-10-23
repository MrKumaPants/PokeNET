using Microsoft.Extensions.Logging.Abstractions;
using PokeNET.Domain.Saving;
using PokeNET.Saving.Providers;
using PokeNET.Saving.Serializers;
using PokeNET.Saving.Services;
using PokeNET.Saving.Validators;
using Xunit;

namespace PokeNET.Tests.Saving;

/// <summary>
/// Comprehensive tests for the SaveSystem implementation.
/// Tests save/load operations, validation, auto-save, import/export, and error handling.
/// </summary>
public class SaveSystemTests : IDisposable
{
    private readonly SaveSystem _saveSystem;
    private readonly FileSystemSaveFileProvider _fileProvider;
    private readonly string _testSaveDirectory;

    public SaveSystemTests()
    {
        // Use a temporary directory for tests
        _testSaveDirectory = Path.Combine(Path.GetTempPath(), "PokeNET_Tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testSaveDirectory);

        var logger = NullLogger<SaveSystem>.Instance;
        var gameStateManager = new GameStateManager(NullLogger<GameStateManager>.Instance);
        var serializer = new JsonSaveSerializer(NullLogger<JsonSaveSerializer>.Instance, prettyPrint: true);
        _fileProvider = new FileSystemSaveFileProvider(NullLogger<FileSystemSaveFileProvider>.Instance, _testSaveDirectory);
        var validator = new SaveValidator(NullLogger<SaveValidator>.Instance, serializer);

        _saveSystem = new SaveSystem(logger, gameStateManager, serializer, _fileProvider, validator);
    }

    [Fact]
    public async Task SaveAsync_CreatesValidSaveFile()
    {
        // Act
        var result = await _saveSystem.SaveAsync("slot1", "Test save");

        // Assert
        Assert.True(result.Success);
        Assert.Equal("slot1", result.SlotId);
        Assert.True(result.FileSizeBytes > 0);
        Assert.True(result.Duration.TotalMilliseconds > 0);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public async Task LoadAsync_RestoresGameState()
    {
        // Arrange
        var saveResult = await _saveSystem.SaveAsync("slot2", "Test save for load");
        Assert.True(saveResult.Success);

        // Act
        var loadResult = await _saveSystem.LoadAsync("slot2");

        // Assert
        Assert.True(loadResult.Success);
        Assert.Equal("slot2", loadResult.SlotId);
        Assert.NotNull(loadResult.GameState);
        Assert.Equal("Test save for load", loadResult.GameState.Description);
        Assert.Null(loadResult.ErrorMessage);
    }

    [Fact]
    public async Task LoadAsync_NonExistentSlot_ReturnsSaveNotFoundException()
    {
        // Act
        var result = await _saveSystem.LoadAsync("nonexistent");

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.Exception);
        Assert.IsType<SaveNotFoundException>(result.Exception);
    }

    [Fact]
    public async Task DeleteAsync_RemovesSaveFile()
    {
        // Arrange
        await _saveSystem.SaveAsync("slot3", "Save to delete");

        // Act
        var deleted = await _saveSystem.DeleteAsync("slot3");

        // Assert
        Assert.True(deleted);

        // Verify file is gone
        var metadata = await _saveSystem.GetSaveMetadataAsync("slot3");
        Assert.Null(metadata);
    }

    [Fact]
    public async Task GetSaveSlotsAsync_ReturnsAllSaves()
    {
        // Arrange
        await _saveSystem.SaveAsync("slot4", "Save 1");
        await _saveSystem.SaveAsync("slot5", "Save 2");
        await _saveSystem.SaveAsync("slot6", "Save 3");

        // Act
        var slots = await _saveSystem.GetSaveSlotsAsync();

        // Assert
        Assert.NotNull(slots);
        Assert.True(slots.Count >= 3);
        Assert.Contains(slots, s => s.SlotId == "slot4");
        Assert.Contains(slots, s => s.SlotId == "slot5");
        Assert.Contains(slots, s => s.SlotId == "slot6");
    }

    [Fact]
    public async Task GetSaveMetadataAsync_ReturnsCorrectMetadata()
    {
        // Arrange
        await _saveSystem.SaveAsync("slot7", "Test metadata");

        // Act
        var metadata = await _saveSystem.GetSaveMetadataAsync("slot7");

        // Assert
        Assert.NotNull(metadata);
        Assert.Equal("slot7", metadata.SlotId);
        Assert.Equal("Test metadata", metadata.Description);
        Assert.Equal("Player", metadata.PlayerName);
        Assert.True(metadata.FileSizeBytes > 0);
    }

    [Fact]
    public async Task ValidateAsync_ValidSave_ReturnsValid()
    {
        // Arrange
        await _saveSystem.SaveAsync("slot8", "Valid save");

        // Act
        var validation = await _saveSystem.ValidateAsync("slot8");

        // Assert
        Assert.True(validation.IsValid);
        Assert.True(validation.Exists);
        Assert.True(validation.ChecksumValid);
        Assert.True(validation.VersionCompatible);
        Assert.Empty(validation.Errors);
    }

    [Fact]
    public async Task SaveAsync_WithNullSlotId_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _saveSystem.SaveAsync(null!));
    }

    [Fact]
    public async Task SaveAsync_WithEmptySlotId_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _saveSystem.SaveAsync(""));
    }

    [Fact]
    public async Task AutoSave_WhenEnabled_SavesPeriodically()
    {
        // Arrange
        _saveSystem.ConfigureAutoSave(enabled: true, intervalSeconds: 1);

        // Wait for auto-save to trigger (allow extra time for execution)
        await Task.Delay(TimeSpan.FromSeconds(2));

        // Assert
        var metadata = await _saveSystem.GetSaveMetadataAsync("autosave");
        Assert.NotNull(metadata); // Auto-save should have created a file

        // Cleanup
        _saveSystem.ConfigureAutoSave(enabled: false);
    }

    [Fact]
    public void ConfigureAutoSave_WithInvalidInterval_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            _saveSystem.ConfigureAutoSave(enabled: true, intervalSeconds: 10));
    }

    [Fact]
    public void GetAutoSaveConfig_ReturnsConfiguration()
    {
        // Arrange
        _saveSystem.ConfigureAutoSave(enabled: true, intervalSeconds: 120);

        // Act
        var config = _saveSystem.GetAutoSaveConfig();

        // Assert
        Assert.NotNull(config);
        Assert.True(config.Enabled);
        Assert.Equal(120, config.IntervalSeconds);
        Assert.Equal("autosave", config.SlotId);
    }

    [Fact]
    public async Task ExportSaveAsync_CreatesExternalCopy()
    {
        // Arrange
        await _saveSystem.SaveAsync("slot9", "Save to export");
        var exportPath = Path.Combine(_testSaveDirectory, "exported_save.sav");

        // Act
        var exported = await _saveSystem.ExportSaveAsync("slot9", exportPath);

        // Assert
        Assert.True(exported);
        Assert.True(File.Exists(exportPath));

        // Cleanup
        File.Delete(exportPath);
    }

    [Fact]
    public async Task ImportSaveAsync_ImportsValidSave()
    {
        // Arrange
        await _saveSystem.SaveAsync("original", "Original save");
        var exportPath = Path.Combine(_testSaveDirectory, "import_test.sav");
        await _saveSystem.ExportSaveAsync("original", exportPath);

        // Act
        var importResult = await _saveSystem.ImportSaveAsync(exportPath, "imported");

        // Assert
        Assert.True(importResult.Success);
        Assert.Equal("imported", importResult.TargetSlotId);
        Assert.True(importResult.ValidationResult.IsValid);

        // Verify imported save loads correctly
        var loadResult = await _saveSystem.LoadAsync("imported");
        Assert.True(loadResult.Success);

        // Cleanup
        File.Delete(exportPath);
    }

    [Fact]
    public async Task ImportSaveAsync_InvalidFile_ReturnsFailure()
    {
        // Arrange
        var invalidPath = Path.Combine(_testSaveDirectory, "invalid.sav");
        await File.WriteAllTextAsync(invalidPath, "Not a valid save file");

        // Act
        var result = await _saveSystem.ImportSaveAsync(invalidPath, "slot10");

        // Assert
        Assert.False(result.Success);
        Assert.False(result.ValidationResult.IsValid);
        Assert.NotEmpty(result.ValidationResult.Errors);

        // Cleanup
        File.Delete(invalidPath);
    }

    [Fact]
    public async Task SaveLoad_PreservesAllData()
    {
        // Arrange
        var description = "Full data preservation test";
        await _saveSystem.SaveAsync("fulltest", description);

        // Act
        var loadResult = await _saveSystem.LoadAsync("fulltest");

        // Assert
        Assert.True(loadResult.Success);
        var state = loadResult.GameState!;

        Assert.Equal(description, state.Description);
        Assert.NotNull(state.Player);
        Assert.NotNull(state.Inventory);
        Assert.NotNull(state.World);
        Assert.NotNull(state.Progress);
        Assert.NotNull(state.Pokedex);
        Assert.NotNull(state.Party);
        Assert.NotNull(state.PokemonBoxes);
    }

    [Fact]
    public async Task MultipleSlots_WorkIndependently()
    {
        // Arrange & Act
        await _saveSystem.SaveAsync("multi1", "Slot 1");
        await _saveSystem.SaveAsync("multi2", "Slot 2");
        await _saveSystem.SaveAsync("multi3", "Slot 3");

        var load1 = await _saveSystem.LoadAsync("multi1");
        var load2 = await _saveSystem.LoadAsync("multi2");
        var load3 = await _saveSystem.LoadAsync("multi3");

        // Assert
        Assert.True(load1.Success && load2.Success && load3.Success);
        Assert.Equal("Slot 1", load1.GameState!.Description);
        Assert.Equal("Slot 2", load2.GameState!.Description);
        Assert.Equal("Slot 3", load3.GameState!.Description);
    }

    public void Dispose()
    {
        // Cleanup: Delete test directory and all files
        if (Directory.Exists(_testSaveDirectory))
        {
            try
            {
                Directory.Delete(_testSaveDirectory, recursive: true);
            }
            catch
            {
                // Ignore cleanup errors in tests
            }
        }
    }
}
