# Refactoring Suggestions - PokeNET

**Review Date:** 2025-10-22
**Reviewer:** Code Review Agent
**Scope:** Refactoring priorities and implementation roadmap

## Executive Summary

This document provides prioritized refactoring suggestions to align the PokeNET codebase with SOLID/DRY principles and the architectural vision outlined in GAME_FRAMEWORK_PLAN.md.

**Total Refactoring Items:** 18
**Critical Priority:** 5
**High Priority:** 6
**Medium Priority:** 7

---

## Critical Priority Refactorings

### 1. Implement Dependency Injection Infrastructure

**Current Issue:**
- No DI container configured
- Direct instantiation throughout
- Static dependencies (LocalizationManager)
- Violates Dependency Inversion Principle

**Refactoring Steps:**

**Step 1: Add Required Packages**
```bash
# In PokeNET.Core
dotnet add package Microsoft.Extensions.DependencyInjection
dotnet add package Microsoft.Extensions.Hosting
dotnet add package Microsoft.Extensions.Logging
dotnet add package Microsoft.Extensions.Logging.Console
dotnet add package Microsoft.Extensions.Configuration
dotnet add package Microsoft.Extensions.Configuration.Json
```

**Step 2: Create Service Interfaces**
```csharp
// PokeNET.Domain/Services/ILocalizationService.cs
namespace PokeNET.Domain.Services;

public interface ILocalizationService
{
    IReadOnlyList<CultureInfo> GetSupportedCultures();
    void SetCulture(string cultureCode);
    string GetString(string key);
}

// PokeNET.Domain/Services/IGameConfiguration.cs
public interface IGameConfiguration
{
    string ContentRootDirectory { get; }
    string DefaultCulture { get; }
    DisplayOrientation SupportedOrientations { get; }
}
```

**Step 3: Implement Services**
```csharp
// PokeNET.Core/Services/ResourceManagerLocalizationService.cs
namespace PokeNET.Core.Services;

public class ResourceManagerLocalizationService : ILocalizationService
{
    private readonly ResourceManager _resourceManager;
    private readonly ILogger<ResourceManagerLocalizationService> _logger;

    public ResourceManagerLocalizationService(
        ILogger<ResourceManagerLocalizationService> logger)
    {
        _logger = logger;
        _resourceManager = new ResourceManager(
            "PokeNET.Core.Localization.Resources",
            Assembly.GetExecutingAssembly());
    }

    public IReadOnlyList<CultureInfo> GetSupportedCultures()
    {
        _logger.LogInformation("Discovering supported cultures");

        var supportedCultures = new List<CultureInfo>();
        var cultures = CultureInfo.GetCultures(CultureTypes.SpecificCultures);

        foreach (var culture in cultures)
        {
            try
            {
                var resourceSet = _resourceManager.GetResourceSet(culture, true, false);
                if (resourceSet != null)
                {
                    supportedCultures.Add(culture);
                }
            }
            catch (MissingManifestResourceException)
            {
                // No resources for this culture
            }
        }

        supportedCultures.Add(CultureInfo.InvariantCulture);

        _logger.LogInformation("Found {Count} supported cultures", supportedCultures.Count);

        return supportedCultures.AsReadOnly();
    }

    public void SetCulture(string cultureCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cultureCode);

        _logger.LogInformation("Setting culture to {CultureCode}", cultureCode);

        try
        {
            var culture = new CultureInfo(cultureCode);
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;

            _logger.LogInformation("Culture set successfully");
        }
        catch (CultureNotFoundException ex)
        {
            _logger.LogError(ex, "Invalid culture code: {CultureCode}", cultureCode);
            throw new ArgumentException(
                $"Invalid culture code: '{cultureCode}'",
                nameof(cultureCode),
                ex);
        }
    }

    public string GetString(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        return _resourceManager.GetString(key) ?? key;
    }
}

// PokeNET.Core/Configuration/GameConfiguration.cs
public class GameConfiguration : IGameConfiguration
{
    public string ContentRootDirectory { get; set; } = "Content";
    public string DefaultCulture { get; set; } = "en-EN";
    public DisplayOrientation SupportedOrientations { get; set; } =
        DisplayOrientation.LandscapeLeft | DisplayOrientation.LandscapeRight;
}
```

