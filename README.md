# Doe Language - Complete Documentation

## Table of Contents

1. [Overview](#overview)
2. [Installation](#installation)
3. [Basic Syntax](#basic-syntax)
4. [Data Types](#data-types)
5. [Variables & Modifiers](#variables--modifiers)
6. [Operators](#operators)
7. [Control Flow](#control-flow)
8. [Functions](#functions)
9. [Data Structures](#data-structures)
10. [Points & Async Patterns](#points--async-patterns)
11. [Loops](#loops)
12. [Plugins](#plugins)
13. [Module System](#module-system)
14. [Examples](#examples)

---

## Overview

**Doe** (also called **Dough**) is a modern programming language with a focus on:
- Clean, readable syntax
- Configuration-first paradigms with `conf` blocks
- Async/await patterns using "points" (message-passing primitives)
- Plugin architecture with C# support
- Windows-focused with a 2D/UI library (`Dough-2d`)

**File Extensions:** `.doe` or `.dough`

**Current Version:** 1.0.0

---

## Installation

### Prerequisites
- Windows OS (primary support)
- .NET runtime (for Dough-2d plugin)

### Runtime Installation

#### Option 1: Latest Release
Download from: https://github.com/Aidanace3/Doe-Language/releases/latest

#### Option 2: Build from Source
```powershell
# Standalone build
.\publish-runtime.ps1

# Windows MSI build
.\build-msi.ps1

# Full release bundle
.\build-release.ps1
```

### VS Code Extension
```powershell
code --install-extension aidanace3.dough-language
```

Or search for "dough-language" in the VS Code marketplace.

### Running Programs
```powershell
.\dough.cmd examples\test.doe
.\dough.cmd --check examples\test.doe      # Syntax check
.\dough.cmd --debug examples\test.doe      # Debug mode
.\dough.cmd --version
.\dough.cmd --runtime-info
```

---

## Basic Syntax

### Hello World
```doe
Print("hello")
```

### Program Structure
```doe
with constants                    // Optional module imports

conf app:                        // Configuration block
{
    str title = "Hello"
    int width = 720
    int height = 420
}

def Main()                       // Main function (optional)
{
    Print(app.title)
}
```

### Comments
```doe
// Single-line comment

(*Multi-line comment*)

(*Labeled comment:*)
```

### Basic Output
```doe
Print("Hello, World!")
Print(42)
Print("Value: " + 42)           // String concatenation
```

---

## Data Types

### Primitive Types

| Type | Keyword | Example | Notes |
|------|---------|---------|-------|
| Integer | `int` | `42` | Whole numbers |
| Float | `flt` | `3.14` | Floating-point numbers |
| String | `str` | `"hello"` | Text values |
| Boolean | `bool` | `@true`, `@false` | Logical values |

### Type Modifiers

```doe
nopoly int x = 10              // Non-polymorphic (fixed type)
const int locked = 12          // Constant (read-only)
nopoly const int hybrid = 5    // Both non-polymorphic and constant
```

### Literal Syntax
```doe
@true                          // Boolean true
@false                         // Boolean false
null                           // Null value
```

---

## Variables & Modifiers

### Declaration & Assignment

```doe
int count = 0
flt ratio = 9 / 2
str name = "Doe"
bool active = @true

// Reassignment
count = 5
name = "Dough"
```

### Modifiers

#### `const`
Makes a variable immutable after initialization.
```doe
const int locked_value = 12
locked_value = 20              // ERROR: Cannot reassign
```

#### `nopoly` (Non-Polymorphic)
Restricts the variable to its declared type; prevents type conversion.
```doe
nopoly int x = 10
x = 3.5                        // ERROR: Cannot assign float to non-poly int
```

#### `locked dict`
Creates an immutable dictionary.
```doe
locked dict(int) scores:
{
    int alpha = 7
    int beta = 11
}
```

---

## Operators

### Arithmetic
```doe
10 + 5         // Addition → 15
10 - 3         // Subtraction → 7
6 * 7          // Multiplication → 42
20 / 5         // Division → 4
9 / 2          // Float division → 4.5
25 % 200       // Percentage → 50
20 %% 6        // Modulo → 2
6 ** 7         // Exponentiation (double-star) → 279936
2 ^ 5          // Exponentiation (caret) → 32
```

### Comparison
```doe
9 > 3          // Greater than → @true
2 < 9          // Less than → @true
7 >= 7         // Greater-or-equal → @true
7 => 7         // Alternative syntax for >= → @true
7 <= 7         // Less-or-equal → @true
5 == 5.0       // Loose equality → @true
5 === 5.0      // Strict equality (type-checked) → @false
```

### Logical
```doe
@true | @false         // Logical OR → @true
@true && @true         // Logical AND → @true
@true *| @false        // Logical XOR → @true
@true !& @true         // Logical NAND → @false
@false !| @false       // Logical NOR → @true
!@false                // Logical NOT → @true
```

### String Concatenation
```doe
"Hello" + " " + "World"    // → "Hello World"
"Count: " + 42             // → "Count: 42"
```

---

## Control Flow

### If / Elif / Else

```doe
if (condition)::then
{
    // statement
} elif (other_condition)::then
{
    // statement
} else::
{
    // statement
}
```

**Example:**
```doe
int mode = 2
str mode_name = "unset"

if (mode == 1)::then
{
    mode_name = "one"
} elif (mode == 2)::then
{
    mode_name = "two"
} else::
{
    mode_name = "other"
}
```

### Unless
Inverse of `if` (executes when condition is false).

```doe
unless (condition)::then
{
    // Executes if condition is false
}
```

**Example:**
```doe
str orientation = "landscape"

unless (monitorpos == "vertical")::then
{
    orientation = "landscape"
}
```

### IfCase (Switch Statement)
```doe
IfCase(variable)
{
    Case: value1:
    {
        // statements
    }
    Case: value2:
    {
        // statements
    }
    Default:
    {
        // statements
    }
}
```

**Example:**
```doe
str classification = "none"
int mode = 2

IfCase(mode)
{
    Case: 1:
        classification = "single"
    Case: 2:
        classification = "double"
    Default:
        classification = "outlier"
}
```

**Multiple Case Values:**
```doe
str device = "Linux"
str selected = "unknown"

IfCase(device)
{
    Case:(Windows, Win, Mac, Macos, Macbook, Linux):
    {
        selected = "desktop"
    }
    Otherwise:
    {
        selected = "other"
    }
}
```

---

## Functions

### Function Declaration
```doe
def function_name()
{
    // body
}

def function_name(param1, param2)
{
    // body
}
```

### Return Values
```doe
def square(n)
{
    return n ** 2
}

def add_three(a, b, c)
{
    return a + b + c
}

// Usage
int result = square(9)              // 81
int sum = add_three(4, 5, 6)        // 15
int nested = square(add_three(1, 2, 3))  // 36
```

### Early Return
```doe
def validate_token(token)
{
    if (token == null)::then
    {
        return()
    }
    // Continue processing...
}
```

---

## Data Structures

### Arrays
```doe
// Declaration with initial values
arr[int] numbers = [10, 20, 30, 40, 50]

// Access (1-indexed)
int first = numbers[1]         // 10
int last = numbers[5]          // 50

// Array helpers
int length = Max(numbers)      // 5 (returns length)
int min_idx = Min(numbers)     // 1 (returns lower bound)
```

### Array Ranges & Slicing
```doe
arr[int] numbers = [10, 20, 30, 40, 50]

// Range slice (2 to 4 inclusive)
arr[int] middle = numbers[2..4]
int mid_length = Max(middle)    // 3
int mid_first = middle[1]       // 20
int mid_last = middle[3]        // 40
```

### Array Type Declaration
```doe
arr[str] tags = ["doe", "language", "modern"]
arr[flt] decimals = [1.5, 2.5, 3.5]
```

### Empty Array Creation
```doe
arr[str] event_log = Arr[str]
```

### Dictionaries
```doe
dict profile:
{
    str name = "Verbose Doe"
    int build = 1
}

// Access with bracket notation
str name = profile["name"]      // "Verbose Doe"
int build = profile["build"]    // 1

// Dynamic assignment
profile["status"] = "active"

// Typed dictionaries
dict(int) scores:
{
    int alpha = 7
    int beta = 11
}
```

### Configuration Blocks (conf)
```doe
conf app:
{
    str title = "Hello"
    int width = 720
    int height = 420
}

// Access with dot notation
Print(app.title)               // "Hello"
int w = app.width              // 720
```

### Maps
```doe
dict base:
{
    int width = 100
    int height = 200
}

dict override:
{
    str title = "Mapped Test"
}

// Merge two dictionaries
dict merged = map(base, override)

// Project specific fields
arr[str] projected = map(merged, "width", "title")
```

### Custom Types (windowtype)
```doe
new windowtype landscape: {
    1080,       // x
    960,        // y
    "Test"      // title
}

new windowtype portrait: {
    960,
    1080,
    null
}

// Access tuple-like values
int landscape_x = landscape[1]     // 1080
int landscape_y = landscape[2]     // 960
str landscape_title = landscape[3] // "Test"
```

---

## Points & Async Patterns

Points are named await-value constructs that create event-driven patterns.

### Basic Point Definition
```doe
(*Logger:) awaitval(message;)
{
    Print("[POINT] Logger received -> " + message)
}
```

### Sending Values to Points

#### Direct Send
```doe
"alpha" >> *Logger
```

#### With Yield (Async)
```doe
yield("beta" >> *Logger)
```

#### Alternative Yield Syntax
```doe
yield "gamma" >> *Logger
```

#### With Alias
```doe
yield "mirrored value" >> *AliasProbe as mirror

// Inside the point, use 'mirror' to access the sent value
(*AliasProbe:) awaitval(incoming;)
{
    str captured = mirror
}
```

### Nested Point Pattern (Advanced)
```doe
(*Conn:) awaitval(IsConnected;)
{
    if(IsConnected == @true)::then {
        (*Auth:) awaitval(Token;)
        {
            if(Token == null)::then {
                return()
            }
            else
            {
                (*Perm:) awaitval(Role;)
                {
                    if(Role == "Admin")::then {
                        (*Crypt:) awaitval(Key;)
                        {
                            // Final execution
                            if(Key == "SECURE")::then {
                                Print("SYSTEM_ACCESS_GRANTED")
                            }
                        }
                        yield("SECURE_KEY" >> *Crypt)
                    }
                }
                yield("Admin" >> *Perm)
            }
        }
        yield("VALID_TOKEN" >> *Auth)
    }
}
yield(@true >> *Conn)
```

---

## Loops

### While-style Loop (as)
```doe
flt counter = 0
flt total = 0

as(@true):
{
    counter = counter + 1
    total = total + counter
    
    if (counter >= 5)::then
    {
        break
    }
}

// counter = 5, total = 15
```

### For-each Loop (each)
```doe
arr[int] numbers = [10, 20, 30, 40, 50]
flt sum = 0

each(item in numbers) do:
{
    sum = sum + item
}

// sum = 150
```

### Break Statement
```doe
as(@true):
{
    if (condition)::then
    {
        break
    }
}
```

---

## Plugins

Doe supports C# plugins with two paradigms:

### Loading Plugins

#### By Name
```doe
with plugin:Your.Plugin.Name

// Use plugin functions
Print(Win_Platform())
Print(Win_MachineName())
```

#### By File Path
```doe
with "../lib/Dough-2d.doe"
```

### Plugin Search Paths
1. Current working directory
2. Importing file directory
3. `plugins/` and `plugin/` folders
4. `DOE_PLUGIN_PATH` environment variable
5. `PATH` environment variable

### Convention-based Plugins
Public static `PluginFunctions` methods:

```csharp
public static class PluginFunctions
{
    public static string Win_Platform()
    {
        return Environment.OSVersion.VersionString;
    }
    
    public static bool Win_IsWindows()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    }
}
```

### Built-in: Dough-2d Plugin

Windows 2D graphics and UI library.

#### Window Functions
```doe
with Dough-2d

int window = dough2d_window("demo", 720, 420)
dough2d_window_show(window)
dough2d_window_set_background(window, 255, 255, 255)  // White background
```

#### GUI Controls
```doe
int label = dough2d_label(window, "hello", 24, 24, 180, 24)
int button = dough2d_button(window, "launch", 24, 64, 120, 32)

dough2d_gui_set_text(label, "new text")
dict last_event = dough2d_gui_get_last_event(window)
dough2d_gui_clear_last_event(window)
```

#### Physics Simulation
```doe
int world = dough2d_world(0, 98)  // gravity x, gravity y
int body = dough2d_body(world, 40, 20, 80, 0, 1, 10, @false)
// x, y, vx, vy, mass, radius, isStatic

dict body_info = dough2d_body_info(body)
Print(body_info.y)

dough2d_physics_step(world, 0.016, 500, 0.8)  // dt, floorY, bounce
dough2d_physics_set_velocity(body, 50, -100)
```

---

## Module System

### Imports
```doe
with constants                      // Built-in module
with "../lib/Dough-2d.doe"         // Relative path
with plugin:Doe.WindowsPlugin.Sample // C# plugin
```

### Library Folders
Doe automatically searches for local modules in:
- `lib/`
- `libs/`
- `library/`
- `libraries/`

### Config Import
```doe
with constants

conf meta:
{
    str device = "Linux"
}
```

---

## Examples

### Example 1: Configuration and Control Flow
```doe
conf meta:
{
    str device = "Linux"
}

conf ratio:
{
    int x = 1080
    int y = 960
}

str monitorpos = "horizontal"
str orientation = "unset"

if (monitorpos == "vertical")::then
{
    orientation = "portrait"
}
else::
{
    orientation = "landscape"
}

new windowtype desktop: {
    ratio.x,
    ratio.y,
    "App"
}

IfCase(meta.device)
{
    Case:(Windows, Win, Mac, Macos, Linux):
    {
        Print("Desktop detected")
    }
    Otherwise:
    {
        Print("Unknown device")
    }
}
```

### Example 2: Functions and Arrays
```doe
def square(n)
{
    return n ** 2
}

def add_three(a, b, c)
{
    return a + b + c
}

arr[int] numbers = [1, 2, 3, 4, 5]
flt sum = 0

each(item in numbers) do:
{
    sum = sum + item
}

Print("Sum: " + sum)          // 15
Print("Square of 3: " + square(3))  // 9
Print("Add: " + add_three(1, 2, 3)) // 6
```

### Example 3: Points and Async
```doe
(*Logger:) awaitval(message;)
{
    Print("[LOG] " + message)
}

(*Counter:) awaitval(value;)
{
    Print("[COUNT] Value is: " + value)
}

"System started" >> *Logger
yield("Ready" >> *Logger)
yield(42 >> *Counter)
```

### Example 4: Dictionary and Mapping
```doe
dict profile:
{
    str name = "Doe"
    int age = 5
}

dict settings:
{
    bool active = @true
}

dict merged = map(profile, settings)

Print(merged["name"])      // Doe
Print(merged["active"])    // @true
```

### Example 5: Full Testing Example
```doe
def assert_equal(label, actual, expected)
{
    if (actual == expected)::then
    {
        Print("[PASS] " + label)
        return @true
    } else::
    {
        Print("[FAIL] " + label)
        return @false
    }
}

assert_equal("basic math", 2 + 2, 4)
assert_equal("string concat", "Hello" + " World", "Hello World")
assert_equal("comparison", 9 > 3, @true)

arr[int] nums = [10, 20, 30]
assert_equal("array access", nums[1], 10)
assert_equal("array length", Max(nums), 3)
```

---

## Common Patterns

### Safe Null Checking
```doe
def validate(token)
{
    if (token == null)::then
    {
        return()
    }
    Print("Token valid: " + token)
}
```

### Type Checking (Strict vs Loose)
```doe
// Loose equality (type coercion)
if (5 == 5.0)::then  // @true

// Strict equality (type must match)
if (5 === 5.0)::then // @false
```

### Loop with Condition
```doe
flt index = 0

as(@true):
{
    index = index + 1
    
    if (index > 10)::then
    {
        break
    }
}
```

### Dynamic Dictionary Access
```doe
dict data:
{
    str status = "ready"
}

data["status"] = "processing"
data["message"] = "In progress"
Print(data["message"])  // "In progress"
```

---

## Tips & Best Practices

1. **Use `conf` blocks** for configuration to keep settings organized
2. **Prefer `nopoly`** for variables where type strictness prevents bugs
3. **Use points (`*Name`)** for event-driven communication between logical sections
4. **Leverage array ranges** (`numbers[2..4]`) for efficient slicing
5. **Use typed arrays** (`arr[str]`, `arr[int]`) to prevent type confusion
6. **Test with the verbose tester** pattern for comprehensive validation
7. **Comment labeled blocks** with `(*Label:*)` for clarity

---

## Troubleshooting

### Syntax Check
```powershell
.\dough.cmd --check your_file.doe
```

### Debug Mode
```powershell
.\dough.cmd --debug your_file.doe
```

### Version Info
```powershell
.\dough.cmd --version
.\dough.cmd --runtime-info
```

---

## References

- **Repository:** https://github.com/Aidanace3/Doe-Language
- **VS Code Extension:** aidanace3.dough-language
- **Release Builds:** https://github.com/Aidanace3/Doe-Language/releases/latest
