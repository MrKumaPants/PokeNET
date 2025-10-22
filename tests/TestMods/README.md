# Test Mods Directory

This directory contains fixture mods used for testing the PokeNET modding system.

## Test Mods

### SimpleMod
A minimal mod with no dependencies. Used for basic discovery and loading tests.

### ModWithDependency
Depends on SimpleMod. Tests dependency resolution and load ordering.

### ModWithConflict
Designed to conflict with other mods. Tests conflict detection and resolution.

### DataMod
Contains JSON data files for testing data overrides.
- `Data/creatures.json` - Test creature data

### ContentMod
Contains content files (sprites, audio) for testing content overrides.
- `Content/sprite.png` - Test sprite placeholder

## Usage

These test mods are used by the test suite in:
- `tests/Unit/Modding/ModDiscoveryTests.cs`
- `tests/Unit/Modding/DependencyResolutionTests.cs`
- `tests/Unit/Modding/ModLoadingTests.cs`
- `tests/Integration/Modding/AssetOverrideTests.cs`
- `tests/Integration/Modding/ModSystemIntegrationTests.cs`

## Structure

Each test mod should have:
- `modinfo.json` - Required manifest file
- Optional: `README.md` - Description of the test mod's purpose
- Optional: `Data/` - JSON data files
- Optional: `Content/` - Asset files
- Optional: `*.dll` - Compiled code assemblies

## Adding New Test Mods

1. Create a new directory under `TestMods/`
2. Add a `modinfo.json` with required fields
3. Add any data or content files needed for tests
4. Document the purpose in this README
5. Reference in relevant test files