**Step 4: Refactor PokeNETGame to Use DI**
```csharp
// PokeNET.Core/PokeNETGame.cs
namespace PokeNET.Core;

public class PokeNETGame : Game
{
    private readonly ILocalizationService _localization;
    private readonly IGameConfiguration _configuration;
    private readonly ILogger<PokeNETGame> _logger;
    private readonly GraphicsDeviceManager _graphics;

    public PokeNETGame(
        ILocalizationService localization,
        IGameConfiguration configuration,
        ILogger<PokeNETGame> logger)
    {
        _localization = localization ?? throw new ArgumentNullException(nameof(localization));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogInformation("Initializing PokeNETGame");

        _graphics = new GraphicsDeviceManager(this);
        Services.AddService(typeof(GraphicsDeviceManager), _graphics);

        Content.RootDirectory = _configuration.ContentRootDirectory;
        _graphics.SupportedOrientations = _configuration.SupportedOrientations;
    }

    protected override void Initialize()
    {
        base.Initialize();

        _logger.LogInformation("Game initialization started");

        // Set up localization
        var cultures = _localization.GetSupportedCultures();
        _logger.LogInformation("Available cultures: {Cultures}",
            string.Join(", ", cultures.Select(c => c.Name)));

        _localization.SetCulture(_configuration.DefaultCulture);

        _logger.LogInformation("Game initialization complete");
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed
            || Keyboard.GetState().IsKeyDown(Keys.Escape))
        {
            _logger.LogInformation("Exit requested");
            Exit();
        }

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.MonoGameOrange);
        base.Draw(gameTime);
    }
}
```

**Step 5: Set Up DI Container in Platform Projects**
```csharp
// PokeNET.DesktopGL/Program.cs
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using PokeNET.Core;
using PokeNET.Core.Services;
using PokeNET.Core.Configuration;
using PokeNET.Domain.Services;

namespace PokeNET.DesktopGL;

internal class Program
{
    private static void Main(string[] args)
    {
        var host = CreateHostBuilder(args).Build();

        using var game = host.Services.GetRequiredService<PokeNETGame>();
        game.Run();
    }

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: true)
                      .AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true)
                      .AddCommandLine(args);
            })
            .ConfigureServices((context, services) =>
            {
                // Configuration
                var gameConfig = new GameConfiguration();
                context.Configuration.GetSection("Game").Bind(gameConfig);
                services.AddSingleton<IGameConfiguration>(gameConfig);

                // Services
                services.AddSingleton<ILocalizationService, ResourceManagerLocalizationService>();

                // Game
                services.AddSingleton<PokeNETGame>();
            });
}
```

**Step 6: Create appsettings.json**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "PokeNET": "Debug"
    }
  },
  "Game": {
    "ContentRootDirectory": "Content",
    "DefaultCulture": "en-EN",
    "SupportedOrientations": "LandscapeLeft,LandscapeRight"
  }
}
```

**Estimated Effort:** 4-6 hours
**Impact:** Critical - Enables all future refactorings

---

### 2. Create Missing Projects

**Current Issue:**
- Missing PokeNET.Domain, PokeNET.ModApi, PokeNET.Tests
- Violates planned architecture

**Refactoring Steps:**

```bash
# Navigate to solution directory
cd /mnt/c/Users/nate0/RiderProjects/PokeNET/PokeNET

# Create Domain project (pure C#, no MonoGame)
dotnet new classlib -n PokeNET.Domain -f net8.0
dotnet sln add PokeNET.Domain/PokeNET.Domain.csproj

# Create ModApi project
dotnet new classlib -n PokeNET.ModApi -f net8.0
dotnet sln add PokeNET.ModApi/PokeNET.ModApi.csproj

# Create Tests project
dotnet new xunit -n PokeNET.Tests -f net8.0
dotnet sln add PokeNET.Tests/PokeNET.Tests.csproj

# Set up project references
cd PokeNET.Core
dotnet add reference ../PokeNET.Domain/PokeNET.Domain.csproj

cd ../PokeNET.ModApi
dotnet add reference ../PokeNET.Domain/PokeNET.Domain.csproj

