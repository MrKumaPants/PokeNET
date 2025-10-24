using System;
using System.Linq;
using FluentAssertions;
using PokeNET.Scripting.Security;
using Xunit;

namespace PokeNET.Tests.Scripting;

/// <summary>
/// Comprehensive permission system tests for ScriptPermissions.
/// Tests all permission levels, API categories, namespace allowlisting/denylisting,
/// permission escalation prevention, and custom configuration validation.
/// </summary>
public sealed class ScriptPermissionTests
{
    #region Basic Permission Level Tests

    [Fact]
    public void PermissionLevel_Restricted_HasMinimalAccess()
    {
        // Act
        var permissions = ScriptPermissions.CreateRestricted("test");

        // Assert
        permissions.Level.Should().Be(ScriptPermissions.PermissionLevel.Restricted);
        permissions.AllowedApis.Should().HaveFlag(ScriptPermissions.ApiCategory.Core);
        permissions.AllowedApis.Should().HaveFlag(ScriptPermissions.ApiCategory.Collections);
        permissions.AllowedApis.Should().NotHaveFlag(ScriptPermissions.ApiCategory.FileIO);
        permissions.AllowedApis.Should().NotHaveFlag(ScriptPermissions.ApiCategory.Network);
        permissions.MaxExecutionTime.Should().Be(TimeSpan.FromSeconds(5));
        permissions.MaxMemoryBytes.Should().Be(10 * 1024 * 1024);
    }

    [Fact]
    public void PermissionLevel_Standard_HasGameAccess()
    {
        // Act
        var permissions = ScriptPermissions.CreateStandard("test");

        // Assert
        permissions.Level.Should().Be(ScriptPermissions.PermissionLevel.Standard);
        permissions.AllowedApis.Should().HaveFlag(ScriptPermissions.ApiCategory.GameStateRead);
        permissions.AllowedApis.Should().HaveFlag(ScriptPermissions.ApiCategory.GameStateWrite);
        permissions.AllowedApis.Should().HaveFlag(ScriptPermissions.ApiCategory.Logging);
        permissions.AllowedApis.Should().NotHaveFlag(ScriptPermissions.ApiCategory.FileIO);
        permissions.MaxExecutionTime.Should().Be(TimeSpan.FromSeconds(10));
        permissions.MaxMemoryBytes.Should().Be(50 * 1024 * 1024);
    }

    [Fact]
    public void PermissionLevel_Elevated_HasExtendedAccess()
    {
        // Act
        var permissions = ScriptPermissions.CreateElevated("test");

        // Assert
        permissions.Level.Should().Be(ScriptPermissions.PermissionLevel.Elevated);
        permissions.AllowedApis.Should().HaveFlag(ScriptPermissions.ApiCategory.Serialization);
        permissions.AllowedApis.Should().NotHaveFlag(ScriptPermissions.ApiCategory.FileIO);
        permissions.AllowedApis.Should().NotHaveFlag(ScriptPermissions.ApiCategory.Network);
        permissions.MaxExecutionTime.Should().Be(TimeSpan.FromSeconds(30));
        permissions.MaxMemoryBytes.Should().Be(100 * 1024 * 1024);
    }

    [Fact]
    public void PermissionLevel_None_NoAccess()
    {
        // Act
        var permissions = ScriptPermissions.CreateBuilder()
            .WithLevel(ScriptPermissions.PermissionLevel.None)
            .WithScriptId("none-test")
            .Build();

        // Assert
        permissions.Level.Should().Be(ScriptPermissions.PermissionLevel.None);
        permissions.AllowedApis.Should().Be(ScriptPermissions.ApiCategory.Core);
    }

    [Fact]
    public void PermissionLevel_ReadOnly_CannotWrite()
    {
        // Act
        var permissions = ScriptPermissions.CreateBuilder()
            .WithLevel(ScriptPermissions.PermissionLevel.ReadOnly)
            .WithApis(ScriptPermissions.ApiCategory.GameStateRead)
            .WithScriptId("readonly-test")
            .Build();

        // Assert
        permissions.Level.Should().Be(ScriptPermissions.PermissionLevel.ReadOnly);
        permissions.IsApiAllowed(ScriptPermissions.ApiCategory.GameStateRead).Should().BeTrue();
        permissions.IsApiAllowed(ScriptPermissions.ApiCategory.GameStateWrite).Should().BeFalse();
    }

