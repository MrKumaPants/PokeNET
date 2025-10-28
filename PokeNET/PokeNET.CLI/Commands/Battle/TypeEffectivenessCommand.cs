using System.ComponentModel;
using PokeNET.CLI.Infrastructure;
using Spectre.Console;
using Spectre.Console.Cli;

namespace PokeNET.CLI.Commands.Battle;

/// <summary>
/// Command to calculate type effectiveness.
/// </summary>
public class TypeEffectivenessCommand : CliCommand<TypeEffectivenessCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [CommandArgument(0, "<ATTACK_TYPE>")]
        [Description("Attacking move's type")]
        public string AttackType { get; set; } = string.Empty;

        [CommandArgument(1, "<DEFENSE_TYPE1>")]
        [Description("Defending Pokemon's primary type")]
        public string DefenseType1 { get; set; } = string.Empty;

        [CommandArgument(2, "[DEFENSE_TYPE2]")]
        [Description("Defending Pokemon's secondary type (optional)")]
        public string? DefenseType2 { get; set; }
    }

    public TypeEffectivenessCommand(CliContext context)
        : base(context) { }

    protected override async Task ExecuteCommandAsync(CommandContext context, Settings settings)
    {
        await AnsiConsole
            .Status()
            .StartAsync(
                "Calculating type effectiveness...",
                async ctx =>
                {
                    double effectiveness;

                    if (string.IsNullOrEmpty(settings.DefenseType2))
                    {
                        effectiveness = await Context.DataApi.GetTypeEffectivenessAsync(
                            settings.AttackType,
                            settings.DefenseType1
                        );
                    }
                    else
                    {
                        effectiveness = await Context.DataApi.GetDualTypeEffectivenessAsync(
                            settings.AttackType,
                            settings.DefenseType1,
                            settings.DefenseType2
                        );
                    }

                    DisplayEffectiveness(
                        settings.AttackType,
                        settings.DefenseType1,
                        settings.DefenseType2,
                        effectiveness
                    );
                }
            );
    }

    private static void DisplayEffectiveness(
        string attackType,
        string defenseType1,
        string? defenseType2,
        double effectiveness
    )
    {
        var defenseTypes = string.IsNullOrEmpty(defenseType2)
            ? defenseType1
            : $"{defenseType1}/{defenseType2}";

        var title = $"{attackType} vs {defenseTypes}";
        var panel = new Panel(new Markup($"[bold white]{title}[/]"))
        {
            Border = BoxBorder.Double,
            BorderStyle = new Style(Color.Aqua),
        };
        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();

        // Determine effectiveness text and color
        var (effectivenessText, color) = effectiveness switch
        {
            0 => ("No Effect", "grey"),
            0.25 => ("Very Not Effective", "red"),
            0.5 => ("Not Very Effective", "orange1"),
            1.0 => ("Normal Damage", "white"),
            2.0 => ("Super Effective", "green"),
            4.0 => ("Super Duper Effective", "lime"),
            _ => ($"{effectiveness}x", "yellow"),
        };

        var multiplierText = effectiveness == 0 ? "0×" : $"{effectiveness}×";

        var table = new Table();
        table.Border(TableBorder.Rounded);
        table.HideHeaders();
        table.AddColumn("Label");
        table.AddColumn("Value");

        table.AddRow("[yellow]Multiplier:[/]", $"[{color} bold]{multiplierText}[/]");
        table.AddRow("[yellow]Effect:[/]", $"[{color}]{effectivenessText}[/]");

        AnsiConsole.Write(table);
    }
}
