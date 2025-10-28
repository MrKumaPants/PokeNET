using PokeNET.CLI.Infrastructure;
using Spectre.Console;
using Spectre.Console.Cli;

namespace PokeNET.CLI.Commands.System;

/// <summary>
/// Command to test data loading and validate integrity.
/// </summary>
public class TestDataCommand : CliCommand<TestDataCommand.Settings>
{
    public class Settings : CommandSettings { }

    public TestDataCommand(CliContext context)
        : base(context) { }

    protected override async Task ExecuteCommandAsync(CommandContext context, Settings settings)
    {
        await AnsiConsole
            .Progress()
            .StartAsync(async ctx =>
            {
                var task = ctx.AddTask("[yellow]Testing data systems[/]");
                task.MaxValue = 5;

                // Test species loading
                task.Description = "[yellow]Testing species data...[/]";
                var species = await Context.DataApi.GetAllSpeciesAsync();
                task.Increment(1);

                var speciesResult = species.Any() ? "[green]✓[/]" : "[red]✗[/]";
                AnsiConsole.MarkupLine($"{speciesResult} Species: {species.Count} loaded");

                // Test move loading
                task.Description = "[yellow]Testing move data...[/]";
                var moves = await Context.DataApi.GetAllMovesAsync();
                task.Increment(1);

                var movesResult = moves.Any() ? "[green]✓[/]" : "[red]✗[/]";
                AnsiConsole.MarkupLine($"{movesResult} Moves: {moves.Count} loaded");

                // Test item loading
                task.Description = "[yellow]Testing item data...[/]";
                var items = await Context.DataApi.GetAllItemsAsync();
                task.Increment(1);

                var itemsResult = items.Any() ? "[green]✓[/]" : "[red]✗[/]";
                AnsiConsole.MarkupLine($"{itemsResult} Items: {items.Count} loaded");

                // Test type loading
                task.Description = "[yellow]Testing type data...[/]";
                var types = await Context.DataApi.GetAllTypesAsync();
                task.Increment(1);

                var typesResult = types.Any() ? "[green]✓[/]" : "[red]✗[/]";
                AnsiConsole.MarkupLine($"{typesResult} Types: {types.Count} loaded");

                // Test encounters loading
                task.Description = "[yellow]Testing encounter data...[/]";
                var encounters = await Context.DataApi.GetAllEncountersAsync();
                task.Increment(1);

                var encountersResult = "[green]✓[/]";
                AnsiConsole.MarkupLine($"{encountersResult} Encounters: {encounters.Count} loaded");

                task.StopTask();

                AnsiConsole.WriteLine();
                var totalItems =
                    species.Count + moves.Count + items.Count + types.Count + encounters.Count;
                var allPassed = species.Any() && moves.Any() && items.Any() && types.Any();

                if (allPassed)
                {
                    AnsiConsole.MarkupLine(
                        $"[green bold]All tests passed![/] Total items loaded: {totalItems}"
                    );
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red bold]Some tests failed![/]");
                }
            });
    }
}
