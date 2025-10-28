using PokeNET.CLI.Infrastructure;
using PokeNET.Core.Data;
using Spectre.Console;
using Spectre.Console.Cli;

namespace PokeNET.CLI.Commands.Data;

/// <summary>
/// Command to list all moves with optional filtering.
/// </summary>
public sealed class ListMovesCommand : CliCommand<ListMovesCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [CommandOption("--type <TYPE>")]
        public string? Type { get; set; }

        [CommandOption("--category <CATEGORY>")]
        public string? Category { get; set; }

        [CommandOption("--limit <COUNT>")]
        public int? Limit { get; set; } = 20;
    }

    public ListMovesCommand(CliContext context) : base(context) { }

    protected override async Task ExecuteCommandAsync(CommandContext context, Settings settings)
    {
        var allMoves = await Context.DataApi.GetAllMovesAsync();
        var moves = allMoves.AsEnumerable();

        // Apply filters
        if (!string.IsNullOrEmpty(settings.Type))
        {
            moves = moves.Where(m => m.Type.Equals(settings.Type, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrEmpty(settings.Category))
        {
            moves = moves.Where(m => m.Category.ToString().Equals(settings.Category, StringComparison.OrdinalIgnoreCase));
        }

        var moveList = moves.ToList();
        var displayMoves = settings.Limit.HasValue ? moveList.Take(settings.Limit.Value) : moveList;

        // Create table
        var table = new Table();
        table.AddColumn("Name");
        table.AddColumn("Type");
        table.AddColumn("Category");
        table.AddColumn("Power");
        table.AddColumn("Accuracy");
        table.AddColumn("PP");

        foreach (var move in displayMoves)
        {
            table.AddRow(
                move.Name,
                $"[{GetTypeColor(move.Type)}]{move.Type}[/]",
                move.Category.ToString(),
                move.Power.ToString(),
                move.Accuracy.ToString(),
                move.PP.ToString()
            );
        }

        AnsiConsole.Write(table);
        AnsiConsole.MarkupLine($"\nShowing {displayMoves.Count()} of {moveList.Count} moves");
    }

    private static string GetTypeColor(string type) => type.ToLower() switch
    {
        "normal" => "white",
        "fire" => "red",
        "water" => "blue",
        "electric" => "yellow",
        "grass" => "green",
        "ice" => "aqua",
        "fighting" => "orange1",
        "poison" => "purple",
        "ground" => "yellow4",
        "flying" => "deepskyblue3",
        "psychic" => "hotpink",
        "bug" => "chartreuse3",
        "rock" => "tan",
        "ghost" => "purple4",
        "dragon" => "purple3",
        "dark" => "grey39",
        "steel" => "grey74",
        "fairy" => "pink1",
        _ => "white"
    };
}

