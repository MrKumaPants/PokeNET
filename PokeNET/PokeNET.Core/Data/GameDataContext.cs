using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace PokeNET.Core.Data;

/// <summary>
/// Entity Framework Core DbContext for Pokemon game data.
/// Uses in-memory database provider for fast, queryable data access.
/// </summary>
public class GameDataContext : DbContext
{
    public DbSet<SpeciesData> Species { get; set; } = null!;
    public DbSet<MoveData> Moves { get; set; } = null!;
    public DbSet<ItemData> Items { get; set; } = null!;
    public DbSet<TypeData> Types { get; set; } = null!;
    public DbSet<EncounterTable> Encounters { get; set; } = null!;

    public GameDataContext(DbContextOptions<GameDataContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureSpeciesEntity(modelBuilder);
        ConfigureMoveEntity(modelBuilder);
        ConfigureItemEntity(modelBuilder);
        ConfigureTypeEntity(modelBuilder);
        ConfigureEncounterEntity(modelBuilder);
    }

    private void ConfigureSpeciesEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SpeciesData>(entity =>
        {
            // Primary key
            entity.HasKey(s => s.Id);

            // Indexes for fast lookups
            entity.HasIndex(s => s.Name).IsUnique();
            entity.HasIndex(s => s.NationalDexNumber);

            // Owned types (complex types stored as part of the entity)
            entity.OwnsOne(
                s => s.BaseStats,
                stats =>
                {
                    stats.Property(s => s.HP).IsRequired();
                    stats.Property(s => s.Attack).IsRequired();
                    stats.Property(s => s.Defense).IsRequired();
                    stats.Property(s => s.SpecialAttack).IsRequired();
                    stats.Property(s => s.SpecialDefense).IsRequired();
                    stats.Property(s => s.Speed).IsRequired();
                }
            );

            // Owned collection for level moves
            // IMPORTANT: For owned entities with OwnsMany, EF Core automatically includes the parent key
            // in the actual database key. However, we still need to define the "business key" which is
            // the composite of Level and MoveName. The full key in the database will be:
            // (SpeciesData.Id, Level, MoveName) - EF Core handles the parent key automatically.
            entity.OwnsMany(
                s => s.LevelMoves,
                move =>
                {
                    // Use a shadow property as primary key to avoid composite key tracking issues
                    // This is similar to how Evolution is configured
                    move.Property<int>("LevelMoveId").ValueGeneratedOnAdd();
                    move.HasKey("LevelMoveId");

                    // Make Level and MoveName required and create unique index
                    move.Property(lm => lm.Level).IsRequired();
                    move.Property(lm => lm.MoveName).IsRequired();
                    move.HasIndex(lm => new { lm.Level, lm.MoveName }); // Index for uniqueness per parent
                }
            );

            // Owned collection for evolutions
            entity.OwnsMany(
                s => s.Evolutions,
                evolution =>
                {
                    evolution.Property<int>("EvolutionId").ValueGeneratedOnAdd();
                    evolution.HasKey("EvolutionId");
                    evolution.Property(e => e.TargetSpeciesId).IsRequired();
                    evolution.Property(e => e.Method).IsRequired();

                    // Store conditions dictionary as JSON
                    evolution
                        .Property(e => e.Conditions)
                        .HasConversion(
                            dict => JsonSerializer.Serialize(dict, (JsonSerializerOptions?)null),
                            json =>
                                JsonSerializer.Deserialize<Dictionary<string, string>>(
                                    json,
                                    (JsonSerializerOptions?)null
                                ) ?? new()
                        )
                        .Metadata.SetValueComparer(CreateDictionaryComparer<string, string>());
                }
            );

            // Primitive collections stored as JSON (EF Core cannot map List<string> to database columns without conversion)
            entity
                .Property(s => s.Types)
                .HasConversion(
                    list => JsonSerializer.Serialize(list, (JsonSerializerOptions?)null),
                    json =>
                        JsonSerializer.Deserialize<List<string>>(json, (JsonSerializerOptions?)null)
                        ?? new()
                )
                .Metadata.SetValueComparer(CreateListComparer<string>());

            entity
                .Property(s => s.Abilities)
                .HasConversion(
                    list => JsonSerializer.Serialize(list, (JsonSerializerOptions?)null),
                    json =>
                        JsonSerializer.Deserialize<List<string>>(json, (JsonSerializerOptions?)null)
                        ?? new()
                )
                .Metadata.SetValueComparer(CreateListComparer<string>());

            entity
                .Property(s => s.EggGroups)
                .HasConversion(
                    list => JsonSerializer.Serialize(list, (JsonSerializerOptions?)null),
                    json =>
                        JsonSerializer.Deserialize<List<string>>(json, (JsonSerializerOptions?)null)
                        ?? new()
                )
                .Metadata.SetValueComparer(CreateListComparer<string>());

            entity
                .Property(s => s.TmMoves)
                .HasConversion(
                    list => JsonSerializer.Serialize(list, (JsonSerializerOptions?)null),
                    json =>
                        JsonSerializer.Deserialize<List<string>>(json, (JsonSerializerOptions?)null)
                        ?? new()
                )
                .Metadata.SetValueComparer(CreateListComparer<string>());

            entity
                .Property(s => s.EggMoves)
                .HasConversion(
                    list => JsonSerializer.Serialize(list, (JsonSerializerOptions?)null),
                    json =>
                        JsonSerializer.Deserialize<List<string>>(json, (JsonSerializerOptions?)null)
                        ?? new()
                )
                .Metadata.SetValueComparer(CreateListComparer<string>());
        });
    }

    private void ConfigureMoveEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MoveData>(entity =>
        {
            entity.HasKey(m => m.Id);

            // Index for fast lookups
            entity.HasIndex(m => m.Name).IsUnique();

            // Index for filtering by type
            entity.HasIndex(m => m.Type);

            // Index for filtering by category
            entity.HasIndex(m => m.Category);

            // Store Flags as JSON
            entity
                .Property(m => m.Flags)
                .HasConversion(
                    list => JsonSerializer.Serialize(list, (JsonSerializerOptions?)null),
                    json =>
                        JsonSerializer.Deserialize<List<string>>(json, (JsonSerializerOptions?)null)
                        ?? new()
                )
                .Metadata.SetValueComparer(CreateListComparer<string>());

            // Store EffectParameters dictionary as JSON
            entity
                .Property(m => m.EffectParameters)
                .HasConversion(
                    dict =>
                        dict == null
                            ? null
                            : JsonSerializer.Serialize(dict, (JsonSerializerOptions?)null),
                    json =>
                        string.IsNullOrEmpty(json)
                            ? null
                            : JsonSerializer.Deserialize<Dictionary<string, object>>(
                                json,
                                (JsonSerializerOptions?)null
                            )
                )
                .Metadata.SetValueComparer(CreateDictionaryComparer<string, object>());
        });
    }

    private void ConfigureItemEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ItemData>(entity =>
        {
            entity.HasKey(i => i.Id);

            // Index for fast lookups
            entity.HasIndex(i => i.Name).IsUnique();

            // Index for filtering by category
            entity.HasIndex(i => i.Category);

            // Store EffectParameters dictionary as JSON
            entity
                .Property(i => i.EffectParameters)
                .HasConversion(
                    dict =>
                        dict == null
                            ? null
                            : JsonSerializer.Serialize(dict, (JsonSerializerOptions?)null),
                    json =>
                        string.IsNullOrEmpty(json)
                            ? null
                            : JsonSerializer.Deserialize<Dictionary<string, object>>(
                                json,
                                (JsonSerializerOptions?)null
                            )
                )
                .Metadata.SetValueComparer(CreateDictionaryComparer<string, object>());
        });
    }

    private void ConfigureTypeEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TypeData>(entity =>
        {
            entity.HasKey(t => t.Id);

            // Index for fast lookups
            entity.HasIndex(t => t.Name).IsUnique();

            // Store Matchups dictionary as JSON
            entity
                .Property(t => t.Matchups)
                .HasConversion(
                    dict => JsonSerializer.Serialize(dict, (JsonSerializerOptions?)null),
                    json =>
                        JsonSerializer.Deserialize<Dictionary<string, double>>(
                            json,
                            (JsonSerializerOptions?)null
                        ) ?? new()
                )
                .Metadata.SetValueComparer(CreateDictionaryComparer<string, double>());
        });
    }

    private void ConfigureEncounterEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EncounterTable>(entity =>
        {
            entity.HasKey(e => e.LocationId);

            // Index for fast lookups
            entity.HasIndex(e => e.LocationName).IsUnique();

            // Owned collections for different encounter types
            entity.OwnsMany(
                e => e.GrassEncounters,
                encounter =>
                {
                    encounter.Property<int>("EncounterId").ValueGeneratedOnAdd();
                    encounter.HasKey("EncounterId");
                }
            );

            entity.OwnsMany(
                e => e.WaterEncounters,
                encounter =>
                {
                    encounter.Property<int>("EncounterId").ValueGeneratedOnAdd();
                    encounter.HasKey("EncounterId");
                }
            );

            entity.OwnsMany(
                e => e.OldRodEncounters,
                encounter =>
                {
                    encounter.Property<int>("EncounterId").ValueGeneratedOnAdd();
                    encounter.HasKey("EncounterId");
                }
            );

            entity.OwnsMany(
                e => e.GoodRodEncounters,
                encounter =>
                {
                    encounter.Property<int>("EncounterId").ValueGeneratedOnAdd();
                    encounter.HasKey("EncounterId");
                }
            );

            entity.OwnsMany(
                e => e.SuperRodEncounters,
                encounter =>
                {
                    encounter.Property<int>("EncounterId").ValueGeneratedOnAdd();
                    encounter.HasKey("EncounterId");
                }
            );

            entity.OwnsMany(
                e => e.CaveEncounters,
                encounter =>
                {
                    encounter.Property<int>("EncounterId").ValueGeneratedOnAdd();
                    encounter.HasKey("EncounterId");
                }
            );

            entity.OwnsMany(
                e => e.SpecialEncounters,
                special =>
                {
                    special.Property<int>("SpecialEncounterId").ValueGeneratedOnAdd();
                    special.HasKey("SpecialEncounterId");

                    // Store Conditions dictionary as JSON
                    special
                        .Property(s => s.Conditions)
                        .HasConversion(
                            dict => JsonSerializer.Serialize(dict, (JsonSerializerOptions?)null),
                            json =>
                                JsonSerializer.Deserialize<Dictionary<string, object>>(
                                    json,
                                    (JsonSerializerOptions?)null
                                ) ?? new()
                        )
                        .Metadata.SetValueComparer(CreateDictionaryComparer<string, object>());
                }
            );
        });
    }

    // ==================== Value Comparer Helpers ====================

    private static ValueComparer<List<T>> CreateListComparer<T>()
        where T : notnull
    {
        return new ListValueComparer<T>();
    }

    private static ValueComparer<Dictionary<TKey, TValue>> CreateDictionaryComparer<TKey, TValue>()
        where TKey : notnull
    {
        return new DictionaryValueComparer<TKey, TValue>();
    }
}

