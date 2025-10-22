# PokeNET Mods Directory

This directory contains mods for PokeNET. Each mod should be in its own subdirectory with a `modinfo.json` file.

## Mod Structure

```
Mods/
├── com.author.modname/           # Mod directory (use mod ID as folder name)
│   ├── modinfo.json              # Mod metadata (required)
│   ├── ModName.dll               # Mod assembly (required)
│   └── Assets/                   # Optional: Custom assets
│       ├── Textures/
│       ├── Audio/
│       └── Data/
└── README.md                     # This file
```

## modinfo.json Format

```json
{
  "id": "com.author.modname",
  "name": "Mod Display Name",
  "version": "1.0.0",
  "author": "Author Name",
  "description": "What this mod does",
  "dependencies": [
    {
      "modId": "com.other.requiredmod",
      "versionConstraint": ">=1.0.0"
    }
  ],
  "loadAfter": ["com.other.mod"],
  "loadBefore": ["com.another.mod"],
  "assemblyName": "ModName.dll"
}
```

## Creating a Mod

1. **Create a new .NET 8.0 class library project**
2. **Reference PokeNET.ModApi** for the IMod interface
3. **Implement IMod**:
```csharp
public class MyMod : IMod
{
    public string Id => "com.author.mymod";
    public string Name => "My Mod";
    public Version Version => new Version(1, 0, 0);

    public void OnLoad(IModContext context)
    {
        context.Logger.LogInformation("Loading {Name}", Name);
        // Initialize your mod here
    }

    public void OnUnload()
    {
        // Clean up resources here
    }
}
```

4. **Add Harmony patches** (optional):
```csharp
[HarmonyPatch(typeof(TargetClass), nameof(TargetClass.MethodName))]
public static class MyPatch
{
    [HarmonyPrefix]
    public static void Prefix()
    {
        // Runs before the original method
    }

    [HarmonyPostfix]
    public static void Postfix()
    {
        // Runs after the original method
    }
}
```

5. **Create modinfo.json** in your mod project
6. **Build and deploy** to `Mods/com.author.mymod/`

## Load Order

Mods are loaded in topological sort order based on:
1. **Dependencies** - Required mods load first
2. **LoadAfter** - Soft dependency, loads after specified mods if they exist
3. **LoadBefore** - Hint to load before other mods

## Asset Overrides

Mods can override base game assets by placing files in the same relative path:
- Game asset: `Content/Textures/pokemon/pikachu.png`
- Mod override: `Mods/com.author.mod/Textures/pokemon/pikachu.png`

The mod asset will be loaded instead of the base game asset.

## Accessing Game Services

Use the `IModContext` to access game services:
```csharp
public void OnLoad(IModContext context)
{
    // Logger
    context.Logger.LogInformation("Mod loaded");

    // Get services
    var eventBus = context.GetService<IEventBus>();
    var assetManager = context.GetRequiredService<IAssetManager>();

    // Mod directory
    var configPath = Path.Combine(context.ModDirectory, "config.json");
}
```

## Best Practices

1. **Use semantic versioning** (MAJOR.MINOR.PATCH)
2. **Declare all dependencies** in modinfo.json
3. **Use namespaced mod IDs** (reverse domain notation)
4. **Log important operations** using context.Logger
5. **Clean up in OnUnload()** to prevent memory leaks
6. **Test with other mods** to avoid conflicts
7. **Document Harmony patches** with clear comments
8. **Handle errors gracefully** - don't crash the game

## Example Mods

- **ExampleMod** - Demonstrates basic mod structure and Harmony patching

## Troubleshooting

**Mod not loading:**
- Check modinfo.json is valid JSON
- Verify assembly name matches modinfo.json
- Check logs for error messages

**Circular dependency error:**
- Review dependencies, loadAfter, and loadBefore
- Remove circular references

**Harmony patch not working:**
- Verify target method exists
- Check patch method signature
- Enable Harmony debug logging
