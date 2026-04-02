# Plugin SDK Overview

The Doe Plugin SDK allows developers to extend the Doe language runtime with custom functionality written in C#/.NET.

## Table of Contents

1. [Architecture](#architecture)
2. [Core Concepts](#core-concepts)
3. [Key Interfaces](#key-interfaces)
4. [Plugin Lifecycle](#plugin-lifecycle)
5. [Next Steps](#next-steps)

## Architecture

The Plugin SDK is built on a simple, extensible architecture:

```
Doe Runtime
    ↓
Plugin Registry
    ↓
IDoePlugin Implementation (Your Plugin)
    ↓
IDoePluginRegistry Functions
```

## Core Concepts

### Plugins

A plugin is a .NET assemblies that implements the `IDoePlugin` interface and registers custom functions with the Doe runtime.

**Key benefits:**
- Extend Doe without modifying the core runtime
- Leverage full .NET platform capabilities
- Platform-specific implementations (Windows, Linux, macOS)
- Easy function registration and invocation

### Functions

Plugin functions are exposed to Doe code as first-class functions that can:
- Accept any number of arguments
- Return any type of value
- Handle null/None values
- Raise exceptions to be caught by Doe code

## Key Interfaces

### IDoePlugin

```csharp
public interface IDoePlugin
{
    string Name { get; }
    void Register(IDoePluginRegistry registry);
}
```

Every plugin must implement this interface.

**Properties:**
- `Name` - Unique identifier for the plugin (used for loading and namespacing)

**Methods:**
- `Register(IDoePluginRegistry registry)` - Called when the plugin is loaded, use this to register all functions

### IDoePluginRegistry

```csharp
public interface IDoePluginRegistry
{
    void RegisterFunction(string name, DoePluginFunction handler);
}
```

Used to register custom functions that will be exposed to Doe code.

**Methods:**
- `RegisterFunction(name, handler)` - Register a new function accessible from Doe code

### DoePluginFunction Delegate

```csharp
public delegate object? DoePluginFunction(IReadOnlyList<object?> args);
```

**Parameters:**
- `args` - List of arguments passed from Doe code (can be empty)

**Returns:**
- Any object value, or `null` for None
- Exceptions are automatically marshaled to Doe callbacks

## Plugin Lifecycle

1. **Discovery** - Runtime scans for plugins (in designated directories)
2. **Loading** - Plugin assemblies are loaded into memory
3. **Instantiation** - Plugin classes implementing `IDoePlugin` are instantiated
4. **Registration** - The `Register()` method is called for each plugin
5. **Availability** - Registered functions become available to Doe code
6. **Unloading** - When runtime stops, plugins are cleaned up

## Next Steps

- **[Getting Started Guide](./PluginSdk-GettingStarted.md)** - Create your first plugin
- **[API Reference](./PluginSdk-APIReference.md)** - Detailed API documentation
- **[Examples](./PluginSdk-Examples.md)** - Sample implementations
- **[Best Practices](./PluginSdk-BestPractices.md)** - Design patterns and tips
