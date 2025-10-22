# PokeNET Asset Loading Performance Analysis

**Analyst**: Performance Analysis Agent
**Date**: 2025-10-22
**Framework**: MonoGame + Custom Asset Management
**Target**: < 100ms asset load time, < 3s initial load

## Executive Summary

This document analyzes asset loading performance for PokeNET, including textures, audio, and data files. It provides strategies for efficient loading, caching, and streaming to maintain smooth gameplay and minimize load times.

### Key Findings

- **Current State**: No custom asset management yet, using MonoGame ContentManager
- **Critical Needs**: Async loading, intelligent caching, mod asset override system
- **Optimization Potential**: 5-10x faster load times with proper caching and async loading
- **Memory Impact**: Proper caching can reduce memory usage by 30-40%

---

## 1. Asset Loading Architecture

### 1.1 Asset Types & Characteristics

| Asset Type | Typical Size | Load Frequency | Caching Priority |
|------------|--------------|----------------|------------------|
| Textures | 100 KB - 10 MB | High | CRITICAL |
| Audio (music) | 5-50 MB | Low | MEDIUM |
| Audio (SFX) | 10-500 KB | High | HIGH |
| JSON data | 1-100 KB | Medium | HIGH |
| Scripts (.csx) | 1-50 KB | Low | MEDIUM |
| Shaders | 1-10 KB | Low | LOW |
| Fonts | 100 KB - 2 MB | Low | MEDIUM |

### 1.2 MonoGame Content Pipeline vs Custom Loading

**MonoGame Content Pipeline** (.xnb files):
- **Pros**: Pre-processed, optimized, compressed, fast to load
- **Cons**: Build step required, harder for mods to override, less flexible

**Custom Asset Loading** (raw files):
- **Pros**: No build step, mod-friendly, flexible asset formats
- **Cons**: Slower initial load, requires custom parsing/processing

**Recommended Hybrid Approach**:
```csharp
public interface IAssetLoader<T>
{
    T Load(string path);
    Task<T> LoadAsync(string path);
}

// Use Content Pipeline for base game assets
public class ContentPipelineLoader : IAssetLoader<Texture2D>
{
    private ContentManager content;

    public Texture2D Load(string path) => content.Load<Texture2D>(path);
    public async Task<Texture2D> LoadAsync(string path)
    {
        return await Task.Run(() => content.Load<Texture2D>(path));
    }
}

// Use custom loader for mod assets and runtime data
public class CustomTextureLoader : IAssetLoader<Texture2D>
{
    private GraphicsDevice device;

    public Texture2D Load(string path)
    {
        using var stream = File.OpenRead(path);
        return Texture2D.FromStream(device, stream);
    }

    public async Task<Texture2D> LoadAsync(string path)
    {
        // Async I/O
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read,
            FileShare.Read, 4096, useAsync: true);

        var bytes = new byte[stream.Length];
        await stream.ReadAsync(bytes, 0, bytes.Length);

        // Texture creation must be on main thread
        return await Task.Run(() =>
        {
            using var ms = new MemoryStream(bytes);
            return Texture2D.FromStream(device, ms);
        });
    }
}
```

---

## 2. Asset Caching Strategy

### 2.1 Multi-Tier Cache Architecture

**Tier 1: Hot Cache** (in-memory, always loaded):
```csharp
// Frequently used assets (UI, common sprites)
public class HotCache
{
    private Dictionary<string, Texture2D> cache = new();
    private HashSet<string> hotAssets;

    // Pre-load critical assets
    public void Initialize()
    {
        hotAssets = new HashSet<string>
        {
            "ui/buttons",
            "ui/icons",
            "sprites/player",
            "fonts/main"
        };

        foreach (var asset in hotAssets)
        {
            cache[asset] = LoadAsset(asset);
        }
    }

    public Texture2D Get(string path) => cache[path];
}
```

