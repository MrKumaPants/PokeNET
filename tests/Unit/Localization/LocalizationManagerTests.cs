using System.Globalization;
using PokeNET.Core.Localization;

namespace PokeNET.Tests.Unit.Localization;

/// <summary>
/// Unit tests for the LocalizationManager.
/// Tests culture management, language switching, and resource loading.
/// </summary>
public class LocalizationManagerTests
{
    [Fact]
    public void GetSupportedCultures_ShouldReturnNonEmptyList()
    {
        // Arrange & Act
        var cultures = LocalizationManager.GetSupportedCultures();

        // Assert
        cultures.Should().NotBeNull();
        cultures.Should().NotBeEmpty();
        cultures.Should().Contain(c => c.Name == LocalizationManager.DEFAULT_CULTURE_CODE);
    }

    [Fact]
    public void SetCulture_WithValidCulture_ShouldUpdateCurrentCulture()
    {
        // Arrange
        var supportedCultures = LocalizationManager.GetSupportedCultures();
        var testCulture = supportedCultures[0];

        // Act
        LocalizationManager.SetCulture(testCulture.Name);

        // Assert
        CultureInfo.CurrentCulture.Name.Should().Be(testCulture.Name);
        CultureInfo.CurrentUICulture.Name.Should().Be(testCulture.Name);
    }

    [Fact]
    public void SetCulture_WithNullOrEmpty_ShouldNotThrow()
    {
        // Arrange & Act
        var actNull = () => LocalizationManager.SetCulture(null);
        var actEmpty = () => LocalizationManager.SetCulture("");

        // Assert
        actNull.Should().NotThrow();
        actEmpty.Should().NotThrow();
    }

    [Theory]
    [InlineData("en-US")]
    [InlineData("es-ES")]
    [InlineData("fr-FR")]
    public void SetCulture_WithSupportedCulture_ShouldSucceed(string cultureName)
    {
        // Arrange & Act
        var act = () => LocalizationManager.SetCulture(cultureName);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void GetSupportedCultures_ShouldContainDefaultCulture()
    {
        // Arrange & Act
        var cultures = LocalizationManager.GetSupportedCultures();

        // Assert
        cultures.Should().Contain(c => c.Name == LocalizationManager.DEFAULT_CULTURE_CODE);
    }

    [Fact]
    public void GetSupportedCultures_ShouldReturnConsistentResults()
    {
        // Arrange & Act
        var firstCall = LocalizationManager.GetSupportedCultures();
        var secondCall = LocalizationManager.GetSupportedCultures();

        // Assert
        firstCall.Should().HaveCount(secondCall.Count);
        firstCall.Should().BeEquivalentTo(secondCall);
    }
}
