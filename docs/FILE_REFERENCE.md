# PokeNET Framework - File Reference Guide

Quick reference for all key files in the PokeNET framework implementation.

## Build Configuration

| File | Purpose |
|------|---------|
| `/Directory.Build.props` | Shared compiler settings, package versions, code quality rules |

## Domain Layer (Pure C# - No MonoGame Dependencies)

### ECS Components
| File | Purpose |
|------|---------|
| `/PokeNET/PokeNET.Domain/ECS/Components/Position.cs` | 2D position in world space |
| `/PokeNET/PokeNET.Domain/ECS/Components/Velocity.cs` | Movement velocity (pixels/sec) |
| `/PokeNET/PokeNET.Domain/ECS/Components/Sprite.cs` | Visual rendering data |
| `/PokeNET/PokeNET.Domain/ECS/Components/Health.cs` | Hit points and damage/healing |
| `/PokeNET/PokeNET.Domain/ECS/Components/Stats.cs` | Pokemon-style stats (ATK, DEF, SPD, etc.) |

### ECS Systems
| File | Purpose |
|------|---------|
| `/PokeNET/PokeNET.Domain/ECS/Systems/ISystem.cs` | Base interface for all systems |
| `/PokeNET/PokeNET.Domain/ECS/Systems/SystemBase.cs` | Abstract base with logging and error handling |
| `/PokeNET/PokeNET.Domain/ECS/Systems/ISystemManager.cs` | System lifecycle management contract |

### ECS Events
| File | Purpose |
|------|---------|
| `/PokeNET/PokeNET.Domain/ECS/Events/IGameEvent.cs` | Base interface for events |
| `/PokeNET/PokeNET.Domain/ECS/Events/IEventBus.cs` | Pub/sub event bus contract |

### Assets
| File | Purpose |
|------|---------|
| `/PokeNET/PokeNET.Domain/Assets/IAssetLoader.cs` | Generic asset loader interface |
| `/PokeNET/PokeNET.Domain/Assets/IAssetManager.cs` | Asset management contract |
| `/PokeNET/PokeNET.Domain/Assets/AssetLoadException.cs` | Asset loading exception type |

## Core Layer (MonoGame Integration)

### ECS Implementation
| File | Purpose |
|------|---------|
| `/PokeNET/PokeNET.Core/ECS/SystemManager.cs` | Concrete system lifecycle manager |
| `/PokeNET/PokeNET.Core/ECS/EventBus.cs` | Thread-safe event bus implementation |

### Assets Implementation
| File | Purpose |
|------|---------|
| `/PokeNET/PokeNET.Core/Assets/AssetManager.cs` | Asset manager with caching and mod support |

### Game Core
| File | Purpose |
|------|---------|
| `/PokeNET/PokeNET.Core/PokeNETGame.cs` | Main game class (MonoGame.Game subclass) |
| `/PokeNET/PokeNET.Core/Localization/LocalizationManager.cs` | Localization/i18n support |

## Platform Layer (DesktopGL Runner)

| File | Purpose |
|------|---------|
| `/PokeNET/PokeNET.DesktopGL/Program.cs` | Entry point with DI composition root |
| `/PokeNET/PokeNET.DesktopGL/appsettings.json` | Base configuration |
| `/PokeNET/PokeNET.DesktopGL/appsettings.Development.json` | Development environment overrides |

## Project Files

| File | Purpose |
|------|---------|
| `/PokeNET/PokeNET.Domain/PokeNET.Domain.csproj` | Domain layer project |
| `/PokeNET/PokeNET.Core/PokeNET.Core.csproj` | Core game project |
| `/PokeNET/PokeNET.DesktopGL/PokeNET.DesktopGL.csproj` | DesktopGL platform runner |
| `/PokeNET/PokeNET.WindowsDX/PokeNET.WindowsDX.csproj` | WindowsDX platform runner |
| `/PokeNET/PokeNET.sln` | Solution file |

## Documentation

| File | Purpose |
|------|---------|
| `/GAME_FRAMEWORK_PLAN.md` | Complete framework development plan |
| `/docs/IMPLEMENTATION_SUMMARY.md` | Phase 1-3 implementation summary |
| `/docs/FILE_REFERENCE.md` | This file - quick reference guide |
| `/CLAUDE.md` | Claude Code configuration and SPARC workflow |

## Key Directories

### Source Code
```
/PokeNET/PokeNET.Domain/       # Pure domain models (no MonoGame)
  ├── ECS/
  │   ├── Components/          # ECS components
  │   ├── Systems/             # ECS system abstractions
  │   └── Events/              # Event system
  └── Assets/                  # Asset abstractions

/PokeNET/PokeNET.Core/         # Core game (MonoGame integration)
  ├── ECS/                     # ECS implementations
  ├── Assets/                  # Asset implementations
  └── Localization/            # i18n support

/PokeNET/PokeNET.DesktopGL/    # DesktopGL platform
/PokeNET/PokeNET.WindowsDX/    # WindowsDX platform
```

### Configuration & Build
```
/Directory.Build.props          # Shared build settings
/.swarm/                        # Swarm coordination memory
/docs/                          # Documentation
```

## Quick Navigation Commands

### Build Commands
```bash
# Restore packages
dotnet restore

# Build all projects
dotnet build

# Build specific project
dotnet build /PokeNET/PokeNET.Domain/PokeNET.Domain.csproj
dotnet build /PokeNET/PokeNET.Core/PokeNET.Core.csproj
dotnet build /PokeNET/PokeNET.DesktopGL/PokeNET.DesktopGL.csproj

# Run the game
dotnet run --project /PokeNET/PokeNET.DesktopGL/PokeNET.DesktopGL.csproj
```

### Project Management
```bash
# Add project to solution
dotnet sln add <project-path>

# Add project reference
dotnet add <project> reference <referenced-project>

# Add NuGet package
dotnet add <project> package <package-name>
```

## Architecture Patterns Used

| Pattern | Location | Purpose |
|---------|----------|---------|
| **Dependency Injection** | `Program.cs` | Loose coupling, testability |
| **Repository** | `AssetManager` | Asset access abstraction |
| **Observer** | `EventBus` | Event-driven communication |
| **Strategy** | `IAssetLoader<T>` | Pluggable asset loading |
| **Template Method** | `SystemBase` | Common system behavior |
| **Factory** | `World.Create()` | ECS world creation |

## SOLID Principles Map

| Principle | Examples |
|-----------|----------|
| **Single Responsibility** | Each component/class has one purpose (Position, Velocity, etc.) |
| **Open/Closed** | IAssetLoader<T>, ISystem - extend via new implementations |
| **Liskov Substitution** | SystemBase hierarchy, all implementations interchangeable |
| **Interface Segregation** | Small interfaces (ISystem, IGameEvent, IAssetLoader) |
| **Dependency Inversion** | All dependencies via abstractions (ILogger, IEventBus, IAssetManager) |

## Next Implementation Targets

### Phase 4 (Modding Framework)
- `ModLoader.cs` - Mod discovery and loading
- `ModManifest.cs` - Mod metadata (modinfo.json)
- Harmony integration for code patches

### Phase 5 (Scripting Engine)
- `ScriptingEngine.cs` - Roslyn C# scripting host
- `ScriptContext.cs` - Safe API surface for scripts
- Script compilation and execution

### Phase 6 (Audio System)
- `AudioManager.cs` - DryWetMidi integration
- Procedural music generation
- Sound effect playback
