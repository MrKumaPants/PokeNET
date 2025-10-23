using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace PokeNET.Scripting.Security;

/// <summary>
/// Validates script code for security vulnerabilities and permission violations.
/// Performs static analysis before compilation to prevent malicious code execution.
/// </summary>
/// <remarks>
/// THREAT MODEL:
///
/// 1. CODE INJECTION ATTACKS
///    - Risk: Malicious code embedded in scripts
///    - Mitigation: Static analysis, syntax tree inspection, keyword blocking
///
/// 2. RESOURCE EXHAUSTION (DoS)
///    - Risk: Infinite loops, excessive memory allocation, CPU bombing
///    - Mitigation: Execution timeout, memory limits, complexity analysis
///
/// 3. UNAUTHORIZED API ACCESS
///    - Risk: Access to file system, network, reflection, unsafe code
///    - Mitigation: Namespace allowlist/denylist, API category validation
///
/// 4. PRIVILEGE ESCALATION
///    - Risk: Scripts attempting to gain higher permissions
///    - Mitigation: Permission level enforcement, runtime checks
///
/// 5. INFORMATION DISCLOSURE
///    - Risk: Reading sensitive data, system information
///    - Mitigation: Sandboxing, limited API surface
///
/// 6. MALICIOUS OPERATIONS
///    - Risk: File deletion, network attacks, system modification
///    - Mitigation: AppDomain/AssemblyLoadContext isolation, permission validation
/// </remarks>
public sealed class SecurityValidator
{
    private readonly ScriptPermissions _permissions;
    private readonly List<SecurityViolation> _violations = new();

    /// <summary>
    /// Security violation detected during validation
    /// </summary>
    public sealed class SecurityViolation
    {
        public enum Severity
        {
            Info,
            Warning,
            Error,
            Critical
        }

        public Severity Level { get; init; }
        public string Message { get; init; } = string.Empty;
        public string Code { get; init; } = string.Empty;
        public int Line { get; init; }
        public int Column { get; init; }
        public string Category { get; init; } = string.Empty;

        public override string ToString() =>
            $"[{Level}] {Category} at ({Line},{Column}): {Message}";
    }

    /// <summary>
    /// Validation result
    /// </summary>
    public sealed class ValidationResult
    {
        public bool IsValid { get; init; }
        public IReadOnlyList<SecurityViolation> Violations { get; init; } = Array.Empty<SecurityViolation>();
        public string Summary { get; init; } = string.Empty;

        public bool HasCriticalViolations =>
            Violations.Any(v => v.Level == SecurityViolation.Severity.Critical);

        public bool HasErrors =>
            Violations.Any(v => v.Level >= SecurityViolation.Severity.Error);
    }

