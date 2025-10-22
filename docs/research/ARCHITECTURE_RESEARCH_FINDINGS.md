# PokeNET Framework Architecture Research Findings

**Research Date:** 2025-10-22
**Researcher:** Research Agent (Hive Mind Swarm)
**Purpose:** Comprehensive analysis of best practices for PokeNET game framework implementation

---

## Executive Summary

This document compiles research findings on implementing a moddable Pokémon-style game framework using:
- **Arch ECS** for high-performance entity-component architecture
- **MonoGame** on .NET 9 for cross-platform game development
- **Harmony** for RimWorld-style runtime code modding
- **Roslyn Scripting** for C# scripting capabilities
- **DryWetMidi** for procedural audio generation

All findings are stored in the Hive Mind coordination memory under the `hive/research/` namespace for swarm agent access.

---

## 1. Arch ECS Architecture

### Core Architecture

**Design Pattern:** Archetype & Chunks
**Storage:** 16KB chunk-based organization for optimal cache efficiency
**Philosophy:** "Bare minimum - no overengineering, no hidden costs"

### Key Characteristics

- **Extremely compact Entity representation** minimizes memory footprint
- **Cache-optimized storage** with archetype-based component grouping
- **Multithreaded query support** for parallel processing
- **AOT-friendly compilation** for maximum runtime performance
- **CommandBuffer system** for safe deferred world modifications

### Component Design Best Practices

```csharp
// ✅ Recommended: Simple record structs
public record struct Position(float X, float Y);
public record struct Velocity(float Dx, float Dy);
public record struct Health(int Current, int Max);
public record struct Stats(int Attack, int Defense, int Speed);

// ❌ Avoid: Complex classes with behavior
// Components should be pure data
```

**Rationale:**
- Structs avoid heap allocations
- Records provide value semantics and equality
- Minimal size optimizes chunk storage
- Pure data enables maximum ECS efficiency

### System Organization

```csharp
// Query-based system execution
var query = new QueryDescription()
    .WithAll<Position, Velocity>()
    .WithNone<Frozen>();

world.Query(in query, (Entity entity, ref Position pos, ref Velocity vel) => {
    pos.X += vel.Dx;
    pos.Y += vel.Dy;
});
```

**Best Practices:**
- Use bulk operations over individual entity queries
- Leverage `WithAll`, `WithAny`, `WithNone` for precise queries
- Prefer lambda-based queries for readability
- Use CommandBuffers for modifications during iteration

### Performance Optimization

| Technique | Benefit | Implementation |
|-----------|---------|----------------|
| Chunk Storage | 16KB cache-aligned chunks | Automatic via Arch |
| Bulk Operations | Reduced iteration overhead | `world.Query()` with batches |
| Multithreading | Parallel query execution | Thread-safe query API |
| AOT Compilation | No runtime JIT overhead | Native AOT compatible |

### MonoGame Integration

```csharp
// Game1.cs - Main game loop integration
public class Game1 : Game
{
    private World _world;
    private List<IUpdateSystem> _updateSystems;
    private List<IRenderSystem> _renderSystems;

    protected override void Initialize()
    {
        _world = World.Create();
        _updateSystems = new List<IUpdateSystem>
        {
            new MovementSystem(_world),
            new PhysicsSystem(_world),
            new AISystem(_world)
        };
        _renderSystems = new List<IRenderSystem>
        {
            new SpriteRenderSystem(_world, GraphicsDevice)
        };
    }

    protected override void Update(GameTime gameTime)
    {
        foreach (var system in _updateSystems)
            system.Update(gameTime.ElapsedGameTime.TotalSeconds);
    }

    protected override void Draw(GameTime gameTime)
    {
        foreach (var system in _renderSystems)
            system.Render(gameTime.ElapsedGameTime.TotalSeconds);
    }
}
```

---

## 2. SOLID Principles in Game Architecture

### Single Responsibility Principle (SRP)

**ECS Application:**
- Each component represents exactly one aspect of an entity
- Each system handles exactly one concern

```csharp
// ✅ Good: Single responsibility components
public record struct Position(float X, float Y);
public record struct Sprite(Texture2D Texture, Rectangle Source);
public record struct Health(int Current, int Max);

// ❌ Bad: God component
public class GameObject
{
    public Vector2 Position;
    public Texture2D Sprite;
    public int Health;
    public void Update() { /* too many responsibilities */ }
}
```

### Open/Closed Principle (OCP)

**ECS Application:**
- Add new components without modifying existing systems
- Systems filter entities by component composition

```csharp
// Adding new behavior without changing existing code
public record struct Poisoned(float DamagePerSecond, float Duration);

// Existing HealthSystem doesn't change
// New PoisonSystem handles Poisoned component
public class PoisonSystem : IUpdateSystem
{
    public void Update(double deltaTime)
    {
        var query = new QueryDescription()
            .WithAll<Health, Poisoned>();

        _world.Query(in query, (ref Health health, ref Poisoned poison) => {
            health.Current -= (int)(poison.DamagePerSecond * deltaTime);
        });
    }
}
```

