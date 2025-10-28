using PokeNET.CLI.Infrastructure;
using Spectre.Console;
using Spectre.Console.Cli;

namespace PokeNET.CLI.Commands.Mod;

/// <summary>
/// Command to list all discovered mods.
/// </summary>
public class ListModsCommand : CliCommand<ListModsCommand.Settings>
{
    public class Settings : CommandSettings { }

    public ListModsCommand(CliContext context)
        : base(context) { }

    protected override async Task ExecuteCommandAsync(CommandContext context, Settings settings)
    {
        await Task.Run(() =>
        {
            var mods = Context.ModLoader.LoadedMods;

            if (!mods.Any())
            {
                AnsiConsole.MarkupLine("[yellow]No mods loaded[/]");
                return;
            }

            var table = new Table();
            table.Border(TableBorder.Rounded);
            table.AddColumn("[yellow]ID[/]");
            table.AddColumn("[yellow]Name[/]");
            table.AddColumn("[yellow]Version[/]");
            table.AddColumn("[yellow]Author[/]");

            foreach (var mod in mods)
            {
                table.AddRow(
                    new string[]
                    {
                        $"[cyan]{mod.Id}[/]",
                        mod.Name,
                        mod.Version.ToString(),
                        mod.Author ?? "[grey]Unknown[/]",
                    }
                );
            }

            AnsiConsole.Write(table);
            AnsiConsole.MarkupLine($"\n[grey]Total: {mods.Count} mod(s) loaded[/]");
        });
    }
}
