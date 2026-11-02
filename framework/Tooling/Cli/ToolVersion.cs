using System.Reflection;

namespace VSA.Cli;

static class ToolVersion {
    // The package version of this tool, for example "0.1.0". VSA.Sdk and the VSA.* packages have the same version.
    public static string Current { get; } = Read();

    static string Read() {
        var version = typeof(ToolVersion).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion;
        var plus = version.IndexOf('+');
        return plus < 0 ? version : version[..plus];
    }
}
