using System.IO.Compression;
using Tests.Shared;

namespace Consumer.Tests;

[NonParallelizable]
public sealed class PackageGraphTests {
    DotnetResult build = null!;

    [OneTimeSetUp]
    public void Build() {
        using var workspace = Workspace.Create(Repo.Path("tests", "Fixtures", "Built", "PackGraph"));
        build = workspace.Dotnet("build", "PackGraph.csproj", "-nodeReuse:false");
    }

    [Test]
    public void The_VSA_analyzer_package_has_no_kernel_analyzer() =>
        Assert.That(Entries("Umpire.Vsa.Analyzers"), Has.None.EndsWith("Umpire.Kernel.Analyzers.dll"));

    [Test]
    public void An_app_with_no_pack_line_gets_the_VSA_pack_and_both_rule_sets() {
        Assert.That(Lines("error UMP010209", "AuditCheck.cs"), Is.Not.Empty, build.Output);
        Assert.That(Lines("warning UMP010101", "LongLine.cs"), Is.Not.Empty, build.Output);
        Assert.That(Lines("UMP010001", ""), Is.Empty, build.Output);
    }

    static IReadOnlyList<string> Entries(string package) {
        using var nupkg = ZipFile.OpenRead(Path.Combine(LocalFeed.Path, $"{package}.{LocalFeed.Version}.nupkg"));
        return [.. nupkg.Entries.Select(e => e.FullName)];
    }

    IEnumerable<string> Lines(string text, string file) => build.Output.Split('\n')
        .Where(line => line.Contains(text, StringComparison.Ordinal) && line.Contains(file, StringComparison.Ordinal));
}
