using System.ComponentModel;
using PokeNET.CLI.Display;
using PokeNET.CLI.Infrastructure;
using PokeNET.Core.Data;
using Spectre.Console;
using Spectre.Console.Cli;

namespace PokeNET.CLI.Commands.Data;

/// <summary>
/// Command to show detailed information about a move.
/// </summary>
public class ShowMoveCommand : CliCommand<ShowMoveCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [CommandArgument(0, "<NAME>")]
        [Description("Move name")]
        public string Name { get; set; } = string.Empty;
    }

    public ShowMoveCommand(CliContext context) : base(context) { }

    protected override async Task ExecuteCommandAsync(CommandContext context, Settings settings)
    {
        await AnsiConsole.Status()
            .StartAsync("Loading move data...", async ctx =>
            {
                var move = await Context.DataApi.GetMoveByNameAsync(settings.Name);

                if (move == null)
                {
                    AnsiConsole.MarkupLine($"[red]Move '{settings.Name}' not found[/]");
                    return;
                }

                ctx.Status("Rendering details...");
                DataDisplayHelper.DisplayMove(move);
            });
    }

    // Removed duplicate display logic - now using DataDisplayHelper
    private static void OldDisplayMoveMethod(MoveData move)
    {
        // Header
        var title = $"{move.Name} ({move.Type})";
        var panel = new Panel(new Markup($"[bold white]{title}[/]"))
        {
            Border = BoxBorder.Double,
            BorderStyle = new Style(Color.Aqua)
        };
        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();

        // Move details table
        var table = new Table();
        table.Border(TableBorder.Rounded);
        table.HideHeaders();
        table.AddColumn("Property");
        table.AddColumn("Value");

        table.AddRow("[yellow]Type:[/]", move.Type);
        table.AddRow("[yellow]Category:[/]", move.Category.ToString());
        table.AddRow("[yellow]Power:[/]", move.Power > 0 ? move.Power.ToString() : "—");
        table.AddRow("[yellow]Accuracy:[/]", move.Accuracy > 0 ? move.Accuracy.ToString() : "—");
        table.AddRow("[yellow]PP:[/]", move.PP.ToString());
        table.AddRow("[yellow]Priority:[/]", move.Priority.ToString());
        table.AddRow("[yellow]Target:[/]", move.Target);

        if (move.EffectChance > 0)
        {
            table.AddRow("[yellow]Effect Chance:[/]", $"{move.EffectChance}%");
        }

        if (move.MakesContact)
        {
            table.AddRow("[yellow]Makes Contact:[/]", "Yes");
        }

        if (!string.IsNullOrEmpty(move.Description))
        {
            table.AddRow("[yellow]Description:[/]", move.Description);
        }

        if (move.Flags?.Any() == true)
        {
            table.AddRow("[cyan]Flags:[/]", string.Join(", ", move.Flags));
        }

        if (!string.IsNullOrEmpty(move.EffectScript))
        {
            table.AddRow("[grey]Effect Script:[/]", $"[grey]{move.EffectScript}[/]");
        }

        if (move.EffectParameters?.Any() == true)
        {
            var paramsStr = string.Join(", ", move.EffectParameters.Select(kvp => $"{kvp.Key}: {kvp.Value}"));
            table.AddRow("[grey]Effect Params:[/]", $"[grey]{paramsStr}[/]");
        }

        AnsiConsole.Write(table);
    }
}