**Modding Application:**
- Harmony patches extend behavior without source code modification
- Mods add new systems and components through plugin architecture

### Liskov Substitution Principle (LSP)

**Interface Contracts:**

```csharp
public interface IUpdateSystem
{
    void Update(double deltaTime);
}

public interface IRenderSystem
{
    void Render(double deltaTime);
}

// All systems are interchangeable through interfaces
public class Game1 : Game
{
    private List<IUpdateSystem> _systems;

    protected override void Update(GameTime gameTime)
    {
        foreach (var system in _systems)
            system.Update(gameTime.ElapsedGameTime.TotalSeconds);
    }
}
```

### Interface Segregation Principle (ISP)

**Focused Interfaces:**

```csharp
// ✅ Good: Small, focused interfaces
public interface IReadSystem
{
    void QueryEntities();
}

public interface IWriteSystem
{
    void ModifyComponents();
}

// ❌ Bad: Monolithic interface
public interface IGameSystem
{
    void Initialize();
    void Update();
    void Render();
    void Shutdown();
    void OnEntityCreated();
    void OnEntityDestroyed();
}
```

**Mod API Application:**
```csharp
// Minimal API surface for mods
public interface IModContext
{
    IEntityFactory EntityFactory { get; }
    IEventBus Events { get; }
}

// NOT exposing entire World or internal systems
```

### Dependency Inversion Principle (DIP)

**Dependency Injection:**

```csharp
// ✅ Good: Depend on abstractions
public class MovementSystem : IUpdateSystem
{
    private readonly World _world;
    private readonly ILogger<MovementSystem> _logger;
    private readonly IPhysicsEngine _physics;

    public MovementSystem(
        World world,
        ILogger<MovementSystem> logger,
        IPhysicsEngine physics)
    {
        _world = world;
        _logger = logger;
        _physics = physics;
    }
}

// ❌ Bad: Depend on concrete types
public class MovementSystem
{
    private PhysicsEngine _physics = new PhysicsEngine();
    private ConsoleLogger _logger = new ConsoleLogger();
}
```

### Common Patterns for Games

| Pattern | Purpose | ECS Application |
|---------|---------|-----------------|
| **Factory** | Consistent entity creation | EntityFactory for spawning creatures |
| **Strategy** | Swappable algorithms | AI behavior trees, battle calculators |
| **Observer** | Decoupled communication | Event bus for system communication |
| **Command** | Input handling | Command pattern for game actions |
| **Repository** | Data access abstraction | Save/load system interface |

---

## 3. MonoGame + .NET 9 Compatibility

### Current Status (January 2025)

**Official Support:**
- MonoGame targets .NET Standard 2.1
- Latest version: 3.8.4.1 (Google/iOS policy updates)
- No official .NET 9 templates available yet

**Platform Support:**
- Desktop: Windows, macOS, Linux
- Mobile: Android, iOS, iPadOS
- Consoles: PlayStation 4/5, Xbox One, Nintendo Switch (requires authorization)

### .NET 9 Migration Strategy

**Recommended Approach:**

1. **Create .NET 9 Console Application First**
```bash
dotnet new console -n PokeNET.DesktopGL -f net9.0
```

2. **Manually Add MonoGame References**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="MonoGame.Framework.DesktopGL" Version="3.8.1.*" />
    <PackageReference Include="MonoGame.Content.Builder.Task" Version="3.8.1.*" />
  </ItemGroup>
</Project>
```

3. **Test Compatibility**
- MonoGame.Framework.DesktopGL should work with .NET 9
- Content Pipeline may require additional configuration
- Test thoroughly on target platforms

### Project Structure (Following GAME_FRAMEWORK_PLAN.md)

```
PokeNET/
├── PokeNET.Core/              # Cross-platform game logic (.NET 9)
│   ├── Game1.cs               # Main game class
│   ├── Systems/               # ECS systems
│   └── Components/            # ECS components
├── PokeNET.DesktopGL/         # Platform runner (DesktopGL, .NET 9)
│   ├── Program.cs             # Entry point with DI/Host
│   └── Content/               # MGCB content project
├── PokeNET.WindowsDX/         # Optional Windows DirectX runner
├── PokeNET.Domain/            # Pure C# domain logic (no MonoGame)
│   ├── Models/
│   └── Contracts/
├── PokeNET.ModApi/            # Public mod API (.NET Standard 2.1)
│   └── Interfaces/
└── PokeNET.Tests/             # Unit tests
```

**Dependency Flow:**
```
DesktopGL/WindowsDX → Core → Domain
                    ↓
                  ModApi → Domain
