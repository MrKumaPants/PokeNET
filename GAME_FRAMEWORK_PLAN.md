# Pokémon Fan Game Framework Development Plan

This plan outlines the major phases for developing a flexible and moddable Pokémon-style game framework using MonoGame, .NET 9, and the specific libraries you've requested.

## Phase 1: Project Scaffolding & Core Setup

1.  **Project Initialization:**
    - A new .NET 9 project will be created. Since official MonoGame templates may not support .NET 9 out of the box, the process involves creating a standard console application and then manually configuring it for MonoGame.
    - The project file (`.csproj`) will be updated to include the necessary NuGet packages:
      - `MonoGame.Framework.DesktopGL`: For cross-platform (Windows, macOS, Linux) game functionality.
      - `Arch`: The foundation of our Entity-Component-System architecture.
      - `Lib.Harmony`: To enable powerful, RimWorld-style code modding.
      - `Microsoft.CodeAnalysis.CSharp.Scripting`: For the Roslyn-based C# scripting engine.
      - `DryWetMidi`: For the procedural audio system.
    - A main `Game` class will be created, serving as the entry point and root of the game loop.

## Phase 2: ECS Architecture with Arch

1.  **World and Game Loop:**
    - The main game class will initialize and own the Arch ECS `World`.
    - The standard MonoGame `Update` and `Draw` methods will be used to drive the ECS systems.
2.  **Core Components and Systems:**
    - Initial, fundamental components will be defined (e.g., `Position`, `Velocity`, `Sprite`, `Health`, `Stats`).
    - Basic systems that operate on these components will be created to handle logic like movement, rendering, and simple game state updates. This will establish a clean, data-driven architecture following SOLID and DRY principles.

## Phase 3: Custom Asset Management (No Content Pipeline)

1.  **Asset Loader:**
    - A custom `AssetManager` class will be built. Its responsibility is to load assets like textures, audio, and data files (`.json`, `.xml`) directly from the filesystem.
    - It will manage an internal cache to prevent reloading the same asset multiple times.
2.  **Moddable Asset Paths:**
    - The asset manager will be designed to search for assets in a specific order: first in any loaded mod directories, and then falling back to the base game's asset directory. This allows mods to easily override existing assets or add new ones.

## Phase 4: RimWorld-Style Modding Framework

1.  **Mod Loader:**
    - A `ModLoader` will be implemented to run on game startup. It will scan a `Mods` directory for subdirectories, each representing a single mod.
    - It will read a manifest file (e.g., `modinfo.json`) from each mod to determine its name, author, version, and dependencies.
    - It will sort mods based on their dependencies to ensure they are loaded in the correct order.
2.  **Mod Types:**
    - **Data Mods:** Mods can add or override data by including JSON or XML files. The asset system will handle loading these.
    - **Content Mods:** Mods can add or replace art and audio assets (`.png`, `.wav`, etc.).
    - **Code Mods:** The `ModLoader` will load `.dll` files from mods and execute a designated entry point. This allows mods to use **Harmony** to patch game code at runtime, enabling deep and complex modifications.

## Phase 5: Roslyn C# Scripting Engine

1.  **Scripting Host:**
    - A `ScriptingEngine` service will be created to host the Roslyn compiler.
    - It will load and execute C# script files (`.cs` or `.csx`) found in the game's data folders or in mod folders.
2.  **Scripting API:**
    - A well-defined API will be exposed to the scripts, allowing them to safely interact with the game world. For example, a script could define a Pokémon move's effect by queuing changes to the ECS world, creating new entities, or triggering events.

## Phase 6: Dynamic Audio with DryWetMidi

1.  **Procedural Music System:**
    - An `AudioManager` will be built that integrates the `DryWetMidi` library.
    - This will allow for the programmatic creation of music that can react to game state (e.g., battle intensity, player location, time of day).
    - The system will also handle playing standard pre-recorded music and sound effects.

## Phase 7: Proof of Concept and Validation

1.  **Example Mod:**
    - To ensure all systems work together seamlessly, a final proof-of-concept mod will be created. This mod will serve as an example for future modders and will validate the framework's capabilities by:
      - Adding a new creature via a JSON file.
      - Providing a custom C# script for a new ability.
      - Using Harmony to modify an existing game mechanic.
      - Including a new procedural music track for a specific event.

