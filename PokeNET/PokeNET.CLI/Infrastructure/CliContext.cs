using Microsoft.Extensions.Logging;
using PokeNET.Core.Data;
using PokeNET.Core.Modding;
using PokeNET.Scripting.Abstractions;

namespace PokeNET.CLI.Infrastructure;

/// <summary>
/// Shared context for all CLI commands.
/// Provides access to core services.
/// </summary>
public class CliContext
{
    /// <summary>
    /// Data API for accessing game data.
    /// </summary>
    public IDataApi DataApi { get; }

    /// <summary>
    /// Mod loader for managing mods.
    /// </summary>
    public IModLoader ModLoader { get; }

    /// <summary>
    /// Scripting engine for executing scripts.
    /// </summary>
    public IScriptingEngine ScriptingEngine { get; }

    /// <summary>
    /// Logger for CLI operations.
    /// </summary>
    public ILogger Logger { get; }

    /// <summary>
    /// Initializes a new instance of the CLI context.
    /// </summary>
    public CliContext(
        IDataApi dataApi,
        IModLoader modLoader,
        IScriptingEngine scriptingEngine,
        ILogger<CliContext> logger)
    {
        DataApi = dataApi;
        ModLoader = modLoader;
        ScriptingEngine = scriptingEngine;
        Logger = logger;
    }
}