**Tier 2: Warm Cache** (WeakReference, loaded on demand):
```csharp
// Recently used assets, can be GC'd if memory pressure
public class WarmCache
{
    private Dictionary<string, WeakReference<Texture2D>> cache = new();
    private Queue<string> lruQueue = new();
    private const int MAX_ENTRIES = 500;

    public Texture2D Get(string path)
    {
        if (cache.TryGetValue(path, out var weakRef) &&
            weakRef.TryGetTarget(out var texture))
        {
            // Still in memory
            TouchLRU(path);
            return texture;
        }

        // Cache miss, reload
        texture = LoadAsset(path);
        cache[path] = new WeakReference<Texture2D>(texture);
        TouchLRU(path);

        return texture;
    }

    private void TouchLRU(string path)
    {
        lruQueue.Enqueue(path);
        if (lruQueue.Count > MAX_ENTRIES)
        {
            var old = lruQueue.Dequeue();
            cache.Remove(old);
        }
    }
}
```

**Tier 3: Cold Storage** (disk, load on demand):
```csharp
// Rarely used assets, loaded from disk as needed
public class ColdStorage
{
    public Texture2D Load(string path)
    {
        // Direct disk load, no caching
        return LoadFromDisk(path);
    }
}
```

### 2.2 Cache Size Management

**Memory Budget Enforcement**:
```csharp
public class AssetCache
{
    private long maxMemoryBytes = 512 * 1024 * 1024; // 512 MB default
    private long currentMemoryBytes = 0;
    private Dictionary<string, CachedAsset> cache = new();

    private struct CachedAsset
    {
        public object Asset;
        public long SizeBytes;
        public DateTime LastAccessed;
        public int AccessCount;
    }

    public T Get<T>(string path)
    {
        if (cache.TryGetValue(path, out var cached))
        {
            cached.LastAccessed = DateTime.UtcNow;
            cached.AccessCount++;
            return (T)cached.Asset;
        }

        // Load new asset
        var asset = LoadAsset<T>(path);
        var size = EstimateSize(asset);

        // Evict if necessary
        while (currentMemoryBytes + size > maxMemoryBytes && cache.Count > 0)
        {
            EvictLeastValuable();
        }

        // Add to cache
        cache[path] = new CachedAsset
        {
            Asset = asset,
            SizeBytes = size,
            LastAccessed = DateTime.UtcNow,
            AccessCount = 1
        };
        currentMemoryBytes += size;

        return asset;
    }

    private void EvictLeastValuable()
    {
        // Eviction score: (access count) / (time since last access)
        var toEvict = cache
            .OrderBy(kvp =>
            {
                var age = (DateTime.UtcNow - kvp.Value.LastAccessed).TotalSeconds;
                return kvp.Value.AccessCount / Math.Max(1, age);
            })
            .First();

        currentMemoryBytes -= toEvict.Value.SizeBytes;
        cache.Remove(toEvict.Key);

        Logger.LogDebug($"Evicted {toEvict.Key} ({toEvict.Value.SizeBytes:N0} bytes)");
    }

    private long EstimateSize(object asset)
    {
        return asset switch
        {
            Texture2D tex => tex.Width * tex.Height * 4, // RGBA
            SoundEffect sfx => sfx.Duration.TotalSeconds * 44100 * 2, // 44.1kHz stereo
            string str => str.Length * 2, // UTF-16
            _ => 1024 // Default estimate
        };
    }
}
```

**Performance Impact**:
- **Cache hit**: ~10-50 ns (dictionary lookup)
- **Cache miss + eviction**: ~1-10 ms (disk I/O + eviction)
- **Memory savings**: 30-40% reduction vs loading everything

---

## 3. Asynchronous Loading Strategy

### 3.1 Background Asset Loading