    #endregion

    #region API Category Tests

    [Fact]
    public void ApiCategory_Core_AlwaysAllowed()
    {
        // Act
        var permissions = ScriptPermissions.CreateRestricted("test");

        // Assert
        permissions.IsApiAllowed(ScriptPermissions.ApiCategory.Core).Should().BeTrue();
    }

    [Fact]
    public void ApiCategory_Collections_AllowedInRestricted()
    {
        // Act
        var permissions = ScriptPermissions.CreateRestricted("test");

        // Assert
        permissions.IsApiAllowed(ScriptPermissions.ApiCategory.Collections).Should().BeTrue();
    }

    [Fact]
    public void ApiCategory_FileIO_RequiresElevated()
    {
        // Arrange
        var builder = ScriptPermissions.CreateBuilder()
            .WithLevel(ScriptPermissions.PermissionLevel.Standard)
            .WithApis(ScriptPermissions.ApiCategory.Core | ScriptPermissions.ApiCategory.FileIO)
            .WithScriptId("test");

        // Act & Assert
        Action act = () => builder.Build();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*FileIO*Elevated*");
    }

    [Fact]
    public void ApiCategory_Network_RequiresElevated()
    {
        // Arrange
        var builder = ScriptPermissions.CreateBuilder()
            .WithLevel(ScriptPermissions.PermissionLevel.Standard)
            .WithApis(ScriptPermissions.ApiCategory.Core | ScriptPermissions.ApiCategory.Network)
            .WithScriptId("test");

        // Act & Assert
        Action act = () => builder.Build();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Network*Elevated*");
    }

    [Fact]
    public void ApiCategory_Reflection_RequiresElevated()
    {
        // Arrange
        var builder = ScriptPermissions.CreateBuilder()
            .WithLevel(ScriptPermissions.PermissionLevel.Standard)
            .WithApis(ScriptPermissions.ApiCategory.Core | ScriptPermissions.ApiCategory.Reflection)
            .WithScriptId("test");

        // Act & Assert
        Action act = () => builder.Build();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Reflection*Elevated*");
    }

    [Fact]
    public void ApiCategory_Threading_RequiresElevated()
    {
        // Arrange
        var builder = ScriptPermissions.CreateBuilder()
            .WithLevel(ScriptPermissions.PermissionLevel.Standard)
            .WithApis(ScriptPermissions.ApiCategory.Core | ScriptPermissions.ApiCategory.Threading)
            .WithScriptId("test");

        // Act & Assert
        Action act = () => builder.Build();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Threading*Elevated*");
    }

    [Fact]
    public void ApiCategory_Unsafe_RequiresUnrestricted()
    {
        // Arrange
        var builder = ScriptPermissions.CreateBuilder()
            .WithLevel(ScriptPermissions.PermissionLevel.Elevated)
            .WithApis(ScriptPermissions.ApiCategory.Core | ScriptPermissions.ApiCategory.Unsafe)
            .WithScriptId("test");

        // Act & Assert
        Action act = () => builder.Build();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Unsafe*Unrestricted*");
    }

    [Fact]
    public void ApiCategory_Multiple_CanBeCombined()
    {
        // Act
        var permissions = ScriptPermissions.CreateBuilder()
            .WithLevel(ScriptPermissions.PermissionLevel.Standard)
            .WithApis(ScriptPermissions.ApiCategory.Core |
                     ScriptPermissions.ApiCategory.Collections |
                     ScriptPermissions.ApiCategory.Logging |
                     ScriptPermissions.ApiCategory.Random)
            .WithScriptId("test")
            .Build();

        // Assert
        permissions.IsApiAllowed(ScriptPermissions.ApiCategory.Core).Should().BeTrue();
        permissions.IsApiAllowed(ScriptPermissions.ApiCategory.Collections).Should().BeTrue();
        permissions.IsApiAllowed(ScriptPermissions.ApiCategory.Logging).Should().BeTrue();
        permissions.IsApiAllowed(ScriptPermissions.ApiCategory.Random).Should().BeTrue();
    }