cd ../PokeNET.Tests
dotnet add reference ../PokeNET.Domain/PokeNET.Domain.csproj
dotnet add reference ../PokeNET.Core/PokeNET.Core.csproj
dotnet add reference ../PokeNET.ModApi/PokeNET.ModApi.csproj

# Add test packages
dotnet add package FluentAssertions
dotnet add package Moq
dotnet add package Microsoft.Extensions.Logging.Abstractions
```

**Project Structure:**
```
PokeNET/
├── PokeNET.Domain/           # Pure domain models, no MonoGame
│   ├── Entities/
│   ├── Components/
│   ├── Services/
│   └── DTOs/
├── PokeNET.Core/             # MonoGame implementation
│   ├── Game/
│   ├── Services/
│   └── Systems/
├── PokeNET.ModApi/           # Stable API for mods
│   ├── Interfaces/
│   └── DTOs/
├── PokeNET.Tests/            # Unit & integration tests
│   ├── Domain/
│   ├── Core/
│   └── ModApi/
├── PokeNET.DesktopGL/        # Platform runner
└── PokeNET.WindowsDX/        # Platform runner
```

**Estimated Effort:** 2 hours
**Impact:** Critical - Foundation for clean architecture

---

### 3. Fix MonoGame Reference in Core Project

**Current Issue:**
```xml
<!-- PokeNET.Core.csproj - WRONG -->
<PackageReference Include="MonoGame.Framework.DesktopGL" Version="3.8.*">
```

**Should be:**
```xml
<!-- PokeNET.Core.csproj - CORRECT -->
<PackageReference Include="MonoGame.Framework" Version="3.8.*" />
```

**Refactoring:**

```xml
<!-- PokeNET.Core/PokeNET.Core.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <TargetFramework>net8.0</TargetFramework>
        <Nullable>enable</Nullable>
        <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    </PropertyGroup>

    <ItemGroup>
        <!-- Use base framework, not platform-specific -->
        <PackageReference Include="MonoGame.Framework" Version="3.8.*" />
    </ItemGroup>

    <ItemGroup>
        <ProjectReference Include="..\PokeNET.Domain\PokeNET.Domain.csproj" />
    </ItemGroup>
</Project>
```

Platform-specific packages go in platform projects:

```xml
<!-- PokeNET.DesktopGL/PokeNET.DesktopGL.csproj -->
<ItemGroup>
    <PackageReference Include="MonoGame.Framework.DesktopGL" Version="3.8.*"/>
    <PackageReference Include="MonoGame.Content.Builder.Task" Version="3.8.*"/>
</ItemGroup>
```

**Estimated Effort:** 30 minutes
**Impact:** Critical - Correct architecture

---

### 4. Enable Nullable Reference Types

**Current Issue:**
- No nullable annotations
- Missing compile-time null safety

**Refactoring:**

Create `Directory.Build.props` at solution root:
```xml
<!-- Directory.Build.props -->
<Project>
    <PropertyGroup>
        <Nullable>enable</Nullable>
        <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
        <LangVersion>12.0</LangVersion>
        <ImplicitUsings>enable</ImplicitUsings>
    </PropertyGroup>

    <ItemGroup>
        <!-- Code analyzers for all projects -->
        <PackageReference Include="Microsoft.CodeAnalysis.NetAnalyzers" Version="8.*">
            <PrivateAssets>all</PrivateAssets>
            <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
        </PackageReference>
    </ItemGroup>
</Project>
```

Update code for nullable annotations:
```csharp
#nullable enable

public class ResourceManagerLocalizationService : ILocalizationService
{
    private readonly ResourceManager _resourceManager;
    private readonly ILogger<ResourceManagerLocalizationService> _logger;

    public ResourceManagerLocalizationService(
        ILogger<ResourceManagerLocalizationService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        // ...
    }

    public string GetString(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        // Explicitly handle potential null
        return _resourceManager.GetString(key) ?? key;
    }
}
```

**Estimated Effort:** 3-4 hours
**Impact:** Critical - Prevents NullReferenceExceptions

---

### 5. Remove Dead Code & Unused Imports

**Issues:**
1. Unused using statement in PokeNETGame.cs (line 8)
2. Unused `languages` list (lines 61-65)

**Refactoring:**

**Before:**
```csharp
using System;
using PokeNET.Core.Localization;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using static System.Net.Mime.MediaTypeNames;  // ❌ UNUSED