**Loading Screen Pattern**:
```csharp
public class AsyncAssetLoader
{
    private List<Task<object>> loadingTasks = new();
    private int totalAssets = 0;
    private int loadedAssets = 0;

    public async Task<T> LoadAsync<T>(string path)
    {
        totalAssets++;

        var task = Task.Run(async () =>
        {
            var asset = await LoadAssetAsync<T>(path);
            Interlocked.Increment(ref loadedAssets);
            return (object)asset;
        });

        loadingTasks.Add(task);
        return await task as T;
    }

    public float Progress => totalAssets > 0 ? (float)loadedAssets / totalAssets : 0;

    public async Task WaitForAll()
    {
        await Task.WhenAll(loadingTasks);
        loadingTasks.Clear();
    }
}

// Usage in loading screen
public class LoadingScreen
{
    private AsyncAssetLoader loader = new();

    public async Task LoadLevel(string levelName)
    {
        // Start all loads
        var textures = loader.LoadAsync<Texture2D>("level/background");
        var music = loader.LoadAsync<SoundEffect>("level/music");
        var data = loader.LoadAsync<LevelData>("level/data.json");

        // Update progress UI
        while (loader.Progress < 1.0f)
        {
            UpdateProgressBar(loader.Progress);
            await Task.Delay(16); // 60 FPS
        }

        // All loaded, transition to game
        await loader.WaitForAll();
    }
}
```

### 3.2 Streaming Asset Loading

**Progressive Texture Loading**:
```csharp
public class StreamingTextureLoader
{
    public async Task<Texture2D> LoadProgressively(string path, Action<float> onProgress)
    {
        var fileInfo = new FileInfo(path);
        var totalBytes = fileInfo.Length;
        var loadedBytes = 0L;

        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read,
            FileShare.Read, 8192, useAsync: true);

        var buffer = new byte[totalBytes];

        // Read in chunks
        const int chunkSize = 64 * 1024; // 64 KB chunks
        var bytesRead = 0;

        while (loadedBytes < totalBytes)
        {
            var toRead = (int)Math.Min(chunkSize, totalBytes - loadedBytes);
            bytesRead = await stream.ReadAsync(buffer, (int)loadedBytes, toRead);

            loadedBytes += bytesRead;
            onProgress?.Invoke((float)loadedBytes / totalBytes);

            // Yield to main thread to keep UI responsive
            await Task.Yield();
        }

        // Create texture from loaded buffer (main thread)
        return await Task.Run(() =>
        {
            using var ms = new MemoryStream(buffer);
            return Texture2D.FromStream(GraphicsDevice, ms);
        });
    }
}

// Usage
var texture = await loader.LoadProgressively("large_texture.png",
    progress => Console.WriteLine($"Loading: {progress * 100:F1}%"));
```

### 3.3 Priority-Based Loading

**Load Queue with Priorities**:
```csharp
public enum LoadPriority
{
    Immediate = 0,  // Blocking, load now
    High = 1,       // Load ASAP
    Normal = 2,     // Load when possible
    Low = 3,        // Background load
    Preload = 4     // Predictive load
}

public class PriorityAssetLoader
{
    private PriorityQueue<LoadRequest, int> loadQueue = new();
    private SemaphoreSlim loadSemaphore = new(4); // 4 concurrent loads
    private CancellationTokenSource cts = new();

    private record LoadRequest(string Path, Type Type, TaskCompletionSource<object> Completion);

    public Task<T> LoadAsync<T>(string path, LoadPriority priority)
    {
        var tcs = new TaskCompletionSource<object>();
        var request = new LoadRequest(path, typeof(T), tcs);

        loadQueue.Enqueue(request, (int)priority);

        // Start background processor if not running
        _ = ProcessQueue();

        return tcs.Task.ContinueWith(t => (T)t.Result);
    }

    private async Task ProcessQueue()
    {
        while (loadQueue.TryDequeue(out var request, out _))
        {
            await loadSemaphore.WaitAsync(cts.Token);

            _ = Task.Run(async () =>
            {
                try
                {
                    var asset = await LoadAssetAsync(request.Path, request.Type);
                    request.Completion.SetResult(asset);
                }
                catch (Exception ex)
                {
                    request.Completion.SetException(ex);
                }
                finally
                {
                    loadSemaphore.Release();
                }
            }, cts.Token);
        }
    }

    public void CancelAll()
    {
        cts.Cancel();
        cts = new CancellationTokenSource();
    }
}

// Usage
// Critical assets load first
var playerTexture = await loader.LoadAsync<Texture2D>("player.png", LoadPriority.High);

// Background assets load when CPU is idle
var bgMusic = await loader.LoadAsync<Song>("background.ogg", LoadPriority.Low);
```

---