```

### Tooling Support

| Editor | .NET 9 Support | MonoGame Support | Recommendation |
|--------|---------------|------------------|----------------|
| Visual Studio 2022 | ✅ Full | ✅ Full | **Recommended** |
| Visual Studio Code | ✅ Full | ⚠️ Manual setup | Good alternative |
| JetBrains Rider | ✅ Full | ✅ Full | Excellent choice |

### Known Considerations

- **Content Pipeline:** May need manual `.mgcb` configuration
- **Platform Runtime:** DesktopGL uses OpenGL, WindowsDX uses DirectX
- **NuGet Restore:** Ensure package sources include MonoGame feed
- **Nullable Reference Types:** Enable for .NET 9, handle warnings in MonoGame code

---

## 4. RimWorld-Style Modding with Harmony

### Harmony Core Concepts

**Purpose:** Runtime monkey patching for C# applications
**License:** MIT
**Package:** `Lib.Harmony` (recommended merged assembly)
**Requirement:** Strong C# Reflection knowledge essential

### Key Design Characteristics

**Multi-Mod Support:**
- Designed for multiple mods patching the same code simultaneously
- Harmony manages patch order and conflict resolution
- Multiple prefixes/postfixes/transpilers can stack on one method

**Safety:**
- No direct DLL modification reduces risk
- Changes are in-memory at runtime
- Easy to disable problematic mods

### Basic Patching Examples

```csharp
using HarmonyLib;

// 1. Create Harmony instance with unique ID
var harmony = new Harmony("com.yourname.pokenet.modname");

// 2. Patch specific method
[HarmonyPatch(typeof(BattleSystem), "CalculateDamage")]
public class BattleSystem_CalculateDamage_Patch
{
    // Prefix: runs before original method
    static bool Prefix(ref int __result, Pokemon attacker, Pokemon defender)
    {
        // Return false to skip original method
        // Return true to continue to original
        return true;
    }

    // Postfix: runs after original method
    static void Postfix(ref int __result, Pokemon attacker, Pokemon defender)
    {
        // Modify result after original calculation
        if (attacker.HasAbility("SuperStrength"))
            __result *= 2;
    }

    // Transpiler: modify IL code (advanced)
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        // Modify method IL code
        return instructions;
    }
}

// 3. Apply all patches in assembly
harmony.PatchAll();
```

### Mod Loader Integration

```csharp
public class ModLoader
{
    private List<LoadedMod> _loadedMods = new();

    public void LoadMods(string modsDirectory)
    {
        foreach (var modDir in Directory.GetDirectories(modsDirectory))
        {
            var manifest = LoadManifest(Path.Combine(modDir, "modinfo.json"));

            // Load DLL if present
            var dllPath = Path.Combine(modDir, "Assemblies", $"{manifest.Id}.dll");
            if (File.Exists(dllPath))
            {
                var assembly = Assembly.LoadFrom(dllPath);

                // Find and instantiate mod entry point
                var modType = assembly.GetTypes()
                    .FirstOrDefault(t => typeof(IMod).IsAssignableFrom(t));

                if (modType != null)
                {
                    var mod = (IMod)Activator.CreateInstance(modType);

                    // Mod applies Harmony patches in OnLoad
                    mod.OnLoad(manifest);

                    _loadedMods.Add(new LoadedMod(manifest, mod, assembly));
                }
            }
        }
    }
}

public interface IMod
{
    void OnLoad(ModManifest manifest);
    void OnUnload();
}
```

### Best Practices

**Patch Design:**
- Use unique Harmony IDs to avoid conflicts (`com.author.game.modname`)
- Document all patches clearly for other modders
- Test with multiple mods loaded simultaneously
- Provide clear error messages when patches fail

**Safety Considerations:**
- Validate input parameters in patches
- Handle null references gracefully
- Log patch applications for debugging
- Provide option to disable specific patches

**Performance:**
- Minimize work in prefix/postfix methods
- Cache reflection results
- Avoid patching high-frequency methods unless necessary
- Use transpilers for complex IL modifications (advanced)

**Compatibility:**
- Check for other mods' patches before applying
- Use `[HarmonyPriority]` to control patch order
- Provide compatibility patches for popular mods
- Version your patch signatures

### Conflict Resolution Strategy

```csharp
// Check if method is already patched
var patches = Harmony.GetPatchInfo(originalMethod);
if (patches != null)
{
    _logger.LogWarning(
        $"Method {originalMethod.Name} already patched by: " +
        string.Join(", ", patches.Owners));
}

// Apply patch with priority
[HarmonyPatch(typeof(TargetClass), "MethodName")]
[HarmonyPriority(Priority.High)] // or Priority.Low, Priority.First, Priority.Last
public class MyPatch { }
```

---

## 5. Roslyn C# Scripting Security

### Scripting API Overview

**Core Class:** `Microsoft.CodeAnalysis.CSharp.Scripting.CSharpScript`
**Package:** `Microsoft.CodeAnalysis.CSharp.Scripting`
**Key Methods:**
- `EvaluateAsync()` - Execute expressions and return results
- `RunAsync()` - Run code preserving state
- `Create()` - Build reusable compiled scripts

### Basic Usage

```csharp
using Microsoft.CodeAnalysis.CSharp.Scripting;

// Simple expression evaluation
int result = await CSharpScript.EvaluateAsync<int>("1 + 2");

// With globals (parameters)
public class ScriptGlobals
{
    public Pokemon Attacker { get; set; }
    public Pokemon Defender { get; set; }
    public Random Random { get; set; }
}

var globals = new ScriptGlobals
{
    Attacker = player.ActivePokemon,
    Defender = enemy.ActivePokemon,
    Random = new Random()
};

