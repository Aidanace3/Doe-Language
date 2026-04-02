# Plugin SDK - API Reference

Complete API documentation for the Doe Plugin SDK.

## Table of Contents

- [IDoePlugin](#idoeplugin)
- [IDoePluginRegistry](#idoepluginregistry)
- [DoePluginFunction Delegate](#doepluginfunction-delegate)
- [Type Marshaling](#type-marshaling)
- [Exception Handling](#exception-handling)

## IDoePlugin

The main interface that all plugins must implement.

### Definition

```csharp
namespace Doe.PluginSdk;

public interface IDoePlugin
{
    string Name { get; }
    void Register(IDoePluginRegistry registry);
}
```

### Properties

#### Name

```csharp
string Name { get; }
```

- **Type:** `string`
- **Purpose:** Unique identifier for the plugin
- **Usage:** Used for plugin discovery, versioning, and function namespacing
- **Constraints:** Should be alphanumeric (no spaces or special characters recommended)
- **Example:** `"WindowsPlugin"`, `"FileSystemPlugin"`

### Methods

#### Register

```csharp
void Register(IDoePluginRegistry registry);
```

- **Purpose:** Called once when the plugin is loaded
- **Parameters:** `registry` - The plugin registry instance
- **Responsibility:** Register all functions this plugin provides
- **Example:**
  ```csharp
  public void Register(IDoePluginRegistry registry)
  {
      registry.RegisterFunction("my_func", MyFunction);
      registry.RegisterFunction("another_func", AnotherFunction);
  }
  ```

## IDoePluginRegistry

Interface for registering custom functions in the Doe runtime.

### Definition

```csharp
namespace Doe.PluginSdk;

public interface IDoePluginRegistry
{
    void RegisterFunction(string name, DoePluginFunction handler);
}
```

### Methods

#### RegisterFunction

```csharp
void RegisterFunction(string name, DoePluginFunction handler);
```

- **Purpose:** Register a function that will be callable from Doe code
- **Parameters:**
  - `name` (string) - Function name as it will appear in Doe code
  - `handler` (DoePluginFunction) - Delegate to handle the function call
- **Constraints:**
  - Function names should be lowercase with underscores (Doe convention)
  - Names must be unique within the plugin
  - Cannot override built-in functions
- **Example:**
  ```csharp
  registry.RegisterFunction("parse_json", (args) => {
      if (args.Count == 0) return null;
      string json = args[0]?.ToString() ?? "";
      return JsonConvert.DeserializeObject(json);
  });
  ```

## DoePluginFunction Delegate

Signature for custom plugin function implementations.

### Definition

```csharp
namespace Doe.PluginSdk;

public delegate object? DoePluginFunction(IReadOnlyList<object?> args);
```

- **Purpose:** Handle invocations of your custom function from Doe code
- **Parameters:** `args` - List of arguments passed from Doe code
- **Return Type:** `object?` - Any value or null
- **Behavior:** Function receives arguments in order, can process and return any .NET object

### Argument Handling

Arguments arrive as `IReadOnlyList<object?>` containing whatever Doe code passed:

```csharp
DoePluginFunction MyFunc = (args) =>
{
    // Check argument count
    if (args.Count < 2)
        return null; // or throw an exception

    // Access arguments by index
    object? first = args[0];
    object? second = args[1];

    // Typically convert to expected types
    string str = first?.ToString() ?? "";
    int num = Convert.ToInt32(second);

    // Process and return
    return $"Result: {str} + {num}";
};
```

## Type Marshaling

When calling plugin functions from Doe code, types are automatically converted:

### From Doe to .NET

| Doe Type | .NET Type | Notes |
|----------|-----------|-------|
| Number | `double` or `long` | Context-dependent |
| String | `string` | Direct conversion |
| Boolean | `bool` | Direct conversion |
| None/Null | `null` | null value |
| Array | `List<object?>` | Dynamic list |
| Object | `Dictionary<string, object?>` | Key-value pairs |

### From .NET to Doe

| .NET Type | Doe Type | Notes |
|-----------|----------|-------|
| `string` | String | Direct conversion |
| `int`, `long`, `float`, `double` | Number | Converted to appropriate numeric type |
| `bool` | Boolean | Direct conversion |
| `null` | None | Null value |
| `IEnumerable` | Array | Enumerable converted to array |
| `IDictionary` | Object | Dictionary converted to object |
| Other objects | Dynamic Object | Reflected to object properties |

### Example

```csharp
registry.RegisterFunction("process", (args) =>
{
    // Doe: let result = process([1, 2, 3], "test")
    
    var list = args[0] as List<object?>;  // Array from Doe
    var name = args[1]?.ToString();        // String from Doe
    
    // Process and return Doe-compatible type
    return new Dictionary<string, object?>
    {
        ["count"] = list?.Count ?? 0,
        ["name"] = name,
        ["values"] = list?.ToArray()
    };
});
```

## Exception Handling

Exceptions thrown in plugin functions are automatically caught and can be handled by Doe code:

```csharp
registry.RegisterFunction("divide", (args) =>
{
    if (args.Count < 2)
        throw new ArgumentException("divide requires two arguments");

    double a = Convert.ToDouble(args[0]);
    double b = Convert.ToDouble(args[1]);

    if (b == 0)
        throw new DivideByZeroException("Cannot divide by zero");

    return a / b;
});
```

Usage in Doe:

```doe
try
    let result = divide(10, 0)
catch e
    println("Error: " + e)
```

---

## See Also

- [Getting Started](./PluginSdk-GettingStarted.md)
- [Examples](./PluginSdk-Examples.md)
- [Best Practices](./PluginSdk-BestPractices.md)
