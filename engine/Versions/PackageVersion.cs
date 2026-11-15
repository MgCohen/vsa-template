namespace Umpire.Engine.Versions;

using System.Diagnostics;
using System.Reflection;

public static class PackageVersion {
    public static string Of(Assembly assembly) =>
        WithoutBuildHash(assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? "");

    public static string Of(string dll) => WithoutBuildHash(FileVersionInfo.GetVersionInfo(dll).ProductVersion ?? "");

    static string WithoutBuildHash(string version) {
        var plus = version.IndexOf('+', StringComparison.Ordinal);
        return plus < 0 ? version : version[..plus];
    }
}
