using Microsoft.Extensions.Logging;
using UniversalTasker.CLI;
using UniversalTasker.Core.Execution;
using UniversalTasker.Core.Workflows;
using UniversalTasker.Serialization;

class Program
{
    private static readonly CancellationTokenSource Cts = new();
    private static bool _verbose = false;
    private static string? _logFilePath = null;

    static async Task<int> Main(string[] args)
    {
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            Cts.Cancel();
            Console.WriteLine("\nInterrupted by user (Ctrl+C)");
        };

        if (args.Length == 0)
        {
            return await InteractiveMode();
        }

        var command = args[0].ToLowerInvariant();
        var remainingArgs = args.Skip(1).ToArray();

        // Check for global flags
        if (remainingArgs.Contains("--verbose") || remainingArgs.Contains("-v"))
        {
            _verbose = true;
            remainingArgs = remainingArgs.Where(a => a != "--verbose" && a != "-v").ToArray();
        }

        // Check for --log-file flag
        var logFileIndex = Array.IndexOf(remainingArgs, "--log-file");
        if (logFileIndex >= 0 && logFileIndex + 1 < remainingArgs.Length)
        {
            _logFilePath = remainingArgs[logFileIndex + 1];
            remainingArgs = remainingArgs.Where((_, i) => i != logFileIndex && i != logFileIndex + 1).ToArray();
        }

