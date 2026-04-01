# Doe Language Documentation

## current V: Dough_V0.9.0

## NOTE: [Install here](https://github.com/Aidanace3/Dough)

## Running Dough Programs

- Direct command: `.\dough.cmd examples\test.doe`
- PowerShell wrapper: `.\dough.ps1 examples\test.doe`
- Silent mode: `.\dough.cmd --silent examples\test.doe`
- Verbose mode: `.\dough.cmd --verbose examples\test.doe`
- Syntax check only: `.\dough.cmd --check examples\test.doe`
- Runtime info: `.\dough.cmd --runtime-info`
- Version: `.\dough.cmd --version`
- Self-contained publish: `.\publish-runtime.ps1`
- Full release build: `.\build-release.ps1`
- If `published\win-x64\Dough.exe` exists, the wrappers prefer it for standalone use.
- If no standalone runtime exists, the wrappers now prefer Release builds before Debug fallback.

## Install In VS Code (Language Support)

- Install from Marketplace:
  - `code --install-extension aidanace3.dough-language`
- Or in VS Code Extensions search:
  - `aidanace3.dough-language`

## Install Runtime (CLI)

- Open: `https://github.com/Aidanace3/Doe-Language/releases/latest`
- Download `dough-runtime-win-x64.zip`
- Extract and run `Dough.exe yourfile.doe`
- Or build your own standalone runtime with `.\publish-runtime.ps1`
- Or build both runtime and VS Code artifacts with `.\build-release.ps1`
- Optional: add the extracted folder to your PATH so `Dough.exe` works from any terminal.

## Add `Launch.Json`

- Add a folder at top called `.vscode`

- Add a file called `Launch.json`
  paste this;
  
```json
  {
  "version": "0.2.0",
  "configurations": [
    {
      "name": "Dough: Run Current File (CLI)",
      "type": "node-terminal",
      "request": "launch",
      "command": "powershell -NoProfile -ExecutionPolicy Bypass -File \"${workspaceFolder}/../Doe-Language/dough.ps1\" \"${file}\""
    },
    {
      "name": "Dough: Debug Current File (CLI)",
      "type": "node-terminal",
      "request": "launch",
      "command": "powershell -NoProfile -ExecutionPolicy Bypass -File \"${workspaceFolder}/../Doe-Language/dough.ps1\" --debug \"${file}\""
    }
  ]
}
```

## Syntax

### Preferred / Legacy / Deprecated

- Preferred
  - `yield` keyword spelling
  - Point dispatch with `>>` or `<<` (for example: `yield value << Point`)
  - `return value >> this` when returning to a parent point context
  - `=` for assignment, `==` for comparison, `===` for strict comparison
  - Screenshot-style aliases now accepted for modern authoring:
    `with ...`, `unless (...)`, `Otherwise:` inside `IfCase`, grouped `Case:(A, B, C):`, dotted access like `meta.device`, and `new type name: { ... }`
- `import` / `with` now execute local `.doe` / `.dough` modules once, resolve relative to the importing file, and search common library folders such as `lib/`, `libs/`, `library/`, and `libraries/`
  - `with plugin:PluginName` loads a C# plugin assembly from common plugin folders such as `plugins/` and `plugin/`
- Legacy (still supported)
  - `yeild` spelling
  - Legacy point-case forms kept for compatibility
- Deprecated
  - `def`/`Funcs`-style declarations remain supported for backward compatibility but should be phased out due to points.

### Section 1: Basic Syntax

#### Other rules