## 4. Mod Asset Override System

### 4.1 Asset Search Path

**Layered Asset Resolution**:
```csharp
public class ModdableAssetManager
{
    private List<string> searchPaths = new();
    private AssetCache cache = new();

    public void Initialize(List<ModInfo> loadedMods)
    {
        // Priority order: Last loaded mod has highest priority
        searchPaths.Clear();

        // Mods (reverse order, last mod wins)
        for (int i = loadedMods.Count - 1; i >= 0; i--)
        {
            searchPaths.Add(loadedMods[i].AssetPath);
        }

        // Base game assets (lowest priority)
        searchPaths.Add("Content");
    }

    public T Load<T>(string relativePath)
    {
        // Try each search path in order
        foreach (var basePath in searchPaths)
        {
            var fullPath = Path.Combine(basePath, relativePath);

            if (File.Exists(fullPath))
            {
                Logger.LogDebug($"Loading asset from: {fullPath}");
                return cache.Get<T>(fullPath);
            }
        }

        throw new FileNotFoundException($"Asset not found: {relativePath}");
    }

    public string ResolvePath(string relativePath)
    {
        foreach (var basePath in searchPaths)
        {
            var fullPath = Path.Combine(basePath, relativePath);
            if (File.Exists(fullPath))
                return fullPath;
        }

        return null;
    }
}

// Example usage
// Base game has: Content/sprites/pikachu.png
// Mod "ShinyPokemon" has: Mods/ShinyPokemon/sprites/pikachu.png
// Mod asset automatically overrides base game asset

var texture = assetManager.Load<Texture2D>("sprites/pikachu.png");
// Loads from Mods/ShinyPokemon/sprites/pikachu.png (mod override)
```

### 4.2 Asset Conflict Detection

**Conflict Resolution**:
```csharp
public class AssetConflictDetector
{
    public record Conflict(string AssetPath, List<string> Sources);

    public List<Conflict> DetectConflicts(List<ModInfo> mods)
    {
        var assetSources = new Dictionary<string, List<string>>();
        var conflicts = new List<Conflict>();

        // Scan all mod assets
        foreach (var mod in mods)
        {
            var assetFiles = Directory.GetFiles(mod.AssetPath, "*.*", SearchOption.AllDirectories);

            foreach (var file in assetFiles)
            {
                var relativePath = Path.GetRelativePath(mod.AssetPath, file);

                if (!assetSources.ContainsKey(relativePath))
                    assetSources[relativePath] = new List<string>();

                assetSources[relativePath].Add(mod.Name);
            }
        }

        // Find conflicts
        foreach (var kvp in assetSources)
        {
            if (kvp.Value.Count > 1)
            {
                conflicts.Add(new Conflict(kvp.Key, kvp.Value));
            }
        }

        return conflicts;
    }

    public void LogConflicts(List<Conflict> conflicts)
    {
        foreach (var conflict in conflicts)
        {
            Logger.LogWarning($"Asset conflict: {conflict.AssetPath}");
            Logger.LogWarning($"  Sources: {string.Join(", ", conflict.Sources)}");
            Logger.LogWarning($"  Winner: {conflict.Sources.Last()} (last loaded)");
        }
    }
}
```

---

## 5. Texture Loading Optimization

### 5.1 Texture Compression

**Platform-Specific Compression**:
```csharp
public class TextureCompressor
{
    public Texture2D LoadCompressed(string path, GraphicsDevice device)
    {
        // Detect platform and use appropriate compression
        if (OperatingSystem.IsWindows())
        {
            // Use DXT compression on Windows
            return LoadDXT(path, device);
        }
        else if (OperatingSystem.IsAndroid() || OperatingSystem.IsIOS())
        {
            // Use ETC2/ASTC on mobile
            return LoadETC2(path, device);
        }
        else
        {
            // Fallback to uncompressed
            return Texture2D.FromFile(device, path);
        }
    }

    private Texture2D LoadDXT(string path, GraphicsDevice device)
    {
        // DXT1: 6:1 compression, no alpha
        // DXT3: 4:1 compression, sharp alpha
        // DXT5: 4:1 compression, smooth alpha

        // Implementation depends on image library
        // StbImageSharp, ImageSharp, or DirectXTex
        throw new NotImplementedException();
    }
}
```

