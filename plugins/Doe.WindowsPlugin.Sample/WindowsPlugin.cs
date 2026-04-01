namespace Doe.WindowsPlugin.Sample;

public static class PluginFunctions
{
    public static string Win_Platform() => Environment.OSVersion.Platform.ToString();

    public static bool Win_IsWindows() => OperatingSystem.IsWindows();

    public static string Win_MachineName() => Environment.MachineName;

    public static string Win_ComposeWindowTitle(string title) => Environment.MachineName + " :: " + title;
}
