using System.ComponentModel;
using PokeNET.CLI.Infrastructure;
using Spectre.Console;
using Spectre.Console.Cli;

namespace PokeNET.CLI.Commands.Mod;

/// <summary>
/// Command to validate mod(s) without loading them.
/// </summary>
public class ValidateModCommand : CliCommand<ValidateModCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [CommandOption("--directory <PATH>")]
        [Description("Mods directory to validate")]
        [DefaultValue("Mods")]
        public string Directory { get; set; } = "Mods";
    }

    public ValidateModCommand(CliContext context)
        : base(context) { }

    protected override async Task ExecuteCommandAsync(CommandContext context, Settings settings)
    {
        await AnsiConsole
            .Status()
            .StartAsync(
                "Validating mods...",
                async ctx =>
                {
                    var report = await Context.ModLoader.ValidateModsAsync(settings.Directory);

                    ctx.Status("Rendering results...");

                    // Header
                    var statusColor = report.IsValid ? "green" : "red";
                    var statusText = report.IsValid ? "PASSED" : "FAILED";

                    var panel = new Panel(new Markup($"[{statusColor} bold]{statusText}[/]"))
                    {
                        Header = new PanelHeader("[yellow]Validation Results[/]"),
                        Border = BoxBorder.Double,
                        BorderStyle = new Style(report.IsValid ? Color.Green : Color.Red),
                    };
                    AnsiConsole.Write(panel);
                    AnsiConsole.WriteLine();

                    // Errors
                    if (report.Errors.Any())
                    {
                        var errorTable = new Table();
                        errorTable.Border(TableBorder.Rounded);
                        errorTable.AddColumn("[red]Mod ID[/]");
                        errorTable.AddColumn("[red]Error Type[/]");
                        errorTable.AddColumn("[red]Message[/]");

                        foreach (var error in report.Errors)
                        {
                            errorTable.AddRow(
                                error.ModId ?? "[grey]Unknown[/]",
                                error.ErrorType.ToString(),
                                error.Message
                            );
                        }

                        AnsiConsole.Write(
                            new Panel(errorTable)
                            {
                                Header = new PanelHeader($"[red]Errors ({report.Errors.Count})[/]"),
                                Border = BoxBorder.Rounded,
                            }
                        );
                        AnsiConsole.WriteLine();
                    }

                    // Warnings
                    if (report.Warnings.Any())
                    {
                        var warningTable = new Table();
                        warningTable.Border(TableBorder.Rounded);
                        warningTable.AddColumn("[yellow]Mod ID[/]");
                        warningTable.AddColumn("[yellow]Warning Type[/]");
                        warningTable.AddColumn("[yellow]Message[/]");

                        foreach (var warning in report.Warnings)
                        {
                            warningTable.AddRow(
                                warning.ModId,
                                warning.WarningType.ToString(),
                                warning.Message
                            );
                        }

                        AnsiConsole.Write(
                            new Panel(warningTable)
                            {
                                Header = new PanelHeader(
                                    $"[yellow]Warnings ({report.Warnings.Count})[/]"
                                ),
                                Border = BoxBorder.Rounded,
                            }
                        );
                        AnsiConsole.WriteLine();
                    }

                    if (!report.Errors.Any() && !report.Warnings.Any())
                    {
                        AnsiConsole.MarkupLine("[green]No issues found![/]");
                    }
                }
            );
    }
}