namespace PokeNET.Core
{
    public class PokeNETGame : Game
    {
        protected override void Initialize()
        {
            base.Initialize();

            // ❌ UNUSED - creates list then never uses it
            List<CultureInfo> cultures = LocalizationManager.GetSupportedCultures();
            var languages = new List<CultureInfo>();
            for (int i = 0; i < cultures.Count; i++)
            {
                languages.Add(cultures[i]);
            }

            var selectedLanguage = LocalizationManager.DEFAULT_CULTURE_CODE;
            LocalizationManager.SetCulture(selectedLanguage);
        }
    }
}
```

**After:**
```csharp
using System;
using PokeNET.Core.Localization;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
// Removed unused using

namespace PokeNET.Core;  // File-scoped namespace

public class PokeNETGame : Game
{
    protected override void Initialize()
    {
        base.Initialize();

        // Simplified - no unused variables
        _localization.SetCulture(_configuration.DefaultCulture);
    }
}
```

**Estimated Effort:** 15 minutes
**Impact:** Low - Code cleanliness

---

## High Priority Refactorings

### 6. Add Phase 1 Dependencies

**Missing NuGet Packages:**

```bash
# PokeNET.Core
dotnet add package Arch --version 1.*
dotnet add package Lib.Harmony --version 2.*
dotnet add package Microsoft.CodeAnalysis.CSharp.Scripting --version 4.*
dotnet add package DryWetMidi --version 7.*

# PokeNET.Domain
dotnet add package Arch.Extended --version 1.*
```

**Estimated Effort:** 1 hour
**Impact:** High - Required for phases 2-6

---

### 7. Implement ECS Foundation (Phase 2)

**Create ECS Architecture:**

```csharp
// PokeNET.Domain/ECS/Components/Position.cs
namespace PokeNET.Domain.ECS.Components;

public struct Position
{
    public float X { get; set; }
    public float Y { get; set; }

    public Position(float x, float y)
    {
        X = x;
        Y = y;
    }
}

// PokeNET.Domain/ECS/Components/Velocity.cs
public struct Velocity
{
    public float X { get; set; }
    public float Y { get; set; }
}

// PokeNET.Domain/ECS/Components/Sprite.cs
public struct Sprite
{
    public string TextureName { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}

// PokeNET.Domain/ECS/Systems/ISystem.cs
public interface ISystem
{
    void Initialize(World world);
    void Update(GameTime gameTime);
}

// PokeNET.Core/ECS/Systems/MovementSystem.cs
public class MovementSystem : ISystem
{
    private readonly ILogger<MovementSystem> _logger;
    private World _world = null!;
    private QueryDescription _query;

    public MovementSystem(ILogger<MovementSystem> logger)
    {
        _logger = logger;
    }

    public void Initialize(World world)
    {
        _world = world;
        _query = new QueryDescription().WithAll<Position, Velocity>();
    }

    public void Update(GameTime gameTime)
    {
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

        _world.Query(in _query, (ref Position pos, ref Velocity vel) =>
        {
            pos.X += vel.X * deltaTime;
            pos.Y += vel.Y * deltaTime;
        });
    }
}

// PokeNET.Core/PokeNETGame.cs
public class PokeNETGame : Game
{
    private readonly World _world;
    private readonly List<ISystem> _systems;

    public PokeNETGame(
        IEnumerable<ISystem> systems,
        ILocalizationService localization,
        IGameConfiguration configuration,
        ILogger<PokeNETGame> logger)
        : base()
    {
        _systems = systems.ToList();
        _world = World.Create();
        _localization = localization;
        _configuration = configuration;
        _logger = logger;

        foreach (var system in _systems)
        {
            system.Initialize(_world);
        }
    }

