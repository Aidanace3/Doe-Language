# Doe.WindowsPlugin.Sample

Sample C# plugin for the Doe runtime.

Build:

```powershell
dotnet build
```

Use from Doe:

```dough
with plugin:Doe.WindowsPlugin.Sample

Print(Win_Platform())
Print(Win_IsWindows())
Print(Win_MachineName())
Print(Win_ComposeWindowTitle("Doe Test"))
```

Available functions:

- `Win_Platform()`
- `Win_IsWindows()`
- `Win_MachineName()`
- `Win_ComposeWindowTitle(title)`

This sample stays pure managed so it is easy to verify. For bigger Windows-only work, this same plugin project can be extended with P/Invoke, WinForms, WPF, or WinUI code.
