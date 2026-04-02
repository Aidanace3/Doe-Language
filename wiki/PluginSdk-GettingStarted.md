# Plugin SDK - Getting Started

A step-by-step guide to create your first Doe plugin.

## Prerequisites

- .NET 10.0 SDK or later
- Visual Studio, VS Code, or another C# IDE
- Basic C# knowledge
- Doe Language installed or available in your project

## Step 1: Create a New Class Library Project

```bash
dotnet new classlib -n MyDoePlugin
cd MyDoePlugin
```

## Step 2: Add Plugin SDK Reference

Add the Doe.PluginSdk NuGet package:

```bash
dotnet add package Doe.PluginSdk
```

Or manually add to your `.csproj`:

```xml
<ItemGroup>
  <ProjectReference Include="path/to/PluginSdk/Doe.PluginSdk.csproj" />
</ItemGroup>
```

## Step 3: Implement IDoePlugin

Create a new class implementing the `IDoePlugin` interface:

```csharp
using Doe.PluginSdk;

namespace MyDoePlugin;

public class MyPlugin : IDoePlugin
{
    public string Name => "MyPlugin";

    public void Register(IDoePluginRegistry registry)
    {
        // Register your functions here
        registry.RegisterFunction("greet", Greet);
        registry.RegisterFunction("add", Add);
    }

    private static object? Greet(IReadOnlyList<object?> args)
    {
        string name = args.Count > 0 ? args[0]?.ToString() ?? "World" : "World";
        return $"Hello, {name}!";
    }

    private static object? Add(IReadOnlyList<object?> args)
    {
        if (args.Count < 2) return null;
        
        double a = Convert.ToDouble(args[0]);
        double b = Convert.ToDouble(args[1]);
        return a + b;
    }
}
```

## Step 4: Build Your Plugin

```bash
dotnet build -c Release
```

## Step 5: Load in Doe

Place your plugin DLL in the plugins directory (location depends on your Doe runtime configuration).

Then use it in Doe code:

```doe
let result = greet("Alice")
println(result)

let sum = add(5, 3)
println(sum)
```

## Complete Example

See the [Examples](./PluginSdk-Examples.md) page for more detailed examples including:
- Windows platform utilities
- File system operations
- Mathematical functions
- Data transformation

## Next Steps

- **[API Reference](./PluginSdk-APIReference.md)** - Understand all available interfaces
- **[Best Practices](./PluginSdk-BestPractices.md)** - Learn design patterns
- **[Troubleshooting](./PluginSdk-Troubleshooting.md)** - Common issues and solutions