        try
        {
            return command switch
            {
                "run" => await RunCommand(remainingArgs),
                "validate" => ValidateCommand(remainingArgs),
                "create" => await CreateCommand(remainingArgs),
                "export" => await ExportCommand(remainingArgs),
                "list-actions" => ListActionsCommand(),
                "list-triggers" => ListTriggersCommand(),
                "list-plugins" => ListPluginsCommand(),
                "--help" or "-h" or "help" => Help(),
                "--version" or "version" => Version(),
                _ => UnknownCommand(command)
            };
        }
        catch (OperationCanceledException)
        {
            return ExitCodes.Interrupted;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error: {ex.Message}");
            Console.ResetColor();

            if (_verbose)
            {
                Console.WriteLine(ex.StackTrace);
            }

            return ExitCodes.GeneralError;
        }
    }

    static async Task<int> RunCommand(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Error: No workflow file specified");
            Console.WriteLine("Usage: tasker run <workflow.json> [--verbose]");
            return ExitCodes.GeneralError;
        }

        var filePath = args[0];
        if (!File.Exists(filePath))
        {
            Console.WriteLine($"Error: File not found: {filePath}");
            return ExitCodes.FileNotFound;
        }

        var logLevel = _verbose ? LogLevel.Debug : LogLevel.Information;
        ILogger logger;
        FileLogger? fileLogger = null;

        if (_logFilePath != null)
        {
            var consoleLogger = new ConsoleLogger("Workflow", logLevel);
            fileLogger = new FileLogger("Workflow", _logFilePath, logLevel);
            logger = new CompositeLogger(consoleLogger, fileLogger);
            Console.WriteLine($"Logging to: {_logFilePath}");
        }
        else
        {
            logger = new ConsoleLogger("Workflow", logLevel);
        }

        LoadPlugins();

        Console.WriteLine($"Loading workflow: {filePath}");

        var serializer = new WorkflowSerializer();
        var workflow = await serializer.LoadAsync(filePath);

        Console.WriteLine($"Workflow: {workflow.Name}");
        Console.WriteLine($"  Actions: {workflow.Actions.Count}");
        Console.WriteLine($"  Triggers: {workflow.Triggers.Count}");
        Console.WriteLine();

        using var host = new WorkflowHost(logger);
        host.Load(workflow);

        var executionResult = ExecutionState.Idle;
        Exception? executionException = null;

        host.ExecutionCompleted += (_, e) =>
        {
            executionResult = e.FinalState;
            executionException = e.Exception;
        };

        if (workflow.Triggers.Count > 0)
        {
            Console.WriteLine("Starting workflow with triggers (press Ctrl+C to stop)...");
            host.Start();

            // Wait for cancellation
            try
            {
                await Task.Delay(Timeout.Infinite, Cts.Token);
            }
            catch (OperationCanceledException)
            {
                host.Stop();
            }
        }
        else
        {
            Console.WriteLine("Executing workflow...");
            await host.ExecuteAsync(Cts.Token);
        }

        fileLogger?.Dispose();

        Console.WriteLine();
        Console.WriteLine($"Execution completed: {executionResult}");

        return executionResult switch
        {
            ExecutionState.Completed => ExitCodes.Success,
            ExecutionState.Cancelled => ExitCodes.Interrupted,
            ExecutionState.Failed => ExitCodes.ExecutionFailed,
            _ => ExitCodes.Success
        };
    }

    static int ValidateCommand(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Error: No workflow file specified");
            Console.WriteLine("Usage: tasker validate <workflow.json>");
            return ExitCodes.GeneralError;
        }

        var filePath = args[0];
        if (!File.Exists(filePath))
        {
            Console.WriteLine($"Error: File not found: {filePath}");
            return ExitCodes.FileNotFound;
        }

        LoadPlugins();

        Console.WriteLine($"Validating: {filePath}");

        var json = File.ReadAllText(filePath);
        var serializer = new WorkflowSerializer();
        var result = serializer.Validate(json);

        if (result.IsValid)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Validation passed!");
            Console.ResetColor();

            // Try to deserialize to show summary
            try
            {
                var workflow = serializer.Deserialize(json);
                Console.WriteLine($"\nWorkflow: {workflow.Name}");
                Console.WriteLine($"  Version: {workflow.Version}");
                Console.WriteLine($"  Actions: {workflow.Actions.Count}");
                Console.WriteLine($"  Triggers: {workflow.Triggers.Count}");
                Console.WriteLine($"  Variables: {workflow.Variables.Count}");
            }
            catch
            {
                // Ignore deserialization errors during validation display
            }

            return ExitCodes.Success;
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Validation failed!");
            Console.ResetColor();

            foreach (var error in result.Errors)
            {
                Console.WriteLine($"  - {error}");
            }

            foreach (var warning in result.Warnings)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"  Warning: {warning}");
                Console.ResetColor();
            }

            return ExitCodes.ValidationFailed;
        }
    }

    static async Task<int> CreateCommand(string[] args)
    {
        var filePath = args.Length > 0 ? args[0] : "workflow.json";

        if (File.Exists(filePath))
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Warning: File already exists: {filePath}");
            Console.ResetColor();
            Console.Write("Overwrite? [y/N] ");
            var response = Console.ReadLine();
            if (!string.Equals(response, "y", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("Cancelled.");
                return ExitCodes.GeneralError;
            }
        }

        var workflow = new Workflow
        {
            Name = System.IO.Path.GetFileNameWithoutExtension(filePath),
            Description = "Created by tasker CLI",
            Version = 1
        };

        var serializer = new WorkflowSerializer();
        await serializer.SaveAsync(workflow, filePath);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"Created workflow: {filePath}");
        Console.ResetColor();
        Console.WriteLine($"  Name: {workflow.Name}");
        Console.WriteLine($"  Edit the file to add actions and triggers.");
        Console.WriteLine($"  Run with: tasker run {filePath}");

        return ExitCodes.Success;
    }

    static async Task<int> ExportCommand(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Error: No workflow file specified");
            Console.WriteLine("Usage: tasker export <workflow.json> [output.ps1]");
            return ExitCodes.GeneralError;
        }

        var inputPath = args[0];
        if (!File.Exists(inputPath))
        {
            Console.WriteLine($"Error: File not found: {inputPath}");
            return ExitCodes.FileNotFound;
        }

        var outputPath = args.Length > 1
            ? args[1]
            : Path.ChangeExtension(inputPath, ".ps1");

        LoadPlugins();

        Console.WriteLine($"Loading workflow: {inputPath}");

        var serializer = new WorkflowSerializer();
        var workflow = await serializer.LoadAsync(inputPath);

        Console.WriteLine($"Workflow: {workflow.Name}");
        Console.WriteLine($"  Actions: {workflow.Actions.Count}");
        Console.WriteLine();

        var exporter = new PowerShellExporter();
        var script = exporter.Export(workflow);

        await File.WriteAllTextAsync(outputPath, script);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"Exported to: {outputPath}");
        Console.ResetColor();
        Console.WriteLine($"  Run with: powershell -ExecutionPolicy Bypass -File {outputPath}");

        return ExitCodes.Success;
    }

    static int ListActionsCommand()
    {
        Console.WriteLine("Available Actions:");
        Console.WriteLine();

        var registry = ActionTypeRegistry.Default;
        var actions = registry.GetAllRegisteredTypes()
            .OrderBy(a => a.Metadata.Category)
            .ThenBy(a => a.Metadata.DisplayName);

        string? currentCategory = null;
        foreach (var (typeId, type, metadata) in actions)
        {
            if (currentCategory != metadata.Category)
            {
                currentCategory = metadata.Category;
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"\n  [{currentCategory}]");
                Console.ResetColor();
            }

            Console.WriteLine($"    {metadata.DisplayName,-20} (type: \"{typeId}\")");
        }

        Console.WriteLine();
        return ExitCodes.Success;
    }

    static int ListTriggersCommand()
    {
        Console.WriteLine("Available Triggers:");
        Console.WriteLine();

        var registry = TriggerTypeRegistry.Default;
        foreach (var (typeId, type, metadata) in registry.GetAllRegisteredTypes())
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write($"  {metadata.DisplayName,-20}");
            Console.ResetColor();
            Console.Write($" (type: \"{typeId}\")");
            if (!string.IsNullOrEmpty(metadata.Description))
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write($" - {metadata.Description}");
                Console.ResetColor();
            }
            Console.WriteLine();
        }

        Console.WriteLine();
        return ExitCodes.Success;
    }

    static int Help()
    {
        PrintUsage();
        return ExitCodes.Success;
    }

    static int ListPluginsCommand()
    {
        var pluginsDir = GetPluginsDirectory();
        var loader = new PluginLoader();
        var plugins = loader.LoadPlugins(pluginsDir, ActionTypeRegistry.Default, TriggerTypeRegistry.Default);

        if (plugins.Count == 0)
        {
            Console.WriteLine("No plugins found.");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"  Plugin directory: {pluginsDir}");
            Console.ResetColor();
            return ExitCodes.Success;
        }

        Console.WriteLine($"Loaded Plugins ({plugins.Count}):");
        Console.WriteLine();

        foreach (var plugin in plugins)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write($"  {plugin.Name}");
            Console.ResetColor();
            Console.WriteLine($" v{plugin.Version} by {plugin.Author}");

            if (!string.IsNullOrEmpty(plugin.Description))
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"    {plugin.Description}");
                Console.ResetColor();
            }

            if (plugin.ActionTypeIds.Count > 0)
            {
                Console.WriteLine($"    Actions: {string.Join(", ", plugin.ActionTypeIds)}");
            }

            if (plugin.TriggerTypeIds.Count > 0)
            {
                Console.WriteLine($"    Triggers: {string.Join(", ", plugin.TriggerTypeIds)}");
            }
        }

        Console.WriteLine();
        return ExitCodes.Success;
    }

    static void LoadPlugins()
    {
        var pluginsDir = GetPluginsDirectory();
        if (!Directory.Exists(pluginsDir)) return;

        var loader = new PluginLoader();
        var plugins = loader.LoadPlugins(pluginsDir, ActionTypeRegistry.Default, TriggerTypeRegistry.Default);

        if (plugins.Count > 0 && _verbose)
        {
            Console.WriteLine($"Loaded {plugins.Count} plugin(s)");
        }
    }

    static string GetPluginsDirectory()
    {
        return Path.Combine(AppContext.BaseDirectory, "plugins");
    }

    static int Version()
    {
        Console.WriteLine("Universal Tasker CLI v4.0.0");
        return ExitCodes.Success;
    }

    static int UnknownCommand(string command)
    {
        Console.WriteLine($"Unknown command: {command}");
        Console.WriteLine("Use 'tasker --help' for usage information.");
        return ExitCodes.GeneralError;
    }

    static async Task<int> InteractiveMode()
    {
        Console.WriteLine("Universal Tasker CLI - Interactive Mode");
        Console.WriteLine("Type 'help' for available commands, 'exit' to quit.");
        Console.WriteLine();

        while (!Cts.Token.IsCancellationRequested)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("tasker> ");
            Console.ResetColor();

            var line = Console.ReadLine();
            if (line == null) break; // EOF (e.g. piped input ended)

            line = line.Trim();
            if (string.IsNullOrEmpty(line)) continue;

            var parts = SplitArgs(line);
            var command = parts[0].ToLowerInvariant();
            var cmdArgs = parts.Skip(1).ToArray();

            if (command is "exit" or "quit") break;

            // Apply flags from the interactive input each iteration
            _verbose = false;
            _logFilePath = null;

            if (cmdArgs.Contains("--verbose") || cmdArgs.Contains("-v"))
            {
                _verbose = true;
                cmdArgs = cmdArgs.Where(a => a != "--verbose" && a != "-v").ToArray();
            }

            var logFileIndex = Array.IndexOf(cmdArgs, "--log-file");
            if (logFileIndex >= 0 && logFileIndex + 1 < cmdArgs.Length)
            {
                _logFilePath = cmdArgs[logFileIndex + 1];
                cmdArgs = cmdArgs.Where((_, i) => i != logFileIndex && i != logFileIndex + 1).ToArray();
            }

            try
            {
                int result = command switch
                {
                    "run" => await RunCommand(cmdArgs),
                    "validate" => ValidateCommand(cmdArgs),
                    "create" => await CreateCommand(cmdArgs),
                    "export" => await ExportCommand(cmdArgs),
                    "list-actions" => ListActionsCommand(),
                    "list-triggers" => ListTriggersCommand(),
                    "list-plugins" => ListPluginsCommand(),
                    "--help" or "-h" or "help" => Help(),
                    "--version" or "version" => Version(),
                    _ => UnknownCommand(command)
                };
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Cancelled.");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error: {ex.Message}");
                Console.ResetColor();
                if (_verbose) Console.WriteLine(ex.StackTrace);
            }

            Console.WriteLine();
        }

        return ExitCodes.Success;
    }

    static string[] SplitArgs(string line)
    {
        var args = new List<string>();
        var current = new System.Text.StringBuilder();
        bool inQuotes = false;
        char quoteChar = '"';

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (inQuotes)
            {
                if (c == quoteChar)
                    inQuotes = false;
                else
                    current.Append(c);
            }
            else if (c == '"' || c == '\'')
            {
                inQuotes = true;
                quoteChar = c;
            }
            else if (c == ' ')
            {
                if (current.Length > 0)
                {
                    args.Add(current.ToString());
                    current.Clear();
                }
            }
            else
            {
                current.Append(c);
            }
        }

        if (current.Length > 0)
            args.Add(current.ToString());

        return args.ToArray();
    }

    static void PrintUsage()
    {
        Console.WriteLine(@"
Universal Tasker CLI

Usage:
  tasker <command> [options]

Commands:
  run <workflow.json>      Run a workflow file
  validate <workflow.json> Validate a workflow file without running
  create [filename.json]   Create a new scaffold workflow file
  export <workflow.json> [output.ps1]
                           Export workflow as a PowerShell script
  list-actions             List all available action types
  list-triggers            List all available trigger types
  list-plugins             List all loaded plugins
  help                     Show this help message
  version                  Show version information

Options:
  --verbose, -v            Enable verbose output
  --log-file <path>        Write log output to a file (use with run)

Examples:
  tasker run my-workflow.json
  tasker run automation.json --verbose
  tasker run workflow.json --log-file output.log
  tasker validate workflow.json
  tasker create my-automation.json
  tasker export workflow.json output.ps1
  tasker list-actions

Exit Codes:
  0   Success
  1   General error
  2   File not found
  3   Validation failed
  4   Execution failed
  130 Interrupted (Ctrl+C)
");
    }
}