    protected override void Update(GameTime gameTime)
    {
        foreach (var system in _systems)
        {
            system.Update(gameTime);
        }

        base.Update(gameTime);
    }
}

// Register systems in DI
services.AddSingleton<ISystem, MovementSystem>();
services.AddSingleton<ISystem, RenderSystem>();
```

**Estimated Effort:** 8-12 hours
**Impact:** High - Core game architecture

---

### 8. Add Unit Tests

**Create Test Infrastructure:**

```csharp
// PokeNET.Tests/Domain/Services/LocalizationServiceTests.cs
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PokeNET.Core.Services;
using Xunit;

namespace PokeNET.Tests.Domain.Services;

public class LocalizationServiceTests
{
    private readonly Mock<ILogger<ResourceManagerLocalizationService>> _loggerMock;
    private readonly ResourceManagerLocalizationService _sut;

    public LocalizationServiceTests()
    {
        _loggerMock = new Mock<ILogger<ResourceManagerLocalizationService>>();
        _sut = new ResourceManagerLocalizationService(_loggerMock.Object);
    }

    [Fact]
    public void GetSupportedCultures_ShouldReturnCultures()
    {
        // Act
        var cultures = _sut.GetSupportedCultures();

        // Assert
        cultures.Should().NotBeEmpty();
        cultures.Should().Contain(CultureInfo.InvariantCulture);
    }

