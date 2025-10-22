# PokeNET Framework Documentation

Welcome to the PokeNET Framework documentation! PokeNET is a flexible, moddable Pokémon-style game framework built with MonoGame, .NET 9, and designed with SOLID principles.

## Table of Contents

### Getting Started
- [Quick Start Guide](developer/quick-start.md)
- [Installation](developer/installation.md)
- [Building from Source](developer/building.md)

### Architecture
- [System Overview](architecture/overview.md)
- [SOLID Principles Implementation](architecture/solid-principles.md)
- [Design Patterns](architecture/design-patterns.md)
- [Project Structure](architecture/project-structure.md)
- [Dependency Flow](architecture/dependencies.md)
- [ECS Architecture](architecture/ecs-architecture.md)

### API Reference
- [ModApi Overview](api/modapi-overview.md)
- [Core Interfaces](api/core-interfaces.md)
- [Component Reference](api/components.md)
- [System Reference](api/systems.md)
- [Event System](api/events.md)
- [Asset API](api/assets.md)
- [Scripting API](api/scripting.md)
- [Audio API](api/audio.md)

### Modding Guide
- [Getting Started with Modding](modding/getting-started.md)
- [Mod Structure and Manifest](modding/mod-structure.md)
- [Data Mods (JSON/XML)](modding/data-mods.md)
- [Content Mods (Assets)](modding/content-mods.md)
- [Code Mods (Harmony)](modding/code-mods.md)
- [Script Mods (C# Scripts)](modding/script-mods.md)
- [Dependency Management](modding/dependencies.md)
- [Conflict Resolution](modding/conflicts.md)
- [Best Practices](modding/best-practices.md)
- [Common Pitfalls](modding/pitfalls.md)

### Developer Guide
- [Development Environment Setup](developer/environment-setup.md)
- [Testing Strategies](developer/testing.md)
- [Debugging Techniques](developer/debugging.md)
- [Hot Reload Workflow](developer/hot-reload.md)
- [Performance Profiling](developer/profiling.md)
- [Contributing Guidelines](developer/contributing.md)
- [Code Style Guide](developer/code-style.md)

### Configuration
- [Configuration System](configuration/system-overview.md)
- [Settings Reference](configuration/settings-reference.md)
- [Environment-Specific Config](configuration/environments.md)
- [Mod Configuration Layers](configuration/mod-layers.md)

### Tutorials
- [Creating Your First Mod](tutorials/first-mod.md)
- [Adding a New Creature](tutorials/new-creature.md)
- [Creating Custom Abilities](tutorials/custom-ability.md)
- [Procedural Music Creation](tutorials/procedural-music.md)
- [Harmony Patching Tutorial](tutorials/harmony-patching.md)

### Examples
- [Complete Example Mod](examples/example-mod/README.md)
- [Code Samples](examples/code-samples.md)

## Key Features

### Entity-Component-System (Arch)
PokeNET uses the high-performance Arch ECS library for game entity management, providing:
- High-performance entity processing
- Flexible component composition
- Efficient system queries
- Cache-friendly data layout

### Modding Framework
Our RimWorld-inspired modding system supports:
- **Data Mods**: JSON/XML data definitions
- **Content Mods**: Custom textures, sounds, and assets
- **Code Mods**: Runtime code patching with Harmony
- **Script Mods**: C# scripting with Roslyn

### Procedural Audio
DryWetMidi integration enables:
- Dynamic, context-aware music generation
- MIDI-based procedural composition
- Event-driven audio responses
- Standard audio playback support

### SOLID Architecture
Built from the ground up with clean architecture principles:
- **Single Responsibility**: Focused, maintainable components
- **Open/Closed**: Extensible without modification
- **Liskov Substitution**: Proper inheritance hierarchies
- **Interface Segregation**: Small, focused interfaces
- **Dependency Inversion**: Abstraction-based design

## Version Information

- **Current Version**: 1.0.0-alpha
- **Framework**: .NET 9
- **Engine**: MonoGame 3.8+
- **ECS**: Arch
- **Last Updated**: 2025-10-22

## Quick Links

- [GitHub Repository](https://github.com/yourusername/PokeNET)
- [Issue Tracker](https://github.com/yourusername/PokeNET/issues)
- [Discussions](https://github.com/yourusername/PokeNET/discussions)
- [Discord Community](https://discord.gg/yourserver)

## Contributing

We welcome contributions! Please see our [Contributing Guidelines](developer/contributing.md) for details on:
- Code of conduct
- Development process
- Pull request guidelines
- Testing requirements

## License

PokeNET is licensed under the MIT License. See [LICENSE](../LICENSE) for details.

## Support

- **Documentation Issues**: [Report here](https://github.com/yourusername/PokeNET/issues)
- **Questions**: Use [GitHub Discussions](https://github.com/yourusername/PokeNET/discussions)
- **Bug Reports**: [Issue Tracker](https://github.com/yourusername/PokeNET/issues)

---

*Documentation generated for PokeNET Framework v1.0.0-alpha*