int damage = await CSharpScript.EvaluateAsync<int>(
    "Attacker.Attack * 2 - Defender.Defense + Random.Next(10)",
    globals: globals);
```

### Performance Optimization

```csharp
// Compile once, execute many times
var script = CSharpScript.Create<int>(
    "Attacker.Attack * 2 - Defender.Defense",
    globalsType: typeof(ScriptGlobals));

script.Compile(); // Pre-compile

// Execute with different parameters
foreach (var battle in battles)
{
    var globals = new ScriptGlobals { Attacker = battle.P1, Defender = battle.P2 };
    var result = await script.RunAsync(globals);
    int damage = result.ReturnValue;
}
```

**Performance Tips:**
- Compile scripts once at startup, reuse for multiple executions
- Use delegates for frequently executed scripts
- Consider script caching based on content hash
- Implement timeout/cancellation for long-running scripts

### Security Considerations

**Critical Understanding:** Roslyn scripts execute **in-process** with **full application permissions**. There is **no native sandboxing**.

**Security Strategies:**

#### 1. API Surface Restriction (Minimal Globals)

```csharp
// ✅ Good: Minimal, focused API
public class SafeScriptGlobals
{
    public IReadOnlyPokemon Attacker { get; }
    public IReadOnlyPokemon Defender { get; }
    public IScriptLogger Log { get; }

    // No file system access
    // No network access
    // No reflection access
    // No dangerous APIs
}

// ❌ Bad: Exposing too much
public class UnsafeScriptGlobals
{
    public Game GameInstance { get; set; } // Full game access!
    public FileSystem FS { get; set; }     // File system access!
}
```

#### 2. AssemblyLoadContext Isolation

```csharp
public class ScriptExecutor
{
    private AssemblyLoadContext _scriptContext;

    public ScriptExecutor()
    {
        _scriptContext = new AssemblyLoadContext("ScriptContext", isCollectible: true);
    }

    public async Task<T> ExecuteScriptAsync<T>(string code, object globals)
    {
        // Scripts load in isolated context
        // Can unload entire context if needed
        var script = CSharpScript.Create<T>(code, globalsType: globals.GetType());
        return (await script.RunAsync(globals)).ReturnValue;
    }

    public void Unload()
    {
        _scriptContext.Unload();
    }
}
```

#### 3. Source Code Validation

```csharp
public class ScriptValidator
{
    private static readonly string[] ForbiddenNamespaces = new[]
    {
        "System.IO",
        "System.Net",
        "System.Reflection",
        "System.Runtime.InteropServices"
    };

    public bool IsScriptSafe(string code)
    {
        // Basic validation: check for forbidden namespaces
        foreach (var ns in ForbiddenNamespaces)
        {
            if (code.Contains($"using {ns}"))
                return false;
        }

        // Additional checks:
        // - No DllImport
        // - No Reflection usage
        // - No Process.Start

        return true;
    }
}
```

#### 4. Timeout and Resource Limits

```csharp
public async Task<T> ExecuteScriptWithTimeoutAsync<T>(
    string code,
    object globals,
    TimeSpan timeout)
{
    using var cts = new CancellationTokenSource(timeout);

    var script = CSharpScript.Create<T>(code, globalsType: globals.GetType());

    try
    {
        var result = await script.RunAsync(globals, cts.Token);
        return result.ReturnValue;
    }
    catch (OperationCanceledException)
    {
        throw new ScriptTimeoutException($"Script execution exceeded {timeout}");
    }
}
```

### Recommended Architecture

```csharp
public interface IScriptingEngine
{
    Task<T> ExecuteAsync<T>(string script, IScriptContext context);
    Script<T> CompileAsync<T>(string script);
}

public interface IScriptContext
{
    IReadOnlyPokemon Attacker { get; }
    IReadOnlyPokemon Defender { get; }
    IEventQueue Events { get; }
    IScriptLogger Logger { get; }
}

public class SafeScriptingEngine : IScriptingEngine
{
    private readonly ILogger<SafeScriptingEngine> _logger;
    private readonly ScriptValidator _validator;
    private readonly TimeSpan _timeout;

    public async Task<T> ExecuteAsync<T>(string script, IScriptContext context)
    {
        // 1. Validate script source
        if (!_validator.IsScriptSafe(script))
            throw new UnsafeScriptException("Script contains forbidden operations");

        // 2. Compile with restricted globals type
        var compiled = CSharpScript.Create<T>(
            script,
            globalsType: typeof(IScriptContext));

        // 3. Execute with timeout
        using var cts = new CancellationTokenSource(_timeout);

        try
        {
            var result = await compiled.RunAsync(context, cts.Token);
            return result.ReturnValue;
        }
        catch (CompilationErrorException ex)
        {
            _logger.LogError(ex, "Script compilation failed");
            throw;
        }
    }
}
```

### Example: Move Effect Scripting

```csharp
// Move definition (data)
{
    "id": "thunderbolt",
    "name": "Thunderbolt",
    "type": "Electric",
    "power": 90,
    "accuracy": 100,
    "effect_script": "moves/thunderbolt.csx"
}

