using System.ComponentModel;
using PokeNET.CLI.Infrastructure;
using Spectre.Console;
using Spectre.Console.Cli;

namespace PokeNET.CLI.Commands.Data;

/// <summary>
/// Command to list all species with optional filters.
/// </summary>
public class ListSpeciesCommand : CliCommand<ListSpeciesCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [CommandOption("--type <TYPE>")]
        [Description("Filter by type (e.g., Fire, Water)")]
        public string? Type { get; set; }

        [CommandOption("--limit <COUNT>")]
        [Description("Maximum number of species to display")]
        [DefaultValue(50)]
        public int Limit { get; set; } = 50;
    }

    public ListSpeciesCommand(CliContext context)
        : base(context) { }

    protected override async Task ExecuteCommandAsync(CommandContext context, Settings settings)
    {
        await AnsiConsole
            .Status()
            .StartAsync(
                "Loading species data...",
                async ctx =>
                {
                    var allSpecies = await Context.DataApi.GetAllSpeciesAsync();

                    // Apply filters
                    var filtered = allSpecies.AsEnumerable();

                    if (!string.IsNullOrEmpty(settings.Type))
                    {
                        filtered = filtered.Where(s =>
                            s.Types.Contains(settings.Type, StringComparer.OrdinalIgnoreCase)
                        );
                    }

                    var species = filtered.Take(settings.Limit).ToList();

                    ctx.Status("Rendering table...");

                    // Create table
                    var table = new Table();
                    table.Border(TableBorder.Rounded);
                    table.AddColumn("[yellow]#[/]");
                    table.AddColumn("[yellow]Name[/]");
                    table.AddColumn("[yellow]Type(s)[/]");
                    table.AddColumn("[yellow]HP[/]");
                    table.AddColumn("[yellow]Atk[/]");
                    table.AddColumn("[yellow]Def[/]");
                    table.AddColumn("[yellow]SpA[/]");
                    table.AddColumn("[yellow]SpD[/]");
                    table.AddColumn("[yellow]Spe[/]");
                    table.AddColumn("[yellow]Total[/]");

                    foreach (var s in species)
                    {
                        var typeStr = string.Join("/", s.Types);
                        var typeColor = GetTypeColor(s.Types[0]);
                        var total = s.BaseStats.Total;

                        table.AddRow(
                            $"[grey]{s.Id:D3}[/]",
                            $"[{typeColor}]{s.Name}[/]",
                            typeStr,
                            s.BaseStats.HP.ToString(),
                            s.BaseStats.Attack.ToString(),
                            s.BaseStats.Defense.ToString(),
                            s.BaseStats.SpecialAttack.ToString(),
                            s.BaseStats.SpecialDefense.ToString(),
                            s.BaseStats.Speed.ToString(),
                            $"[bold]{total}[/]"
                        );
                    }

                    AnsiConsole.Write(table);
                    AnsiConsole.MarkupLine(
                        $"\n[grey]Showing {species.Count} of {allSpecies.Count} species[/]"
                    );
                }
            );
    }

    private static string GetTypeColor(string type)
    {
        return type.ToLowerInvariant() switch
        {
            "fire" => "red",
            "water" => "blue",
            "grass" => "green",
            "electric" => "yellow",
            "psychic" => "magenta",
            "ice" => "cyan",
            "dragon" => "purple",
            "dark" => "grey",
            "fairy" => "pink",
            "fighting" => "orange1",
            "flying" => "lightblue",
            "poison" => "purple",
            "ground" => "orange3",
            "rock" => "grey",
            "bug" => "lime",
            "ghost" => "purple",
            "steel" => "grey",
            "normal" => "white",
            _ => "white",
        };
    }
}