**Memory Savings**:
- **DXT1**: 8:1 compression (1024x1024 RGBA: 4 MB → 512 KB)
- **DXT5**: 4:1 compression (1024x1024 RGBA: 4 MB → 1 MB)
- **Quality**: Near-lossless for most game textures

### 5.2 Mipmapping Strategy

**Generate Mipmaps for Performance**:
```csharp
public class MipmapGenerator
{
    public Texture2D GenerateMipmaps(Texture2D source, GraphicsDevice device)
    {
        // Calculate mipmap levels
        var levels = 1 + (int)Math.Floor(Math.Log(Math.Max(source.Width, source.Height), 2));

        // Create texture with mipmaps
        var mipmapped = new Texture2D(device, source.Width, source.Height, true, source.Format);

        // Copy base level
        var data = new Color[source.Width * source.Height];
        source.GetData(data);
        mipmapped.SetData(0, null, data, 0, data.Length);

        // Generate mipmap levels
        for (int level = 1; level < levels; level++)
        {
            var mipData = GenerateMipLevel(data, source.Width >> (level - 1), source.Height >> (level - 1));
            mipmapped.SetData(level, null, mipData, 0, mipData.Length);
        }

        return mipmapped;
    }

    private Color[] GenerateMipLevel(Color[] source, int width, int height)
    {
        var mipWidth = Math.Max(1, width / 2);
        var mipHeight = Math.Max(1, height / 2);
        var mipData = new Color[mipWidth * mipHeight];

        // Box filter downsampling
        for (int y = 0; y < mipHeight; y++)
        {
            for (int x = 0; x < mipWidth; x++)
            {
                var c00 = source[(y * 2) * width + (x * 2)];
                var c10 = source[(y * 2) * width + (x * 2 + 1)];
                var c01 = source[(y * 2 + 1) * width + (x * 2)];
                var c11 = source[(y * 2 + 1) * width + (x * 2 + 1)];

                mipData[y * mipWidth + x] = new Color(
                    (c00.R + c10.R + c01.R + c11.R) / 4,
                    (c00.G + c10.G + c01.G + c11.G) / 4,
                    (c00.B + c10.B + c01.B + c11.B) / 4,
                    (c00.A + c10.A + c01.A + c11.A) / 4
                );
            }
        }

        return mipData;
    }
}
```

**Benefits**:
- **GPU Performance**: 10-30% faster rendering (fewer texture cache misses)
- **Visual Quality**: Reduces aliasing at distance
- **Memory**: Adds 33% memory overhead but worth it

### 5.3 Texture Atlas/Sprite Sheets

**Batch Loading & Rendering**:
```csharp
public class TextureAtlas
{
    private Texture2D atlasTexture;
    private Dictionary<string, Rectangle> sprites = new();

    public void LoadAtlas(string atlasPath, string metadataPath)
    {
        // Load packed texture
        atlasTexture = Texture2D.FromFile(device, atlasPath);

        // Load sprite metadata (JSON)
        var metadata = JsonSerializer.Deserialize<AtlasMetadata>(File.ReadAllText(metadataPath));

        foreach (var sprite in metadata.Sprites)
        {
            sprites[sprite.Name] = new Rectangle(sprite.X, sprite.Y, sprite.Width, sprite.Height);
        }

        Logger.LogInformation($"Loaded atlas with {sprites.Count} sprites");
    }

    public Rectangle GetSpriteRect(string name) => sprites[name];

    public void Draw(SpriteBatch batch, string spriteName, Vector2 position)
    {
        batch.Draw(atlasTexture, position, sprites[spriteName], Color.White);
    }
}

// Benefits:
// - Single texture load instead of hundreds
// - Reduces draw calls (batch rendering)
// - Better GPU cache utilization
// - 5-10x faster than individual sprite loading
```

---

## 6. Audio Loading Optimization

### 6.1 Audio Format Selection

**Format Comparison**:

| Format | Size | Quality | Streaming | Use Case |
|--------|------|---------|-----------|----------|
| WAV | Large | Perfect | No | Short SFX |
| OGG | Medium | Excellent | Yes | Music, ambient |
| MP3 | Small | Good | Yes | Voice, music |
| XNB (compressed) | Small | Good | No | MonoGame pipeline |

**Recommended Strategy**:
```csharp
public class AudioLoader
{
    // Short sound effects: Load to memory (WAV)
    public SoundEffect LoadSFX(string path)
    {
        // WAV loads directly to memory
        return SoundEffect.FromFile(path);
    }

    // Music: Stream from disk (OGG)
    public Song LoadMusic(string path)
    {
        // OGG streams, doesn't load entire file
        return Song.FromUri(path, new Uri(path));
    }

    // Voice: Compressed, streamed (MP3/OGG)
    public Song LoadVoice(string path)
    {
        return Song.FromUri(path, new Uri(path));
    }
}
```

### 6.2 Audio Caching & Streaming

**SFX Cache** (in-memory, frequently used):
```csharp
public class SFXCache
{
    private Dictionary<string, SoundEffect> cache = new();
    private long maxMemoryBytes = 50 * 1024 * 1024; // 50 MB
    private long currentMemoryBytes = 0;

    public SoundEffect Get(string path)
    {
        if (cache.TryGetValue(path, out var sfx))
            return sfx;

        // Load and cache
        sfx = SoundEffect.FromFile(path);
        var size = EstimateSFXSize(sfx);

        if (currentMemoryBytes + size > maxMemoryBytes)
        {
            EvictLRU();
        }

        cache[path] = sfx;
        currentMemoryBytes += size;

        return sfx;
    }

    private long EstimateSFXSize(SoundEffect sfx)
    {
        // Estimate: duration * sample rate * channels * bytes per sample
        return (long)(sfx.Duration.TotalSeconds * 44100 * 2 * 2);
    }
}
```

**Music Streaming** (no memory cache, always stream):
```csharp
public class MusicPlayer
{
    private Song currentSong;

    public void PlayMusic(string path)
    {
        // Stop previous
        MediaPlayer.Stop();

        // Load and play (streaming)
        currentSong = Song.FromUri(path, new Uri(path));
        MediaPlayer.Play(currentSong);

        // No memory overhead, streams from disk
    }
}
```

---

## 7. Data File Loading (JSON/XML)

### 7.1 Efficient Parsing

**Streaming JSON Parsing**:
```csharp
using System.Text.Json;

public class DataLoader
{
    // Standard loading (loads entire file to memory)
    public T LoadStandard<T>(string path)
    {
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<T>(json);
    }

    // Streaming loading (minimal memory)
    public async Task<T> LoadStreamingAsync<T>(string path)
    {
        using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<T>(stream);
    }

    // Memory-efficient, especially for large files
}
```

**Performance Comparison** (1 MB JSON file):
- **Standard**: 10-20 ms, 2 MB memory (file + parsed object)
- **Streaming**: 15-25 ms, 0.5 MB memory (parsed object only)

### 7.2 Data Caching

**Schema-Based Caching**:
```csharp
public class DataCache
{
    private Dictionary<Type, Dictionary<string, object>> cache = new();

    public T Get<T>(string path) where T : class
    {
        var typeCache = cache.GetValueOrDefault(typeof(T));
        if (typeCache != null && typeCache.TryGetValue(path, out var data))
        {
            return (T)data;
        }

        // Load and cache
        var loaded = LoadData<T>(path);

        if (!cache.ContainsKey(typeof(T)))
            cache[typeof(T)] = new Dictionary<string, object>();

        cache[typeof(T)][path] = loaded;
        return loaded;
    }

    // Example: Pokemon data
    var pikachu = dataCache.Get<PokemonData>("data/pokemon/pikachu.json");
    // Subsequent calls return cached instance
}
```

---

## 8. Performance Benchmarks

### 8.1 Load Time Targets

