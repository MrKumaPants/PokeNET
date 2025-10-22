# Simple Code Mod Example

This example demonstrates how to create a code mod using Harmony to patch game behavior at runtime.

## What This Mod Does

Creates a simple Harmony-based mod that:
- Doubles all damage dealt in battles
- Logs battle events to console
- Adds a custom message when critical hits occur

## Prerequisites

- Visual Studio 2022 or JetBrains Rider
- .NET 6.0 SDK or later
- PokeNET installed
- Basic C# knowledge

## File Structure

```
simple-code-mod/
├── README.md                    # This file
├── modinfo.json                 # Mod manifest
├── SimpleCodeMod.sln            # Visual Studio solution
├── SimpleCodeMod.csproj         # Project file
└── Source/
    ├── SimpleCodeMod.cs         # Main mod class
    └── Patches/
        ├── DamagePatch.cs       # Damage modification patch
        └── CriticalPatch.cs     # Critical hit message patch
```

## Step-by-Step Guide

### Step 1: Create Project

1. **Open Visual Studio/Rider**

2. **Create new Class Library project:**
   - File → New → Project
   - Choose "Class Library (.NET 6.0)"
   - Name: "SimpleCodeMod"
   - Location: `Mods/SimpleCodeMod/`

### Step 2: Install Dependencies

Add NuGet packages. Edit `SimpleCodeMod.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net6.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <!-- Harmony for runtime patching -->
    <PackageReference Include="Lib.Harmony" Version="2.2.2" />
  </ItemGroup>

  <ItemGroup>
    <!-- PokeNET references -->
    <Reference Include="PokeNET.Core">
      <HintPath>../../PokeNET.Core.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="PokeNET.ModApi">
      <HintPath>../../PokeNET.ModApi.dll</HintPath>
      <Private>false</Private>
    </Reference>
  </ItemGroup>

  <!-- Copy DLL to Assemblies folder on build -->
  <Target Name="PostBuild" AfterTargets="PostBuildEvent">
    <MakeDir Directories="$(ProjectDir)Assemblies" Condition="!Exists('$(ProjectDir)Assemblies')" />
    <Copy SourceFiles="$(TargetPath)" DestinationFolder="$(ProjectDir)Assemblies" />
  </Target>

</Project>
```

### Step 3: Create Main Mod Class

Create `Source/SimpleCodeMod.cs`:

```csharp
using System;
using HarmonyLib;
using PokeNET.ModApi;

namespace SimpleCodeMod
{
    /// <summary>
    /// Main mod class implementing IMod interface.
    /// </summary>
    public class SimpleCodeMod : IMod
    {
        // Mod identification
        public string Id => "com.example.simplecodemod";
        public string Name => "Simple Code Mod";
        public Version Version => new Version(1, 0, 0);

        // Harmony instance for patching
        private Harmony _harmony;

        // Store context for later use
        private IModContext _context;

        /// <summary>
        /// Called when mod is loaded by PokeNET.
        /// </summary>
        public void OnLoad(IModContext context)
        {
            _context = context;

            try
            {
                // Create Harmony instance with unique ID
                _harmony = new Harmony(Id);

                // Apply all patches in this assembly
                _harmony.PatchAll();

                // Subscribe to game events
                context.Events.OnBattleStart += OnBattleStart;
                context.Events.OnDamageDealt += OnDamageDealt;

                context.Logger.Info($"{Name} v{Version} loaded successfully!");
                context.Logger.Info("All Harmony patches applied.");
            }
            catch (Exception ex)
            {
                context.Logger.Error($"Failed to load {Name}", ex);
            }
        }

        /// <summary>
        /// Called when mod is unloaded.
        /// </summary>
        public void OnUnload()
        {
            try
            {
                // Remove all Harmony patches
                _harmony?.UnpatchAll(Id);

                // Unsubscribe from events
                if (_context != null)
                {
                    _context.Events.OnBattleStart -= OnBattleStart;
                    _context.Events.OnDamageDealt -= OnDamageDealt;
                }

                _context?.Logger.Info($"{Name} unloaded successfully.");
            }
            catch (Exception ex)
            {
                _context?.Logger.Error($"Error unloading {Name}", ex);
            }
        }

        /// <summary>
        /// Event handler for battle start.
        /// </summary>
        private void OnBattleStart(object sender, BattleEventArgs e)
        {
            _context.Logger.Info($"Battle started: {e.PlayerCreature.Name} vs {e.OpponentCreature.Name}");
        }

        /// <summary>
        /// Event handler for damage dealt.
        /// </summary>
        private void OnDamageDealt(object sender, DamageEventArgs e)
        {
            _context.Logger.Info($"{e.Attacker.Name} dealt {e.Damage} damage to {e.Defender.Name}");

            if (e.IsCritical)
            {
                _context.Logger.Info("Critical hit!");
            }
        }
    }
}
```

