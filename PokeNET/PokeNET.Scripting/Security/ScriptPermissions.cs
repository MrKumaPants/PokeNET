using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace PokeNET.Scripting.Security;

/// <summary>
/// Defines permissions and API access levels for script execution.
/// Implements principle of least privilege with explicit allowlisting.
/// </summary>
/// <remarks>
/// SECURITY DESIGN:
/// - Default-deny: Scripts have NO permissions unless explicitly granted
/// - Allowlist-based: Only permitted APIs are accessible
/// - Granular control: Fine-grained permission levels
/// - Immutable: Permissions cannot be modified after creation
/// </remarks>
public sealed class ScriptPermissions
{
    /// <summary>
    /// Permission level for script execution
    /// </summary>
    public enum PermissionLevel
    {
        /// <summary>No permissions - ultra-restricted (default)</summary>
        None = 0,

        /// <summary>Basic computation only - no I/O or external access</summary>
        Restricted = 1,

        /// <summary>Can read game state, no modifications</summary>
        ReadOnly = 2,

        /// <summary>Can modify game state within limits</summary>
        Standard = 3,

        /// <summary>Extended permissions for trusted scripts</summary>
        Elevated = 4,

        /// <summary>Full access - only for system scripts (DANGEROUS)</summary>
        Unrestricted = 99,
    }

    /// <summary>
    /// API categories that can be allowed/denied
    /// </summary>
    [Flags]
    public enum ApiCategory
    {
        None = 0,

        /// <summary>Basic math, string operations</summary>
        Core = 1 << 0,

        /// <summary>LINQ, collections</summary>
        Collections = 1 << 1,

        /// <summary>Read game state (Pokemon, battles, etc.)</summary>
        GameStateRead = 1 << 2,

        /// <summary>Modify game state</summary>
        GameStateWrite = 1 << 3,

        /// <summary>Logging and diagnostics</summary>
        Logging = 1 << 4,

        /// <summary>Random number generation</summary>
        Random = 1 << 5,

        /// <summary>Time/date operations</summary>
        DateTime = 1 << 6,

        /// <summary>Serialization (JSON, etc.) - RESTRICTED</summary>
        Serialization = 1 << 7,

        /// <summary>File I/O - DANGEROUS</summary>
        FileIO = 1 << 8,

        /// <summary>Network access - DANGEROUS</summary>
        Network = 1 << 9,

        /// <summary>Reflection - DANGEROUS</summary>
        Reflection = 1 << 10,

        /// <summary>Threading - DANGEROUS</summary>
        Threading = 1 << 11,

        /// <summary>Unsafe code - EXTREMELY DANGEROUS</summary>
        Unsafe = 1 << 12,
    }

    /// <summary>
    /// Gets the permission level
    /// </summary>
    public PermissionLevel Level { get; }

    /// <summary>
    /// Gets the allowed API categories
    /// </summary>
    public ApiCategory AllowedApis { get; }

    /// <summary>
    /// Gets the maximum execution time allowed
    /// </summary>
    public TimeSpan MaxExecutionTime { get; }

    /// <summary>
    /// Gets the maximum memory allocation allowed (bytes)
    /// </summary>
    public long MaxMemoryBytes { get; }

    /// <summary>
    /// Gets the allowed namespaces (allowlist)
    /// </summary>
    public ImmutableHashSet<string> AllowedNamespaces { get; }

    /// <summary>
    /// Gets the denied namespaces (denylist - takes precedence)
    /// </summary>
    public ImmutableHashSet<string> DeniedNamespaces { get; }

    /// <summary>
    /// Gets whether the script can call external assemblies
    /// </summary>
    public bool CanLoadExternalAssemblies { get; }

    /// <summary>
    /// Gets the script identifier for audit logging
    /// </summary>
    public string ScriptId { get; }