// thunderbolt.csx
if (Random.Next(100) < 10)
{
    await Events.QueueStatus(Defender, "Paralyzed");
    await Log.Info($"{Defender.Name} was paralyzed!");
}

return new MoveResult
{
    Damage = (Attacker.SpecialAttack * 90 / Defender.SpecialDefense) * TypeMultiplier,
    Success = true
};
```

---

## 6. Procedural Audio with DryWetMidi

### Library Overview

**Package:** `Melanchall.DryWetMidi`
**NuGet:** Available in standard and nativeless variants
**License:** MIT
**Platform:** .NET Standard 2.0+, Unity Asset Store

### Core Capabilities

| Feature | Description |
|---------|-------------|
| **File I/O** | Read/write Standard MIDI Files (SMF) |
| **Devices** | Send/receive MIDI from hardware and virtual devices |
| **High-Level API** | Work with notes and chords vs raw events |
| **Playback** | Real-time MIDI playback engine |
| **Recording** | Capture incoming MIDI input |
| **Composition** | Pattern API for programmatic music |
| **Music Theory** | Scale and chord utilities |

### Procedural Music Generation

**Pattern API Example:**

```csharp
using Melanchall.DryWetMidi.Composing;
using Melanchall.DryWetMidi.MusicTheory;

// Create melody pattern
var melody = new PatternBuilder()
    .SetNoteLength(MusicalTimeSpan.Eighth)
    .SetOctave(Octave.Get(4))

    // Battle intro
    .Note(NoteName.C)
    .Note(NoteName.E)
    .Note(NoteName.G)
    .Note(NoteName.C, Octave.Get(5))

    // Repeat with variation
    .StepBack(MusicalTimeSpan.Half)
    .Note(NoteName.D)
    .Note(NoteName.F)
    .Note(NoteName.A)

    .Build();

// Convert to MIDI file
var midiFile = melody.ToFile(TempoMap.Default);
midiFile.Write("battle_intro.mid");
```

### Dynamic Music System

```csharp
public class ProceduralMusicManager
{
    private readonly ILogger<ProceduralMusicManager> _logger;
    private Playback _currentPlayback;
    private OutputDevice _outputDevice;

    public void GenerateBattleMusic(BattleState state)
    {
        // Generate music based on battle state
        var tempo = state.Intensity * 20 + 100; // 100-200 BPM
        var pattern = CreateBattlePattern(state);

        var tempoMap = TempoMap.Create(Tempo.FromBeatsPerMinute(tempo));
        var midiFile = pattern.ToFile(tempoMap);

        PlayMidi(midiFile);
    }

    private Pattern CreateBattlePattern(BattleState state)
    {
        var builder = new PatternBuilder();

        // Low health = minor scale (tense)
        if (state.PlayerHealthPercent < 0.3f)
        {
            builder.SetScale(Scale.GetByName("C minor"));
        }
        // High health = major scale (confident)
        else
        {
            builder.SetScale(Scale.GetByName("C major"));
        }

        // Add rhythmic pattern
        builder
            .SetNoteLength(MusicalTimeSpan.Eighth)
            .Note(NoteName.C)
            .Note(NoteName.E)
            .Note(NoteName.G);

        return builder.Build();
    }

    private void PlayMidi(MidiFile midiFile)
    {
        _currentPlayback?.Stop();
        _currentPlayback?.Dispose();

        _currentPlayback = midiFile.GetPlayback(_outputDevice);
        _currentPlayback.Start();
    }
}
```

### Game Integration Patterns

#### 1. Real-Time Rhythm Game

```csharp
public class RhythmGameController
{
    private readonly IInputDevice _midiInputDevice;
    private readonly Queue<MidiEvent> _noteQueue = new();

    public void Initialize()
    {
        _midiInputDevice = InputDevice.GetByName("MIDI Keyboard");
        _midiInputDevice.EventReceived += OnMidiEventReceived;
        _midiInputDevice.StartEventsListening();
    }

    private void OnMidiEventReceived(object sender, MidiEventReceivedEventArgs e)
    {
        if (e.Event is NoteOnEvent noteOn)
        {
            _noteQueue.Enqueue(e.Event);
            CheckNoteAccuracy(noteOn);
        }
    }

    private void CheckNoteAccuracy(NoteOnEvent note)
    {
        // Check if note matches expected pattern
        // Award points based on timing accuracy
    }
}
```

#### 2. Adaptive Background Music

```csharp
public class AdaptiveMusicSystem
{
    private readonly Dictionary<string, MidiFile> _musicLayers = new();
    private readonly List<Playback> _activePlaybacks = new();

    public void Initialize()
    {
        // Load music layers
        _musicLayers["ambient"] = MidiFile.Read("ambient_layer.mid");
        _musicLayers["tension"] = MidiFile.Read("tension_layer.mid");
        _musicLayers["action"] = MidiFile.Read("action_layer.mid");
    }

