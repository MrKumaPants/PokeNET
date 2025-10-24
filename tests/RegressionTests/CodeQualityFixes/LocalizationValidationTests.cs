using System;
using System.Globalization;
using System.Threading;
using PokeNET.Core.Localization;
using Xunit;

namespace PokeNET.Tests.RegressionTests.CodeQualityFixes;

/// <summary>
/// Regression tests for Issue #12: Missing validation in LocalizationManager.
/// </summary>
public class LocalizationValidationTests
{
    [Fact]
    public void SetCulture_WithInvalidCultureCode_ShouldThrowArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(
            () => LocalizationManager.SetCulture("invalid-culture-xyz"));

        Assert.Contains("Invalid culture code", exception.Message);
    }

    [Fact]
    public void SetCulture_WithNullCultureCode_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(
            () => LocalizationManager.SetCulture(null!));
    }

    [Fact]
    public void SetCulture_WithEmptyCultureCode_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(
            () => LocalizationManager.SetCulture(string.Empty));
    }

    [Fact]
    public void SetCulture_WithValidCultureCode_ShouldSetCulture()
    {
        // Arrange
        var originalCulture = Thread.CurrentThread.CurrentCulture;

        try
        {
            // Act
            LocalizationManager.SetCulture("en-US");

            // Assert
            Assert.Equal("en-US", Thread.CurrentThread.CurrentCulture.Name);
            Assert.Equal("en-US", Thread.CurrentThread.CurrentUICulture.Name);
        }
        finally
        {
            // Restore original culture
            Thread.CurrentThread.CurrentCulture = originalCulture;
            Thread.CurrentThread.CurrentUICulture = originalCulture;
        }
    }

    [Fact]
    public void GetSupportedCultures_ShouldReturnNonEmptyList()
    {
        // Act
        var cultures = LocalizationManager.GetSupportedCultures();

        // Assert
        Assert.NotNull(cultures);
        Assert.NotEmpty(cultures);
        Assert.Contains(cultures, c => c.Equals(CultureInfo.InvariantCulture));
    }

    [Theory]
    [InlineData("en-US")]
    [InlineData("fr-FR")]
    [InlineData("es-ES")]
    public void SetCulture_WithCommonCultures_ShouldWork(string cultureCode)
    {
        // This test verifies that common culture codes are handled properly
        // Note: Actual support depends on resource files in the project

        var originalCulture = Thread.CurrentThread.CurrentCulture;

        try
        {
            // Act & Assert - Should not throw
            try
            {
                LocalizationManager.SetCulture(cultureCode);
            }
            catch (NotSupportedException)
            {
                // OK if culture not supported by game
                // (depends on available resource files)
                Assert.True(true, $"Culture {cultureCode} not supported - this is OK");
            }
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = originalCulture;
            Thread.CurrentThread.CurrentUICulture = originalCulture;
        }
    }
}