| Asset Type | Size | Sync Load | Async Load | Cached |
|------------|------|-----------|------------|--------|
| Small texture (256x256) | 256 KB | 2-5 ms | 1-3 ms | 0.01 ms |
| Medium texture (1024x1024) | 4 MB | 20-50 ms | 10-30 ms | 0.01 ms |
| Large texture (4096x4096) | 64 MB | 200-500 ms | 100-300 ms | 0.01 ms |
| SFX (1s, 44.1kHz) | 176 KB | 1-2 ms | 0.5-1 ms | 0.01 ms |
| Music (3min, OGG) | 5 MB | 10-20 ms | 5-10 ms | N/A (streamed) |
| JSON data (100 KB) | 100 KB | 5-10 ms | 3-7 ms | 0.01 ms |

### 8.2 Benchmark Implementation

```csharp
using BenchmarkDotNet.Attributes;

[MemoryDiagnoser]
public class AssetLoadingBenchmarks
{
    private AssetCache cache;
    private string texturePath = "test_texture.png";

    [GlobalSetup]
    public void Setup()
    {
        cache = new AssetCache();
    }

    [Benchmark]
    public Texture2D LoadTextureSynchronous()
    {
        return Texture2D.FromFile(GraphicsDevice, texturePath);
    }

    [Benchmark]
    public async Task<Texture2D> LoadTextureAsynchronous()
    {
        return await Task.Run(() => Texture2D.FromFile(GraphicsDevice, texturePath));
    }

    [Benchmark]
    public Texture2D LoadTextureFromCache()
    {
        return cache.Get<Texture2D>(texturePath);
    }
}
```

**Expected Results**:
```
| Method                    | Mean      | Allocated |
|-------------------------- |---------- |---------- |
| LoadTextureSynchronous    | 25.5 ms   | 4.2 MB    |
| LoadTextureAsynchronous   | 15.3 ms   | 4.2 MB    |
| LoadTextureFromCache      | 0.015 ms  | 24 B      |
```

---

## 9. Loading Screen Best Practices

### 9.1 Progressive Loading UI

**Loading Screen with Progress**:
```csharp
public class LoadingScreen
{
    private AsyncAssetLoader loader;
    private float progress = 0;
    private string currentAsset = "";

    public async Task LoadGameAssets()
    {
        var assets = new[]
        {
            ("Textures", "sprites/pokemon/*.png"),
            ("Audio", "sounds/*.wav"),
            ("Music", "music/*.ogg"),
            ("Data", "data/*.json")
        };

        var totalAssets = 0;
        var loadedAssets = 0;

        foreach (var (category, pattern) in assets)
        {
            var files = Directory.GetFiles("Content", pattern);
            totalAssets += files.Length;

            foreach (var file in files)
            {
                currentAsset = Path.GetFileName(file);
                await loader.LoadAsync<object>(file);
                loadedAssets++;

                progress = (float)loadedAssets / totalAssets;
                UpdateProgressBar(progress, $"Loading {category}: {currentAsset}");

                // Yield to render progress
                await Task.Delay(1);
            }
        }
    }

    public void Draw(SpriteBatch batch)
    {
        // Draw progress bar
        var barWidth = 400;
        var barHeight = 30;
        var fillWidth = (int)(barWidth * progress);

        DrawRectangle(batch, 100, 300, barWidth, barHeight, Color.Gray);
        DrawRectangle(batch, 100, 300, fillWidth, barHeight, Color.Green);

        // Draw text
        batch.DrawString(font, $"Loading... {progress * 100:F0}%", new Vector2(100, 340), Color.White);
        batch.DrawString(font, currentAsset, new Vector2(100, 370), Color.LightGray);
    }
}
```

### 9.2 Predictive Preloading

**Load Assets Based on Player Position**:
```csharp
public class PredictiveLoader
{
    private HashSet<string> loadedRegions = new();
    private AsyncAssetLoader loader;

    public void Update(Vector2 playerPosition)
    {
        var currentRegion = GetRegion(playerPosition);
        var adjacentRegions = GetAdjacentRegions(currentRegion);

        // Preload adjacent regions
        foreach (var region in adjacentRegions)
        {
            if (!loadedRegions.Contains(region))
            {
                _ = LoadRegionAsync(region);
                loadedRegions.Add(region);
            }
        }

        // Unload distant regions
        var distantRegions = loadedRegions.Except(adjacentRegions).ToList();
        foreach (var region in distantRegions)
        {
            UnloadRegion(region);
            loadedRegions.Remove(region);
        }
    }

    private async Task LoadRegionAsync(string region)
    {
        var assets = GetAssetsForRegion(region);
        foreach (var asset in assets)
        {
            await loader.LoadAsync<object>(asset, LoadPriority.Preload);
        }
    }
}
```