    public void UpdateMusicForGameState(GameState state)
    {
        // Fade layers in/out based on game state
        if (state.EnemiesNearby > 0)
        {
            FadeInLayer("tension", 2.0f);
        }

        if (state.InCombat)
        {
            FadeInLayer("action", 1.0f);
            FadeOutLayer("ambient", 2.0f);
        }
    }

    private void FadeInLayer(string layerName, float duration)
    {
        // Implement volume fade logic
    }
}
```

#### 3. Procedural Sound Effects

```csharp
public class ProceduralSFXGenerator
{
    public MidiFile GenerateHitSound(float impact)
    {
        var pattern = new PatternBuilder()
            .SetNoteLength(MusicalTimeSpan.Sixteenth)
            .SetOctave(Octave.Get(2))
            .Note(NoteName.C)
            .Note(NoteName.FSharp); // Tritone for "harsh" sound

        var tempoMap = TempoMap.Create(Tempo.FromBeatsPerMinute(300));
        return pattern.ToFile(tempoMap);
    }

    public MidiFile GeneratePowerUpSound()
    {
        var pattern = new PatternBuilder()
            .SetNoteLength(MusicalTimeSpan.Sixteenth)
            .SetOctave(Octave.Get(4));

        // Ascending arpeggio
        foreach (var note in new[] { NoteName.C, NoteName.E, NoteName.G, NoteName.C })
        {
            pattern.Note(note);
        }

        return pattern.Build().ToFile(TempoMap.Default);
    }
}
```

### Performance Considerations

**Optimization Strategies:**

```csharp
// Pre-generate and cache common patterns
public class MusicCache
{
    private readonly Dictionary<string, MidiFile> _cache = new();

    public MidiFile GetOrGenerate(string key, Func<MidiFile> generator)
    {
        if (!_cache.TryGetValue(key, out var midiFile))
        {
            midiFile = generator();
            _cache[key] = midiFile;
        }
        return midiFile;
    }
}

// Use async loading for large MIDI files
public async Task<MidiFile> LoadMidiAsync(string path)
{
    return await Task.Run(() => MidiFile.Read(path));
}
```

**Configuration:**

```csharp
var readingSettings = new ReadingSettings
{
    InvalidChunkSizePolicy = InvalidChunkSizePolicy.Ignore,
    NotEnoughBytesPolicy = NotEnoughBytesPolicy.Ignore
};

var midiFile = MidiFile.Read("song.mid", readingSettings);
```

### Example: Pokémon Battle Music System

```csharp
public class PokemonBattleMusicSystem
{
    private readonly OutputDevice _outputDevice;
    private Playback _playback;

    public void OnBattleStart(Battle battle)
    {
        var music = GenerateBattleTheme(battle);
        _playback = music.GetPlayback(_outputDevice);
        _playback.Loop = true;
        _playback.Start();
    }

    public void OnHealthChanged(Pokemon pokemon, float healthPercent)
    {
        if (healthPercent < 0.25f)
        {
            // Speed up tempo for low health tension
            _playback.Speed = 1.2;
        }
    }

    private MidiFile GenerateBattleTheme(Battle battle)
    {
        var isLegendary = battle.OpponentPokemon.IsLegendary;
        var pattern = new PatternBuilder();

        if (isLegendary)
        {
            // Epic orchestral theme
            pattern
                .SetScale(Scale.GetByName("C major"))
                .SetOctave(Octave.Get(3))
                .Note(NoteName.C, MusicalTimeSpan.Quarter)
                .Note(NoteName.G, MusicalTimeSpan.Quarter)
                .Note(NoteName.C, Octave.Get(4), MusicalTimeSpan.Half);
        }
        else
        {
            // Standard battle theme
            pattern
                .SetScale(Scale.GetByName("A minor"))
                .SetNoteLength(MusicalTimeSpan.Eighth)
                .Note(NoteName.A)
                .Note(NoteName.C)
                .Note(NoteName.E);
        }

        return pattern.Build().ToFile(
            TempoMap.Create(Tempo.FromBeatsPerMinute(140)));
    }
}
```

---

## 7. Dependency Resolution for Mod Loading

### Algorithm Selection

**Recommended:** Topological sorting using Kahn's Algorithm
**Time Complexity:** O(V + E) where V = mods, E = dependencies
**Advantages:** Intuitive, detects cycles, deterministic ordering

### Kahn's Algorithm Implementation

```csharp
public class ModDependencyResolver
{
    public List<ModManifest> ResolveDependencies(List<ModManifest> mods)
    {
        // Build adjacency list (graph)
        var graph = BuildDependencyGraph(mods);
        var inDegree = CalculateInDegrees(graph);

        // Queue for mods with no dependencies
        var queue = new Queue<ModManifest>();
        foreach (var mod in mods)
        {
            if (inDegree[mod.Id] == 0)
                queue.Enqueue(mod);
        }

        var loadOrder = new List<ModManifest>();

        while (queue.Count > 0)
        {
            var mod = queue.Dequeue();
            loadOrder.Add(mod);

            // Remove edges from this mod
            if (graph.TryGetValue(mod.Id, out var dependencies))
            {
                foreach (var dep in dependencies)
                {
                    inDegree[dep]--;
                    if (inDegree[dep] == 0)
                    {
                        var depMod = mods.First(m => m.Id == dep);
                        queue.Enqueue(depMod);
                    }
                }
            }
        }

        // Detect cycles
        if (loadOrder.Count != mods.Count)
        {
            throw new CircularDependencyException(
                "Circular dependency detected in mod load order");
        }

        return loadOrder;
    }

