# Universal Tasker

> A Windows desktop automation tool for building, running, and exporting trigger-based workflows — available as both a graphical editor and a headless CLI.

## Overview

Universal Tasker lets you compose automation workflows from a library of built-in actions (mouse clicks, key presses, delays, loops, conditionals, variable management) and attach them to triggers (timers, hotkeys, file-system events). Workflows are saved as human-readable JSON files that can be executed directly, validated ahead of time, or exported to standalone PowerShell scripts.

The project ships two end-user entry points:

- **UniversalTasker.UI** — a WPF desktop editor for visually building and running workflows with a live execution log, undo/redo, and drag-and-drop reordering.
- **UniversalTasker.CLI** (`tasker.exe`) — a command-line tool for running, validating, scaffolding, and exporting workflows from scripts or CI pipelines.

A plugin system allows third-party assemblies to contribute additional action and trigger types without modifying the core library.

## Prerequisites

| Requirement | Version |
|---|---|
| .NET SDK | 10.0 or later |
| Windows | Required (WPF and Windows API P/Invoke are used throughout) |
| Visual Studio 2022 | Recommended for the UI project; optional for CLI-only work |

> Universal Tasker targets `net10.0-windows` exclusively. It cannot be built or run on Linux or macOS.

## Installation

### Clone and build from source

```bash
git clone <repository-url>
cd Tasker
dotnet build UniversalTasker.sln
```

### Build a specific project

```bash
# CLI only
dotnet build src/UniversalTasker.CLI/UniversalTasker.CLI.csproj

# WPF UI only
dotnet build src/UniversalTasker.UI/UniversalTasker.UI.csproj
```

### Publish a self-contained CLI executable

```bash
dotnet publish src/UniversalTasker.CLI/UniversalTasker.CLI.csproj \
  -c Release \
  -r win-x64 \
  --self-contained true \
  -o publish/cli
```

The compiled CLI binary is named `tasker.exe`. The WPF application binary is `UniversalTasker.UI.exe`.

## Usage

### Graphical Editor (UniversalTasker.UI)

Launch `UniversalTasker.UI.exe` to open the workflow editor. The window is divided into three panels:

- **Actions** (left) — ordered list of actions in the workflow; drag to reorder.
- **Triggers / Variables** (center) — event sources that start the workflow and initial variable values.
- **Properties** (right) — settings for the currently selected action or trigger.

**Keyboard shortcuts**

| Shortcut | Action |
|---|---|
| `Ctrl+N` | New workflow |
| `Ctrl+O` | Open workflow file |
| `Ctrl+S` | Save |
| `Ctrl+Shift+S` | Save As |
| `Ctrl+Z` | Undo |
| `Ctrl+Y` | Redo |

**Hotkeys** for starting and stopping execution are configurable in the collapsible "Hotkey Settings" panel. The defaults are `F6` (Start) and `F7` (Stop). A toggle mode is available so a single key starts and stops execution.

**PowerShell export** is available from `File > Export as PowerShell Script...` or from the CLI (see below).

---

### CLI (tasker.exe)

```
tasker <command> [options]
```

#### Commands

| Command | Description |
|---|---|
| `run <workflow.json>` | Execute a workflow file |
| `validate <workflow.json>` | Validate a workflow file without running it |
| `create [filename.json]` | Scaffold a new empty workflow file |
| `export <workflow.json> [output.ps1]` | Export a workflow as a PowerShell script |
| `list-actions` | Print all available action types |
| `list-triggers` | Print all available trigger types |
| `list-plugins` | List plugins loaded from the `plugins/` directory |
| `help` | Show usage information |
| `version` | Print the version string |

#### Global options

| Flag | Description |
|---|---|
| `--verbose`, `-v` | Enable debug-level log output |
| `--log-file <path>` | Write log output to a file (used with `run`) |

#### Examples

```bash
# Scaffold a new workflow file
tasker create my-automation.json

# Run a workflow (no triggers — executes once and exits)
tasker run my-automation.json

# Run a trigger-based workflow (stays alive until Ctrl+C)
tasker run my-automation.json --verbose

# Run and write logs to a file simultaneously
tasker run my-automation.json --log-file run.log

# Validate a workflow before running
tasker validate my-automation.json

# Export to a standalone PowerShell script
tasker export my-automation.json my-automation.ps1

# Inspect available action and trigger types
tasker list-actions
tasker list-triggers
```

#### Exit codes

| Code | Meaning |
|---|---|
| `0` | Success |
| `1` | General error |
| `2` | File not found |
| `3` | Validation failed |
| `4` | Execution failed |
| `130` | Interrupted by Ctrl+C |

---

## Workflow File Format