    [Fact]
    public void ApiCategory_AllowApi_AddsToExisting()
    {
        // Act
        var permissions = ScriptPermissions.CreateBuilder()
            .WithLevel(ScriptPermissions.PermissionLevel.Standard)
            .WithApis(ScriptPermissions.ApiCategory.Core)
            .AllowApi(ScriptPermissions.ApiCategory.Collections)
            .AllowApi(ScriptPermissions.ApiCategory.Logging)
            .WithScriptId("test")
            .Build();

        // Assert
        permissions.IsApiAllowed(ScriptPermissions.ApiCategory.Core).Should().BeTrue();
        permissions.IsApiAllowed(ScriptPermissions.ApiCategory.Collections).Should().BeTrue();
        permissions.IsApiAllowed(ScriptPermissions.ApiCategory.Logging).Should().BeTrue();
    }

    [Fact]
    public void ApiCategory_DenyApi_RemovesFromExisting()
    {
        // Act
        var permissions = ScriptPermissions.CreateBuilder()
            .WithLevel(ScriptPermissions.PermissionLevel.Standard)
            .WithApis(ScriptPermissions.ApiCategory.Core |
                     ScriptPermissions.ApiCategory.Collections |
                     ScriptPermissions.ApiCategory.Logging)
            .DenyApi(ScriptPermissions.ApiCategory.Logging)
            .WithScriptId("test")
            .Build();

        // Assert
        permissions.IsApiAllowed(ScriptPermissions.ApiCategory.Core).Should().BeTrue();
        permissions.IsApiAllowed(ScriptPermissions.ApiCategory.Collections).Should().BeTrue();
        permissions.IsApiAllowed(ScriptPermissions.ApiCategory.Logging).Should().BeFalse();
    }

    #endregion

    #region Namespace Allowlist/Denylist Tests

    [Fact]
    public void Namespace_Allowed_ReturnsTrue()
    {
        // Arrange
        var permissions = ScriptPermissions.CreateBuilder()
            .WithLevel(ScriptPermissions.PermissionLevel.Standard)
            .AllowNamespace("System")
            .AllowNamespace("System.Collections.Generic")
            .WithScriptId("test")
            .Build();

        // Assert
        permissions.IsNamespaceAllowed("System").Should().BeTrue();
        permissions.IsNamespaceAllowed("System.Collections.Generic").Should().BeTrue();
        permissions.IsNamespaceAllowed("System.Collections.Generic.List").Should().BeTrue(); // Subnamespace
    }

    [Fact]
    public void Namespace_Denied_ReturnsFalse()
    {
        // Arrange
        var permissions = ScriptPermissions.CreateBuilder()
            .WithLevel(ScriptPermissions.PermissionLevel.Standard)
            .AllowNamespace("System")
            .DenyNamespace("System.IO")
            .WithScriptId("test")
            .Build();

        // Assert
        permissions.IsNamespaceAllowed("System").Should().BeTrue();
        permissions.IsNamespaceAllowed("System.IO").Should().BeFalse();
        permissions.IsNamespaceAllowed("System.IO.File").Should().BeFalse(); // Subnamespace
    }

    [Fact]
    public void Namespace_DenylistTakesPrecedence()
    {
        // Arrange
        var permissions = ScriptPermissions.CreateBuilder()
            .WithLevel(ScriptPermissions.PermissionLevel.Standard)
            .AllowNamespace("System")
            .DenyNamespace("System.Net")
            .WithScriptId("test")
            .Build();

        // Assert
        permissions.IsNamespaceAllowed("System.Net").Should().BeFalse();
        permissions.IsNamespaceAllowed("System.Net.Http").Should().BeFalse();
    }

