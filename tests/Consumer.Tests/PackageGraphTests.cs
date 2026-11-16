using System.IO.Compression;
using Tests.Shared;

namespace Consumer.Tests;

[NonParallelizable]
public sealed class PackageGraphTests {
    DotnetResult build = null!;
    string assets = "";

    [OneTimeSetUp]
    public void Build() {
        using var workspace = Workspace.Create(Repo.Path("tests", "Fixtures", "Built", "PackGraph"));
        build = workspace.Dotnet("build", "PackGraph.csproj", "-nodeReuse:false");
        var assetsFile = Path.Combine(workspace.Src, "artifacts~", "obj", "PackGraph", "project.assets.json");
        assets = File.ReadAllText(assetsFile);
    }

    [TestCase("Umpire.Kernel.Analyzers")]
    [TestCase("Umpire.Vsa.Analyzers")]
    public void Each_analyzer_package_has_its_values_and_props(string package) =>
        Assert.That(Entries(package),
            Is.SupersetOf(new[] { $"build/{package}.props", $"build/{package}.globalconfig" }));

    [Test]
    public void The_VSA_analyzer_package_holds_the_kernel_analyzer_in_its_own_folder() =>
        Assert.That(Entries("Umpire.Vsa.Analyzers"), Is.SupersetOf(new[] {
            "analyzers/dotnet/cs/Umpire.Vsa.Analyzers.dll",
            "analyzers/dotnet/cs/Umpire.Kernel.Analyzers.dll",
            "build/Umpire.Kernel.Analyzers.globalconfig",
        }));

    [Test]
    public void An_app_gets_the_kernel_analyzer_only_from_the_pack_analyzer_package() =>
        Assert.That(assets, Does.Contain("\"Umpire.Vsa.Analyzers/").And.Not.Contain("\"Umpire.Kernel.Analyzers/"));

    [Test]
    public void An_app_with_no_pack_line_gets_the_VSA_pack_and_both_rule_sets() {
        Assert.That(Lines("error UMP010209", "AuditCheck.cs"), Is.Not.Empty, build.Output);
        Assert.That(Lines("warning UMP010101", "LongLine.cs"), Is.Not.Empty, build.Output);
        Assert.That(Lines("UMP010001"), Is.Empty, build.Output);
        Assert.That(Lines("CS8032"), Is.Empty, build.Output);
    }

    static IReadOnlyList<string> Entries(string package) {
        using var nupkg = ZipFile.OpenRead(Path.Combine(LocalFeed.Path, $"{package}.{LocalFeed.Version}.nupkg"));
        return [.. nupkg.Entries.Select(e => e.FullName)];
    }

    IEnumerable<string> Lines(string text) =>
        build.Output.Split('\n').Where(line => line.Contains(text, StringComparison.Ordinal));

    IEnumerable<string> Lines(string text, string file) =>
        Lines(text).Where(line => line.Contains(file, StringComparison.Ordinal));
}