- An independent bool is prefixed by an `@`
- - eg. `as (@true);`
- Any casing works, even if specifically noted here, (eg, ’Print` = `print`)

#### Operators

ARITH Operators: `+`,`**`,`-`,`/`,`%`,`^`,`%%`.

- `+`: Add
- `**`: Multiply ( \* is used for points )
- `-`: Subtract
- `/`: Divide
- `%`: Percentage of
- `^`: Exponentiate
- `%%`: Remdiv (Modulo)
  
CON Operators: `>`, `<`, `=>`, `<=`, `!`, `|`, `*|`, `!&`, `!|`, `&&`, `!&`.

- `>`: Greater
- `<`: Less
- `=>`: Equal or Greater
- `<=`: Equal or Less
- `!`: Not
- `|`: Or
- `*|`: Xor
- `&&`: Xand (Common And)
- `!&`: Nand
- `!|`: Nor
  
#### I/O

#### Input

- `readln(n)` - reads in line #n
- `Input("Prompt")` - accepts user input
  - `-H` - hide input with asterisks
  - `-W n` - adds a time limit to input

#### Output

- `Print("x")` - simple output
- Use `+` to concatenate variables with text

#### Types

- `NoPoly` - keep type; no polymorphism
- `Const` - keep value constant
- `Str`, `String` - text value
- `Int` - no-decimal numeral
- `Flt` - decimal numeral
- `Arr[Type]` - array with specified types
- `Max(Arr)` - append upper limit to array
- `Min(Arr)` - opposite (lower limit)

#### Conditions

**If Statement:**

```dough
if(condition)::then
{
    // code
}
```

/(
something cool you can do is change the `Then` after the `::` to a `Break` or `Func()`
to directly do a Break or Run a Function after check.
)\

eg.

```Dough
NoPoly Const Int X = 5

def FunctionA {
  Print("hi")
}

if ( X == 5 )::FunctionA()
{
  Return X >> this
}
```

**Else Statement:**

```dough
else::
{
    // otherwise code
}
```

**Switch Statement:**

```dough
IfCase(x)
{
    Case: X is N:
    {
        // code
    }
    Default: X is Outlier
    {
        // code for outlier cases
    }
}
```

#### Dictionaries, Configs & Functions

- `Dict` - create a dictionary (see Section 2.3)
- `Conf` - create an importable config dictionary
- `map(dict, overlayDict)` - merge configs/dicts into a new dict
- `map(dict, "key1", "key2")` - project selected values into an array
- `Return` - written as `Return n >> (point)`
- `Funcs` - Depracated

#### Points

- Written as `(*POINTNAME)`
- Used for `YEILD` and `RETURN`
- Use `this` (not `*this`) for parent point context
- `awaitval` executes a function as soon as a value is taken from yeild
- `yeild(var >> *Point)` sends a value to a point.
- `exit(*Point)` removes point from list. use after cases and functions
- `Store(Val Asa Valname >> *Point)` saves a value to a point
- - accessible with `request(x << *Point.Valname)`
- - Another way to store it is defining it in the Point's function it's connected to
- Examples in Section 2.4

---

## Section 2: Examples & Syntax

### 2.1 Loops

Point loop

```dough
{
(*); 
{
  //code
  loop ( l >> this x10 )
  return l >> this
}
exit(*this)
}
```

Built in loops:

as (while)

```dough
as(@true):
{
  //code
  if(@stopcondition)::break
}
```

each in (foreach)

```dough
each(x in [arr]) do:
{
  //code
}
```

### 2.2 Arrays

To define a typed array:

```dough
name = [1, 2, 3]
```

### 2.3 Dictionaries

Define a dict:

```dough
dict ExampleDict:
{
    // variables go here
};
```

Define a locked (single type) dict:

```dough
locked dict(type):
{
    // variables of specified type
}
```

Define a config:

```dough
conf WindowBase:
{
    int width = 1280
    int height = 720
    str title = "Doe Window"
}
```

Configs are imported the same way as normal modules and behave as dictionaries at runtime.

Compose imported configs:

```dough
with display_config

dict FinalWindow = map(base_window, debug_window)
arr picked = map(FinalWindow, "width", "title")
```

to use a dictionary variable; `Dict.Varname’

### 2.4 Points

```dough
(*Taking:) awaitval(x;)
{
    print(x)
}

// other stuff
x = 5
x >> *Taking
```

**Output:** `5`

```dough
// Example of a continuous listener
(*LogStream:) awaitval(msg;)
{
    Print("LOG: " + msg)
}

// Later in the code
if ( Logval == 1 )::Then
  {"message 1" >> *LogStream}
elif ( Logvak == 2 )::Then
  {"message 2" >> *LogStream}
else::Break
```

### 2.6 C# Plugins

- Use `with plugin:Your.Plugin.Name` to load a C# plugin assembly.
- Plugins are searched from the current project, the importing file's folder, and common plugin folders like `plugins/` and `plugin/`.
- Two authoring styles are supported:
  - Simple convention plugin: a public static `PluginFunctions` class whose public static methods become callable from Doe.
  - SDK plugin: implement `IDoePlugin` from [DoePluginContracts.cs](/c:/Users/Norberg/DoeLang/Doe-Language/PluginSdk/DoePluginContracts.cs).
- Example sample plugin:
  - [Doe.WindowsPlugin.Sample.csproj](/c:/Users/Norberg/DoeLang/Doe-Language/plugins/Doe.WindowsPlugin.Sample/Doe.WindowsPlugin.Sample.csproj)
  - [WindowsPlugin.cs](/c:/Users/Norberg/DoeLang/Doe-Language/plugins/Doe.WindowsPlugin.Sample/WindowsPlugin.cs)
- Example Doe usage:
  - [plugin_demo.doe](/c:/Users/Norberg/DoeLang/Doe-Language/examples/plugin_demo.doe)

### 2.5 Conditionals

#### 2.5.1 Cases

```dough
*Case ifCase(n;)
  *Case << 5 :: Then
  {/(code goes here)\}
  *Case << outlier? // equivelant to `Default` in clang
  {/(code goes here)\};
Exit(*case)
```

#### 2.5.2 If/Else

```dough
NoPoly Int x = 7
if ( X > 6 )::then
  x = 7
else::break

```

### Footnotes