    // Dangerous keywords that require elevated permissions
    private static readonly HashSet<string> DangerousKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "Process", "ProcessStartInfo", "Registry", "RegistryKey",
        "AppDomain", "Assembly.Load", "Activator.CreateInstance",
        "Marshal", "GCHandle", "IntPtr", "UIntPtr",
        "DllImport", "UnmanagedFunctionPointer",
        "Thread", "Task.Run", "ThreadPool",
        "File", "Directory", "FileStream", "StreamReader", "StreamWriter",
        "WebClient", "HttpClient", "Socket", "TcpClient", "UdpClient",
        "Reflection.Emit", "DynamicMethod", "ILGenerator"
    };

    // Patterns that indicate potentially malicious code
    private static readonly Regex[] MaliciousPatterns = new[]
    {
        new Regex(@"while\s*\(\s*true\s*\)", RegexOptions.IgnoreCase), // Infinite loops
        new Regex(@"for\s*\(\s*;\s*;\s*\)", RegexOptions.IgnoreCase), // Infinite loops
        new Regex(@"goto\s+", RegexOptions.IgnoreCase), // Goto statements (code smell)
        new Regex(@"#pragma\s+warning\s+disable", RegexOptions.IgnoreCase), // Suppressing warnings
        new Regex(@"Activator\.CreateInstance", RegexOptions.IgnoreCase), // Dynamic type creation
        new Regex(@"Type\.GetType", RegexOptions.IgnoreCase), // Type reflection
    };

    public SecurityValidator(ScriptPermissions permissions)
    {
        _permissions = permissions ?? throw new ArgumentNullException(nameof(permissions));
    }

    /// <summary>
    /// Validates script code against security policies
    /// </summary>
    public ValidationResult Validate(string code, string? fileName = null)
    {
        _violations.Clear();

        if (string.IsNullOrWhiteSpace(code))
        {
            AddViolation(SecurityViolation.Severity.Error, "Empty or null script code", "EMPTY_CODE");
            return CreateResult();
        }

        try
        {
            // Parse the code into a syntax tree
            var syntaxTree = CSharpSyntaxTree.ParseText(code, path: fileName ?? "script.cs");
            var root = syntaxTree.GetCompilationUnitRoot();

            // Check for compilation errors
            ValidateSyntax(syntaxTree);

            // Perform security analysis
            ValidateUsings(root);
            ValidateMethods(root);
            ValidateTypes(root);
            ValidateUnsafeCode(root);
            ValidateDangerousPatterns(code);
            ValidateComplexity(root);

            return CreateResult();
        }
        catch (Exception ex)
        {
            AddViolation(SecurityViolation.Severity.Critical,
                $"Validation failed: {ex.Message}", "VALIDATION_ERROR");
            return CreateResult();
        }
    }

    private void ValidateSyntax(SyntaxTree syntaxTree)
    {
        var diagnostics = syntaxTree.GetDiagnostics();
        foreach (var diagnostic in diagnostics)
        {
            if (diagnostic.Severity == DiagnosticSeverity.Error)
            {
                var lineSpan = diagnostic.Location.GetLineSpan();
                AddViolation(
                    SecurityViolation.Severity.Error,
                    $"Syntax error: {diagnostic.GetMessage()}",
                    "SYNTAX_ERROR",
                    lineSpan.StartLinePosition.Line + 1,
                    lineSpan.StartLinePosition.Character + 1
                );
            }
        }
    }

    private void ValidateUsings(CompilationUnitSyntax root)
    {
        foreach (var usingDirective in root.Usings)
        {
            var ns = usingDirective.Name?.ToString() ?? string.Empty;

            if (!_permissions.IsNamespaceAllowed(ns))
            {
                var lineSpan = usingDirective.GetLocation().GetLineSpan();
                AddViolation(
                    SecurityViolation.Severity.Error,
                    $"Namespace '{ns}' is not allowed by security policy",
                    "FORBIDDEN_NAMESPACE",
                    lineSpan.StartLinePosition.Line + 1,
                    lineSpan.StartLinePosition.Character + 1,
                    "Namespace Violation"
                );
            }

            // Check for dangerous namespaces
            if (ns.StartsWith("System.IO", StringComparison.OrdinalIgnoreCase) &&
                !_permissions.IsApiAllowed(ScriptPermissions.ApiCategory.FileIO))
            {
                var lineSpan = usingDirective.GetLocation().GetLineSpan();
                AddViolation(
                    SecurityViolation.Severity.Critical,
                    $"File I/O operations (namespace '{ns}') require FileIO permission",
                    "FORBIDDEN_FILEIO",
                    lineSpan.StartLinePosition.Line + 1,
                    lineSpan.StartLinePosition.Character + 1,
                    "API Access Violation"
                );
            }

            if (ns.StartsWith("System.Net", StringComparison.OrdinalIgnoreCase) &&
                !_permissions.IsApiAllowed(ScriptPermissions.ApiCategory.Network))
            {
                var lineSpan = usingDirective.GetLocation().GetLineSpan();
                AddViolation(
                    SecurityViolation.Severity.Critical,
                    $"Network operations (namespace '{ns}') require Network permission",
                    "FORBIDDEN_NETWORK",
                    lineSpan.StartLinePosition.Line + 1,
                    lineSpan.StartLinePosition.Character + 1,
                    "API Access Violation"
                );
            }

            if (ns.Contains("Reflection", StringComparison.OrdinalIgnoreCase) &&
                !_permissions.IsApiAllowed(ScriptPermissions.ApiCategory.Reflection))
            {
                var lineSpan = usingDirective.GetLocation().GetLineSpan();
                AddViolation(
                    SecurityViolation.Severity.Critical,
                    $"Reflection (namespace '{ns}') requires Reflection permission",
                    "FORBIDDEN_REFLECTION",
                    lineSpan.StartLinePosition.Line + 1,
                    lineSpan.StartLinePosition.Character + 1,
                    "API Access Violation"
                );
            }
        }
    }

    private void ValidateMethods(CompilationUnitSyntax root)
    {
        var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>();

        foreach (var method in methods)
        {
            // Check for async methods without threading permission
            if (method.Modifiers.Any(m => m.IsKind(SyntaxKind.AsyncKeyword)) &&
                !_permissions.IsApiAllowed(ScriptPermissions.ApiCategory.Threading))
            {
                var lineSpan = method.GetLocation().GetLineSpan();
                AddViolation(
                    SecurityViolation.Severity.Error,
                    "Async methods require Threading permission",
                    "FORBIDDEN_ASYNC",
                    lineSpan.StartLinePosition.Line + 1,
                    lineSpan.StartLinePosition.Character + 1,
                    "Threading Violation"
                );
            }

            // Check for unsafe methods
            if (method.Modifiers.Any(m => m.IsKind(SyntaxKind.UnsafeKeyword)))
            {
                var lineSpan = method.GetLocation().GetLineSpan();
                AddViolation(
                    SecurityViolation.Severity.Critical,
                    "Unsafe code requires Unrestricted permission level",
                    "FORBIDDEN_UNSAFE",
                    lineSpan.StartLinePosition.Line + 1,
                    lineSpan.StartLinePosition.Character + 1,
                    "Unsafe Code Violation"
                );
            }

            // Check method body for dangerous calls
            ValidateMethodBody(method);
        }
    }

    private void ValidateMethodBody(MethodDeclarationSyntax method)
    {
        if (method.Body == null)
            return;

        var identifiers = method.Body.DescendantNodes().OfType<IdentifierNameSyntax>();

        foreach (var identifier in identifiers)
        {
            var name = identifier.Identifier.Text;

            if (DangerousKeywords.Contains(name))
            {
                var lineSpan = identifier.GetLocation().GetLineSpan();
                AddViolation(
                    SecurityViolation.Severity.Warning,
                    $"Potentially dangerous API usage: '{name}'",
                    "DANGEROUS_API",
                    lineSpan.StartLinePosition.Line + 1,
                    lineSpan.StartLinePosition.Character + 1,
                    "API Security Warning"
                );
            }
        }
    }

    private void ValidateTypes(CompilationUnitSyntax root)
    {
        var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>();

        foreach (var classDecl in classes)
        {
            // Check for unsafe classes
            if (classDecl.Modifiers.Any(m => m.IsKind(SyntaxKind.UnsafeKeyword)))
            {
                var lineSpan = classDecl.GetLocation().GetLineSpan();
                AddViolation(
                    SecurityViolation.Severity.Critical,
                    "Unsafe classes require Unrestricted permission level",
                    "FORBIDDEN_UNSAFE_CLASS",
                    lineSpan.StartLinePosition.Line + 1,
                    lineSpan.StartLinePosition.Character + 1,
                    "Unsafe Code Violation"
                );
            }
        }
    }

    private void ValidateUnsafeCode(CompilationUnitSyntax root)
    {
        // Check for pointer types
        var pointerTypes = root.DescendantNodes().OfType<PointerTypeSyntax>();
        if (pointerTypes.Any() && !_permissions.IsApiAllowed(ScriptPermissions.ApiCategory.Unsafe))
        {
            var first = pointerTypes.First();
            var lineSpan = first.GetLocation().GetLineSpan();
            AddViolation(
                SecurityViolation.Severity.Critical,
                "Pointer types require Unsafe permission",
                "FORBIDDEN_POINTERS",
                lineSpan.StartLinePosition.Line + 1,
                lineSpan.StartLinePosition.Character + 1,
                "Unsafe Code Violation"
            );
        }

        // Check for unsafe statements
        var unsafeStatements = root.DescendantNodes().OfType<UnsafeStatementSyntax>();
        if (unsafeStatements.Any() && !_permissions.IsApiAllowed(ScriptPermissions.ApiCategory.Unsafe))
        {
            var first = unsafeStatements.First();
            var lineSpan = first.GetLocation().GetLineSpan();
            AddViolation(
                SecurityViolation.Severity.Critical,
                "Unsafe statements require Unsafe permission",
                "FORBIDDEN_UNSAFE_STATEMENT",
                lineSpan.StartLinePosition.Line + 1,
                lineSpan.StartLinePosition.Character + 1,
                "Unsafe Code Violation"
            );
        }
    }

    private void ValidateDangerousPatterns(string code)
    {
        var lines = code.Split('\n');

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];

            foreach (var pattern in MaliciousPatterns)
            {
                if (pattern.IsMatch(line))
                {
                    AddViolation(
                        SecurityViolation.Severity.Warning,
                        $"Potentially dangerous code pattern: {pattern}",
                        "SUSPICIOUS_PATTERN",
                        i + 1,
                        0,
                        "Code Pattern Warning"
                    );
                }
            }
        }
    }

    private void ValidateComplexity(CompilationUnitSyntax root)
    {
        // Calculate cyclomatic complexity (simplified)
        var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>();

        foreach (var method in methods)
        {
            var complexity = CalculateComplexity(method);

            if (complexity > 20) // Threshold for high complexity
            {
                var lineSpan = method.GetLocation().GetLineSpan();
                AddViolation(
                    SecurityViolation.Severity.Warning,
                    $"Method '{method.Identifier.Text}' has high complexity ({complexity}). " +
                    "Complex code may hide malicious logic.",
                    "HIGH_COMPLEXITY",
                    lineSpan.StartLinePosition.Line + 1,
                    lineSpan.StartLinePosition.Character + 1,
                    "Code Quality Warning"
                );
            }
        }
    }

    private int CalculateComplexity(MethodDeclarationSyntax method)
    {
        if (method.Body == null)
            return 1;

        var complexity = 1; // Base complexity

        // Count decision points
        complexity += method.Body.DescendantNodes().OfType<IfStatementSyntax>().Count();
        complexity += method.Body.DescendantNodes().OfType<WhileStatementSyntax>().Count();
        complexity += method.Body.DescendantNodes().OfType<ForStatementSyntax>().Count();
        complexity += method.Body.DescendantNodes().OfType<ForEachStatementSyntax>().Count();
        complexity += method.Body.DescendantNodes().OfType<CaseSwitchLabelSyntax>().Count();
        complexity += method.Body.DescendantNodes().OfType<CatchClauseSyntax>().Count();

        return complexity;
    }

    private void AddViolation(
        SecurityViolation.Severity severity,
        string message,
        string code,
        int line = 0,
        int column = 0,
        string category = "Security")
    {
        _violations.Add(new SecurityViolation
        {
            Level = severity,
            Message = message,
            Code = code,
            Line = line,
            Column = column,
            Category = category
        });
    }

    private ValidationResult CreateResult()
    {
        var isValid = !_violations.Any(v => v.Level >= SecurityViolation.Severity.Error);

        var summary = isValid
            ? $"Validation passed with {_violations.Count} warning(s)"
            : $"Validation failed with {_violations.Count(v => v.Level >= SecurityViolation.Severity.Error)} error(s)";

        return new ValidationResult
        {
            IsValid = isValid,
            Violations = _violations.ToArray(),
            Summary = summary
        };
    }
}
