using PokeNET.CLI.Infrastructure;
using PokeNET.Core.Data;
using Spectre.Console;
using Spectre.Console.Cli;

namespace PokeNET.CLI.Commands.Data;

/// <summary>
/// Command to list all items with optional filtering.
/// </summary>
public sealed class ListItemsCommand : CliCommand<ListItemsCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [CommandOption("--category <CATEGORY>")]
        public string? Category { get; set; }

        [CommandOption("--limit <COUNT>")]
        public int? Limit { get; set; } = 20;
    }

    public ListItemsCommand(CliContext context)
        : base(context) { }

    protected override async Task ExecuteCommandAsync(CommandContext context, Settings settings)
    {
        var allItems = await Context.DataApi.GetAllItemsAsync();
        var items = allItems.AsEnumerable();

        // Apply filters
        if (!string.IsNullOrEmpty(settings.Category))
        {
            items = items.Where(i =>
                i.Category.ToString().Equals(settings.Category, StringComparison.OrdinalIgnoreCase)
            );
        }

        var itemList = items.ToList();
        var displayItems = settings.Limit.HasValue ? itemList.Take(settings.Limit.Value) : itemList;

        // Create table
        var table = new Table();
        table.AddColumn("ID");
        table.AddColumn("Name");
        table.AddColumn("Category");
        table.AddColumn("Buy Price");
        table.AddColumn("Sell Price");
        table.AddColumn("Description");

        foreach (var item in displayItems)
        {
            var description =
                item.Description.Length > 50
                    ? item.Description.Substring(0, 47) + "..."
                    : item.Description;

            table.AddRow(
                item.Id.ToString(),
                item.Name,
                item.Category.ToString(),
                $"¥{item.BuyPrice}",
                $"¥{item.SellPrice}",
                description
            );
        }

        AnsiConsole.Write(table);
        AnsiConsole.MarkupLine($"\nShowing {displayItems.Count()} of {itemList.Count} items");
    }
}
