# Plugin SDK - Examples

Real-world plugin examples and code samples.

## Table of Contents

1. [Basic Math Plugin](#basic-math-plugin)
2. [Windows Platform Plugin](#windows-platform-plugin)
3. [String Utilities Plugin](#string-utilities-plugin)
4. [Data Processing Plugin](#data-processing-plugin)

## Basic Math Plugin

A simple plugin exposing mathematical functions.

### Code

```csharp
using Doe.PluginSdk;

namespace Math.Plugin;

public class MathPlugin : IDoePlugin
{
    public string Name => "MathPlugin";

    public void Register(IDoePluginRegistry registry)
    {
        registry.RegisterFunction("add", Add);
        registry.RegisterFunction("subtract", Subtract);
        registry.RegisterFunction("multiply", Multiply);
        registry.RegisterFunction("divide", Divide);
        registry.RegisterFunction("power", Power);
        registry.RegisterFunction("sqrt", SquareRoot);
    }

    private static object? Add(IReadOnlyList<object?> args) =>
        args.Count >= 2 ? Convert.ToDouble(args[0]) + Convert.ToDouble(args[1]) : null;

    private static object? Subtract(IReadOnlyList<object?> args) =>
        args.Count >= 2 ? Convert.ToDouble(args[0]) - Convert.ToDouble(args[1]) : null;

    private static object? Multiply(IReadOnlyList<object?> args) =>
        args.Count >= 2 ? Convert.ToDouble(args[0]) * Convert.ToDouble(args[1]) : null;

    private static object? Divide(IReadOnlyList<object?> args)
    {
        if (args.Count < 2) return null;
        double b = Convert.ToDouble(args[1]);
        if (b == 0) throw new DivideByZeroException("Cannot divide by zero");
        return Convert.ToDouble(args[0]) / b;
    }

    private static object? Power(IReadOnlyList<object?> args) =>
        args.Count >= 2 ? System.Math.Pow(Convert.ToDouble(args[0]), Convert.ToDouble(args[1])) : null;

    private static object? SquareRoot(IReadOnlyList<object?> args) =>
        args.Count >= 1 ? System.Math.Sqrt(Convert.ToDouble(args[0])) : null;
}
```

### Usage in Doe

```doe
let a = add(10, 5)
println(a)  // 15

let b = divide(20, 4)
println(b)  // 5.0

let c = power(2, 8)
println(c)  // 256.0

let d = sqrt(16)
println(d)  // 4.0
```

## Windows Platform Plugin

Plugin exposing Windows-specific platform information.

### Code

```csharp
using Doe.PluginSdk;

namespace Doe.WindowsPlugin.Sample;

public class WindowsPlugin : IDoePlugin
{
    public string Name => "WindowsPlugin";

    public void Register(IDoePluginRegistry registry)
    {
        registry.RegisterFunction("win_platform", Win_Platform);
        registry.RegisterFunction("win_is_windows", Win_IsWindows);
        registry.RegisterFunction("win_machine_name", Win_MachineName);
        registry.RegisterFunction("win_compose_window_title", Win_ComposeWindowTitle);
        registry.RegisterFunction("win_username", Win_Username);
        registry.RegisterFunction("win_temp_path", Win_TempPath);
    }

    private static object? Win_Platform(IReadOnlyList<object?> args) =>
        Environment.OSVersion.Platform.ToString();

    private static object? Win_IsWindows(IReadOnlyList<object?> args) =>
        OperatingSystem.IsWindows();

    private static object? Win_MachineName(IReadOnlyList<object?> args) =>
        Environment.MachineName;

    private static object? Win_ComposeWindowTitle(IReadOnlyList<object?> args) =>
        args.Count > 0
            ? $"{Environment.MachineName} :: {args[0]?.ToString() ?? ""}"
            : Environment.MachineName;

    private static object? Win_Username(IReadOnlyList<object?> args) =>
        Environment.UserName;

    private static object? Win_TempPath(IReadOnlyList<object?> args) =>
        System.IO.Path.GetTempPath();
}
```

### Usage in Doe

```doe
if win_is_windows()
    println("Running on Windows")
    println("Machine: " + win_machine_name())
    println("User: " + win_username())
    println("Temp path: " + win_temp_path())
end
```

## String Utilities Plugin

Plugin providing string manipulation functions.

### Code

```csharp
using Doe.PluginSdk;

namespace StringUtils.Plugin;

public class StringUtilsPlugin : IDoePlugin
{
    public string Name => "StringUtils";

    public void Register(IDoePluginRegistry registry)
    {
        registry.RegisterFunction("str_upper", Upper);
        registry.RegisterFunction("str_lower", Lower);
        registry.RegisterFunction("str_reverse", Reverse);
        registry.RegisterFunction("str_length", Length);
        registry.RegisterFunction("str_contains", Contains);
        registry.RegisterFunction("str_replace", Replace);
        registry.RegisterFunction("str_split", Split);
        registry.RegisterFunction("str_join", Join);
    }

    private static object? Upper(IReadOnlyList<object?> args) =>
        args.Count > 0 ? args[0]?.ToString()?.ToUpper() : null;

    private static object? Lower(IReadOnlyList<object?> args) =>
        args.Count > 0 ? args[0]?.ToString()?.ToLower() : null;

    private static object? Reverse(IReadOnlyList<object?> args)
    {
        if (args.Count == 0) return null;
        string str = args[0]?.ToString() ?? "";
        char[] chars = str.ToCharArray();
        System.Array.Reverse(chars);
        return new string(chars);
    }

    private static object? Length(IReadOnlyList<object?> args) =>
        args.Count > 0 ? args[0]?.ToString()?.Length ?? 0 : null;

    private static object? Contains(IReadOnlyList<object?> args) =>
        args.Count >= 2 
            ? args[0]?.ToString()?.Contains(args[1]?.ToString() ?? "") ?? false
            : false;

    private static object? Replace(IReadOnlyList<object?> args) =>
        args.Count >= 3
            ? args[0]?.ToString()?.Replace(args[1]?.ToString() ?? "", args[2]?.ToString() ?? "")
            : args[0];

    private static object? Split(IReadOnlyList<object?> args)
    {
        if (args.Count < 2) return null;
        string str = args[0]?.ToString() ?? "";
        string separator = args[1]?.ToString() ?? "";
        return new System.Collections.Generic.List<object?>(
            str.Split(separator).AsEnumerable().Cast<object?>()
        );
    }

    private static object? Join(IReadOnlyList<object?> args)
    {
        if (args.Count < 2) return null;
        string separator = args[0]?.ToString() ?? "";
        var items = args[1] as System.Collections.Generic.IEnumerable<object?>;
        if (items == null) return args[1]?.ToString();
        return string.Join(separator, items.Select(x => x?.ToString() ?? ""));
    }
}
```

### Usage in Doe

```doe
let text = "Hello World"
println(str_upper(text))        // HELLO WORLD
println(str_lower(text))        // hello world
println(str_length(text))       // 11
println(str_contains(text, "World"))  // true

let words = str_split("a,b,c", ",")
// words = ["a", "b", "c"]

let joined = str_join("-", words)
println(joined)                 // a-b-c
```

## Data Processing Plugin

Plugin handling data aggregation and transformation.

### Code

```csharp
using Doe.PluginSdk;
using System.Collections.Generic;
using System.Linq;

namespace DataTools.Plugin;

public class DataProcessingPlugin : IDoePlugin
{
    public string Name => "DataProcessing";

    public void Register(IDoePluginRegistry registry)
    {
        registry.RegisterFunction("sum", Sum);
        registry.RegisterFunction("average", Average);
        registry.RegisterFunction("min", Min);
        registry.RegisterFunction("max", Max);
        registry.RegisterFunction("count", Count);
        registry.RegisterFunction("filter", Filter);
    }

    private static object? Sum(IReadOnlyList<object?> args)
    {
        if (args.Count == 0) return 0.0;
        var items = args[0] as System.Collections.IEnumerable;
        if (items == null) return null;
        
        double sum = 0;
        foreach (var item in items)
        {
            sum += Convert.ToDouble(item);
        }
        return sum;
    }

    private static object? Average(IReadOnlyList<object?> args)
    {
        if (args.Count == 0) return null;
        var items = args[0] as System.Collections.IEnumerable;
        if (items == null) return null;

        var list = items.Cast<object?>().ToList();
        if (list.Count == 0) return null;
        
        return list.Sum(x => Convert.ToDouble(x)) / list.Count;
    }

    private static object? Min(IReadOnlyList<object?> args)
    {
        if (args.Count == 0) return null;
        var items = args[0] as System.Collections.IEnumerable;
        if (items == null) return null;
        
        var list = items.Cast<object?>().ToList();
        return list.Count > 0 ? list.Min(x => Convert.ToDouble(x)) : null;
    }

    private static object? Max(IReadOnlyList<object?> args)
    {
        if (args.Count == 0) return null;
        var items = args[0] as System.Collections.IEnumerable;
        if (items == null) return null;
        
        var list = items.Cast<object?>().ToList();
        return list.Count > 0 ? list.Max(x => Convert.ToDouble(x)) : null;
    }

    private static object? Count(IReadOnlyList<object?> args)
    {
        if (args.Count == 0) return 0;
        var items = args[0] as System.Collections.IEnumerable;
        if (items == null) return 1;
        
        return items.Cast<object?>().Count();
    }

    private static object? Filter(IReadOnlyList<object?> args)
    {
        if (args.Count < 2) return args.Count > 0 ? args[0] : null;
        // In practice, you'd accept a predicate (would require callback support)
        return args[0];
    }
}
```

### Usage in Doe

```doe
let numbers = [10, 20, 30, 40, 50]

println("Sum: " + sum(numbers))       // Sum: 150
println("Average: " + average(numbers)) // Average: 30
println("Min: " + min(numbers))       // Min: 10
println("Max: " + max(numbers))       // Max: 50
println("Count: " + count(numbers))   // Count: 5
```

---

## Running the Examples

1. Create a new C# class library project
2. Add Doe.PluginSdk reference
3. Copy the example code
4. Build: `dotnet build -c Release`
5. Place compiled DLL in the plugins directory
6. Use functions in Doe code

---

## Next Steps

- **[API Reference](./PluginSdk-APIReference.md)** - Complete API documentation
- **[Best Practices](./PluginSdk-BestPractices.md)** - Design patterns and guidelines
- **[Contributing](../Contributing.md)** - Share your plugins
