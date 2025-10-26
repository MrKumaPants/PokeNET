# PokeNET Framework - Documentation Index

**Version**: 1.0.0-alpha  
**Last Updated**: October 26, 2025

---

## 🎯 Quick Start

**New to the project?**
1. **[ACTIONABLE_TASKS.md](ACTIONABLE_TASKS.md)** - All outstanding work (single source of truth)
2. **[PROJECT_STATUS.md](PROJECT_STATUS.md)** - Current implementation status & MVP roadmap
3. **[ARCHITECTURE.md](ARCHITECTURE.md)** - System architecture and design
4. **[codebase-audit-2025-10-26.md](codebase-audit-2025-10-26.md)** - Comprehensive audit findings

---

## 📚 Core Documentation

### Essential Guides
| Document | Purpose | Audience |
|----------|---------|----------|
| [README.md](README.md) | Main documentation entry point | Everyone |
| [ACTIONABLE_TASKS.md](ACTIONABLE_TASKS.md) | All TODOs and implementation work | Developers |
| [PROJECT_STATUS.md](PROJECT_STATUS.md) | Implementation status, MVP roadmap | Project managers, devs |
| [ARCHITECTURE.md](ARCHITECTURE.md) | System architecture, design patterns | Architects, senior devs |
| [TESTING_GUIDE.md](TESTING_GUIDE.md) | Testing strategies and practices | QA, developers |
| [API_REFERENCE.md](API_REFERENCE.md) | Complete API documentation | All developers |

### Audit & Analysis
| Document | Purpose |
|----------|---------|
| [codebase-audit-2025-10-26.md](codebase-audit-2025-10-26.md) | Comprehensive codebase audit |
| [audits/performance-bottleneck-analysis.md](audits/performance-bottleneck-analysis.md) | Performance analysis |

---

## 🏗️ Architecture Documentation

**Main**: [ARCHITECTURE.md](ARCHITECTURE.md) - Complete architecture overview

### Detailed Architecture Docs
| Document | Topic |
|----------|-------|
| [architecture/solid-principles.md](architecture/solid-principles.md) | SOLID principles implementation |
| [architecture/c4-architecture-diagrams.md](architecture/c4-architecture-diagrams.md) | C4 model diagrams |
| [architecture/save-system-architecture.md](architecture/save-system-architecture.md) | Save/load system design |
| [architecture/input-system.md](architecture/input-system.md) | Input system & command pattern |
| [architecture/harmony-integration.md](architecture/harmony-integration.md) | Harmony patching integration |
| [architecture/component-factory-pattern.md](architecture/component-factory-pattern.md) | Component factory pattern |
| [architecture/EntityFactory-Pattern.md](architecture/EntityFactory-Pattern.md) | Entity factory pattern |
| [architecture/modding-system.md](architecture/modding-system.md) | Modding system architecture |

### Architecture Decision Records (ADRs)
| ADR | Topic |
|-----|-------|
| [architecture/ADR-001-ModManifest-Interface-Segregation.md](architecture/ADR-001-ModManifest-Interface-Segregation.md) | Mod manifest ISP |
| [architecture/ADR-001-Scripting-System-Interfaces.md](architecture/ADR-001-Scripting-System-Interfaces.md) | Scripting interfaces |
| [architecture/adrs/ADR-004-Event-API-ISP-Fix.md](architecture/adrs/ADR-004-Event-API-ISP-Fix.md) | Event API ISP fix |

---

## 🔌 API Documentation

**Main**: [API_REFERENCE.md](API_REFERENCE.md) - Complete API reference

### Sub-System APIs
| System | Documentation |
|--------|---------------|
| **Audio** | [AUDIO_INDEX.md](AUDIO_INDEX.md) - Audio system index |
| | [api/audio.md](api/audio.md) - Audio API reference |
| **Scripting** | [SCRIPTING_INDEX.md](SCRIPTING_INDEX.md) - Scripting system index |
| | [api/scripting.md](api/scripting.md) - Scripting API reference |
| **Modding** | [api/modapi-overview.md](api/modapi-overview.md) - Mod API overview |
| | [api/modapi-reference.md](api/modapi-reference.md) - Complete mod API reference |
| **Components** | [api/components.md](api/components.md) - ECS components |