Workflows are stored as JSON. Use `tasker create workflow.json` to generate a skeleton, then edit it manually or use the UI editor.

### Minimal example

```json
{
  "name": "My Automation",
  "description": "Click and wait example",
  "version": 1,
  "settings": {
    "stopOnError": true,
    "maxExecutionTimeSeconds": 0,
    "logLevel": "Information",
    "enableTriggersOnStart": true,
    "allowConcurrentExecution": false
  },
  "variables": {
    "counter": 0
  },
  "triggers": [],
  "actions": [
    {
      "$type": "mouseclick",
      "name": "Click OK",
      "x": 960,
      "y": 540,
      "button": "Left",
      "clickCount": 1
    },
    {
      "$type": "delay",
      "name": "Wait 500ms",
      "durationMs": 500
    }
  ]
}
```

### Trigger-based workflow (timer)

```json
{
  "name": "Periodic Automation",
  "triggers": [
    {
      "$type": "timer",
      "name": "Every 30 Seconds",
      "interval": "00:00:30",
      "fireImmediately": false,
      "isEnabled": true,
      "configuration": {
        "debounceTime": "00:00:00",
        "throttleTime": "00:00:00",
        "enabledOnStart": true,
        "maxFireCount": 0
      }
    }
  ],
  "actions": []
}
```

### Conditional and loop example

```json
{
  "name": "Loop with Condition",
  "variables": { "i": 0 },
  "triggers": [],
  "actions": [
    {
      "$type": "repeat",
      "name": "Main Loop",
      "repeatCount": 10,
      "counterVariable": "i",
      "actions": [
        {
          "$type": "condition",
          "name": "Check Counter",
          "condition": {
            "leftOperand": "{i}",
            "operator": "GreaterOrEqual",
            "rightOperand": "5"
          },
          "thenActions": [
            { "$type": "break", "name": "Stop Early" }
          ],
          "elseActions": [
            { "$type": "delay", "name": "Wait", "durationMs": 200 }
          ]
        }
      ]
    }
  ]
}
```

---

## Actions Reference

### Input

| Type ID | Description |
|---|---|
| `mouseclick` | Move the cursor and click a mouse button. Properties: `x`, `y`, `button` (`Left`/`Right`/`Middle`), `clickCount`. |
| `keypress` | Simulate a key press with optional modifier keys. Properties: `virtualKeyCode` (hex), `ctrl`, `alt`, `shift`. |

### Flow Control

| Type ID | Description |
|---|---|
| `delay` | Pause execution. Property: `durationMs` (integer milliseconds). |
| `sequence` | Group actions into a named block. Property: `actions` (array). |
| `condition` | If/Else branch. Properties: `condition`, `thenActions`, `elseActions`. |
| `repeat` | Repeat a block N times. Properties: `repeatCount`, `counterVariable`, `actions`. |
| `while` | Loop while a condition is true. Properties: `condition`, `maxIterations`, `actions`. |
| `break` | Exit the enclosing loop immediately. |
| `continue` | Skip to the next loop iteration. |

### Variables

| Type ID | Description |
|---|---|
| `setvariable` | Assign or update a variable. Properties: `variableName`, `value`, `evaluateAsExpression`. |

### Conditions

Conditions support the following comparison operators: `Equals`, `NotEquals`, `LessThan`, `GreaterThan`, `LessOrEqual`, `GreaterOrEqual`, `Contains`, `StartsWith`, `EndsWith`.

Operands can be:
- Variable references: `{varName}`
- String literals: `"hello"` or `'hello'`
- Numeric literals: `42`, `3.14`
- Built-in variables: `{timestamp}`, `{now}`, `{date}`, `{time}`, `{mousex}`, `{mousey}`

Conditions can be chained with `logicalOp` (`And`/`Or`) and a `nextCondition` object.

### Variable Interpolation

String values in `setvariable` and condition operands support `{varName}` interpolation. For example, a value of `"Hello, {name}!"` is expanded at runtime using the current value of the `name` variable.

---

## Triggers Reference

| Type ID | Description |
|---|---|
| `timer` | Fire at a fixed interval. Properties: `interval` (TimeSpan string, e.g. `"00:01:00"`), `fireImmediately`, `startTime`. |
| `hotkey` | Fire when a keyboard shortcut is pressed. Properties: `virtualKeyCode`, `ctrl`, `alt`, `shift`. |
| `filesystem` | Fire when files or directories change. Properties: `path`, `filter` (e.g. `"*.log"`), `includeSubdirectories`, `watchCreated`, `watchChanged`, `watchDeleted`, `watchRenamed`. |

Every trigger exposes a `configuration` object with:

| Property | Default | Description |
|---|---|---|
| `debounceTime` | `"00:00:00"` | Minimum gap between fires; additional fires within this window are ignored. |
| `throttleTime` | `"00:00:00"` | Maximum fire frequency within a time window. |
| `enabledOnStart` | `true` | Whether the trigger is active when the workflow starts. |
| `maxFireCount` | `0` (unlimited) | Maximum number of times the trigger may fire. |

When a trigger fires, the following variables are automatically injected into the execution context:

- `trigger_name` — the trigger's display name
- `trigger_fired_at` — the timestamp of the event
- Additional trigger-specific keys (e.g. `trigger_fullPath`, `trigger_changeType` for filesystem triggers)

---

## Workflow Settings Reference

The `settings` object inside a workflow JSON controls runtime behaviour:

| Property | Type | Default | Description |
|---|---|---|---|
| `stopOnError` | bool | `true` | Halt execution if any action throws an exception. |
| `maxExecutionTimeSeconds` | int | `0` | Hard timeout in seconds. `0` means no limit. |
| `logLevel` | string | `"Information"` | Minimum log level (`Trace`, `Debug`, `Information`, `Warning`, `Error`). |
| `enableTriggersOnStart` | bool | `true` | Activate all triggers when the workflow host starts. |
| `allowConcurrentExecution` | bool | `false` | When `false`, a second trigger fire is dropped if execution is already in progress. |

---

## Plugin System

Universal Tasker discovers plugins at startup from a `plugins/` directory located next to the executable. Each plugin lives in its own subdirectory and must contain a `plugin.json` manifest:

```json
{
  "name": "My Plugin",
  "version": "1.0.0",
  "author": "Author Name",
  "description": "Adds custom actions.",
  "assembly": "MyPlugin.dll"
}
```

The assembly referenced by `assembly` must export one or more of:

- **Action classes** — implement `IAction`, carry `[ActionMetadata(typeId, displayName, category)]`.
- **Trigger classes** — implement `ITrigger`, carry `[TriggerMetadata(typeId, displayName, description)]`.
- **Plugin entry point** — implement `IActionPlugin`; its `Initialize()` method is called once after load.

Plugin assemblies are loaded with `AssemblyLoadContext.Default`, so they share the same runtime as the host. List loaded plugins with:

```bash
tasker list-plugins
```

---

## Project Structure

```
Tasker/
├── UniversalTasker.sln
└── src/
    ├── UniversalTasker.Core/           # Domain model — no UI or serialization dependencies
    │   ├── Actions/                    # IAction implementations + ExecutionContext
    │   ├── Execution/                  # ExecutionEngine (used by UI)
    │   ├── Expressions/                # Expression evaluator and Condition model
    │   ├── Input/                      # Windows API wrappers: keyboard/mouse hooks + simulator
    │   ├── Plugins/                    # IActionPlugin interface and PluginManifest model
    │   ├── Triggers/                   # ITrigger implementations + TriggerManager
    │   └── Workflows/                  # Workflow, WorkflowHost, WorkflowSettings
    ├── UniversalTasker.Serialization/  # JSON serialization, validation, plugin loader, PS exporter
    ├── UniversalTasker.CLI/            # Console entry point (tasker.exe)
    ├── UniversalTasker.UI/             # WPF desktop editor (UniversalTasker.UI.exe)
    │   ├── ViewModels/                 # MVVM view models (CommunityToolkit.Mvvm)
    │   ├── Views/                      # XAML windows and action editor templates
    │   └── Services/                   # HotkeyService, UndoManager, drag-drop behavior
    └── UniversalTasker.Tests/          # xUnit test suite
        ├── Actions/
        ├── Execution/
        ├── Expressions/
        ├── Plugins/
        ├── Serialization/              # Round-trip and validation tests
        ├── Triggers/
        └── Workflows/
```

---

## Development

### Run the test suite

```bash
dotnet test UniversalTasker.sln
```

### Run tests with code coverage

```bash
dotnet test UniversalTasker.sln --collect:"XPlat Code Coverage"
```

Coverage collection uses `coverlet.collector` (included in the test project).

### Run a specific test project

```bash
dotnet test src/UniversalTasker.Tests/UniversalTasker.Tests.csproj
```

### Build in Release mode

```bash
dotnet build UniversalTasker.sln -c Release
```

### Extending the core library

1. Create a class that implements `IAction` (or inherits `ActionBase`) in `UniversalTasker.Core/Actions/`.
2. Decorate it with `[ActionMetadata("your-type-id", "Display Name", "Category")]`.
3. The type is automatically registered in `ActionTypeRegistry.Default` via reflection at startup.
4. Use the same pattern for new triggers by implementing `TriggerBase` and applying `[TriggerMetadata(...)]`.

To ship an extension as a plugin rather than modifying the core, follow the Plugin System instructions above.
