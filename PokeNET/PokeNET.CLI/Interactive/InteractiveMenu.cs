using PokeNET.CLI.Display;
using PokeNET.CLI.Infrastructure;
using PokeNET.Core.Data;
using Spectre.Console;

namespace PokeNET.CLI.Interactive;

/// <summary>
/// Interactive menu system for the CLI.
/// Provides a user-friendly menu interface when no command-line arguments are provided.
/// </summary>
public class InteractiveMenu
{
    private readonly CliContext _context;
    private bool _running = true;

    public InteractiveMenu(CliContext context)
    {
        _context = context;
    }

    public async Task RunAsync()
    {
        // Display welcome banner
        DisplayBanner();

        while (_running)
        {
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[yellow]What would you like to do?[/]")
                    .PageSize(10)
                    .AddChoices(
                        "Browse Data (Species, Moves, Items, Types)",
                        "Battle Calculator",
                        "System Tests",
                        "Mod Manager",
                        "Exit"
                    )
            );

            AnsiConsole.Clear();
            DisplayBanner();

            try
            {
                switch (choice)
                {
                    case "Browse Data (Species, Moves, Items, Types)":
                        await DataBrowserMenu();
                        break;
                    case "Battle Calculator":
                        await BattleMenu();
                        break;
                    case "System Tests":
                        await SystemTestsMenu();
                        break;
                    case "Mod Manager":
                        await ModManagerMenu();
                        break;
                    case "Exit":
                        _running = false;
                        AnsiConsole.MarkupLine("[green]Goodbye![/]");
                        break;
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
                AnsiConsole.MarkupLine("[grey]Press any key to continue...[/]");
                Console.ReadKey(true);
            }

            if (_running)
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[grey]Press any key to return to main menu...[/]");
                Console.ReadKey(true);
                AnsiConsole.Clear();
                DisplayBanner();
            }
        }
    }

    private static void DisplayBanner()
    {
        var banner = new Panel(
            Align.Center(
                new Markup("[bold yellow]PokeNET CLI Tool[/]\n[dim]Interactive Testing Environment[/]")
            )
        )
        {
            Border = BoxBorder.Double,
            BorderStyle = new Style(Color.Yellow)
        };
        AnsiConsole.Write(banner);
        AnsiConsole.WriteLine();
    }

    private async Task DataBrowserMenu()
    {
        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[yellow]What data would you like to browse?[/]")
                .AddChoices("Species", "Moves", "Items", "Types", "Back")
        );

        switch (choice)
        {
            case "Species":
                await BrowseSpecies();
                break;
            case "Moves":
                await BrowseMoves();
                break;
            case "Items":
                await BrowseItems();
                break;
            case "Types":
                await BrowseTypes();
                break;
        }
    }

    private async Task BrowseSpecies()
    {
        var species = await AnsiConsole.Status()
            .StartAsync("Loading species...", async ctx =>
            {
                return await _context.DataApi.GetAllSpeciesAsync();
            });

        if (!species.Any())
        {
            AnsiConsole.MarkupLine("[yellow]No species found[/]");
            return;
        }

        var selected = AnsiConsole.Prompt(
            new SelectionPrompt<SpeciesData>()
                .Title("[yellow]Select a species to view details:[/]")
                .PageSize(15)
                .AddChoices(species.Take(50))
                .UseConverter(s => $"#{s.NationalDexNumber:D3} {s.Name} ({string.Join("/", s.Types)})")
        );

        AnsiConsole.Clear();
        DisplayBanner();
        DataDisplayHelper.DisplaySpecies(selected);
    }

    private async Task BrowseMoves()
    {
        var moves = await AnsiConsole.Status()
            .StartAsync("Loading moves...", async ctx =>
            {
                return await _context.DataApi.GetAllMovesAsync();
            });

        if (!moves.Any())
        {
            AnsiConsole.MarkupLine("[yellow]No moves found[/]");
            return;
        }

        var selected = AnsiConsole.Prompt(
            new SelectionPrompt<MoveData>()
                .Title("[yellow]Select a move to view details:[/]")
                .PageSize(15)
                .AddChoices(moves.Take(50))
                .UseConverter(m => $"{m.Name} ({m.Type}) - Pwr: {(m.Power > 0 ? m.Power.ToString() : "—")}")
        );

        AnsiConsole.Clear();
        DisplayBanner();
        DataDisplayHelper.DisplayMove(selected);
    }

    private async Task BrowseItems()
    {
        var items = await AnsiConsole.Status()
            .StartAsync("Loading items...", async ctx =>
            {
                return await _context.DataApi.GetAllItemsAsync();
            });

        if (!items.Any())
        {
            AnsiConsole.MarkupLine("[yellow]No items found[/]");
            return;
        }

        var selected = AnsiConsole.Prompt(
            new SelectionPrompt<ItemData>()
                .Title("[yellow]Select an item to view details:[/]")
                .PageSize(15)
                .AddChoices(items.Take(50))
                .UseConverter(i => $"{i.Name} ({i.Category})")
        );

        AnsiConsole.Clear();
        DisplayBanner();
        DataDisplayHelper.DisplayItem(selected);
    }

    private async Task BrowseTypes()
    {
        var types = await AnsiConsole.Status()
            .StartAsync("Loading types...", async ctx =>
            {
                return await _context.DataApi.GetAllTypesAsync();
            });

        if (!types.Any())
        {
            AnsiConsole.MarkupLine("[yellow]No types found[/]");
            return;
        }

        var selected = AnsiConsole.Prompt(
            new SelectionPrompt<TypeData>()
                .Title("[yellow]Select a type to view effectiveness:[/]")
                .PageSize(15)
                .AddChoices(types)
                .UseConverter(t => t.Name)
        );

        AnsiConsole.Clear();
        DisplayBanner();
        DataDisplayHelper.DisplayType(selected, types);
    }

    private async Task BattleMenu()
    {
        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[yellow]Battle Calculator Options:[/]")
                .AddChoices("Calculate Stats", "Type Effectiveness", "Back")
        );

        switch (choice)
        {
            case "Calculate Stats":
                await CalculateStatsInteractive();
                break;
            case "Type Effectiveness":
                await CalculateTypeEffectiveness();
                break;
        }
    }

    private async Task CalculateStatsInteractive()
    {
        var speciesName = AnsiConsole.Ask<string>("[yellow]Enter species name:[/]");
        var species = await _context.DataApi.GetSpeciesByNameAsync(speciesName);

        if (species == null)
        {
            AnsiConsole.MarkupLine($"[red]Species '{speciesName}' not found[/]");
            return;
        }

        var level = AnsiConsole.Ask("[yellow]Enter level (1-100):[/]", 50);
        
        AnsiConsole.MarkupLine("\n[yellow]Calculating with 31 IVs, 0 EVs, and Hardy nature...[/]");
        AnsiConsole.MarkupLine("[dim]Use the command-line mode for custom IVs/EVs/Nature[/]\n");

        var stats = new Core.ECS.Components.PokemonStats
        {
            IV_HP = 31, IV_Attack = 31, IV_Defense = 31,
            IV_SpAttack = 31, IV_SpDefense = 31, IV_Speed = 31
        };

        Core.Battle.StatCalculator.RecalculateAllStats(
            ref stats,
            species.BaseStats.HP, species.BaseStats.Attack, species.BaseStats.Defense,
            species.BaseStats.SpecialAttack, species.BaseStats.SpecialDefense, species.BaseStats.Speed,
            level,
            Core.ECS.Components.Nature.Hardy
        );

        var table = new Table();
        table.Border(TableBorder.Rounded);
        table.AddColumn("[yellow]Stat[/]");
        table.AddColumn("[yellow]Base[/]");
        table.AddColumn("[yellow]Final[/]");

        table.AddRow("HP", species.BaseStats.HP.ToString(), $"[green bold]{stats.MaxHP}[/]");
        table.AddRow("Attack", species.BaseStats.Attack.ToString(), $"[green bold]{stats.Attack}[/]");
        table.AddRow("Defense", species.BaseStats.Defense.ToString(), $"[green bold]{stats.Defense}[/]");
        table.AddRow("Sp. Attack", species.BaseStats.SpecialAttack.ToString(), $"[green bold]{stats.SpAttack}[/]");
        table.AddRow("Sp. Defense", species.BaseStats.SpecialDefense.ToString(), $"[green bold]{stats.SpDefense}[/]");
        table.AddRow("Speed", species.BaseStats.Speed.ToString(), $"[green bold]{stats.Speed}[/]");

        AnsiConsole.Write(new Panel(table)
        {
            Header = new PanelHeader($"[yellow]{species.Name} Stats at Level {level}[/]"),
            Border = BoxBorder.Rounded
        });
    }

    private async Task CalculateTypeEffectiveness()
    {
        var attackType = AnsiConsole.Ask<string>("[yellow]Enter attacking type:[/]");
        var defenseType1 = AnsiConsole.Ask<string>("[yellow]Enter defending type 1:[/]");
        var defenseType2 = AnsiConsole.Confirm("[yellow]Does the defender have a second type?[/]", false)
            ? AnsiConsole.Ask<string>("[yellow]Enter defending type 2:[/]")
            : null;

        double effectiveness;
        if (string.IsNullOrEmpty(defenseType2))
        {
            effectiveness = await _context.DataApi.GetTypeEffectivenessAsync(attackType, defenseType1);
        }
        else
        {
            effectiveness = await _context.DataApi.GetDualTypeEffectivenessAsync(attackType, defenseType1, defenseType2);
        }

        var defenseTypes = string.IsNullOrEmpty(defenseType2) ? defenseType1 : $"{defenseType1}/{defenseType2}";
        var (effectivenessText, color) = effectiveness switch
        {
            0 => ("No Effect", "grey"),
            0.25 => ("Very Not Effective", "red"),
            0.5 => ("Not Very Effective", "orange1"),
            1.0 => ("Normal Damage", "white"),
            2.0 => ("Super Effective", "green"),
            4.0 => ("Super Duper Effective", "lime"),
            _ => ($"{effectiveness}x", "yellow")
        };

        var panel = new Panel(new Markup($"[{color} bold]{effectivenessText}[/]\n{effectiveness}× damage"))
        {
            Header = new PanelHeader($"[yellow]{attackType} vs {defenseTypes}[/]"),
            Border = BoxBorder.Double,
            BorderStyle = new Style(Color.Aqua)
        };
        AnsiConsole.Write(panel);
    }

    private async Task SystemTestsMenu()
    {
        await AnsiConsole.Progress()
            .StartAsync(async ctx =>
            {
                var task = ctx.AddTask("[yellow]Running system tests[/]");
                task.MaxValue = 5;

                task.Description = "[yellow]Testing species data...[/]";
                var species = await _context.DataApi.GetAllSpeciesAsync();
                task.Increment(1);
                AnsiConsole.MarkupLine($"[green]✓[/] Species: {species.Count} loaded");

                task.Description = "[yellow]Testing move data...[/]";
                var moves = await _context.DataApi.GetAllMovesAsync();
                task.Increment(1);
                AnsiConsole.MarkupLine($"[green]✓[/] Moves: {moves.Count} loaded");

                task.Description = "[yellow]Testing item data...[/]";
                var items = await _context.DataApi.GetAllItemsAsync();
                task.Increment(1);
                AnsiConsole.MarkupLine($"[green]✓[/] Items: {items.Count} loaded");

                task.Description = "[yellow]Testing type data...[/]";
                var types = await _context.DataApi.GetAllTypesAsync();
                task.Increment(1);
                AnsiConsole.MarkupLine($"[green]✓[/] Types: {types.Count} loaded");

                task.Description = "[yellow]Testing encounter data...[/]";
                var encounters = await _context.DataApi.GetAllEncountersAsync();
                task.Increment(1);
                AnsiConsole.MarkupLine($"[green]✓[/] Encounters: {encounters.Count} loaded");

                task.StopTask();

                AnsiConsole.WriteLine();
                var totalItems = species.Count + moves.Count + items.Count + types.Count + encounters.Count;
                AnsiConsole.MarkupLine($"[green bold]All tests passed![/] Total items: {totalItems}");
            });
    }

    private async Task ModManagerMenu()
    {
        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[yellow]Mod Manager Options:[/]")
                .AddChoices("List Loaded Mods", "Validate Mods", "Back")
        );

        switch (choice)
        {
            case "List Loaded Mods":
                await ListMods();
                break;
            case "Validate Mods":
                await ValidateMods();
                break;
        }
    }

    private async Task ListMods()
    {
        await Task.Run(() =>
        {
            var mods = _context.ModLoader.LoadedMods;

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
                table.AddRow(new string[]
                {
                    $"[cyan]{mod.Id}[/]",
                    mod.Name,
                    mod.Version.ToString(),
                    mod.Author ?? "[grey]Unknown[/]"
                });
            }

            AnsiConsole.Write(table);
            AnsiConsole.MarkupLine($"\n[grey]Total: {mods.Count} mod(s)[/]");
        });
    }

    private async Task ValidateMods()
    {
        var report = await AnsiConsole.Status()
            .StartAsync("Validating mods...", async ctx =>
            {
                return await _context.ModLoader.ValidateModsAsync("Mods");
            });

        var statusColor = report.IsValid ? "green" : "red";
        var statusText = report.IsValid ? "PASSED" : "FAILED";
        
        var panel = new Panel(new Markup($"[{statusColor} bold]{statusText}[/]"))
        {
            Header = new PanelHeader("[yellow]Validation Results[/]"),
            Border = BoxBorder.Double,
            BorderStyle = new Style(report.IsValid ? Color.Green : Color.Red)
        };
        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();

        if (report.Errors.Any())
        {
            AnsiConsole.MarkupLine($"[red]Errors: {report.Errors.Count}[/]");
            foreach (var error in report.Errors.Take(5))
            {
                AnsiConsole.MarkupLine($"  [red]•[/] {error.Message}");
            }
        }

        if (report.Warnings.Any())
        {
            AnsiConsole.MarkupLine($"[yellow]Warnings: {report.Warnings.Count}[/]");
            foreach (var warning in report.Warnings.Take(5))
            {
                AnsiConsole.MarkupLine($"  [yellow]•[/] {warning.Message}");
            }
        }

        if (!report.Errors.Any() && !report.Warnings.Any())
        {
            AnsiConsole.MarkupLine("[green]No issues found![/]");
        }
    }
}