---

## 🎮 Audio System

**Index**: [AUDIO_INDEX.md](AUDIO_INDEX.md)

| Document | Purpose |
|----------|---------|
| [audio/getting-started.md](audio/getting-started.md) | Quick start guide |
| [audio/procedural-music.md](audio/procedural-music.md) | Procedural music generation |
| [audio/configuration.md](audio/configuration.md) | Configuration reference |
| [audio/best-practices.md](audio/best-practices.md) | Performance & quality tips |
| [audio/audio-reactions.md](audio/audio-reactions.md) | Audio reaction system |
| [api/audio.md](api/audio.md) | Complete API reference |

---

## 📜 Scripting System

**Index**: [SCRIPTING_INDEX.md](SCRIPTING_INDEX.md)

| Document | Purpose |
|----------|---------|
| [modding/scripting-guide.md](modding/scripting-guide.md) | Scripting guide for mod developers |
| [api/scripting.md](api/scripting.md) | Complete scripting API reference |
| [security/script-security.md](security/script-security.md) | Security model & sandboxing |

---

## 🎨 Modding System

| Document | Purpose |
|----------|---------|
| [modding/getting-started.md](modding/getting-started.md) | Getting started with modding |
| [modding/modding-guide.md](modding/modding-guide.md) | Complete modding guide |
| [modding/scripting-guide.md](modding/scripting-guide.md) | Script modding guide |
| [api/modapi-overview.md](api/modapi-overview.md) | Mod API overview |
| [api/modapi-reference.md](api/modapi-reference.md) | Complete mod API reference |
| [tutorials/mod-development.md](tutorials/mod-development.md) | Mod development tutorial |

---

## 🧪 Testing & Quality

| Document | Purpose |
|----------|---------|
| [TESTING_GUIDE.md](TESTING_GUIDE.md) | Complete testing guide |
| [reviews/solid-compliance.md](reviews/solid-compliance.md) | SOLID compliance review |
| [reviews/architecture-compliance.md](reviews/architecture-compliance.md) | Architecture compliance |
| [reviews/security-audit.md](reviews/security-audit.md) | Security audit summary |
| [security/security-audit.md](security/security-audit.md) | Complete security audit |
| [security/script-security.md](security/script-security.md) | Script security model |

---

## 📊 Performance

| Document | Purpose |
|----------|---------|
| [performance/README.md](performance/README.md) | Performance index |
| [performance/optimization-roadmap.md](performance/optimization-roadmap.md) | Optimization roadmap |
| [performance/ecs-profiling.md](performance/ecs-profiling.md) | ECS profiling guide |
| [performance/memory-analysis.md](performance/memory-analysis.md) | Memory analysis |
| [performance/asset-loading.md](performance/asset-loading.md) | Asset loading performance |
| [performance/script-performance.md](performance/script-performance.md) | Script performance |
| [audits/performance-bottleneck-analysis.md](audits/performance-bottleneck-analysis.md) | Bottleneck analysis |
| [README-BENCHMARKS.md](README-BENCHMARKS.md) | Benchmarking guide |

---

## 🔬 Research & Analysis

| Document | Purpose |
|----------|---------|
| [research/ARCHITECTURE_RESEARCH_FINDINGS.md](research/ARCHITECTURE_RESEARCH_FINDINGS.md) | Architecture research |
| [research/QUICK_REFERENCE.md](research/QUICK_REFERENCE.md) | Quick reference guide |
| [research/save-system-best-practices.md](research/save-system-best-practices.md) | Save system patterns |

---

## 💻 Developer Resources

