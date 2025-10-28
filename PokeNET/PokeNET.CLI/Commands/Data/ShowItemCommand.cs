using System.ComponentModel;
using PokeNET.CLI.Display;
using PokeNET.CLI.Infrastructure;
using PokeNET.Core.Data;
using Spectre.Console;
using Spectre.Console.Cli;

namespace PokeNET.CLI.Commands.Data;

/// <summary>
/// Command to show detailed information about an item.
/// </summary>
public class ShowItemCommand : CliCommand<ShowItemCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [CommandArgument(0, "<NAME>")]
        [Description("Item name")]
        public string Name { get; set; } = string.Empty;
    }

    public ShowItemCommand(CliContext context)
        : base(context) { }

    protected override async Task ExecuteCommandAsync(CommandContext context, Settings settings)
    {
        await AnsiConsole
            .Status()
            .StartAsync(
                "Loading item data...",
                async ctx =>
                {
                    var item = await Context.DataApi.GetItemByNameAsync(settings.Name);

                    if (item == null)
                    {
                        AnsiConsole.MarkupLine($"[red]Item '{settings.Name}' not found[/]");
                        return;
                    }

                    ctx.Status("Rendering details...");
                    DataDisplayHelper.DisplayItem(item);
                }
            );
    }

    // Removed duplicate display logic - now using DataDisplayHelper
    private static void OldDisplayItemMethod(ItemData item)
    {
        // Header
        var panel = new Panel(new Markup($"[bold white]{item.Name}[/]"))
        {
            Border = BoxBorder.Double,
            BorderStyle = new Style(Color.Green),
        };
        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();

        // Item details table
        var table = new Table();
        table.Border(TableBorder.Rounded);
        table.HideHeaders();
        table.AddColumn("Property");
        table.AddColumn("Value");

        table.AddRow("[yellow]ID:[/]", item.Id.ToString());
        table.AddRow("[yellow]Category:[/]", item.Category.ToString());

        if (!string.IsNullOrEmpty(item.Description))
        {
            table.AddRow("[yellow]Description:[/]", item.Description);
        }

        if (item.BuyPrice > 0)
        {
            table.AddRow("[yellow]Buy Price:[/]", $"₽{item.BuyPrice}");
        }

        if (item.SellPrice > 0)
        {
            table.AddRow("[yellow]Sell Price:[/]", $"₽{item.SellPrice}");
        }

        // Usage properties
        table.AddRow("[cyan]Consumable:[/]", item.Consumable ? "Yes" : "No");
        table.AddRow("[cyan]Usable in Battle:[/]", item.UsableInBattle ? "Yes" : "No");
        table.AddRow("[cyan]Usable Outside Battle:[/]", item.UsableOutsideBattle ? "Yes" : "No");
        table.AddRow("[cyan]Holdable:[/]", item.Holdable ? "Yes" : "No");

        if (!string.IsNullOrEmpty(item.SpritePath))
        {
            table.AddRow("[grey]Sprite:[/]", $"[grey]{item.SpritePath}[/]");
        }

        if (!string.IsNullOrEmpty(item.EffectScript))
        {
            table.AddRow("[grey]Effect Script:[/]", $"[grey]{item.EffectScript}[/]");
        }

        if (item.EffectParameters?.Any() == true)
        {
            var paramsStr = string.Join(
                ", ",
                item.EffectParameters.Select(kvp => $"{kvp.Key}: {kvp.Value}")
            );
            table.AddRow("[grey]Effect Params:[/]", $"[grey]{paramsStr}[/]");
        }

        AnsiConsole.Write(table);
    }
}