### Step 4: Create Damage Patch

Create `Source/Patches/DamagePatch.cs`:

```csharp
using HarmonyLib;
using PokeNET.Core.Battle;

namespace SimpleCodeMod.Patches
{
    /// <summary>
    /// Patch to double all damage dealt.
    /// </summary>
    [HarmonyPatch(typeof(BattleSystem), "CalculateDamage")]
    public class DamagePatch
    {
        /// <summary>
        /// Postfix runs AFTER the original method.
        /// We use __result to modify the return value.
        /// </summary>
        /// <param name="__result">The damage value calculated by original method</param>
        static void Postfix(ref int __result)
        {
            // Double the damage
            __result *= 2;
        }
    }
}
```

**Explanation:**
- `[HarmonyPatch]` tells Harmony what method to patch
- `Postfix` runs after the original method
- `__result` is a special parameter containing the return value
- `ref` allows us to modify the return value

### Step 5: Create Critical Hit Patch

Create `Source/Patches/CriticalPatch.cs`:

```csharp
using HarmonyLib;
using PokeNET.Core.Battle;
using System;

namespace SimpleCodeMod.Patches
{
    /// <summary>
    /// Patch to display custom message on critical hits.
    /// </summary>
    [HarmonyPatch(typeof(BattleSystem), "CheckCriticalHit")]
    public class CriticalPatch
    {
        /// <summary>
        /// Prefix runs BEFORE the original method.
        /// We can access method parameters and skip the original method if needed.
        /// </summary>
        static void Prefix(Creature attacker, Move move)
        {
            Console.WriteLine($"{attacker.Name} is attacking with {move.Name}!");
        }

        /// <summary>
        /// Postfix runs AFTER the original method.
        /// We use __result to check if it was a critical hit.
        /// </summary>
        static void Postfix(bool __result, Creature attacker)
        {
            if (__result)
            {
                Console.WriteLine($"A critical hit from {attacker.Name}!");
            }
        }
    }
}
```

**Explanation:**
- `Prefix` runs before the method
- We can access method parameters
- `Postfix` checks the return value
- Both can run together on the same method

### Step 6: Create modinfo.json

Create `modinfo.json` in the project root:

```json
{
  "id": "com.example.simplecodemod",
  "name": "Simple Code Mod",
  "version": "1.0.0",
  "author": "YourName",
  "description": "Doubles damage and adds custom battle messages using Harmony",
  "dependencies": [
    {
      "id": "com.pokenet.core",
      "version": ">=1.0.0"
    }
  ]
}
```

### Step 7: Build and Test

1. **Build the project:**
   - Build → Build Solution (Ctrl+Shift+B)
   - DLL will be copied to `Assemblies/` folder automatically

2. **Verify file structure:**
   ```
   Mods/SimpleCodeMod/
   ├── modinfo.json
   └── Assemblies/
       └── SimpleCodeMod.dll
   ```

3. **Launch PokeNET:**
   - Mod should appear in mod manager
   - Check logs for "loaded successfully" message

4. **Test in-game:**
   - Start a battle
   - Damage should be doubled
   - Check console for custom messages

## Advanced Harmony Patterns

### Skipping Original Method

Use `Prefix` to skip the original method entirely:

```csharp
[HarmonyPatch(typeof(CaptureSystem), "CalculateCatchRate")]
public class AutoCatchPatch
{
    static bool Prefix(ref float __result, Item ball)
    {
        if (ball.Id == "master_ball")
        {
            __result = 1.0f; // 100% catch rate
            return false;    // Skip original method
        }
        return true; // Run original method
    }
}
```