---

## 10. Optimization Roadmap

### 10.1 Phase 3: Asset System Foundation

**Priority: CRITICAL**

1. **Implement Moddable Asset Manager**
   - Search path system (mods → base game)
   - Asset override detection
   - **Expected Impact**: Foundation for modding system

2. **Basic Async Loading**
   - Async texture/audio loading
   - Loading screen with progress
   - **Expected Impact**: 50-70% reduction in perceived load time

3. **Simple LRU Cache**
   - Texture cache with size limit
   - WeakReference for auto-eviction
   - **Expected Impact**: 3-5x faster asset access

### 10.2 Phase 4: Advanced Optimization

**Priority: HIGH**

1. **Priority Loading Queue**
   - Immediate/High/Normal/Low/Preload priorities
   - Concurrent loading (4-8 threads)
   - **Expected Impact**: Better load order, smoother experience

2. **Texture Atlas Support**
   - Pack small sprites into atlases
   - Reduce individual texture loads
   - **Expected Impact**: 5-10x faster sprite loading

3. **Asset Compression**
   - DXT compression for textures
   - OGG compression for audio
   - **Expected Impact**: 4-8x smaller asset sizes

### 10.3 Phase 5: Production Features

**Priority: MEDIUM**

1. **Streaming LOD System**
   - Load low-res textures first
   - Stream high-res in background
   - **Expected Impact**: Instant startup, better UX

2. **Asset Dependency Graph**
   - Track asset dependencies
   - Load dependencies first
   - **Expected Impact**: Prevent missing asset errors

3. **Hot Reload Support**
   - Watch file changes
   - Reload assets at runtime
   - **Expected Impact**: Better developer experience

---

## 11. Recommendations

### 11.1 Immediate Actions

1. **Design moddable asset system** - Search path hierarchy
2. **Implement basic caching** - WeakReference + LRU
3. **Add async loading support** - For loading screens
4. **Profile asset load times** - Identify slowest assets

### 11.2 Best Practices

- **Always cache frequently used assets** (UI, common sprites)
- **Stream large assets** (music, cutscenes)
- **Use async loading for non-critical assets**
- **Compress textures** (DXT on Windows, ETC2 on mobile)
- **Generate mipmaps** for all textures
- **Batch small textures** into atlases
- **Monitor memory usage** and enforce budgets
- **Preload assets predictively** based on player position

### 11.3 Performance Targets

| Metric | Target | Warning | Critical |
|--------|--------|---------|----------|
| Initial load time | < 3s | 5s | 10s |
| Asset load (small) | < 5ms | 10ms | 20ms |
| Asset load (large) | < 50ms | 100ms | 200ms |
| Cache hit rate | > 90% | 80% | 70% |
| Texture memory | < 400 MB | 500 MB | 600 MB |
| Audio memory | < 150 MB | 200 MB | 250 MB |

---

## Conclusion

Efficient asset loading is critical for PokeNET's performance and mod support. The key strategies are:

1. **Async Everything**: Don't block the main thread
2. **Cache Intelligently**: Hot/Warm/Cold tiers
3. **Mod-Friendly**: Search path system for overrides
4. **Compress Assets**: DXT textures, OGG audio
5. **Monitor Memory**: Enforce budgets, evict LRU

**Estimated Performance** (well-optimized implementation):
- **Initial load**: < 3 seconds (vs 10-20s naive)
- **Asset cache hit**: 10-50 ns (vs 10-50 ms disk)
- **Memory usage**: 30-40% reduction (vs loading everything)
- **Mod asset override**: Seamless, zero overhead

Following these recommendations will ensure PokeNET has fast, responsive asset loading that supports a rich modding ecosystem.