| Document | Purpose |
|----------|---------|
| [developer/quick-start.md](developer/quick-start.md) | Developer quick start |
| [ecs/README.md](ecs/README.md) | ECS documentation |

---

## 📝 Examples

| Example | Purpose |
|---------|---------|
| [examples/example-mod/](examples/example-mod/) | Complete example mod |
| [examples/simple-data-mod/](examples/simple-data-mod/) | Simple data mod example |
| [examples/simple-content-mod/](examples/simple-content-mod/) | Simple content mod example |
| [examples/simple-code-mod/](examples/simple-code-mod/) | Simple code mod example |
| [examples/scripts/](examples/scripts/) | Script examples |
| [examples/system-manager-metrics-usage.md](examples/system-manager-metrics-usage.md) | System metrics example |

---

## 📦 Archive

Historical documentation preserved for reference:
- [archive/](archive/) - Phase reports, old audits, migration summaries

---

## 🗂️ File Locations Quick Reference

### Project Structure
```
PokeNET/
├── PokeNET/
│   ├── PokeNET.Domain/          # Pure C# domain models
│   │   ├── ECS/
│   │   │   ├── Components/      # ECS components
│   │   │   ├── Systems/         # System interfaces
│   │   │   ├── Commands/        # Command pattern
│   │   │   ├── Events/          # Event system
│   │   │   └── Relationships/   # Entity relationships
│   │   ├── Input/               # Input system
│   │   ├── Assets/              # Asset abstractions
│   │   └── Modding/             # Mod interfaces
│   ├── PokeNET.Core/            # MonoGame integration
│   │   ├── ECS/                 # ECS implementations
│   │   ├── Assets/              # Asset manager
│   │   ├── Content/             # Game content
│   │   └── PokeNETGame.cs       # Main game class
│   ├── PokeNET.Audio/           # Audio system
│   │   ├── Services/            # Audio services
│   │   ├── Procedural/          # Procedural music
│   │   ├── Mixing/              # Audio mixing
│   │   └── Reactive/            # Reactive audio
│   ├── PokeNET.Scripting/       # Roslyn scripting
│   │   ├── Services/            # Script engine
│   │   ├── Security/            # Sandboxing
│   │   └── Diagnostics/         # Script diagnostics
│   ├── PokeNET.ModAPI/          # Stable mod API (planned)
│   ├── PokeNET.DesktopGL/       # Desktop runner
│   └── PokeNET.WindowsDX/       # Windows DX runner
├── benchmarks/                  # Performance benchmarks
├── Mods/                        # Mod directory
├── docs/                        # This documentation
└── tests/                       # Unit tests (planned)
```

### Key Files

#### Build Configuration
- `Directory.Build.props` - Shared compiler settings, package versions

#### Entry Points
- `PokeNET/PokeNET.DesktopGL/Program.cs` - Desktop entry point with DI setup
- `PokeNET/PokeNET.Core/PokeNETGame.cs` - Main game class

#### Configuration
- `PokeNET/PokeNET.DesktopGL/appsettings.json` - Base configuration
- `PokeNET/PokeNET.DesktopGL/appsettings.Development.json` - Dev overrides
- `config/audio-reactions.json` - Audio reaction configuration
- `config/InputConfig.json` - Input binding configuration

---

## 📖 Documentation Standards

### Naming Conventions
- **Indices**: `INDEX.md` or `<SYSTEM>_INDEX.md`
- **Guides**: `<topic>-guide.md` or `<topic>.md`
- **Architecture**: `<topic>-architecture.md` or `<topic>.md` in `architecture/`
- **ADRs**: `ADR-###-<topic>.md`

### Document Headers
All documentation should include:
```markdown
# Title

**Date**: YYYY-MM-DD
**Status**: Draft | Review | Complete | Obsolete
**Audience**: Developers | Mod Authors | All
```

---

**Last Updated**: October 26, 2025  
**Maintained By**: PokeNET Core Team

