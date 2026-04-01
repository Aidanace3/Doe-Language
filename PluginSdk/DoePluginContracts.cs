namespace Doe.PluginSdk;

public delegate object? DoePluginFunction(IReadOnlyList<object?> args);

public interface IDoePlugin
{
    string Name { get; }

    void Register(IDoePluginRegistry registry);
}

public interface IDoePluginRegistry
{
    void RegisterFunction(string name, DoePluginFunction handler);
}
