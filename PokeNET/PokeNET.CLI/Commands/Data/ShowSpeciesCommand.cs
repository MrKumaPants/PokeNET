using System.ComponentModel;
using PokeNET.CLI.Display;
using PokeNET.CLI.Infrastructure;
using PokeNET.Core.Data;
using Spectre.Console;
using Spectre.Console.Cli;

namespace PokeNET.CLI.Commands.Data;

/// <summary>
/// Command to show detailed information about a species.
/// </summary>
public class ShowSpeciesCommand : CliCommand<ShowSpeciesCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [CommandArgument(0, "<NAME_OR_ID>")]
        [Description("Species name or Pokedex number")]
        public string Identifier { get; set; } = string.Empty;
    }

    public ShowSpeciesCommand(CliContext context)
        : base(context) { }

    protected override async Task ExecuteCommandAsync(CommandContext context, Settings settings)
    {
        await AnsiConsole
            .Status()
            .StartAsync(
                "Loading species data...",
                async ctx =>
                {
                    // Try to parse as ID first
                    SpeciesData? species = null;
                    if (int.TryParse(settings.Identifier, out var dexNumber))
                    {
                        // Try to find by National Dex Number
                        var allSpecies = await Context.DataApi.GetAllSpeciesAsync();
                        species = allSpecies.FirstOrDefault(s => s.NationalDexNumber == dexNumber);
                    }
                    else
                    {
                        // Try by name or string ID
                        species = await Context.DataApi.GetSpeciesByNameAsync(settings.Identifier);
                        if (species == null)
                        {
                            // Try by ID as fallback
                            species = await Context.DataApi.GetSpeciesAsync(settings.Identifier);
                        }
                    }

                    if (species == null)
                    {
                        AnsiConsole.MarkupLine(
                            $"[red]Species '{settings.Identifier}' not found[/]"
                        );
                        return;
                    }

                    ctx.Status("Rendering details...");
                    DataDisplayHelper.DisplaySpecies(species);
                }
            );
    }

    // Removed duplicate display logic - now using DataDisplayHelper
    private static void OldDisplaySpeciesMethod(SpeciesData species)
    {
        // Header
        var typeStr = string.Join("/", species.Types);
        var title = $"#{species.Id:D3} {species.Name} ({typeStr})";

        var panel = new Panel(new Markup($"[bold white]{title}[/]"))
        {
            Border = BoxBorder.Double,
            BorderStyle = new Style(Color.Yellow),
        };
        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();

        // Base stats table
        var statsTable = new Table();
        statsTable.Border(TableBorder.Rounded);
        statsTable.AddColumn("[yellow]Stat[/]");
        statsTable.AddColumn("[yellow]Value[/]");
        statsTable.AddColumn("[yellow]Bar[/]");

        var total = species.BaseStats.Total;

        AddStatRow(statsTable, "HP", species.BaseStats.HP);
        AddStatRow(statsTable, "Attack", species.BaseStats.Attack);
        AddStatRow(statsTable, "Defense", species.BaseStats.Defense);
        AddStatRow(statsTable, "Sp. Attack", species.BaseStats.SpecialAttack);
        AddStatRow(statsTable, "Sp. Defense", species.BaseStats.SpecialDefense);
        AddStatRow(statsTable, "Speed", species.BaseStats.Speed);
        statsTable.AddRow("[bold]Total[/]", $"[bold]{total}[/]", "");

        AnsiConsole.Write(
            new Panel(statsTable)
            {
                Header = new PanelHeader("[yellow]Base Stats[/]"),
                Border = BoxBorder.Rounded,
            }
        );
        AnsiConsole.WriteLine();

        // Additional info
        var infoTable = new Table();
        infoTable.Border(TableBorder.Rounded);
        infoTable.HideHeaders();
        infoTable.AddColumn("Property");
        infoTable.AddColumn("Value");

        if (species.Abilities?.Any() == true)
        {
            infoTable.AddRow("[yellow]Abilities:[/]", string.Join(", ", species.Abilities));
        }

        if (!string.IsNullOrEmpty(species.HiddenAbility))
        {
            infoTable.AddRow("[yellow]Hidden Ability:[/]", species.HiddenAbility);
        }

        if (species.Evolutions?.Any() == true)
        {
            var evos = string.Join(
                ", ",
                species.Evolutions.Select(e => $"{e.TargetSpeciesId} at Lv.{e.RequiredLevel}")
            );
            infoTable.AddRow("[yellow]Evolves Into:[/]", evos);
        }

        if (!string.IsNullOrEmpty(species.Description))
        {
            infoTable.AddRow("[yellow]Description:[/]", species.Description);
        }

        if (infoTable.Rows.Count > 0)
        {
            AnsiConsole.Write(infoTable);
            AnsiConsole.WriteLine();
        }

        // Physical characteristics & breeding info
        var detailsTable = new Table();
        detailsTable.Border(TableBorder.Rounded);
        detailsTable.AddColumn("[cyan]Property[/]");
        detailsTable.AddColumn("[cyan]Value[/]");

        detailsTable.AddRow("Height", $"{species.Height / 10.0:F1}m");
        detailsTable.AddRow("Weight", $"{species.Weight / 10.0:F1}kg");
        detailsTable.AddRow("Growth Rate", species.GrowthRate);
        detailsTable.AddRow("Base Experience", species.BaseExperience.ToString());
        detailsTable.AddRow("Catch Rate", $"{species.CatchRate} / 255");
        detailsTable.AddRow("Base Friendship", species.BaseFriendship.ToString());

        var genderRatio = species.GenderRatio switch
        {
            -1 => "Genderless",
            0 => "100% Male",
            254 => "100% Female",
            _ => $"{(species.GenderRatio / 254.0 * 100):F1}% Female",
        };
        detailsTable.AddRow("Gender Ratio", genderRatio);

        if (species.EggGroups?.Any() == true)
        {
            detailsTable.AddRow("Egg Groups", string.Join(", ", species.EggGroups));
            detailsTable.AddRow("Hatch Steps", species.HatchSteps.ToString());
        }

        AnsiConsole.Write(
            new Panel(detailsTable)
            {
                Header = new PanelHeader("[cyan]Details[/]"),
                Border = BoxBorder.Rounded,
            }
        );
        AnsiConsole.WriteLine();

        // Level-up moves
        if (species.LevelMoves?.Any() == true)
        {
            var movesTable = new Table();
            movesTable.Border(TableBorder.Rounded);
            movesTable.AddColumn("[green]Level[/]");
            movesTable.AddColumn("[green]Move[/]");

            foreach (var move in species.LevelMoves.OrderBy(m => m.Level))
            {
                movesTable.AddRow(move.Level.ToString(), move.MoveName);
            }

            AnsiConsole.Write(
                new Panel(movesTable)
                {
                    Header = new PanelHeader("[green]Level-up Moves[/]"),
                    Border = BoxBorder.Rounded,
                }
            );
            AnsiConsole.WriteLine();
        }

        // TM moves
        if (species.TmMoves?.Any() == true)
        {
            var tmText = string.Join(", ", species.TmMoves);
            AnsiConsole.MarkupLine($"[green]TM Moves:[/] {tmText}");
            AnsiConsole.WriteLine();
        }

        // Egg moves
        if (species.EggMoves?.Any() == true)
        {
            var eggText = string.Join(", ", species.EggMoves);
            AnsiConsole.MarkupLine($"[purple]Egg Moves:[/] {eggText}");
        }
    }

    private static void AddStatRow(Table table, string statName, int value)
    {
        const int maxStat = 255;
        var barLength = (int)((value / (double)maxStat) * 20);
        var bar = new string('█', barLength) + new string('░', 20 - barLength);

        var color = value switch
        {
            >= 150 => "green",
            >= 100 => "yellow",
            >= 50 => "orange1",
            _ => "red",
        };

        table.AddRow(statName, $"[{color}]{value}[/]", $"[{color}]{bar}[/]");
    }
}