    private Dictionary<string, List<string>> BuildDependencyGraph(
        List<ModManifest> mods)
    {
        var graph = new Dictionary<string, List<string>>();

        foreach (var mod in mods)
        {
            graph[mod.Id] = new List<string>();

            foreach (var dep in mod.Dependencies)
            {
                // Reverse graph: mod depends on dep, so dep -> mod
                if (!graph.ContainsKey(dep.ModId))
                    graph[dep.ModId] = new List<string>();

                graph[dep.ModId].Add(mod.Id);
            }
        }

        return graph;
    }

    private Dictionary<string, int> CalculateInDegrees(
        Dictionary<string, List<string>> graph)
    {
        var inDegree = new Dictionary<string, int>();

        foreach (var node in graph.Keys)
            inDegree[node] = 0;

        foreach (var edges in graph.Values)
        {
            foreach (var target in edges)
            {
                inDegree[target]++;
            }
        }

        return inDegree;
    }
}
```

### Mod Manifest Structure

```csharp
public class ModManifest
{
    public string Id { get; set; }              // "com.author.modname"
    public string Name { get; set; }            // "Awesome Mod"
    public string Version { get; set; }         // "1.2.3"
    public string Author { get; set; }
    public List<ModDependency> Dependencies { get; set; }
    public int Priority { get; set; } = 0;      // For equal-dependency ordering
}

public class ModDependency
{
    public string ModId { get; set; }
    public string VersionRange { get; set; }    // ">=1.0.0 <2.0.0"
    public bool Optional { get; set; }
}
```

**Example `modinfo.json`:**
```json
{
    "id": "com.player.electrictype",
    "name": "Electric Type Expansion",
    "version": "1.0.0",
    "author": "PlayerName",
    "dependencies": [
        {
            "modId": "com.player.corelib",
            "versionRange": ">=2.0.0 <3.0.0",
            "optional": false
        }
    ],
    "priority": 10
}
```

### Version Range Validation

```csharp
using System.Text.RegularExpressions;

public class VersionRangeValidator
{
    public bool IsSatisfied(string installedVersion, string requiredRange)
    {
        if (string.IsNullOrEmpty(requiredRange))
            return true;

        var version = ParseVersion(installedVersion);

        // Parse range: ">=1.0.0 <2.0.0"
        var parts = requiredRange.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        foreach (var part in parts)
        {
            var match = Regex.Match(part, @"(>=|>|<=|<|=)(.+)");
            if (!match.Success) continue;

            var op = match.Groups[1].Value;
            var compareVersion = ParseVersion(match.Groups[2].Value);

            if (!CompareVersions(version, op, compareVersion))
                return false;
        }

        return true;
    }

    private Version ParseVersion(string versionString)
    {
        return Version.Parse(versionString);
    }

    private bool CompareVersions(Version a, string op, Version b)
    {
        return op switch
        {
            ">=" => a >= b,
            ">" => a > b,
            "<=" => a <= b,
            "<" => a < b,
            "=" => a == b,
            _ => false
        };
    }
}
```

### Advanced Features

#### Optional Dependencies with Fallback

```csharp
public class ModLoader
{
    public List<ModManifest> LoadMods(string modsDirectory)
    {
        var allMods = DiscoverMods(modsDirectory);
        var loadableMods = new List<ModManifest>();

        foreach (var mod in allMods)
        {
            bool canLoad = true;

            foreach (var dep in mod.Dependencies)
            {
                var depMod = allMods.FirstOrDefault(m => m.Id == dep.ModId);

                if (depMod == null)
                {
                    if (dep.Optional)
                    {
                        _logger.LogWarning(
                            $"Optional dependency {dep.ModId} not found for {mod.Id}");
                    }
                    else
                    {
                        _logger.LogError(
                            $"Required dependency {dep.ModId} missing for {mod.Id}");
                        canLoad = false;
                        break;
                    }
                }
                else if (!_versionValidator.IsSatisfied(depMod.Version, dep.VersionRange))
                {
                    _logger.LogError(
                        $"Dependency version mismatch: {mod.Id} requires " +
                        $"{dep.ModId} {dep.VersionRange}, found {depMod.Version}");
                    canLoad = false;
                    break;
                }
            }

            if (canLoad)
                loadableMods.Add(mod);
        }

        return _dependencyResolver.ResolveDependencies(loadableMods);
    }
}
```

#### Deterministic Ordering with Priority

```csharp
public List<ModManifest> ResolveDependenciesWithPriority(List<ModManifest> mods)
{
    var loadOrder = ResolveDependencies(mods); // Topological sort

    // Stable sort by priority for mods with no dependencies between them
    return loadOrder
        .OrderBy(m => m.Priority)
        .ThenBy(m => m.Id)  // Alphabetical for determinism
        .ToList();
}
```

#### Cycle Detection with Path

```csharp
public class CircularDependencyException : Exception
{
    public List<string> DependencyPath { get; }

