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

    [TestCase("Umpire.Kernels.Analyzers")]
    [TestCase("Umpire.Vsa.Analyzers")]
    public void Each_analyzer_package_holds_only_analyzer_DLLs(string package) {
        var analyzers = Entries(package).Where(entry => entry.StartsWith("analyzers/", StringComparison.Ordinal)).ToList();
        Assert.That(analyzers, Has.Member($"analyzers/dotnet/cs/{package}.dll"));
        Assert.That(analyzers, Has.All.Matches<string>(entry => entry.EndsWith(".Analyzers.dll", StringComparison.Ordinal)));
    }

    [TestCase("Umpire.Kernels.Analyzers")]
    [TestCase("Umpire.Vsa.Analyzers")]
    public void Each_analyzer_package_holds_the_notices_of_the_embedded_libraries(string package) =>
        Assert.That(Entries(package), Has.Member("THIRD-PARTY-NOTICES.txt"));

    [TestCase("Umpire.Kernels.Analyzers")]
    [TestCase("Umpire.Vsa.Analyzers")]
    public void No_analyzer_package_has_a_rule_values_file(string package) =>
        Assert.That(Entries(package).Where(entry => entry.StartsWith("build", StringComparison.Ordinal)), Is.Empty);

    [Test]
    public void The_VSA_analyzer_package_holds_the_kernel_analyzer_in_its_own_folder() =>
        Assert.That(Entries("Umpire.Vsa.Analyzers"), Is.SupersetOf(new[] {
            "analyzers/dotnet/cs/Umpire.Vsa.Analyzers.dll",
            "analyzers/dotnet/cs/Umpire.Kernels.Analyzers.dll",
        }));

    [Test]
    public void The_runtime_package_brings_the_pack_analyzers() {
        var dependencies = Nuspec("Umpire").Descendants().Where(e => e.Name.LocalName == "dependency")
            .Where(e => (string?)e.Attribute("id") == "Umpire.Vsa.Analyzers").ToList();
        Assert.That(dependencies, Is.Not.Empty);
        Assert.That(dependencies.Select(e => (string?)e.Attribute("exclude") ?? ""),
            Has.None.Contains("Analyzers").And.None.Contains("Build"));
    }

    [Test]
    public void An_app_gets_the_kernel_analyzer_only_from_the_pack_analyzer_package() =>
        Assert.That(assets, Does.Contain("\"Umpire.Vsa.Analyzers/").And.Not.Contain("\"Umpire.Kernels.Analyzers/"));

    [Test]
    public void An_app_with_no_pack_line_gets_the_VSA_pack_and_both_rule_sets() {
        Assert.That(Lines("error UMP010209", "AuditCheck.cs"), Is.Not.Empty, build.Output);
        Assert.That(Lines("UMP010101", "LongLine.cs"), Is.Not.Empty, build.Output);
        Assert.That(Lines("UMP010001"), Is.Empty, build.Output);
        Assert.That(Lines("CS8032"), Is.Empty, build.Output);
    }

    static IReadOnlyList<string> Entries(string package) {
        using var nupkg = ZipFile.OpenRead(Path.Combine(LocalFeed.Path, $"{package}.{LocalFeed.Version}.nupkg"));
        return [.. nupkg.Entries.Select(e => e.FullName)];
    }

    static System.Xml.Linq.XDocument Nuspec(string package) {
        using var nupkg = ZipFile.OpenRead(Path.Combine(LocalFeed.Path, $"{package}.{LocalFeed.Version}.nupkg"));
        using var stream = nupkg.GetEntry($"{package}.nuspec")!.Open();
        return System.Xml.Linq.XDocument.Load(stream);
    }

    IEnumerable<string> Lines(string text) =>
        build.Output.Split('\n').Where(line => line.Contains(text, StringComparison.Ordinal));

    IEnumerable<string> Lines(string text, string file) =>
        Lines(text).Where(line => line.Contains(file, StringComparison.Ordinal));
}