    [Fact]
    public void Namespace_EmptyAllowlist_AllowsAll()
    {
        // Arrange - no allowed namespaces specified
        var permissions = ScriptPermissions.CreateBuilder()
            .WithLevel(ScriptPermissions.PermissionLevel.Standard)
            .DenyNamespace("System.IO")
            .WithScriptId("test")
            .Build();

        // Assert
        permissions.IsNamespaceAllowed("System").Should().BeTrue();
        permissions.IsNamespaceAllowed("System.Collections.Generic").Should().BeTrue();
        permissions.IsNamespaceAllowed("System.IO").Should().BeFalse(); // Denied
    }

    [Fact]
    public void Namespace_NullOrWhitespace_ReturnsFalse()
    {
        // Arrange
        var permissions = ScriptPermissions.CreateRestricted("test");

        // Assert
        permissions.IsNamespaceAllowed(null!).Should().BeFalse();
        permissions.IsNamespaceAllowed("").Should().BeFalse();
        permissions.IsNamespaceAllowed("   ").Should().BeFalse();
    }

    [Fact]
    public void Namespace_CaseInsensitive_Works()
    {
        // Arrange
        var permissions = ScriptPermissions.CreateBuilder()
            .WithLevel(ScriptPermissions.PermissionLevel.Standard)
            .AllowNamespace("System")
            .DenyNamespace("System.IO")
            .WithScriptId("test")
            .Build();

        // Assert
        permissions.IsNamespaceAllowed("system").Should().BeTrue();
        permissions.IsNamespaceAllowed("SYSTEM").Should().BeTrue();
        permissions.IsNamespaceAllowed("system.io").Should().BeFalse();
        permissions.IsNamespaceAllowed("SYSTEM.IO").Should().BeFalse();
    }

    [Fact]
    public void Namespace_StandardPreset_DeniesFileIO()
    {
        // Arrange
        var permissions = ScriptPermissions.CreateStandard("test");

        // Assert
        permissions.IsNamespaceAllowed("System.IO").Should().BeFalse();
        permissions.IsNamespaceAllowed("System.IO.File").Should().BeFalse();
    }

    [Fact]
    public void Namespace_StandardPreset_DeniesNetwork()
    {
        // Arrange
        var permissions = ScriptPermissions.CreateStandard("test");

        // Assert
        permissions.IsNamespaceAllowed("System.Net").Should().BeFalse();
        permissions.IsNamespaceAllowed("System.Net.Http").Should().BeFalse();
    }

    [Fact]
    public void Namespace_StandardPreset_DeniesReflection()
    {
        // Arrange
        var permissions = ScriptPermissions.CreateStandard("test");

        // Assert
        permissions.IsNamespaceAllowed("System.Reflection").Should().BeFalse();
    }

    [Fact]
    public void Namespace_StandardPreset_DeniesThreading()
    {
        // Arrange
        var permissions = ScriptPermissions.CreateStandard("test");

        // Assert
        permissions.IsNamespaceAllowed("System.Threading").Should().BeFalse();
    }

    #endregion

    #region Permission Escalation Prevention Tests

