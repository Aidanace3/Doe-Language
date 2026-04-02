# Plugin SDK - Troubleshooting

Common issues and solutions when developing Doe plugins.

## Table of Contents

1. [Plugin Not Loading](#plugin-not-loading)
2. [Functions Not Available](#functions-not-available)
3. [Type Conversion Issues](#type-conversion-issues)
4. [Performance Problems](#performance-problems)
5. [Getting Help](#getting-help)

## Plugin Not Loading

### Symptom
Plugin assembly is not discovered by the Doe runtime.

### Possible Causes & Solutions

**1. DLL not in correct directory**
```bash
# Check if DLL is in the plugins folder
ls /path/to/doe/plugins/
```

**2. Assembly name mismatch**
Make sure the compiled DLL name matches what the runtime expects:
```bash
# Build and check output
dotnet build -c Release
# Output should be: bin/Release/net10.0/YourPlugin.dll
```

**3. Missing dependencies**
Your plugin may depend on other assemblies:
```bash
# Include all required DLLs in plugins folder
# Or set plugin path to bin folder
```

**4. Runtime not scanning plugins folder**
Check Doe runtime configuration:
```csharp
// In Program.cs or initialization code
var pluginPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins");
if (!Directory.Exists(pluginPath))
{
    Directory.CreateDirectory(pluginPath);
}
```

### Diagnostic Steps

1. Enable verbose logging in Doe runtime
2. Check for exception messages in console
3. Verify DLL exists and is readable
4. Confirm IDoePlugin implementation

## Functions Not Available

### Symptom
Plugin loads but functions aren't callable from Doe code.

### Possible Causes & Solutions

**1. Register() method not called**
Ensure Register method is public and doesn't throw:
```csharp
public void Register(IDoePluginRegistry registry)
{
    try
    {
        registry.RegisterFunction("my_func", MyFunc);
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"Registration failed: {ex}");
        throw;
    }
}
```

**2. Function name already registered**
Function names must be unique across all plugins:
```csharp
// Check for naming conflicts
registry.RegisterFunction("get_name", GetName);
registry.RegisterFunction("get_name", DifferentGetName);  // ERROR: duplicate
```

Solution: Use plugin prefix
```csharp
registry.RegisterFunction("my_plugin_get_name", GetName);
```

**3. Function references null or invalid delegate**
```csharp
// Bad
DoePluginFunction handler = null;
registry.RegisterFunction("bad_func", handler);  // Will fail

// Good
registry.RegisterFunction("good_func", MyFunction);
registry.RegisterFunction("inline_func", (args) => DateTime.Now.ToString());
```

### Diagnostic Steps

1. Add debug output in Register():
```csharp
public void Register(IDoePluginRegistry registry)
{
    Console.WriteLine($"[{Name}] Registering functions...");
    registry.RegisterFunction("test_func", TestFunc);
    Console.WriteLine($"[{Name}] Registration complete");
}
```

2. Call plugin test function from Doe:
```doe
let result = my_plugin_test_func()
println(result)
```

## Type Conversion Issues

### Symptom
Arguments are passed but converted incorrectly, causing exceptions or wrong results.

### Example Issues

**1. Type mismatch on input**
```csharp
// Bad: Assumes string without checking
private static object? Process(IReadOnlyList<object?> args)
{
    string str = (string)args[0];  // Can crash if not string!
}

// Good: Safe conversion
private static object? Process(IReadOnlyList<object?> args)
{
    if (args.Count == 0) return null;
    string str = args[0]?.ToString() ?? "";  // Safe
}
```

**2. Numeric conversion precision loss**
```doe
// Doe: using large integer
let bignum = 9007199254740992  // Beyond double precision

// C#: May lose precision
double d = Convert.ToDouble(args[0]);  // Precision lost
```

Solution: Use `decimal` or `long` for precise numeric handling
```csharp
private static object? ProcessNumber(IReadOnlyList<object?> args)
{
    if (args.Count == 0) return null;
    
    // Try parsing as long first (no precision loss)
    if (long.TryParse(args[0]?.ToString(), out long l))
        return l;
    
    // Fall back to double
    if (double.TryParse(args[0]?.ToString(), out double d))
        return d;
    
    return null;
}
```

**3. Collection handling**
```doe
// Doe: passing array
let arr = [1, 2, 3]
let result = process_array(arr)
```

```csharp
// C#: Receiving as IEnumerable vs List
private static object? ProcessArray(IReadOnlyList<object?> args)
{
    if (args.Count == 0) return null;
    
    // Correct: Handle different collection types
    var items = args[0] switch
    {
        List<object?> list => list,
        object[] array => array.ToList(),
        System.Collections.IEnumerable enumerable => 
            enumerable.Cast<object?>().ToList(),
        _ => null
    };
    
    if (items == null) return null;
    
    // Process items...
    return items.Count;
}
```

### Diagnostic Steps

1. Add type logging:
```csharp
private static object? Process(IReadOnlyList<object?> args)
{
    if (args.Count > 0)
    {
        var type = args[0]?.GetType().Name ?? "null";
        Console.WriteLine($"Received type: {type}, value: {args[0]}");
    }
    // Continue...
}
```

2. Test from Doe with various types:
```doe
let s = process("string")
let n = process(42)
let a = process([1, 2, 3])
let o = process({key: "value"})
```

## Performance Problems

### Symptom
Plugin functions are slow, freezing or hanging the Doe runtime.

### Possible Causes & Solutions

**1. Blocking I/O operations**
```csharp
// Bad: Blocking read from large file
registry.RegisterFunction("load_file", (args) =>
{
    string path = args[0]?.ToString() ?? "";
    return File.ReadAllText(path);  // Blocks entire runtime!
});

// Better: Provide async version (if runtime supports)
// Or document that function may block and keep operations small
```

**2. Infinite loops or recursion**
```csharp
// Bad: No termination condition
private static object? BadLoop(IReadOnlyList<object?> args)
{
    while (true) { /* ... */ }
}

// Good: Clear termination
private static object? GoodLoop(IReadOnlyList<object?> args)
{
    if (args.Count == 0) return null;
    
    int max = Convert.ToInt32(args[0]);
    for (int i = 0; i < max && i < 1000000; i++)  // Safety limit
    {
        // Process...
    }
    return max;
}
```

**3. Expensive resource creation per call**
```csharp
// Bad: Create regex on every call
registry.RegisterFunction("validate_email", (args) =>
{
    var regex = new System.Text.RegularExpressions.Regex(@"^[^\s@]+@[^\s@]+\.[^\s@]+$");
    return regex.IsMatch(args[0]?.ToString() ?? "");
});

// Good: Cache regex
private static readonly System.Text.RegularExpressions.Regex EmailRegex = 
    new(@"^[^\s@]+@[^\s@]+\.[^\s@]+$");

registry.RegisterFunction("validate_email", (args) =>
{
    return EmailRegex.IsMatch(args[0]?.ToString() ?? "");
});
```

### Diagnostic Steps

1. Profile with stopwatch:
```csharp
registry.RegisterFunction("slow_operation", (args) =>
{
    var sw = System.Diagnostics.Stopwatch.StartNew();
    var result = ExpensiveOperation(args);
    sw.Stop();
    Console.WriteLine($"Operation took {sw.ElapsedMilliseconds}ms");
    return result;
});
```

2. Add timeouts:
```csharp
var result = Task.Run(async () => await SlowOperation(args))
    .Wait(TimeSpan.FromSeconds(5));
```

## Getting Help

If you continue to experience issues:

1. **Check existing issues**: https://github.com/Aidanace3/Doe-Language/issues
2. **Enable debug logging**: Set registry HKEY_LOCAL_MACHINE\SOFTWARE\Doe\Debug=1
3. **Provide details**:
   - Plugin code
   - Doe code that triggers the issue
   - Full error message and stack trace
   - Operating system and .NET version

### Example Bug Report

```
Plugin: MyPlugin v1.0
Doe Version: 1.0.0
.NET Version: 10.0.4
OS: Windows 11

Issue: Function returns wrong value
Reproduction:
  Plugin code: [paste function here]
  Doe code: [paste calling code here]
  
Expected: result == 5
Actual: result == null

Error message: [if any]
Debug log output: [if available]
```

---

## See Also

- [API Reference](./PluginSdk-APIReference.md)
- [Best Practices](./PluginSdk-BestPractices.md)
- [GitHub Issues](https://github.com/Aidanace3/Doe-Language/issues)