    public CircularDependencyException(List<string> path)
        : base($"Circular dependency: {string.Join(" -> ", path)}")
    {
        DependencyPath = path;
    }
}

// DFS-based cycle detection with path tracking
public List<string> DetectCycle(Dictionary<string, List<string>> graph)
{
    var visited = new HashSet<string>();
    var recursionStack = new HashSet<string>();
    var path = new List<string>();

    foreach (var node in graph.Keys)
    {
        if (DFSCycleDetect(node, graph, visited, recursionStack, path))
            return path;
    }

    return null; // No cycle
}

private bool DFSCycleDetect(
    string node,
    Dictionary<string, List<string>> graph,
    HashSet<string> visited,
    HashSet<string> recursionStack,
    List<string> path)
{
    if (recursionStack.Contains(node))
    {
        // Cycle detected
        path.Add(node);
        return true;
    }

    if (visited.Contains(node))
        return false;

    visited.Add(node);
    recursionStack.Add(node);
    path.Add(node);

    foreach (var neighbor in graph[node])
    {
        if (DFSCycleDetect(neighbor, graph, visited, recursionStack, path))
            return true;
    }

    recursionStack.Remove(node);
    path.RemoveAt(path.Count - 1);
    return false;
}
```

---

## 8. Integration Recommendations

### Startup Sequence

```csharp
public class Program
{
    public static void Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                // 1. Core services
                services.AddSingleton<World>(World.Create());
                services.AddSingleton<IAssetManager, AssetManager>();

                // 2. Modding infrastructure
                services.AddSingleton<IModLoader, ModLoader>();
                services.AddSingleton<ModDependencyResolver>();
                services.AddSingleton<VersionRangeValidator>();

                // 3. Scripting engine
                services.AddSingleton<IScriptingEngine, SafeScriptingEngine>();
                services.AddSingleton<ScriptValidator>();

                // 4. Audio system
                services.AddSingleton<IAudioManager, ProceduralAudioManager>();

                // 5. ECS systems
                services.AddSingleton<IUpdateSystem, MovementSystem>();
                services.AddSingleton<IUpdateSystem, PhysicsSystem>();
                services.AddSingleton<IRenderSystem, SpriteRenderSystem>();

                // 6. Game instance
                services.AddSingleton<Game1>();
            })
            .Build();

        // Load mods before starting game
        var modLoader = host.Services.GetRequiredService<IModLoader>();
        modLoader.LoadMods("./Mods");

        // Start game
        var game = host.Services.GetRequiredService<Game1>();
        game.Run();
    }
}
```

### Performance Monitoring

```csharp
public class PerformanceMonitor
{
    private readonly ILogger<PerformanceMonitor> _logger;
    private Stopwatch _frameTimer = new();

    public void BeginFrame()
    {
        _frameTimer.Restart();
    }

    public void EndFrame()
    {
        var elapsed = _frameTimer.Elapsed.TotalMilliseconds;

        if (elapsed > 16.67) // 60 FPS target
        {
            _logger.LogWarning(
                $"Frame took {elapsed:F2}ms (target: 16.67ms for 60 FPS)");
        }

        // Log to telemetry system
        Activity.Current?.SetTag("frame_time_ms", elapsed);
    }
}
```

---

## 9. Summary of Key Recommendations

### Architecture
- ✅ Use Arch ECS with record struct components
- ✅ Implement SOLID principles throughout all systems
- ✅ Separate concerns: Core, Domain, ModApi projects
- ✅ Use dependency injection for all services

### Performance
- ✅ Pre-compile scripts at startup, execute many times
- ✅ Use object pooling for frequently created components
- ✅ Leverage Arch's chunk-based storage for cache locality
- ✅ Monitor frame times and optimize critical paths

### Modding
- ✅ Use Kahn's algorithm for dependency resolution
- ✅ Implement version range validation
- ✅ Support optional dependencies with graceful fallback
- ✅ Provide clear error messages for mod conflicts

### Security
- ✅ Minimal API surface for scripts (restricted globals)
- ✅ Validate script source before execution
- ✅ Implement timeouts and cancellation
- ✅ Consider AssemblyLoadContext isolation

### Audio
- ✅ Pre-generate and cache common MIDI patterns
- ✅ Use adaptive music system based on game state
- ✅ Leverage DryWetMidi's Pattern API for composition
- ✅ Support real-time MIDI input for rhythm gameplay

---

## 10. Next Steps for Implementation

1. **Phase 1:** Project scaffolding with .NET 9 and MonoGame
2. **Phase 2:** Implement core Arch ECS systems
3. **Phase 3:** Build modding infrastructure with Harmony
4. **Phase 4:** Integrate Roslyn scripting engine
5. **Phase 5:** Add DryWetMidi audio system
6. **Phase 6:** Create example mod demonstrating all systems

All research findings are available in Hive Mind memory under `hive/research/*` namespace.

---

**End of Research Document**
