# Plugin SDK - Best Practices

Guidelines and patterns for building high-quality Doe plugins.

## Table of Contents

1. [Naming Conventions](#naming-conventions)
2. [Function Design](#function-design)
3. [Error Handling](#error-handling)
4. [Performance Considerations](#performance-considerations)
5. [Documentation](#documentation)
6. [Testing](#testing)
7. [Distribution](#distribution)

## Naming Conventions

### Plugin Name

```csharp
public string Name => "WindowsPlugin";  // Good: Clear, single word/CamelCase
public string Name => "windows_tools";  // Acceptable: snake_case
public string Name => "W";              // Bad: Too vague
```

### Function Names

Follow Doe language conventions for function naming:

```csharp
// Good: snake_case, descriptive
registry.RegisterFunction("get_user_name", GetUserName);
registry.RegisterFunction("format_date", FormatDate);
registry.RegisterFunction("parse_json", ParseJson);

// Bad: camelCase in Doe convention
registry.RegisterFunction("getUserName", GetUserName);
registry.RegisterFunction("formatDate", FormatDate);

// Bad: Too generic
registry.RegisterFunction("do_thing", DoThing);
registry.RegisterFunction("process", Process);
```

### Prefixes for Platform-Specific Functions

For platform-specific plugins, use a common prefix:

```csharp
registry.RegisterFunction("win_machine_name", MachineName);      // Windows
registry.RegisterFunction("win_username", Username);
registry.RegisterFunction("win_is_windows", IsWindows);

registry.RegisterFunction("linux_user_shell", UserShell);        // Linux
registry.RegisterFunction("linux_load_average", LoadAverage);
```

## Function Design

### Single Responsibility

Each function should do one thing well:

```csharp
// Good: Single responsibility
registry.RegisterFunction("to_upper", (args) => args[0]?.ToString()?.ToUpper());
registry.RegisterFunction("to_lower", (args) => args[0]?.ToString()?.ToLower());

// Bad: Tries to do too much
registry.RegisterFunction("string_transform", (args) =>
{
    // Handles case, trimming, encoding, etc.
});
```

### Consistent Signatures

Maintain consistency in how functions handle arguments:

```csharp
// Good: All functions check arg count, return null if insufficient
private static object? SafeOperation(IReadOnlyList<object?> args, int minArgs)
{
    if (args.Count < minArgs)
        return null;  // or throw ArgumentException
    // Process...
}

// Bad: Inconsistent argument handling
registry.RegisterFunction("func1", args => args[0]?.ToString());
registry.RegisterFunction("func2", args => args?.Count > 0 ? args[0] : null);
registry.RegisterFunction("func3", args => (string)args[0]);  // Can crash
```

### Clear Return Types

Be explicit about what your function returns:

```csharp
// Good: Clear intent
registry.RegisterFunction("is_valid_email", ValidateEmail);
// Returns: bool (true/false)

registry.RegisterFunction("get_file_size", GetFileSize);
// Returns: long (bytes) or null

registry.RegisterFunction("parse_config", ParseConfig);
// Returns: Dictionary<string, object?> (object representation)

// Bad: Ambiguous returns
registry.RegisterFunction("check_thing", (args) => { /* ??? */ });
```

## Error Handling

### Graceful Degradation

```csharp
// Good: Handle missing arguments gracefully
private static object? Divide(IReadOnlyList<object?> args)
{
    if (args.Count < 2)
        return null;  // Default behavior
    
    try
    {
        double a = Convert.ToDouble(args[0]);
        double b = Convert.ToDouble(args[1]);
        
        if (b == 0)
            throw new DivideByZeroException("Divisor cannot be zero");
        
        return a / b;
    }
    catch (FormatException)
    {
        return null;  // Arguments not numeric
    }
}
```

### Meaningful Exceptions

```csharp
// Good: Specific, informative exceptions
if (args.Count < 2)
    throw new ArgumentException("function_name requires 2 arguments");

if (invalidState)
    throw new InvalidOperationException("Operation not supported in current state");

// Bad: Generic or silent failures
if (args.Count < 2)
    return null;  // No indication of why

if (invalidState)
    return 0;  // Misleading result
```

### Exception Documentation

Document what exceptions your function can throw:

```csharp
/// <summary>
/// Reads and parses a JSON file.
/// </summary>
/// <param name="args">args[0] = file path (string)</param>
/// <returns>Parsed JSON object or null</returns>
/// <exception cref="FileNotFoundException">When file doesn't exist</exception>
/// <exception cref="FormatException">When JSON is invalid</exception>
private static object? ReadJsonFile(IReadOnlyList<object?> args)
{
    if (args.Count == 0)
        throw new ArgumentException("Path required");
    
    string path = args[0]?.ToString() ?? throw new ArgumentNullException();
    
    if (!File.Exists(path))
        throw new FileNotFoundException($"File not found: {path}");
    
    // Parse and return...
}
```

## Performance Considerations

### Avoid Blocking Operations

```csharp
// Acceptable: Fast file operation
registry.RegisterFunction("file_exists", (args) =>
{
    string path = args[0]?.ToString() ?? "";
    return File.Exists(path);  // Quick operation
});

// Consider: Long-running operation (may block Doe runtime)
registry.RegisterFunction("download_file", (args) =>
{
    // Long download... runtime is blocked
});
```

### Cache Results when Appropriate

```csharp
private static Dictionary<string, object?> _configCache = new();

private static object? GetConfig(IReadOnlyList<object?> args)
{
    string key = args[0]?.ToString() ?? "default";
    
    if (_configCache.TryGetValue(key, out var cached))
        return cached;
    
    // Load and cache...
    var result = LoadConfiguration(key);
    _configCache[key] = result;
    return result;
}
```

### Optimize Type Conversions

```csharp
// Good: Efficient conversion
private static double SafeDouble(object? obj) =>
    double.TryParse(obj?.ToString(), out double d) ? d : 0.0;

registry.RegisterFunction("add", (args) =>
    args.Count >= 2 ? SafeDouble(args[0]) + SafeDouble(args[1]) : null
);

// Bad: Multiple conversions
registry.RegisterFunction("add", (args) =>
    args.Count >= 2 
        ? (double)Convert.ChangeType(args[0], typeof(double)) 
        + (double)Convert.ChangeType(args[1], typeof(double))
        : null
);
```

## Documentation

### Document Your Plugin

Create a README.md in your plugin project:

```markdown
# MyAwesomePlugin

Extended description of what your plugin does.

## Features

- Feature 1
- Feature 2
- Feature 3

## Functions

### my_function(arg1, arg2)

Description of what the function does.

**Arguments:**
- `arg1`: Type and description
- `arg2`: Type and description

**Returns:** Type and description

**Example:**
\`\`\`doe
let result = my_function(10, 20)
println(result)
\`\`\`

**Throws:**
- `ArgumentException`: When arguments are invalid

## Installation

\`\`\`bash
dotnet add package MyAwesomePlugin
\`\`\`

## License

MIT
```

### Inline Documentation

```csharp
/// <summary>
/// Calculates the factorial of a number.
/// </summary>
/// <param name="args">args[0] = number to factorial (numeric)</param>
/// <returns>Factorial result as double, or null if invalid input</returns>
/// <remarks>
/// Large numbers may cause overflow. Consider using a big integer library
/// for numbers > 20.
/// </remarks>
private static object? Factorial(IReadOnlyList<object?> args)
{
    // Implementation...
}
```

## Testing

### Unit Test Your Functions

```csharp
using Xunit;

public class MathPluginTests
{
    private readonly MathPlugin _plugin = new();

    [Fact]
    public void Divide_WithValidArguments_ReturnsCorrectResult()
    {
        var args = new object?[] { 20.0, 4.0 };
        var result = _plugin.Divide(args);
        Assert.Equal(5.0, result);
    }

    [Fact]
    public void Divide_WithZeroDivisor_ThrowsException()
    {
        var args = new object?[] { 20.0, 0.0 };
        Assert.Throws<DivideByZeroException>(() => _plugin.Divide(args));
    }

    [Fact]
    public void Divide_WithInsufficientArgs_ReturnsNull()
    {
        var args = new object?[] { 20.0 };
        var result = _plugin.Divide(args);
        Assert.Null(result);
    }
}
```

### Integration Test with Doe

```doe
// test.doe
let result = divide(20, 4)
assert(result == 5, "divide(20, 4) should equal 5")

try
    let bad = divide(10, 0)
    assert(false, "Should have thrown exception")
catch e
    assert(true, "Correctly threw exception")
end

println("All tests passed!")
```

## Distribution

### Version Your Plugin

Use semantic versioning in your .csproj:

```xml
<Version>1.0.0</Version>
<PackageVersion>1.0.0</PackageVersion>
```

### Publish to NuGet

```bash
dotnet pack -c Release
dotnet nuget push bin/Release/MyPlugin.1.0.0.nupkg --api-key <key> --source https://api.nuget.org/v3/index.json
```

### Include License

Always include a LICENSE file:

```
MIT License

Copyright (c) 2024 [Your Name]

Permission is hereby granted, free of charge, to any person obtaining a copy...
```

---

## See Also

- [API Reference](./PluginSdk-APIReference.md)
- [Getting Started](./PluginSdk-GettingStarted.md)
- [Examples](./PluginSdk-Examples.md)
