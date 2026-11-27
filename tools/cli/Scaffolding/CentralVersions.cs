namespace Umpire.Cli.Scaffolding;

using System.Text.RegularExpressions;

static partial class CentralVersions {
    static readonly string[] Files = ["Directory.Packages.props", "Directory.Build.props"];

    public static bool Above(string folder) {
        for (var current = new DirectoryInfo(Path.GetFullPath(folder)); current is not null; current = current.Parent) {
            if (Files.Select(name => Path.Combine(current.FullName, name)).Where(File.Exists).Any(file => On(File.ReadAllText(file)))) {
                return true;
            }
        }
        return false;
    }

    public static bool On(string text) => Switch().IsMatch(text);

    public static string Overrides(string projectText) =>
        PackageVersion().Replace(projectText, "${start}VersionOverride=");

    [GeneratedRegex(@"<ManagePackageVersionsCentrally>\s*true\s*</ManagePackageVersionsCentrally>", RegexOptions.IgnoreCase)]
    private static partial Regex Switch();

    [GeneratedRegex(@"(?<start><PackageReference\s+Include=""[^""]+""\s+)Version=")]
    private static partial Regex PackageVersion();
}