### Accessing Private Fields

Use Harmony's Traverse to access private fields:

```csharp
static void Postfix(Creature creature)
{
    // Access private field
    var hiddenPower = Traverse.Create(creature)
        .Field("_hiddenPower")
        .GetValue<int>();

    Console.WriteLine($"Hidden power: {hiddenPower}");
}
```

### Using State Between Prefix and Postfix

Use `__state` to pass data:

```csharp
[HarmonyPatch(typeof(BattleSystem), "ExecuteTurn")]
public class TurnTimingPatch
{
    // Prefix saves start time
    static void Prefix(out long __state)
    {
        __state = DateTime.Now.Ticks;
    }

    // Postfix calculates elapsed time
    static void Postfix(long __state)
    {
        var elapsed = DateTime.Now.Ticks - __state;
        var ms = elapsed / TimeSpan.TicksPerMillisecond;
        Console.WriteLine($"Turn took {ms}ms");
    }
}
```

### Transpiler (IL Code Modification)

For advanced users - modify the IL code directly:

```csharp
using System.Collections.Generic;
using System.Reflection.Emit;

[HarmonyPatch(typeof(Experience), "GainExp")]
public class ExpMultiplierTranspiler
{
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        foreach (var instruction in instructions)
        {
            // Find where experience is loaded
            if (instruction.opcode == OpCodes.Ldarg_1)
            {
                yield return instruction;
                yield return new CodeInstruction(OpCodes.Ldc_I4_2); // Load 2
                yield return new CodeInstruction(OpCodes.Mul);       // Multiply
            }
            else
            {
                yield return instruction;
            }
        }
    }
}
```

## Debugging

### Enable Harmony Debug Mode

Add to your `OnLoad` method:

```csharp
public void OnLoad(IModContext context)
{
    // Enable Harmony debugging
    Harmony.DEBUG = true;

    _harmony = new Harmony(Id);
    _harmony.PatchAll();
}
```

### Log Patch Details

```csharp
public void OnLoad(IModContext context)
{
    _harmony = new Harmony(Id);
    _harmony.PatchAll();

    // Log all applied patches
    var patches = Harmony.GetAllPatchedMethods();
    foreach (var method in patches)
    {
        context.Logger.Debug($"Patched: {method.Name}");
    }
}
```

### Common Issues

**Patch Not Applying:**
- Check method name and signature
- Verify target class exists
- Check for typos in `HarmonyPatch` attribute
- Enable Harmony.DEBUG

**Game Crashes:**
- Check for null references
- Verify parameter types match
- Don't modify critical game state
- Use try-catch in patches

**Unexpected Behavior:**
- Check patch order with other mods
- Use `[HarmonyPriority]` attribute
- Verify `__result` is correct type
- Check if prefix returns false

## Best Practices

1. **Always use try-catch** in `OnLoad` and `OnUnload`
2. **Log important events** for debugging
3. **Clean up resources** in `OnUnload`
4. **Test with other mods** to check compatibility
5. **Use meaningful names** for patch classes
6. **Document your patches** with comments
7. **Handle null cases** safely
8. **Don't patch frequently-called methods** (performance)

## Next Steps

Once you've mastered basic code mods:

1. **Complex Harmony patches** - Transpilers and reverse patches
2. **Event-driven mods** - Use game events extensively
3. **Inter-mod APIs** - Create APIs for other mods
4. **Custom game systems** - Add entirely new mechanics

## Resources

- **Modding Guide**: `/docs/modding/phase4-modding-guide.md`
- **API Reference**: `/docs/api/modapi-phase4.md`
- **Harmony Documentation**: https://harmony.pardeike.net/
- **More Examples**: `/docs/examples/`

## Build Script (Optional)

Create `build.bat` for easy building:

```batch
@echo off
echo Building SimpleCodeMod...
dotnet build SimpleCodeMod.csproj -c Release
echo Build complete! DLL is in Assemblies/
pause
```

Or `build.sh` for Linux/Mac:

```bash
#!/bin/bash
echo "Building SimpleCodeMod..."
dotnet build SimpleCodeMod.csproj -c Release
echo "Build complete! DLL is in Assemblies/"
```
