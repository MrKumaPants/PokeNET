# PokeNET Architecture Quick Reference

**Last Updated:** 2025-10-22
**Research Agent:** Hive Mind Swarm
**Full Documentation:** [ARCHITECTURE_RESEARCH_FINDINGS.md](/mnt/c/Users/nate0/RiderProjects/PokeNET/docs/research/ARCHITECTURE_RESEARCH_FINDINGS.md)

---

## 📚 Access Research via Memory

All research is stored in Hive Mind coordination memory:

```bash
# Query specific topic
npx claude-flow@alpha memory query "hive/research/arch-ecs-findings" --namespace coordination
npx claude-flow@alpha memory query "hive/research/solid-principles-games" --namespace coordination

# Query all research
npx claude-flow@alpha memory query "hive/research" --namespace coordination
```

**Available Memory Keys:**
- `hive/research/summary` - Executive summary with key recommendations
- `hive/research/arch-ecs-findings` - Arch ECS architecture and best practices
- `hive/research/monogame-net9` - MonoGame .NET 9 compatibility
- `hive/research/harmony-modding` - Harmony patching for mods
- `hive/research/drywetmidi-audio` - Procedural audio with DryWetMidi
- `hive/research/roslyn-scripting` - Roslyn scripting security
- `hive/research/dependency-resolution` - Mod dependency algorithms
- `hive/research/solid-principles-games` - SOLID principles in games

---

## ⚡ Quick Recommendations

### Arch ECS
```csharp
// ✅ Component design
public record struct Position(float X, float Y);
public record struct Velocity(float Dx, float Dy);

// ✅ System queries
var query = new QueryDescription().WithAll<Position, Velocity>();
world.Query(in query, (ref Position pos, ref Velocity vel) => {
    pos.X += vel.Dx;
    pos.Y += vel.Dy;
});
```

### SOLID Principles
- **SRP:** One component = one aspect (Position, Health, Sprite)
- **OCP:** Add components without modifying systems
- **LSP:** IUpdateSystem, IRenderSystem interfaces
- **ISP:** Small focused interfaces, not monolithic
- **DIP:** Inject ILogger, IAssetManager dependencies

### Modding (Harmony)
```csharp
var harmony = new Harmony("com.author.modname");

[HarmonyPatch(typeof(BattleSystem), "CalculateDamage")]
static void Postfix(ref int __result, Pokemon attacker) {
    if (attacker.HasAbility("SuperStrength"))
        __result *= 2;
}
```

### Scripting (Roslyn)
```csharp
// ✅ Minimal, safe API
public interface IScriptContext {
    IReadOnlyPokemon Attacker { get; }
    IReadOnlyPokemon Defender { get; }
    IEventQueue Events { get; }
}

// ✅ Pre-compile, execute many
var script = CSharpScript.Create<int>(code, globalsType: typeof(IScriptContext));
script.Compile();
var result = await script.RunAsync(context);
```

### Audio (DryWetMidi)
```csharp
// ✅ Procedural pattern generation
var pattern = new PatternBuilder()
    .SetNoteLength(MusicalTimeSpan.Eighth)
    .Note(NoteName.C)
    .Note(NoteName.E)
    .Note(NoteName.G)
    .Build();

var midiFile = pattern.ToFile(TempoMap.Default);
```

### Mod Loading (Dependency Resolution)
```csharp
// ✅ Kahn's algorithm for topological sort
// ✅ Version range validation: ">=1.0.0 <2.0.0"
// ✅ Optional dependency support with graceful fallback
// ✅ Cycle detection with clear error messages
```

---

## 📁 Project Structure

```
PokeNET/
├── PokeNET.Core/              # Cross-platform (.NET 9)
│   ├── Game1.cs               # Main game loop
│   ├── Systems/               # ECS systems
│   └── Components/            # ECS components
├── PokeNET.DesktopGL/         # Platform runner (.NET 9)
│   ├── Program.cs             # DI/Host entry point
│   └── Content/               # MGCB content
├── PokeNET.Domain/            # Pure C# (no MonoGame)
│   ├── Models/
│   └── Contracts/
├── PokeNET.ModApi/            # Public API (.NET Standard 2.1)
│   └── Interfaces/
├── PokeNET.Tests/             # Unit tests
└── docs/
    └── research/              # This directory
```

**Dependency Flow:**
```
DesktopGL → Core → Domain
          ↓
        ModApi → Domain
```

---

## 🚀 Implementation Phases

1. **Phase 1:** Project scaffolding with .NET 9 + MonoGame
2. **Phase 2:** Implement Arch ECS core systems
3. **Phase 3:** Build Harmony-based modding infrastructure
4. **Phase 4:** Integrate Roslyn scripting engine
5. **Phase 5:** Add DryWetMidi audio system
6. **Phase 6:** Create comprehensive example mod

---

## 📦 Key NuGet Packages

```xml
<!-- Core Framework -->
<PackageReference Include="MonoGame.Framework.DesktopGL" Version="3.8.1.*" />
<PackageReference Include="Arch" Version="*" />

<!-- Modding -->
<PackageReference Include="Lib.Harmony" Version="*" />

<!-- Scripting -->
<PackageReference Include="Microsoft.CodeAnalysis.CSharp.Scripting" Version="*" />

<!-- Audio -->
<PackageReference Include="Melanchall.DryWetMidi" Version="*" />

<!-- DI and Logging -->
<PackageReference Include="Microsoft.Extensions.Hosting" Version="*" />
<PackageReference Include="Microsoft.Extensions.Logging" Version="*" />
<PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="*" />
```

---

## ⚠️ Critical Considerations

### .NET 9 + MonoGame
- No official templates yet; manual setup required
- MonoGame targets .NET Standard 2.1, works with .NET 9
- Test thoroughly on all platforms

### Roslyn Security
- **Scripts execute in-process with full permissions**
- No native sandboxing available
- Use minimal globals API
- Validate source before compilation
- Implement timeouts and cancellation

### Harmony Safety
- Multiple mods can patch same method
- Use unique Harmony IDs
- Document all patches
- Test with multiple mods

### Performance
- Pre-compile scripts at startup
- Cache generated MIDI patterns
- Use Arch's chunk-based storage
- Monitor frame times (target: 16.67ms for 60 FPS)

---

## 🧠 For Swarm Agents

**Before implementing features:**
1. Query relevant research from memory
2. Follow SOLID principles from `hive/research/solid-principles-games`
3. Use patterns from architecture research
4. Coordinate through memory system

**Key coordination memory patterns:**
```bash
# Store implementation decisions
npx claude-flow@alpha memory store hive/implementation/[feature] '[json]' --namespace coordination

# Query prior decisions
npx claude-flow@alpha memory query "hive/implementation" --namespace coordination

# Share findings
npx claude-flow@alpha hooks notify --message "[status update]"
```

---

**Research Complete ✅**
All findings available in memory and full documentation.