    private ScriptPermissions(
        PermissionLevel level,
        ApiCategory allowedApis,
        TimeSpan maxExecutionTime,
        long maxMemoryBytes,
        IEnumerable<string> allowedNamespaces,
        IEnumerable<string> deniedNamespaces,
        bool canLoadExternalAssemblies,
        string scriptId
    )
    {
        Level = level;
        AllowedApis = allowedApis;
        MaxExecutionTime = maxExecutionTime;
        MaxMemoryBytes = maxMemoryBytes;
        AllowedNamespaces =
            allowedNamespaces?.ToImmutableHashSet() ?? ImmutableHashSet<string>.Empty;
        DeniedNamespaces = deniedNamespaces?.ToImmutableHashSet() ?? ImmutableHashSet<string>.Empty;
        CanLoadExternalAssemblies = canLoadExternalAssemblies;
        ScriptId = scriptId ?? Guid.NewGuid().ToString();
    }

    /// <summary>
    /// Creates a builder for constructing permissions
    /// </summary>
    public static Builder CreateBuilder() => new Builder();

    /// <summary>
    /// Creates default restricted permissions (safest option)
    /// </summary>
    public static ScriptPermissions CreateRestricted(string? scriptId = null)
    {
        return new Builder()
            .WithLevel(PermissionLevel.Restricted)
            .WithApis(ApiCategory.Core | ApiCategory.Collections)
            .WithTimeout(TimeSpan.FromSeconds(5))
            .WithMaxMemory(10 * 1024 * 1024) // 10 MB
            .WithScriptId(scriptId ?? "restricted-script")
            .Build();
    }

    /// <summary>
    /// Creates standard game script permissions
    /// </summary>
    public static ScriptPermissions CreateStandard(string? scriptId = null)
    {
        return new Builder()
            .WithLevel(PermissionLevel.Standard)
            .WithApis(
                ApiCategory.Core
                    | ApiCategory.Collections
                    | ApiCategory.GameStateRead
                    | ApiCategory.GameStateWrite
                    | ApiCategory.Logging
                    | ApiCategory.Random
                    | ApiCategory.DateTime
            )
            .WithTimeout(TimeSpan.FromSeconds(10))
            .WithMaxMemory(50 * 1024 * 1024) // 50 MB
            .AllowNamespace("PokeNET.Game")
            .AllowNamespace("PokeNET.Scripting.API")
            .AllowNamespace("System")
            .AllowNamespace("System.Linq")
            .AllowNamespace("System.Collections.Generic")
            .DenyNamespace("System.IO")
            .DenyNamespace("System.Net")
            .DenyNamespace("System.Reflection")
            .DenyNamespace("System.Threading")
            .WithScriptId(scriptId ?? "standard-script")
            .Build();
    }

    /// <summary>
    /// Creates elevated permissions for trusted scripts
    /// </summary>
    public static ScriptPermissions CreateElevated(string? scriptId = null)
    {
        return new Builder()
            .WithLevel(PermissionLevel.Elevated)
            .WithApis(
                ApiCategory.Core
                    | ApiCategory.Collections
                    | ApiCategory.GameStateRead
                    | ApiCategory.GameStateWrite
                    | ApiCategory.Logging
                    | ApiCategory.Random
                    | ApiCategory.DateTime
                    | ApiCategory.Serialization
            )
            .WithTimeout(TimeSpan.FromSeconds(30))
            .WithMaxMemory(100 * 1024 * 1024) // 100 MB
            .AllowNamespace("PokeNET")
            .AllowNamespace("System")
            .DenyNamespace("System.IO")
            .DenyNamespace("System.Net")
            .DenyNamespace("System.Reflection.Emit")
            .WithScriptId(scriptId ?? "elevated-script")
            .Build();
    }

    /// <summary>
    /// Checks if a specific API category is allowed
    /// </summary>
    public bool IsApiAllowed(ApiCategory category)
    {
        return AllowedApis.HasFlag(category);
    }

    /// <summary>
    /// Checks if a namespace is allowed
    /// </summary>
    public bool IsNamespaceAllowed(string ns)
    {
        if (string.IsNullOrWhiteSpace(ns))
            return false;

        // Denylist takes precedence
        if (
            DeniedNamespaces.Any(denied =>
                ns.StartsWith(denied, StringComparison.OrdinalIgnoreCase)
            )
        )
            return false;

        // Empty allowlist means all allowed (except denied)
        if (AllowedNamespaces.Count == 0)
            return true;

        return AllowedNamespaces.Any(allowed =>
            ns.StartsWith(allowed, StringComparison.OrdinalIgnoreCase)
        );
    }

