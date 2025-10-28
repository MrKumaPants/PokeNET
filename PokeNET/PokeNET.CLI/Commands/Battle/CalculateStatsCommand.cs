using System.ComponentModel;
using PokeNET.CLI.Infrastructure;
using PokeNET.Core.Battle;
using PokeNET.Core.Data;
using PokeNET.Core.ECS.Components;
using Spectre.Console;
using Spectre.Console.Cli;

namespace PokeNET.CLI.Commands.Battle;

/// <summary>
/// Command to calculate Pokemon stats.
/// </summary>
public class CalculateStatsCommand : CliCommand<CalculateStatsCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [CommandArgument(0, "<SPECIES>")]
        [Description("Species name or Pokedex number")]
        public string Species { get; set; } = string.Empty;

        [CommandOption("--level <LEVEL>")]
        [Description("Pokemon level (1-100)")]
        [DefaultValue(50)]
        public int Level { get; set; } = 50;

        [CommandOption("--nature <NATURE>")]
        [Description("Pokemon nature (e.g., Adamant, Timid)")]
        public string? Nature { get; set; }

        [CommandOption("--ivs <IVS>")]
        [Description("IVs in format: HP,Atk,Def,SpA,SpD,Spe (0-31 each)")]
        [DefaultValue("31,31,31,31,31,31")]
        public string IVs { get; set; } = "31,31,31,31,31,31";

        [CommandOption("--evs <EVS>")]
        [Description("EVs in format: HP,Atk,Def,SpA,SpD,Spe (0-252 each, max 510 total)")]
        [DefaultValue("0,0,0,0,0,0")]
        public string EVs { get; set; } = "0,0,0,0,0,0";
    }

    public CalculateStatsCommand(CliContext context)
        : base(context) { }

    protected override async Task ExecuteCommandAsync(CommandContext context, Settings settings)
    {
        await AnsiConsole
            .Status()
            .StartAsync(
                "Loading species data...",
                async ctx =>
                {
                    // Get species
                    SpeciesData? species = null;
                    if (int.TryParse(settings.Species, out var dexNumber))
                    {
                        // Try to find by National Dex Number
                        var allSpecies = await Context.DataApi.GetAllSpeciesAsync();
                        species = allSpecies.FirstOrDefault(s => s.NationalDexNumber == dexNumber);
                    }
                    else
                    {
                        // Try by name or string ID
                        species = await Context.DataApi.GetSpeciesByNameAsync(settings.Species);
                        if (species == null)
                        {
                            // Try by ID as fallback
                            species = await Context.DataApi.GetSpeciesAsync(settings.Species);
                        }
                    }

                    if (species == null)
                    {
                        AnsiConsole.MarkupLine($"[red]Species '{settings.Species}' not found[/]");
                        return;
                    }

                    // Parse IVs and EVs
                    var ivs = ParseStats(settings.IVs);
                    var evs = ParseStats(settings.EVs);

                    if (ivs == null || evs == null)
                    {
                        AnsiConsole.MarkupLine(
                            "[red]Invalid IVs or EVs format. Use: HP,Atk,Def,SpA,SpD,Spe[/]"
                        );
                        return;
                    }

                    // Parse nature
                    var nature = Nature.Hardy;
                    if (!string.IsNullOrEmpty(settings.Nature))
                    {
                        if (!Enum.TryParse<Nature>(settings.Nature, true, out nature))
                        {
                            AnsiConsole.MarkupLine($"[red]Invalid nature: {settings.Nature}[/]");
                            return;
                        }
                    }

                    ctx.Status("Calculating stats...");

                    // Create stats component
                    var stats = new PokemonStats
                    {
                        IV_HP = ivs[0],
                        IV_Attack = ivs[1],
                        IV_Defense = ivs[2],
                        IV_SpAttack = ivs[3],
                        IV_SpDefense = ivs[4],
                        IV_Speed = ivs[5],
                        EV_HP = evs[0],
                        EV_Attack = evs[1],
                        EV_Defense = evs[2],
                        EV_SpAttack = evs[3],
                        EV_SpDefense = evs[4],
                        EV_Speed = evs[5],
                    };

                    // Validate
                    if (!StatCalculator.ValidateIVs(stats))
                    {
                        AnsiConsole.MarkupLine("[red]Invalid IVs (must be 0-31 each)[/]");
                        return;
                    }

                    if (!StatCalculator.ValidateEVs(stats))
                    {
                        AnsiConsole.MarkupLine("[red]Invalid EVs (max 252 per stat, 510 total)[/]");
                        return;
                    }

                    // Calculate stats
                    StatCalculator.RecalculateAllStats(
                        ref stats,
                        species.BaseStats.HP,
                        species.BaseStats.Attack,
                        species.BaseStats.Defense,
                        species.BaseStats.SpecialAttack,
                        species.BaseStats.SpecialDefense,
                        species.BaseStats.Speed,
                        settings.Level,
                        nature
                    );

                    DisplayStats(species, stats, settings.Level, nature);
                }
            );
    }

    private static int[]? ParseStats(string statString)
    {
        var parts = statString.Split(',');
        if (parts.Length != 6)
            return null;

        var stats = new int[6];
        for (int i = 0; i < 6; i++)
        {
            if (!int.TryParse(parts[i].Trim(), out stats[i]))
                return null;
        }
        return stats;
    }

    private static void DisplayStats(
        SpeciesData species,
        PokemonStats stats,
        int level,
        Nature nature
    )
    {
        // Header
        var title = $"{species.Name} - Level {level} ({nature})";
        var panel = new Panel(new Markup($"[bold white]{title}[/]"))
        {
            Border = BoxBorder.Double,
            BorderStyle = new Style(Color.Yellow),
        };
        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();

        // Stats table
        var table = new Table();
        table.Border(TableBorder.Rounded);
        table.AddColumn("[yellow]Stat[/]");
        table.AddColumn("[yellow]Base[/]");
        table.AddColumn("[yellow]IV[/]");
        table.AddColumn("[yellow]EV[/]");
        table.AddColumn("[yellow]Final[/]");

        AddStatRow(table, "HP", species.BaseStats.HP, stats.IV_HP, stats.EV_HP, stats.MaxHP);
        AddStatRow(
            table,
            "Attack",
            species.BaseStats.Attack,
            stats.IV_Attack,
            stats.EV_Attack,
            stats.Attack
        );
        AddStatRow(
            table,
            "Defense",
            species.BaseStats.Defense,
            stats.IV_Defense,
            stats.EV_Defense,
            stats.Defense
        );
        AddStatRow(
            table,
            "Sp. Attack",
            species.BaseStats.SpecialAttack,
            stats.IV_SpAttack,
            stats.EV_SpAttack,
            stats.SpAttack
        );
        AddStatRow(
            table,
            "Sp. Defense",
            species.BaseStats.SpecialDefense,
            stats.IV_SpDefense,
            stats.EV_SpDefense,
            stats.SpDefense
        );
        AddStatRow(
            table,
            "Speed",
            species.BaseStats.Speed,
            stats.IV_Speed,
            stats.EV_Speed,
            stats.Speed
        );

        AnsiConsole.Write(table);

        var totalEVs =
            stats.EV_HP
            + stats.EV_Attack
            + stats.EV_Defense
            + stats.EV_SpAttack
            + stats.EV_SpDefense
            + stats.EV_Speed;
        AnsiConsole.MarkupLine($"\n[grey]Total EVs: {totalEVs}/510[/]");
    }

    private static void AddStatRow(
        Table table,
        string name,
        int baseValue,
        int iv,
        int ev,
        int final
    )
    {
        table.AddRow(
            name,
            baseValue.ToString(),
            iv.ToString(),
            ev.ToString(),
            $"[green bold]{final}[/]"
        );
    }
}