    [Theory]
    [InlineData("en-US")]
    [InlineData("fr-FR")]
    [InlineData("es-ES")]
    public void SetCulture_WithValidCulture_ShouldSetCulture(string cultureCode)
    {
        // Act
        _sut.SetCulture(cultureCode);

        // Assert
        Thread.CurrentThread.CurrentCulture.Name.Should().Be(cultureCode);
        Thread.CurrentThread.CurrentUICulture.Name.Should().Be(cultureCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void SetCulture_WithInvalidCulture_ShouldThrowArgumentException(string cultureCode)
    {
        // Act
        Action act = () => _sut.SetCulture(cultureCode);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SetCulture_WithUnknownCulture_ShouldThrowArgumentException()
    {
        // Act
        Action act = () => _sut.SetCulture("xx-XX");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Invalid culture code*");
    }
}
```

**Estimated Effort:** 4-6 hours
**Impact:** High - Quality assurance

---

### 9. Create Asset Management System (Phase 3)

```csharp
// PokeNET.Domain/Services/IAssetLoader.cs
public interface IAssetLoader<T>
{
    T Load(string path);
    Task<T> LoadAsync(string path, CancellationToken cancellationToken = default);
}

// PokeNET.Core/Services/AssetManager.cs
public class AssetManager : IAssetManager
{
    private readonly Dictionary<Type, object> _loaders = new();
    private readonly Dictionary<string, object> _cache = new();
    private readonly ILogger<AssetManager> _logger;
    private readonly AssetSecurityConfiguration _security;

    public void RegisterLoader<T>(IAssetLoader<T> loader)
    {
        _loaders[typeof(T)] = loader;
    }

    public T Load<T>(string path)
    {
        // Security validation
        if (!_security.ValidatePath(path))
            throw new SecurityException($"Invalid asset path: {path}");

        // Check cache
        var cacheKey = $"{typeof(T).Name}:{path}";
        if (_cache.TryGetValue(cacheKey, out var cached))
        {
            _logger.LogDebug("Asset loaded from cache: {Path}", path);
            return (T)cached;
        }

        // Load asset
        if (!_loaders.TryGetValue(typeof(T), out var loaderObj))
            throw new InvalidOperationException($"No loader registered for {typeof(T).Name}");

        var loader = (IAssetLoader<T>)loaderObj;
        var asset = loader.Load(path);

        // Cache
        _cache[cacheKey] = asset;

        _logger.LogInformation("Asset loaded: {Path}", path);

        return asset;
    }
}
```

**Estimated Effort:** 6-8 hours
**Impact:** High - Required for Phase 4

---

## Medium Priority Refactorings

### 10. Modernize C# Syntax

**Use File-Scoped Namespaces:**
```csharp
// Old
namespace PokeNET.Core
{
    public class PokeNETGame : Game
    {
    }
}

// New
namespace PokeNET.Core;

public class PokeNETGame : Game
{
}
```

**Use Target-Typed New:**
```csharp
// Old
List<CultureInfo> cultures = new List<CultureInfo>();

// New
List<CultureInfo> cultures = new();
```

**Use Primary Constructors (C# 12):**
```csharp
// Old
public class MovementSystem : ISystem
{
    private readonly ILogger<MovementSystem> _logger;

    public MovementSystem(ILogger<MovementSystem> logger)
    {
        _logger = logger;
    }
}

// New
public class MovementSystem(ILogger<MovementSystem> logger) : ISystem
{
    // logger is automatically a field
}
```

**Estimated Effort:** 2 hours
**Impact:** Medium - Code modernization

---

### 11. Add EditorConfig

```ini
# .editorconfig
root = true

[*]
charset = utf-8
end_of_line = crlf
insert_final_newline = true
trim_trailing_whitespace = true

[*.cs]
indent_style = space
indent_size = 4

# Nullable reference types
csharp_nullable_value = enable

# Using directives
dotnet_sort_system_directives_first = true
dotnet_separate_import_directive_groups = false

# Naming conventions
dotnet_naming_rule.private_fields_should_be_camelcase.symbols = private_fields
dotnet_naming_rule.private_fields_should_be_camelcase.style = _camelcase
dotnet_naming_rule.private_fields_should_be_camelcase.severity = warning

dotnet_naming_symbols.private_fields.applicable_kinds = field
dotnet_naming_symbols.private_fields.applicable_accessibilities = private

dotnet_naming_style._camelcase.required_prefix = _
dotnet_naming_style._camelcase.capitalization = camel_case

# Code style rules
csharp_prefer_braces = true:warning
csharp_prefer_simple_using_statement = true:suggestion
```

**Estimated Effort:** 1 hour
**Impact:** Medium - Code consistency

---

### 12-18. Additional Refactorings

12. **Create Mod Loading Infrastructure** (Phase 4) - 12-16 hours
13. **Implement Scripting Engine** (Phase 5) - 8-12 hours
14. **Add Audio System** (Phase 6) - 6-8 hours
15. **Create Save/Load System** (Phase 7) - 8-10 hours
16. **Add Observability** (Phase 9) - 4-6 hours
17. **Implement Hot Reload** (Phase 11) - 6-8 hours
18. **Add Security Measures** (Phase 14) - 10-15 hours

---

## Refactoring Roadmap

### Week 1: Foundation
- [ ] Create missing projects (Domain, ModApi, Tests)
- [ ] Implement DI infrastructure
- [ ] Fix MonoGame reference issue
- [ ] Enable nullable reference types
- [ ] Remove dead code

**Estimated: 10-14 hours**

### Week 2: Core Systems
- [ ] Add Phase 1 NuGet packages
- [ ] Implement ECS foundation
- [ ] Add unit test infrastructure
- [ ] Write initial tests

**Estimated: 16-20 hours**

### Week 3: Asset & Mod Systems
- [ ] Create asset management system
- [ ] Implement mod loader foundation
- [ ] Add security infrastructure
- [ ] Create mod API

**Estimated: 20-24 hours**

### Week 4: Advanced Features
- [ ] Scripting engine
- [ ] Audio system basics
- [ ] Save/load system
- [ ] Documentation

**Estimated: 18-22 hours**

---

## Total Estimated Effort

| Priority | Items | Hours |
|----------|-------|-------|
| Critical | 5 | 10-14 |
| High | 6 | 30-38 |
| Medium | 7 | 48-62 |
| **Total** | **18** | **88-114 hours** |

---

## Success Metrics

### Code Quality
- [ ] Test coverage > 80%
- [ ] No SOLID violations
- [ ] All analyzers passing
- [ ] No technical debt

### Architecture
- [ ] All planned projects created
- [ ] Dependency direction correct
- [ ] DI used throughout
- [ ] Interfaces defined

### Features
- [ ] Phase 1-3 complete
- [ ] ECS working
- [ ] Asset loading working
- [ ] Mod loading foundation

---

## Conclusion

The refactoring roadmap focuses on building solid architectural foundations before implementing advanced features. The critical priority items (Weeks 1-2) will establish the infrastructure needed for all future development.

**Next Steps:**
1. Start with Critical Priority items
2. Complete foundation before adding features
3. Test each refactoring thoroughly
4. Document architectural decisions
5. Review progress weekly

**Remember:** Refactoring is not optional - it's essential for long-term maintainability and success of the project.