    /// <summary>
    /// Builder for constructing ScriptPermissions
    /// </summary>
    public sealed class Builder
    {
        private PermissionLevel _level = PermissionLevel.Restricted;
        private ApiCategory _allowedApis = ApiCategory.Core;
        private TimeSpan _maxExecutionTime = TimeSpan.FromSeconds(5);
        private long _maxMemoryBytes = 10 * 1024 * 1024; // 10 MB default
        private HashSet<string> _allowedNamespaces = new();
        private HashSet<string> _deniedNamespaces = new();
        private bool _canLoadExternalAssemblies = false;
        private string? _scriptId;

        public Builder WithLevel(PermissionLevel level)
        {
            _level = level;
            return this;
        }

        public Builder WithApis(ApiCategory apis)
        {
            _allowedApis = apis;
            return this;
        }

        public Builder AllowApi(ApiCategory api)
        {
            _allowedApis |= api;
            return this;
        }

        public Builder DenyApi(ApiCategory api)
        {
            _allowedApis &= ~api;
            return this;
        }

        public Builder WithTimeout(TimeSpan timeout)
        {
            if (timeout <= TimeSpan.Zero || timeout > TimeSpan.FromMinutes(5))
                throw new ArgumentException(
                    "Timeout must be between 0 and 5 minutes",
                    nameof(timeout)
                );

            _maxExecutionTime = timeout;
            return this;
        }

        public Builder WithMaxMemory(long bytes)
        {
            if (bytes <= 0 || bytes > 1024L * 1024 * 1024) // Max 1 GB
                throw new ArgumentException(
                    "Memory limit must be between 0 and 1 GB",
                    nameof(bytes)
                );

            _maxMemoryBytes = bytes;
            return this;
        }

        public Builder AllowNamespace(string ns)
        {
            _allowedNamespaces.Add(ns);
            return this;
        }

        public Builder DenyNamespace(string ns)
        {
            _deniedNamespaces.Add(ns);
            return this;
        }

        public Builder WithExternalAssemblies(bool allow)
        {
            _canLoadExternalAssemblies = allow;
            return this;
        }

        public Builder WithScriptId(string scriptId)
        {
            _scriptId = scriptId;
            return this;
        }

        public ScriptPermissions Build()
        {
            // Validate permission level consistency
            ValidatePermissions();

            return new ScriptPermissions(
                _level,
                _allowedApis,
                _maxExecutionTime,
                _maxMemoryBytes,
                _allowedNamespaces,
                _deniedNamespaces,
                _canLoadExternalAssemblies,
                _scriptId ?? Guid.NewGuid().ToString()
            );
        }

        private void ValidatePermissions()
        {
            // Ensure dangerous APIs require elevated permissions
            if (_level < PermissionLevel.Elevated)
            {
                if (_allowedApis.HasFlag(ApiCategory.FileIO))
                    throw new InvalidOperationException(
                        "FileIO requires Elevated permission level"
                    );
                if (_allowedApis.HasFlag(ApiCategory.Network))
                    throw new InvalidOperationException(
                        "Network requires Elevated permission level"
                    );
                if (_allowedApis.HasFlag(ApiCategory.Reflection))
                    throw new InvalidOperationException(
                        "Reflection requires Elevated permission level"
                    );
                if (_allowedApis.HasFlag(ApiCategory.Threading))
                    throw new InvalidOperationException(
                        "Threading requires Elevated permission level"
                    );
            }

            if (_level < PermissionLevel.Unrestricted)
            {
                if (_allowedApis.HasFlag(ApiCategory.Unsafe))
                    throw new InvalidOperationException(
                        "Unsafe code requires Unrestricted permission level"
                    );
            }
        }
    }

    /// <summary>
    /// Gets a string representation for audit logging
    /// </summary>
    public override string ToString()
    {
        return $"ScriptPermissions[{ScriptId}]: Level={Level}, APIs={AllowedApis}, "
            + $"Timeout={MaxExecutionTime.TotalSeconds}s, MaxMem={MaxMemoryBytes / 1024 / 1024}MB";
    }
}