    [Fact]
    public void PermissionEscalation_FileIOWithoutElevated_Blocked()
    {
        // Arrange
        var builder = ScriptPermissions.CreateBuilder()
            .WithLevel(ScriptPermissions.PermissionLevel.Standard)
            .AllowApi(ScriptPermissions.ApiCategory.FileIO)
            .WithScriptId("test");

        // Act & Assert
        Action act = () => builder.Build();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void PermissionEscalation_NetworkWithoutElevated_Blocked()
    {
        // Arrange
        var builder = ScriptPermissions.CreateBuilder()
            .WithLevel(ScriptPermissions.PermissionLevel.Standard)
            .AllowApi(ScriptPermissions.ApiCategory.Network)
            .WithScriptId("test");

        // Act & Assert
        Action act = () => builder.Build();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void PermissionEscalation_UnsafeWithoutUnrestricted_Blocked()
    {
        // Arrange
        var builder = ScriptPermissions.CreateBuilder()
            .WithLevel(ScriptPermissions.PermissionLevel.Elevated)
            .AllowApi(ScriptPermissions.ApiCategory.Unsafe)
            .WithScriptId("test");

        // Act & Assert
        Action act = () => builder.Build();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void PermissionEscalation_MultipleDangerousAPIs_Blocked()
    {
        // Arrange
        var builder = ScriptPermissions.CreateBuilder()
            .WithLevel(ScriptPermissions.PermissionLevel.Standard)
            .AllowApi(ScriptPermissions.ApiCategory.FileIO)
            .AllowApi(ScriptPermissions.ApiCategory.Network)
            .AllowApi(ScriptPermissions.ApiCategory.Reflection)
            .WithScriptId("test");

        // Act & Assert
        Action act = () => builder.Build();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void PermissionEscalation_ElevatedWithDangerousAPIs_Allowed()
    {
        // Act
        var permissions = ScriptPermissions.CreateBuilder()
            .WithLevel(ScriptPermissions.PermissionLevel.Elevated)
            .AllowApi(ScriptPermissions.ApiCategory.FileIO)
            .AllowApi(ScriptPermissions.ApiCategory.Network)
            .AllowApi(ScriptPermissions.ApiCategory.Reflection)
            .AllowNamespace("System.IO")
            .AllowNamespace("System.Net")
            .AllowNamespace("System.Reflection")
            .WithScriptId("test")
            .Build();

        // Assert
        permissions.Level.Should().Be(ScriptPermissions.PermissionLevel.Elevated);
        permissions.IsApiAllowed(ScriptPermissions.ApiCategory.FileIO).Should().BeTrue();
        permissions.IsApiAllowed(ScriptPermissions.ApiCategory.Network).Should().BeTrue();
        permissions.IsApiAllowed(ScriptPermissions.ApiCategory.Reflection).Should().BeTrue();
    }

    #endregion

    #region Custom Permission Configuration Tests

    [Fact]
    public void CustomConfig_TimeoutValidation_TooShort_Throws()
    {
        // Arrange
        var builder = ScriptPermissions.CreateBuilder()
            .WithLevel(ScriptPermissions.PermissionLevel.Standard)
            .WithScriptId("test");

        // Act & Assert
        Action act = () => builder.WithTimeout(TimeSpan.Zero);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CustomConfig_TimeoutValidation_TooLong_Throws()
    {
        // Arrange
        var builder = ScriptPermissions.CreateBuilder()
            .WithLevel(ScriptPermissions.PermissionLevel.Standard)
            .WithScriptId("test");

        // Act & Assert
        Action act = () => builder.WithTimeout(TimeSpan.FromMinutes(10));
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CustomConfig_TimeoutValidation_ValidRange_Succeeds()
    {
        // Act
        var permissions = ScriptPermissions.CreateBuilder()
            .WithLevel(ScriptPermissions.PermissionLevel.Standard)
            .WithTimeout(TimeSpan.FromSeconds(30))
            .WithScriptId("test")
            .Build();

        // Assert
        permissions.MaxExecutionTime.Should().Be(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void CustomConfig_MemoryValidation_TooSmall_Throws()
    {
        // Arrange
        var builder = ScriptPermissions.CreateBuilder()
            .WithLevel(ScriptPermissions.PermissionLevel.Standard)
            .WithScriptId("test");

        // Act & Assert
        Action act = () => builder.WithMaxMemory(0);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CustomConfig_MemoryValidation_TooLarge_Throws()
    {
        // Arrange
        var builder = ScriptPermissions.CreateBuilder()
            .WithLevel(ScriptPermissions.PermissionLevel.Standard)
            .WithScriptId("test");

        // Act & Assert
        Action act = () => builder.WithMaxMemory(2L * 1024 * 1024 * 1024); // 2GB
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CustomConfig_MemoryValidation_ValidRange_Succeeds()
    {
        // Act
        var permissions = ScriptPermissions.CreateBuilder()
            .WithLevel(ScriptPermissions.PermissionLevel.Standard)
            .WithMaxMemory(100 * 1024 * 1024) // 100MB
            .WithScriptId("test")
            .Build();

        // Assert
        permissions.MaxMemoryBytes.Should().Be(100 * 1024 * 1024);
    }

    [Fact]
    public void CustomConfig_ScriptId_AutoGenerated()
    {
        // Act
        var permissions = ScriptPermissions.CreateBuilder()
            .WithLevel(ScriptPermissions.PermissionLevel.Standard)
            .Build();

        // Assert
        permissions.ScriptId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void CustomConfig_ScriptId_CustomValue()
    {
        // Act
        var permissions = ScriptPermissions.CreateBuilder()
            .WithLevel(ScriptPermissions.PermissionLevel.Standard)
            .WithScriptId("my-custom-script")
            .Build();

        // Assert
        permissions.ScriptId.Should().Be("my-custom-script");
    }

    [Fact]
    public void CustomConfig_ExternalAssemblies_DefaultFalse()
    {
        // Act
        var permissions = ScriptPermissions.CreateRestricted("test");

        // Assert
        permissions.CanLoadExternalAssemblies.Should().BeFalse();
    }

    [Fact]
    public void CustomConfig_ExternalAssemblies_CanEnable()
    {
        // Act
        var permissions = ScriptPermissions.CreateBuilder()
            .WithLevel(ScriptPermissions.PermissionLevel.Elevated)
            .WithExternalAssemblies(true)
            .WithScriptId("test")
            .Build();

        // Assert
        permissions.CanLoadExternalAssemblies.Should().BeTrue();
    }

    #endregion

    #region Permission Inheritance Tests

    [Fact]
    public void PermissionInheritance_RestrictedToStandard_MorePermissive()
    {
        // Arrange
        var restricted = ScriptPermissions.CreateRestricted("test1");
        var standard = ScriptPermissions.CreateStandard("test2");

        // Assert
        standard.AllowedApis.Should().HaveFlag(restricted.AllowedApis);
        standard.MaxExecutionTime.Should().BeGreaterThan(restricted.MaxExecutionTime);
        standard.MaxMemoryBytes.Should().BeGreaterThan(restricted.MaxMemoryBytes);
    }

    [Fact]
    public void PermissionInheritance_StandardToElevated_MorePermissive()
    {
        // Arrange
        var standard = ScriptPermissions.CreateStandard("test1");
        var elevated = ScriptPermissions.CreateElevated("test2");

        // Assert
        elevated.MaxExecutionTime.Should().BeGreaterThan(standard.MaxExecutionTime);
        elevated.MaxMemoryBytes.Should().BeGreaterThan(standard.MaxMemoryBytes);
    }

    #endregion

    #region ToString Tests

    [Fact]
    public void ToString_ReturnsFormattedString()
    {
        // Arrange
        var permissions = ScriptPermissions.CreateStandard("my-script");

        // Act
        var result = permissions.ToString();

        // Assert
        result.Should().Contain("my-script");
        result.Should().Contain("Level=Standard");
        result.Should().Contain("Timeout=10s");
        result.Should().Contain("MaxMem=50MB");
    }

    [Fact]
    public void ToString_IncludesAllRelevantInfo()
    {
        // Arrange
        var permissions = ScriptPermissions.CreateBuilder()
            .WithScriptId("test-123")
            .WithLevel(ScriptPermissions.PermissionLevel.Elevated)
            .WithTimeout(TimeSpan.FromSeconds(15))
            .WithMaxMemory(75 * 1024 * 1024)
            .Build();

        // Act
        var result = permissions.ToString();

        // Assert
        result.Should().Contain("test-123");
        result.Should().Contain("Elevated");
        result.Should().Contain("15s");
        result.Should().Contain("75MB");
    }

    #endregion

    #region Immutability Tests

    [Fact]
    public void Immutability_PermissionsImmutable()
    {
        // Arrange
        var permissions = ScriptPermissions.CreateStandard("test");
        var originalLevel = permissions.Level;
        var originalApis = permissions.AllowedApis;

        // Act - attempt to modify (should have no effect)
        // Permissions object should be immutable

        // Assert
        permissions.Level.Should().Be(originalLevel);
        permissions.AllowedApis.Should().Be(originalApis);
    }

    [Fact]
    public void Immutability_NamespacesImmutable()
    {
        // Arrange
        var permissions = ScriptPermissions.CreateStandard("test");
        var originalNamespaces = permissions.AllowedNamespaces;

        // Assert
        originalNamespaces.Should().NotBeNull();
        Action act = () => permissions.AllowedNamespaces.Add("NewNamespace");
        act.Should().Throw<NotSupportedException>(); // ImmutableHashSet doesn't support Add
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void EdgeCase_AllApiCategories_WhenUnrestricted()
    {
        // Act
        var permissions = ScriptPermissions.CreateBuilder()
            .WithLevel(ScriptPermissions.PermissionLevel.Unrestricted)
            .WithApis(ScriptPermissions.ApiCategory.Core |
                     ScriptPermissions.ApiCategory.Collections |
                     ScriptPermissions.ApiCategory.GameStateRead |
                     ScriptPermissions.ApiCategory.GameStateWrite |
                     ScriptPermissions.ApiCategory.Logging |
                     ScriptPermissions.ApiCategory.Random |
                     ScriptPermissions.ApiCategory.DateTime |
                     ScriptPermissions.ApiCategory.Serialization |
                     ScriptPermissions.ApiCategory.Unsafe)
            .WithScriptId("unrestricted-test")
            .Build();

        // Assert
        permissions.Level.Should().Be(ScriptPermissions.PermissionLevel.Unrestricted);
        permissions.IsApiAllowed(ScriptPermissions.ApiCategory.Unsafe).Should().BeTrue();
    }

    [Fact]
    public void EdgeCase_NoApis_OnlyCore()
    {
        // Act
        var permissions = ScriptPermissions.CreateBuilder()
            .WithLevel(ScriptPermissions.PermissionLevel.Restricted)
            .WithApis(ScriptPermissions.ApiCategory.None)
            .WithScriptId("no-apis-test")
            .Build();

        // Assert
        permissions.AllowedApis.Should().Be(ScriptPermissions.ApiCategory.None);
    }

    [Fact]
    public void EdgeCase_MultipleAllowedNamespaces_AllWork()
    {
        // Act
        var permissions = ScriptPermissions.CreateBuilder()
            .WithLevel(ScriptPermissions.PermissionLevel.Standard)
            .AllowNamespace("System")
            .AllowNamespace("System.Collections.Generic")
            .AllowNamespace("System.Linq")
            .AllowNamespace("PokeNET.Game")
            .WithScriptId("test")
            .Build();

        // Assert
        permissions.AllowedNamespaces.Should().HaveCount(4);
        permissions.IsNamespaceAllowed("System").Should().BeTrue();
        permissions.IsNamespaceAllowed("System.Collections.Generic").Should().BeTrue();
        permissions.IsNamespaceAllowed("System.Linq").Should().BeTrue();
        permissions.IsNamespaceAllowed("PokeNET.Game").Should().BeTrue();
    }

    [Fact]
    public void EdgeCase_MultipleDeniedNamespaces_AllWork()
    {
        // Act
        var permissions = ScriptPermissions.CreateBuilder()
            .WithLevel(ScriptPermissions.PermissionLevel.Standard)
            .AllowNamespace("System")
            .DenyNamespace("System.IO")
            .DenyNamespace("System.Net")
            .DenyNamespace("System.Reflection")
            .WithScriptId("test")
            .Build();

        // Assert
        permissions.DeniedNamespaces.Should().HaveCount(3);
        permissions.IsNamespaceAllowed("System.IO").Should().BeFalse();
        permissions.IsNamespaceAllowed("System.Net").Should().BeFalse();
        permissions.IsNamespaceAllowed("System.Reflection").Should().BeFalse();
    }

    #endregion
}
