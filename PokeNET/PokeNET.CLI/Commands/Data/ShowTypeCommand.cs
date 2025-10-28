using System.ComponentModel;
using PokeNET.CLI.Display;
using PokeNET.CLI.Infrastructure;
using PokeNET.Core.Data;
using Spectre.Console;
using Spectre.Console.Cli;

namespace PokeNET.CLI.Commands.Data;

/// <summary>
/// Command to show type effectiveness information.
/// </summary>
public class ShowTypeCommand : CliCommand<ShowTypeCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [CommandArgument(0, "<TYPE>")]
        [Description("Type name (e.g., Fire, Water)")]
        public string TypeName { get; set; } = string.Empty;
    }

    public ShowTypeCommand(CliContext context) : base(context) { }

    protected override async Task ExecuteCommandAsync(CommandContext context, Settings settings)
    {
        await AnsiConsole.Status()
            .StartAsync("Loading type data...", async ctx =>
            {
                var typeData = await Context.DataApi.GetTypeByNameAsync(settings.TypeName);

                if (typeData == null)
                {
                    AnsiConsole.MarkupLine($"[red]Type '{settings.TypeName}' not found[/]");
                    return;
                }

                ctx.Status("Calculating defensive matchups...");
                var allTypes = await Context.DataApi.GetAllTypesAsync();

                ctx.Status("Rendering type chart...");
                DataDisplayHelper.DisplayType(typeData, allTypes);
            });
    }

    // Removed duplicate display logic - now using DataDisplayHelper
    private static void OldDisplayTypeMethod(TypeData typeData, IReadOnlyList<TypeData> allTypes)
    {
        // Header
        var panel = new Panel(new Markup($"[bold white]{typeData.Name} Type[/]"))
        {
            Border = BoxBorder.Double,
            BorderStyle = new Style(Color.Yellow)
        };
        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();

        // Offensive effectiveness (this type attacking others)
        var offensiveTable = new Table();
        offensiveTable.Border(TableBorder.Rounded);
        offensiveTable.AddColumn("[yellow]Effectiveness[/]");
        offensiveTable.AddColumn("[yellow]Types[/]");

        var superEffectiveOffense = typeData.Matchups
            .Where(kvp => kvp.Value > 1.0)
            .Select(kvp => kvp.Key)
            .ToList();

        var notVeryEffectiveOffense = typeData.Matchups
            .Where(kvp => kvp.Value > 0 && kvp.Value < 1.0)
            .Select(kvp => kvp.Key)
            .ToList();

        var noEffectOffense = typeData.Matchups
            .Where(kvp => kvp.Value == 0)
            .Select(kvp => kvp.Key)
            .ToList();

        if (superEffectiveOffense.Any())
        {
            offensiveTable.AddRow(
                "[green]Super Effective (2x)[/]",
                string.Join(", ", superEffectiveOffense)
            );
        }

        if (notVeryEffectiveOffense.Any())
        {
            offensiveTable.AddRow(
                "[orange1]Not Very Effective (0.5x)[/]",
                string.Join(", ", notVeryEffectiveOffense)
            );
        }

        if (noEffectOffense.Any())
        {
            offensiveTable.AddRow(
                "[red]No Effect (0x)[/]",
                string.Join(", ", noEffectOffense)
            );
        }

        AnsiConsole.Write(new Panel(offensiveTable)
        {
            Header = new PanelHeader($"[yellow]{typeData.Name} Type Attacking[/]"),
            Border = BoxBorder.Rounded
        });
        AnsiConsole.WriteLine();

        // Defensive effectiveness (other types attacking this type)
        var defensiveTable = new Table();
        defensiveTable.Border(TableBorder.Rounded);
        defensiveTable.AddColumn("[cyan]Effectiveness[/]");
        defensiveTable.AddColumn("[cyan]Types[/]");

        var superEffectiveDefense = new List<string>();
        var notVeryEffectiveDefense = new List<string>();
        var noEffectDefense = new List<string>();

        foreach (var otherType in allTypes)
        {
            if (otherType.Matchups.TryGetValue(typeData.Name, out var effectiveness))
            {
                if (effectiveness > 1.0)
                {
                    superEffectiveDefense.Add(otherType.Name);
                }
                else if (effectiveness > 0 && effectiveness < 1.0)
                {
                    notVeryEffectiveDefense.Add(otherType.Name);
                }
                else if (effectiveness == 0)
                {
                    noEffectDefense.Add(otherType.Name);
                }
            }
        }

        if (superEffectiveDefense.Any())
        {
            defensiveTable.AddRow(
                "[red]Weak to (2x)[/]",
                string.Join(", ", superEffectiveDefense)
            );
        }

        if (notVeryEffectiveDefense.Any())
        {
            defensiveTable.AddRow(
                "[green]Resists (0.5x)[/]",
                string.Join(", ", notVeryEffectiveDefense)
            );
        }

        if (noEffectDefense.Any())
        {
            defensiveTable.AddRow(
                "[blue]Immune to (0x)[/]",
                string.Join(", ", noEffectDefense)
            );
        }

        AnsiConsole.Write(new Panel(defensiveTable)
        {
            Header = new PanelHeader($"[cyan]{typeData.Name} Type Defending[/]"),
            Border = BoxBorder.Rounded
        });
    }
}

