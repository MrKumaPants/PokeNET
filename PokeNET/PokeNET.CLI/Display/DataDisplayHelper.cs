using PokeNET.Core.Data;
using Spectre.Console;

namespace PokeNET.CLI.Display;

/// <summary>
/// Shared display methods for rendering data in both commands and interactive menu.
/// </summary>
public static class DataDisplayHelper
{
    public static void DisplaySpecies(SpeciesData species)
    {
        // Header
        var typeStr = string.Join("/", species.Types);
        var title = $"#{species.NationalDexNumber:D3} {species.Name} ({typeStr})";
        
        var panel = new Panel(new Markup($"[bold white]{title}[/]"))
        {
            Border = BoxBorder.Double,
            BorderStyle = new Style(Color.Yellow)
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

        AnsiConsole.Write(new Panel(statsTable)
        {
            Header = new PanelHeader("[yellow]Base Stats[/]"),
            Border = BoxBorder.Rounded
        });
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
            var evos = string.Join(", ", species.Evolutions.Select(e => 
                $"{e.TargetSpeciesId} at Lv.{e.RequiredLevel}"));
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
            _ => $"{(species.GenderRatio / 254.0 * 100):F1}% Female"
        };
        detailsTable.AddRow("Gender Ratio", genderRatio);
        
        if (species.EggGroups?.Any() == true)
        {
            detailsTable.AddRow("Egg Groups", string.Join(", ", species.EggGroups));
            detailsTable.AddRow("Hatch Steps", species.HatchSteps.ToString());
        }

        AnsiConsole.Write(new Panel(detailsTable)
        {
            Header = new PanelHeader("[cyan]Details[/]"),
            Border = BoxBorder.Rounded
        });
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

            AnsiConsole.Write(new Panel(movesTable)
            {
                Header = new PanelHeader("[green]Level-up Moves[/]"),
                Border = BoxBorder.Rounded
            });
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

    public static void DisplayMove(MoveData move)
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

    public static void DisplayItem(ItemData item)
    {
        // Header
        var panel = new Panel(new Markup($"[bold white]{item.Name}[/]"))
        {
            Border = BoxBorder.Double,
            BorderStyle = new Style(Color.Green)
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
            var paramsStr = string.Join(", ", item.EffectParameters.Select(kvp => $"{kvp.Key}: {kvp.Value}"));
            table.AddRow("[grey]Effect Params:[/]", $"[grey]{paramsStr}[/]");
        }

        AnsiConsole.Write(table);
    }

    public static void DisplayType(TypeData typeData, IReadOnlyList<TypeData> allTypes)
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
            _ => "red"
        };

        table.AddRow(
            statName,
            $"[{color}]{value}[/]",
            $"[{color}]{bar}[/]"
        );
    }
}

