# PokeNET.ModAPI

**Stable modding API for PokeNET game development**

## Overview

PokeNET.ModAPI provides a stable, versioned interface for creating game modifications. This API abstracts the underlying ECS implementation to ensure mods remain compatible across game updates.

## Features

- **Entity Management** - Spawn, destroy, and manage entities with components
- **Asset Loading** - Load and register custom textures, models, and resources
- **Event System** - Type-safe event subscription and publishing
- **World Queries** - Efficient entity queries and ECS access
- **Logging** - Scoped logging for each mod
- **JSON Serialization** - Built-in support for data persistence

## Installation

```bash
dotnet add package PokeNET.ModAPI
```

## Quick Start

```csharp
using PokeNET.ModAPI.Interfaces;
using PokeNET.ModAPI.DTOs;

public class MyMod : IMod
{
    public void Initialize(IModApi api)
    {
        // Spawn a custom entity
        var entity = api.EntityApi.SpawnEntity(new EntityDefinition
        {
            Name = "CustomPokemon",
            Tag = "pokemon",
            ComponentData = new List<ComponentData>
            {
                new ComponentData
                {
                    Type = "HealthComponent",
                    Data = new { MaxHealth = 100 }
                }
            }
        });

        // Subscribe to events
        api.EventApi.Subscribe<EntitySpawnedEvent>(e =>
        {
            api.Logger.Info($"Entity spawned: {e.EntityId}");
        });

        // Load custom assets
        var texture = api.AssetApi.LoadAsset<Texture2D>("textures/custom.png");

        api.Logger.Info("MyMod initialized!");
    }
}
```

## API Interfaces

### IModApi
Main entry point providing access to all subsystems:
- `EntityApi` - Entity and component management
- `AssetApi` - Asset loading and registration
- `EventApi` - Event system
- `WorldApi` - World queries
- `Logger` - Scoped logging

### IEntityApi
Manage entities and components:
- `SpawnEntity(definition)` - Create new entities
- `DestroyEntity(entityId)` - Remove entities
- `AddComponent<T>(entityId, data)` - Add components
- `GetComponent<T>(entityId)` - Retrieve components

### IAssetApi
Handle game assets:
- `LoadAsset<T>(path)` - Load from disk
- `RegisterAsset<T>(id, asset)` - Register in memory
- `GetAsset<T>(id)` - Retrieve registered assets

### IEventApi
Type-safe event system:
- `Subscribe<T>(handler)` - Listen for events
- `Publish<T>(eventData)` - Send events
- `Unsubscribe<T>(handler)` - Remove listeners

### IWorldApi
Query the game world:
- `QueryEntities(predicate)` - Find entities
- `GetEntitiesWithComponent<T>()` - Component-based queries
- `GetWorld()` - Direct ECS access (advanced)

## Data Transfer Objects

### EntityDefinition
Complete entity representation with metadata and components.

### ComponentData
Type-safe component data with JSON serialization support.

### ModMetadata
Mod package information for loading and dependency resolution.

## Versioning

This API follows semantic versioning (SemVer):
- **MAJOR** - Breaking changes
- **MINOR** - New features (backward compatible)
- **PATCH** - Bug fixes

Current Version: **0.1.0-alpha**

## Documentation

Full API documentation is available at: [docs.pokenet.dev](https://docs.pokenet.dev)

## License

MIT License - See LICENSE file for details

## Support

- GitHub Issues: [github.com/pokenet/pokenet/issues](https://github.com/pokenet/pokenet/issues)
- Discord: [discord.gg/pokenet](https://discord.gg/pokenet)