/// <summary>
/// Value comparer for List collections stored as JSON.
/// </summary>
internal class ListValueComparer<T> : ValueComparer<List<T>>
    where T : notnull
{
    public ListValueComparer()
        : base(
            (c1, c2) => CompareLists(c1, c2),
            c => GetListHashCode(c),
            c => c == null ? new List<T>() : new List<T>(c)
        ) { }

    private static bool CompareLists(List<T>? c1, List<T>? c2)
    {
        if (ReferenceEquals(c1, c2))
            return true;
        if (c1 == null || c2 == null)
            return false;
        if (c1.Count != c2.Count)
            return false;
        return c1.SequenceEqual(c2);
    }

    private static int GetListHashCode(List<T>? c)
    {
        if (c == null)
            return 0;
        var hash = new HashCode();
        foreach (var item in c)
        {
            hash.Add(item);
        }
        return hash.ToHashCode();
    }
}

/// <summary>
/// Value comparer for Dictionary collections stored as JSON.
/// </summary>
internal class DictionaryValueComparer<TKey, TValue> : ValueComparer<Dictionary<TKey, TValue>>
    where TKey : notnull
{
    public DictionaryValueComparer()
        : base(
            (c1, c2) => CompareDictionaries(c1, c2),
            c => GetDictionaryHashCode(c),
            c => c == null ? new Dictionary<TKey, TValue>() : new Dictionary<TKey, TValue>(c)
        ) { }

    private static bool CompareDictionaries(
        Dictionary<TKey, TValue>? c1,
        Dictionary<TKey, TValue>? c2
    )
    {
        if (ReferenceEquals(c1, c2))
            return true;
        if (c1 == null || c2 == null)
            return false;
        if (c1.Count != c2.Count)
            return false;

        foreach (var kvp in c1)
        {
            if (!c2.TryGetValue(kvp.Key, out var value))
                return false;
            if (!object.Equals(kvp.Value, value))
                return false;
        }
        return true;
    }

    private static int GetDictionaryHashCode(Dictionary<TKey, TValue>? c)
    {
        if (c == null)
            return 0;
        var hash = new HashCode();
        foreach (var kvp in c)
        {
            hash.Add(kvp.Key);
            if (kvp.Value != null)
            {
                hash.Add(kvp.Value);
            }
        }
        return hash.ToHashCode();
    }
}
