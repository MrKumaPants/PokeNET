using PokeNET.CLI.Infrastructure;
using Spectre.Console.Cli;

namespace PokeNET.CLI.Commands;

/// <summary>
/// Base class for CLI commands that need access to CliContext.
/// </summary>
/// <typeparam name="TSettings">Command settings type.</typeparam>
public abstract class CliCommand<TSettings> : Spectre.Console.Cli.AsyncCommand<TSettings>
    where TSettings : CommandSettings
{
    protected CliContext Context { get; }

    protected CliCommand(CliContext context)
    {
        Context = context;
    }

    public sealed override async Task<int> ExecuteAsync(CommandContext context, TSettings settings, CancellationToken cancellationToken)
    {
        try
        {
            await ExecuteCommandAsync(context, settings);
            return 0;
        }
        catch (Exception ex)
        {
            Spectre.Console.AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
#if DEBUG
            Spectre.Console.AnsiConsole.WriteException(ex);
#endif
            return 1;
        }
    }

    /// <summary>
    /// Executes the command logic.
    /// </summary>
    protected abstract Task ExecuteCommandAsync(CommandContext context, TSettings settings);
}
