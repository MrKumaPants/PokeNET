# Pokémon Fan Game Framework Development Plan

This plan outlines the major phases for developing a flexible and moddable Pokémon-style game framework using MonoGame, .NET 9, and the specific libraries you've requested. The architecture follows SOLID and DRY principles throughout all components.

## Phase 0: Architectural Principles

1. **SOLID Principles:**

   - **Single Responsibility:** Each class and component will have one focused responsibility
   - **Open/Closed:** Systems designed to be extended without modifying existing code
   - **Liskov Substitution:** Proper inheritance hierarchies with polymorphic behavior
   - **Interface Segregation:** Small, focused interfaces rather than monolithic ones
   - **Dependency Inversion:** High-level modules depend on abstractions, not details

2. **DRY (Don't Repeat Yourself):**

   - Common functionality extracted into reusable components
   - Shared data structures and algorithms in utility classes
   - Configuration-driven design to avoid code duplication

3. **Design Patterns:**
   - **Factory** patterns for creating entities and components
   - **Strategy** patterns for swappable algorithms (AI, battle logic)
   - **Observer** pattern for event-driven communication
   - **Command** pattern for input handling and game actions
   - **Dependency Injection** for loose coupling between systems

## Solution & Project Structure

Align with MonoGame conventions where the `*.Core` project contains the cross-platform game and rendering, and `*.DesktopGL`/`*.WindowsDX` are platform-specific runners:

- **PokeNET.Core**: Cross-platform game project. Contains `Game` subclass, rendering, input, ECS systems, asset access. References `MonoGame.Framework` (no platform runtime).
- **PokeNET.DesktopGL**: Platform runner (DesktopGL). References `PokeNET.Core`. Contains entry `Program` with `Host`/DI/bootstrap, content pipeline config, and platform packaging.
- **PokeNET.WindowsDX** (optional): Windows-only runner (DirectX). Same responsibilities as DesktopGL, referencing `PokeNET.Core`.
- **PokeNET.Domain**: Pure domain models, ECS contracts/abstractions, data DTOs, utilities. No MonoGame references; maximizes testability.
- **PokeNET.ModApi**: Stable, versioned interfaces and DTOs exposed to mods; smallest surface necessary. Depends on `PokeNET.Domain` only.
- **PokeNET.Tests**: Unit/integration tests for `Domain/Core/ModApi` (avoid graphics in unit tests).

Optional modules as growth warrants:

- **PokeNET.Scripting** (Roslyn host), **PokeNET.Audio** (DryWetMidi integration), **PokeNET.Assets** (advanced asset pipeline), **PokeNET.Tools.\*** (validators, content tools).

Dependency direction (no cycles):

```
DesktopGL/WindowsDX -> { Core, ModApi, Scripting?, Audio?, Assets? }
Core                -> { Domain, Assets?, Audio? }
ModApi              -> { Domain }
Scripting/Audio/Assets -> { Domain, ModApi }
Mods                -> { ModApi } only
```

Guidelines and enforcement:

- `Core` references `MonoGame.Framework` only; platform projects pull runtime-specific bits and own the `Program` entry.
- Keep `Domain` free of MonoGame/platform specifics; put logic you want to test here.
- Cross boundaries via DI; inject `ILogger<T>` and configuration through interfaces. Host/DI lives in platform runners; `Core` consumes services.
- Publish `PokeNET.ModApi` as a NuGet package for mod authors (semantic versioning, changelog).
- Share compiler settings via `Directory.Build.props` (nullable enabled, analyzers, warnings-as-errors where feasible).
- Add architecture tests (e.g., `NetArchTest.Rules`) to assert dependency rules and prevent regressions.
- Organize solution folders mirroring module boundaries and keep build output paths distinct per project.

## Phase 1: Project Scaffolding & Core Setup

1. **Project Initialization:**

   - A new .NET 9 project will be created. Since official MonoGame templates may not support .NET 9 out of the box, the process involves creating a standard console application and then manually configuring it for MonoGame.
   - The project file (`.csproj`) will be updated to include the necessary NuGet packages:
     - `MonoGame.Framework.DesktopGL`: For cross-platform (Windows, macOS, Linux) game functionality.
     - `Arch`: The foundation of our Entity-Component-System architecture.
     - `Lib.Harmony`: To enable powerful, RimWorld-style code modding.
     - `Microsoft.CodeAnalysis.CSharp.Scripting`: For the Roslyn-based C# scripting engine.
     - `DryWetMidi`: For the procedural audio system.
     - `Microsoft.Extensions.Logging`: Structured logging framework.
     - `Microsoft.Extensions.Logging.Console`: Console logging provider.
     - `Microsoft.Extensions.Logging.Debug`: Debug output logging.
     - `Microsoft.Extensions.Configuration`: For externalized configuration.
     - `Microsoft.Extensions.Configuration.Binder`: Strongly-typed configuration binding.
     - `Microsoft.Extensions.Hosting`: Generic host, lifetime, config and logging wiring.
     - `Microsoft.Extensions.DependencyInjection`: Dependency Injection container.
   - A main `Game` class will be created, serving as the entry point and root of the game loop.

2. **Logging Infrastructure:**

   - Implement a centralized logging system using Microsoft.Extensions.Logging
   - Create custom log categories for different subsystems (rendering, audio, gameplay)
   - Configure appropriate log levels for different build configurations
   - Add performance logging for critical paths with structured logging

3. **Hosting & Dependency Injection:**
   - Bootstrap a `Host` via `Host.CreateDefaultBuilder()` to unify configuration, logging, and DI.
   - Register core services (ECS world factory, systems, `AssetManager`, `AudioManager`, `ModLoader`, `ScriptingEngine`).
   - Inject `ILogger<T>` and configuration into systems/services; avoid singletons except where immutable.
   - Define a composition root in `Program.cs` that builds the host, creates the `Game` instance via DI, and runs it.
   - Use environment-based configuration (Development/Release) to toggle diagnostics and logging levels.

## Phase 2: ECS Architecture with Arch

1. **World and Game Loop:**

   - The main game class will initialize and own the Arch ECS `World`.
   - The standard MonoGame `Update` and `Draw` methods will be used to drive the ECS systems.
   - Abstract interfaces for system lifecycles following SOLID principles.

2. **Core Components and Systems:**

   - Initial, fundamental components will be defined (e.g., `Position`, `Velocity`, `Sprite`, `Health`, `Stats`).
   - Components follow Single Responsibility Principle - each represents one aspect of game entities.
   - Basic systems that operate on these components will be created to handle logic like movement, rendering, and simple game state updates.
   - Systems will be designed with Open/Closed Principle in mind - extendable without modification.
   - Dependency Injection used for system dependencies following Dependency Inversion Principle.

3. **Systems Communication:**
   - Event-based communication between systems to maintain loose coupling
   - Message bus for broadcasting game events
   - System interface contracts to ensure proper integration

## Phase 3: Custom Asset Management (No Content Pipeline)

1. **Asset Loader:**

   - A custom `AssetManager` class will be built. Its responsibility is to load assets like textures, audio, and data files (`.json`, `.xml`) directly from the filesystem.
   - It will manage an internal cache to prevent reloading the same asset multiple times.
   - Abstract `IAssetLoader<T>` interface enables adding new asset types without modifying existing code (Open/Closed).
   - Comprehensive logging for asset loading performance and failures.

2. **Moddable Asset Paths:**
   - The asset manager will be designed to search for assets in a specific order: first in any loaded mod directories, and then falling back to the base game's asset directory. This allows mods to easily override existing assets or add new ones.
   - Asset resolution strategy follows Strategy pattern for extensibility.

## Phase 4: RimWorld-Style Modding Framework

1. **Mod Loader:**

   - A `ModLoader` will be implemented to run on game startup. It will scan a `Mods` directory for subdirectories, each representing a single mod.
   - It will read a manifest file (e.g., `modinfo.json`) from each mod to determine its name, author, version, and dependencies.
   - It will sort mods based on their dependencies to ensure they are loaded in the correct order.
   - Detailed logging of mod loading process, conflicts, and dependency resolution.

2. **Mod Types:**

   - **Data Mods:** Mods can add or override data by including JSON or XML files. The asset system will handle loading these.
   - **Content Mods:** Mods can add or replace art and audio assets (`.png`, `.wav`, etc.).
   - **Code Mods:** The `ModLoader` will load `.dll` files from mods and execute a designated entry point. This allows mods to use **Harmony** to patch game code at runtime, enabling deep and complex modifications.
   - All mod types follow a common `IMod` interface (Interface Segregation Principle).

3. **Mod API:**
   - Well-defined, stable API surface for mods to interact with
   - Versioned interfaces to support backward compatibility
   - Documentation generation for mod developers

## Phase 5: Roslyn C# Scripting Engine

1. **Scripting Host:**

   - A `ScriptingEngine` service will be created to host the Roslyn compiler.
   - It will load and execute C# script files (`.cs` or `.csx`) found in the game's data folders or in mod folders.
   - Sandboxing for script execution with appropriate security boundaries.
   - Error handling and logging for script compilation and execution failures.

2. **Scripting API:**
   - A well-defined API will be exposed to the scripts, allowing them to safely interact with the game world. For example, a script could define a Pokémon move's effect by queuing changes to the ECS world, creating new entities, or triggering events.
   - Scripts use dependency injection to request only the services they need (Interface Segregation).
   - Diagnostic tools for script performance monitoring.

## Phase 6: Dynamic Audio with DryWetMidi

1. **Procedural Music System:**

   - An `AudioManager` will be built that integrates the `DryWetMidi` library.
   - This will allow for the programmatic creation of music that can react to game state (e.g., battle intensity, player location, time of day).
   - The system will also handle playing standard pre-recorded music and sound effects.
   - Audio subsystems follow Single Responsibility - separate components for playback, mixing, procedural generation.
   - Adapter pattern used to integrate DryWetMidi with the game's audio system.

2. **Audio Service Layer:**
   - Abstract `IAudioService` with implementation specifics hidden (Dependency Inversion)
   - Separate interfaces for different audio concerns (Interface Segregation)
   - Event-based notification system for audio state changes

## Phase 7: Game State and Save System

1. **Serialization Framework:**

   - Component-based serialization system compatible with ECS architecture
   - Delta compression for efficient save files
   - Version migration system for save compatibility across game updates

2. **State Management:**
   - Immutable game state for predictable behavior and easier testing
   - State transition validation to prevent invalid game states
   - Comprehensive logging of state changes for debugging

## Phase 8: Proof of Concept and Validation

1. **Example Mod:**

   - To ensure all systems work together seamlessly, a final proof-of-concept mod will be created. This mod will serve as an example for future modders and will validate the framework's capabilities by:
     - Adding a new creature via a JSON file.
     - Providing a custom C# script for a new ability.
     - Using Harmony to modify an existing game mechanic.
     - Including a new procedural music track for a specific event.

2. **Testing Strategy:**

   - Unit tests for core systems following SOLID principles
   - Integration tests for subsystem interactions
   - Performance benchmarks for critical paths
   - Mod compatibility testing framework

3. **Documentation:**
   - Architecture documentation explaining SOLID implementations
   - API references generated from code
   - Developer guides for extending the framework
   - Modding tutorials with examples

## Phase 9: Observability & Telemetry

1. **Metrics & Tracing:**

   - Integrate OpenTelemetry for metrics and traces; instrument key systems (update loop, rendering, asset load, scripting, audio).
   - Provide pluggable exporters (console/logging by default; optional OTLP endpoint for advanced users).
   - Correlate logs with trace/span IDs for end-to-end debugging.

2. **Crash Reporting:**

   - Centralized unhandled exception handler that logs enriched context and writes crash dumps.
   - Optional integration point for third-party crash reporters; off by default and opt-in.

3. **Performance Tooling:**
   - Frame timing HUD (dev only), GC allocation counters, and asset load timings.
   - Periodic health metrics (FPS, entity count, draw calls) with sampling controls.

## Phase 10: Data Schema & Validation

1. **Schemas:**

   - Define JSON Schemas for data (creatures, moves, items, maps, animations) with version fields.
   - Validate on load; fail-fast in Development, collect-and-report in Release.

2. **Migrations:**

   - Provide a migrator to upgrade data between schema versions; log precise diffs.
   - Maintain backward-compat adapters to keep older mods working where feasible.

3. **Determinism & Ordering:**
   - Deterministic mod load order (explicit dependencies > priority > name) and stable hashing for cache keys.
   - Content index with duplicate detection and conflict reporting.

## Phase 11: Developer Experience & Hot Reload

1. **Live Reload:**

   - File watchers for assets and JSON; hot-reload data and textures safely at frame boundaries.
   - Roslyn script recompile on change with graceful fallback when errors occur.

2. **Debug Console & Tools:**
   - In-game dev console for commands (spawn, teleport, give item, set flag).
   - Input record/replay and deterministic step mode for debugging and tests.
   - Entity/component inspector with selection and live component value viewing.

## Phase 12: Localization & Accessibility

1. **Localization (i18n):**

   - Use `Microsoft.Extensions.Localization` with resource-backed strings; support mod-provided translations.
   - Text rendering with font fallback and locale-aware formatting; consider RTL support roadmap.

2. **Accessibility (a11y):**
   - Remappable controls, subtitle/caption options, motion reduction, colorblind-friendly palettes.
   - Configurable text size and contrast modes; non-blocking UI notifications.

## Phase 13: Build, Packaging & CI/CD

1. **Build Targets:**

   - Configure `dotnet publish` for Windows/macOS/Linux with runtime-specific builds.
   - Single-file, trimmed Release builds with content directory layout for mods.

2. **CI/CD:**

   - GitHub Actions pipeline: restore, build, test, package artifacts per OS.
   - Static analysis (Analyzers), code coverage, and artifact signing (optional).

3. **Distribution:**
   - Zip/tarball artifacts with `Mods/` folder scaffold and sample config.
   - Versioning and release notes generation.

## Phase 14: Security & Mod Sandboxing

1. **Threat Model:**

   - Recognize risks from Harmony patches and scripts: code execution, resource abuse, privacy.

2. **Mitigations:**

   - Trust levels for mods (trusted/unsafe) with clear UI warnings.
   - Load code mods in isolated `AssemblyLoadContext`; restrict reflection via allowlisted APIs.
   - Script API surface is minimal and capability-based; no raw file/network by default.
   - Optional mod signature verification and checksum reporting.

3. **Runtime Guards:**
   - Timeouts and cancellation tokens for scripts; per-frame budget enforcement.
   - Safe asset paths and path traversal prevention in loaders.

## Phase 15: Risks & Non-Goals

1. **Key Risks:**

   - Over-coupling between systems; mitigated via DI and eventing.
   - Performance regressions from excessive allocations; mitigated via pooling and profiling.
   - Mod compatibility churn; mitigated with versioned APIs and migration tooling.

2. **Non-Goals (Initial):**
   - Networked multiplayer, advanced physics, and console platform support.
   - Full screen-reader support; focus first on robust captions and input remap.

## Implementation Recommendations

### 1. Process & Validation

1. **Iterative Development**:

   - Reorganize phases into iterative cycles with deliverable milestones
   - Create first playable prototype after Phase 2 with minimal features
   - Implement "walking skeleton" of end-to-end flow early to validate architecture
   - Set up "preview builds" cadence for internal testing (bi-weekly)

2. **MVP Definition**:
   - Define clear MVP feature set for first playable version
   - Prioritize core gameplay loop over secondary systems
   - Create milestone roadmap with specific completion criteria
   - Establish key metrics for evaluating prototype success

### 2. Performance & Resources

1. **Object Pooling**:

   - Implement object pooling for frequently created/destroyed components
   - Create specialized `ComponentPool<T>` for each component type
   - Use value types (structs) for core components to reduce heap allocations
   - Profile GC pressure during development with memory snapshots

2. **Async Loading**:

   - Add async asset loading pipeline with cancellation support
   - Implement loading screens with progress reporting
   - Use background loading for non-critical assets
   - Consider asset streaming for large maps/areas

3. **Memory Budget**:
   - Define memory budgets per subsystem (rendering, audio, gameplay)
   - Add memory usage tracking and reporting in debug builds
   - Implement texture compression strategy for different platforms
   - Add asset unloading for unused resources

### 3. Content Pipeline

1. **MonoGame Integration**:

   - Clarify how `.mgcb` content projects integrate with custom asset loader
   - Support both content pipeline and raw asset loading as needed
   - Add converters between MonoGame asset types and ECS components
   - Document content workflow for artists/designers

2. **Shader Management**:
   - Add shader management system with hot-reload support
   - Support cross-platform shader compilation (HLSL/GLSL)
   - Implement shader variant system for different quality settings
   - Create material system to combine textures and shaders

### 4. Threading Model

1. **Thread Safety**:

   - Define clear threading model (main thread vs worker threads)
   - Mark thread-safe vs thread-unsafe systems explicitly
   - Add synchronization primitives for cross-thread communication
   - Implement job system for parallelizable work

2. **Hot Reload Safety**:
   - Add synchronization points for safe hot reloading
   - Implement versioning for reloaded scripts/assets
   - Create rollback mechanism for failed hot reloads
   - Design clear ownership model for dynamically loaded content

### 5. Configuration System

1. **Layered Configuration**:

   - Implement layered config: defaults → user settings → mod settings
   - Add conflict resolution strategy for competing mod settings
   - Support override tags to control setting priority
   - Create UI for managing configuration layers

2. **Mod Conflicts**:
   - Add dependency version ranges for mods beyond simple ordering
   - Implement conflict detection with user-friendly reporting
   - Create mod compatibility database/registry
   - Support conditional mod features based on other mods

### 6. Input System

1. **Device Abstraction**:

   - Create `IInputDevice` abstraction with specialized implementations
   - Support simultaneous input from multiple devices
   - Implement device hot-plugging and detection
   - Add input context system for different game states

2. **Accessibility Improvements**:
   - Detailed input remapping UI with conflict detection
   - Support for adaptive controllers and accessibility hardware
   - Add alternative input methods for common actions
   - Implement auto-detection for accessibility needs

### 7. Serialization

1. **Save Format**:

   - Define binary or JSON-based save format with compression
   - Implement save versioning with forward/backward compatibility
   - Add save verification and corruption detection
   - Support cloud save synchronization

2. **ECS Integration**:
   - Create clear serialization strategy for components and entities
   - Add serialization attributes for component fields
   - Implement delta serialization for network/save efficiency
   - Support partial world saves for large worlds

### 8. Timeline & Scope

1. **Milestone Planning**:

   - Month 1-2: Core ECS, rendering, basic gameplay loop
   - Month 3-4: Asset system, basic modding, initial content
   - Month 5-6: Advanced systems (audio, scripting), example game
   - Month 7-8: Polish, optimization, documentation, release

2. **Risk Management**:
   - Identify critical path items and potential blockers
   - Create contingency plans for technical challenges
   - Establish regular architecture review checkpoints
   - Define clear "cut line" for features if schedule slips

