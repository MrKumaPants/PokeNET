# PokeNET

> A Pokemon-inspired game engine built with modern C# architecture principles

**Status**: 🟡 In Development | **Build**: ✅ Compiles | **Architecture**: 🟢 Production-Ready

---

## Quick Start

```bash
# Clone and build
git clone https://github.com/yourusername/PokeNET.git
cd PokeNET
dotnet restore
dotnet build

# Run the game
dotnet run --project PokeNET/PokeNET.DesktopGL
```

## Key Technologies

- **ECS Framework**: Arch v2.1.0 (archetype-based ECS) + Source Generators v1.1.0
- **Rendering**: MonoGame 3.8 (cross-platform)
- **Audio**: DryWetMIDI v8.0.2 (procedural music + reactive audio engine)
- **Modding**: Harmony v2.x (runtime patching) + plugin system
- **Scripting**: Roslyn v4.14.0 (C# scripts with sandboxing)
- **Persistence**: Arch.Persistence v2.0.0

## Documentation

📚 **Start Here**: [`docs/PROJECT_STATUS.md`](docs/PROJECT_STATUS.md) - Current implementation state and MVP roadmap

### Core Documentation
- **[Architecture Overview](docs/ARCHITECTURE.md)** - System design and component reference
- **[Project Status](docs/PROJECT_STATUS.md)** - Implementation status, blockers, and roadmap
- **[Codebase Audit](docs/codebase-audit-2025-10-26.md)** - Comprehensive audit findings and recommendations
- **[API Reference](docs/API_REFERENCE.md)** - Complete API documentation

### Developer Guides
- **[Quick Start](docs/developer/quick-start.md)** - Get started developing
- **[Modding Guide](docs/modding/getting-started.md)** - Create mods for PokeNET
- **[Testing Guide](docs/TESTING_GUIDE.md)** - Testing strategies and coverage
- **[File Reference](docs/FILE_REFERENCE.md)** - Project structure reference

### Specialized Topics
- **[Audio System](docs/AUDIO_DOCUMENTATION_INDEX.md)** - DryWetMIDI integration and reactive audio
- **[Scripting System](docs/SCRIPTING_DOCUMENTATION_INDEX.md)** - Roslyn integration and security
- **[ECS Architecture](docs/ecs/README.md)** - Entity-Component-System patterns

## Features

### 🎮 Entity-Component-System (Arch)
High-performance archetype-based ECS for game entity management:
- Cache-friendly data layout
- Source generator optimized queries
- Relationship tracking for entity graphs
- World persistence for save/load

### 🔧 Modding Framework
RimWorld-inspired modding system with:
- **Data Mods**: JSON definitions for species, moves, items
- **Content Mods**: Custom textures, sounds, and assets
- **Code Mods**: Runtime patching with Harmony
- **Script Mods**: C# scripting with security sandbox

### 🎵 Procedural Audio
DryWetMIDI-powered audio system:
- Dynamic, context-aware music generation
- Event-driven reactive audio
- Crossfade transitions and ducking
- Multi-channel mixing

### 🏗️ SOLID Architecture
Built with clean architecture principles:
- **Domain Layer**: Pure C# game logic (no dependencies)
- **Core Layer**: Platform integration (MonoGame, systems)
- **ModAPI Layer**: Stable, versioned mod interfaces
- **Extension Modules**: Audio, Scripting, Assets (optional)

## Project Status

**Current Focus**: Implementing MVP gameplay systems (see [PROJECT_STATUS.md](docs/PROJECT_STATUS.md))

### ✅ Complete & Production-Ready
- Audio System (90%+ test coverage)
- Save System (90%+ test coverage)
- Scripting Engine (50% test coverage)
- Modding Framework (30% test coverage)
- ECS Core (Arch integration)

### 🟡 In Progress
- Battle System (40% - missing type chart, move DB)
- Rendering System (70% - needs move to Core layer)
- Pokemon Data (30% - models exist, loaders missing)

### ❌ MVP Blockers
- **Data Loaders**: No `IDataApi` implementation (CRITICAL)
- **UI Systems**: Battle UI, party screen, menus (CRITICAL)
- **Type Chart**: Type effectiveness system (CRITICAL)
- **Pokemon Mechanics**: Encounters, AI, evolution, status effects

See [`docs/codebase-audit-2025-10-26.md`](docs/codebase-audit-2025-10-26.md) for detailed findings.

## Getting Started

### Prerequisites
- .NET 9.0 SDK
- MonoGame 3.8+
- (Optional) Visual Studio 2022 / Rider / VS Code

### Building

```bash
# Restore packages
dotnet restore

# Build all projects
dotnet build

# Run tests
dotnet test

# Run game
dotnet run --project PokeNET/PokeNET.DesktopGL
```

### Creating a Mod

See [`docs/modding/getting-started.md`](docs/modding/getting-started.md) for full guide.

```
MyMod/
├── modinfo.json          # Mod manifest
├── Data/
│   └── species.json      # Custom Pokemon data
├── Content/
│   └── Sprites/          # Custom textures
└── Source/
    └── MyMod.cs          # C# code (optional)
```

## Contributing

We welcome contributions! Please see:
- [Contributing Guidelines](CONTRIBUTING.md)
- [Architecture Documentation](docs/ARCHITECTURE.md)
- [Code Style Guide](docs/developer/code-style.md)

## Roadmap to MVP

Per [`PROJECT_STATUS.md`](docs/PROJECT_STATUS.md):

1. **Phase 1: Data Infrastructure** (1 week) - Implement IDataApi, JSON loaders, TypeChart
2. **Phase 2: Core Gameplay** (2 weeks) - Battle UI, party screen, save integration
3. **Phase 3: Pokemon Mechanics** (1 week) - Encounters, AI, status effects, evolution
4. **Phase 4: Polish** (1 week) - Fix extension files, unify APIs, comprehensive tests

**Total Estimate**: 5 weeks to playable MVP

## License

MIT License - See [LICENSE](LICENSE) for details

## Support

- **Documentation**: [`docs/`](docs/)
- **Issues**: [GitHub Issues](https://github.com/yourusername/PokeNET/issues)
- **Discussions**: [GitHub Discussions](https://github.com/yourusername/PokeNET/discussions)

---

**Version**: 1.0.0-alpha | **Last Updated**: 2025-10-26

