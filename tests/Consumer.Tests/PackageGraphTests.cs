using System.IO.Compression;
using System.Text.Json;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Tests.Shared;

namespace Consumer.Tests;

[NonParallelizable]
[Category("Package")]
public sealed class PackageGraphTests {
    DotnetResult build = null!;
    string assets = "";

    IReadOnlyList<string> analyzers = [];
    DotnetResult noPackBuild = null!;
    string noPackAssets = "";
    IReadOnlyList<string> noPackAnalyzers = [];

    [OneTimeSetUp]
    public void Build() {
        using var workspace = Workspace.Create(Repo.Path("tests", "Fixtures", "Built", "PackGraph"));
        build = workspace.Dotnet("build", "PackGraph.csproj", "-nodeReuse:false");
        analyzers = Analyzers(workspace, "PackGraph.csproj");
        var assetsFile = Path.Combine(workspace.Src, "artifacts~", "obj", "PackGraph", "project.assets.json");
        assets = File.ReadAllText(assetsFile);
        using var noPack = Workspace.Create(Repo.Path("tests", "Fixtures", "Built", "NoPack"));
        noPackBuild = noPack.Dotnet("build", "NoPack.csproj", "-nodeReuse:false");
        noPackAnalyzers = Analyzers(noPack, "NoPack.csproj");
        noPackAssets = File.ReadAllText(Path.Combine(noPack.Src, "artifacts~", "obj", "NoPack", "project.assets.json"));
    }

    [Test]
    public void An_app_with_no_default_pack_builds_and_gets_the_kernel_rules() {
        Assert.That(noPackBuild.Succeeded, Is.True, noPackBuild.Output);
        Assert.That(Lines(noPackBuild, "UMP010101", "LongLine.cs"), Is.Not.Empty, noPackBuild.Output);
    }

    [Test]
    public void An_app_with_no_default_pack_gets_no_VSA_package() =>
        Assert.That(noPackAssets, Does.Contain("\"Umpire/").And.Contain("\"Umpire.Engine/").And.Not.Contain("\"Umpire.Vsa/"));

    [Test]
    public void An_app_with_no_default_pack_compiles_with_the_analyzer_entry_of_the_engine() {
        var names = noPackAnalyzers.Select(Path.GetFileName).ToList();
        Assert.That(names, Has.Exactly(1).EqualTo("Umpire.Engine.Analyzer.dll"), string.Join("\n", noPackAnalyzers));
        Assert.That(noPackAnalyzers.Select(Path.GetDirectoryName).Distinct(), Has.Exactly(1).Items, string.Join("\n", noPackAnalyzers));
    }

    static IReadOnlyList<string> Analyzers(Workspace workspace, string project) {
        var result = workspace.Dotnet("msbuild", project, "-t:ResolveReferences", "-getItem:Analyzer", "-nodeReuse:false")
            .EnsureSuccess();
        using var json = JsonDocument.Parse(result.Output[result.Output.IndexOf('{')..]);
        return [.. json.RootElement.GetProperty("Items").GetProperty("Analyzer").EnumerateArray()
            .Select(item => item.GetProperty("FullPath").GetString()!)
            .Where(path => Path.GetFileName(path).StartsWith("Umpire.", StringComparison.Ordinal))];
    }

    [Test]
    public void The_engine_package_holds_the_analyzer_entry_and_each_DLL_that_it_loads() =>
        Assert.That(Entries("Umpire.Engine"), Is.SupersetOf(new[] {
            "analyzers/dotnet/cs/Umpire.Engine.Analyzer.dll",
            "analyzers/dotnet/cs/Umpire.Engine.dll",
            "analyzers/dotnet/cs/Umpire.dll",
            "analyzers/dotnet/cs/Umpire.Vsa.dll",
            "analyzers/dotnet/cs/Microsoft.Extensions.DependencyInjection.dll",
            "analyzers/dotnet/cs/Microsoft.Bcl.AsyncInterfaces.dll",
        }));

    [Test]
    public void The_engine_package_holds_no_DLL_that_the_compiler_gives() =>
        Assert.That(Entries("Umpire.Engine").Where(entry => entry.StartsWith("analyzers/", StringComparison.Ordinal))
            .Select(entry => entry["analyzers/dotnet/cs/".Length..])
            .Where(name => name.StartsWith("Microsoft.CodeAnalysis", StringComparison.Ordinal)
                || (name.StartsWith("System.", StringComparison.Ordinal) && name != "System.Reflection.MetadataLoadContext.dll")),
            Is.Empty);

    [Test]
    public void No_old_analyzer_package_is_in_the_feed() =>
        Assert.That(LocalFeed.Packages, Has.None.EqualTo("Umpire.Kernels.Analyzers").And.None.EqualTo("Umpire.Vsa.Analyzers"));

    [Test]
    public void The_core_package_brings_the_analyzer_of_the_engine() {
        var dependencies = Nuspec("Umpire").Descendants().Where(e => e.Name.LocalName == "dependency")
            .Where(e => (string?)e.Attribute("id") == "Umpire.Engine").ToList();
        Assert.That(dependencies, Is.Not.Empty);
        Assert.That(dependencies.Select(e => (string?)e.Attribute("exclude") ?? ""),
            Has.None.Contains("Analyzers").And.None.Contains("Build"));
    }

    [Test]
    public void The_core_package_depends_on_no_pack_and_no_definition_package() =>
        Assert.That(Dependencies("Umpire"), Has.None.StartsWith("Umpire.Vsa").And.None.Contains("Definition"));

    [Test]
    public void The_VSA_package_depends_on_the_core_and_on_no_definition_package() =>
        Assert.That(Dependencies("Umpire.Vsa"), Has.Member("Umpire").And.None.Contains("Definition"));

    [TestCase("Umpire.Web")]
    [TestCase("Umpire.Testing")]
    [TestCase("Umpire.Vsa.Definition")]
    public void A_package_with_VSA_code_depends_on_the_VSA_package(string package) =>
        Assert.That(Dependencies(package), Has.Member("Umpire.Vsa"));

    [Test]
    public void The_test_package_depends_on_the_definition_packages() =>
        Assert.That(Dependencies("Umpire.Testing"), Is.SupersetOf(new[] { "Umpire.Vsa", "Umpire.Definition", "Umpire.Vsa.Definition" }));

    [TestCase("Umpire", "Umpire.Architecture")]
    [TestCase("Umpire", "Umpire.Kernels.Blocks")]
    [TestCase("Umpire", "Umpire.Vsa")]
    [TestCase("Umpire.Vsa", "Umpire.Architecture")]
    [TestCase("Umpire.Vsa", "Umpire.Kernels")]
    [TestCase("Umpire.Vsa", "Umpire.Messages")]
    [TestCase("Umpire.Vsa", "Umpire.Vsa.Definition")]
    [TestCase("Umpire.Vsa", "Umpire.Vsa.Views")]
    public void A_runtime_DLL_holds_no_namespace_of_another_package(string package, string space) =>
        Assert.That(Namespaces(package), Has.None.EqualTo(space).And.None.StartsWith(space + "."));

    [Test]
    public void An_app_gets_no_definition_package() =>
        Assert.That(assets, Does.Not.Contain("\"Umpire.Definition/").And.Not.Contain("\"Umpire.Vsa.Definition/"));

    [Test]
    public void An_app_with_the_default_pack_gets_the_core_and_the_VSA_package() =>
        Assert.That(assets, Does.Contain("\"Umpire/").And.Contain("\"Umpire.Vsa/").And.Contain("\"Umpire.Engine/"));

    [Test]
    public void An_app_compiles_with_one_analyzer_entry_from_the_engine_package() {
        var entry = analyzers.Where(path => Path.GetFileName(path) == "Umpire.Engine.Analyzer.dll").ToList();
        Assert.That(entry, Has.Count.EqualTo(1), string.Join("\n", analyzers));
        Assert.That(entry[0], Does.Contain("umpire.engine"), string.Join("\n", analyzers));
        Assert.That(analyzers.Select(Path.GetDirectoryName).Distinct(), Has.Exactly(1).Items, string.Join("\n", analyzers));
    }

    [Test]
    public void An_app_with_no_pack_line_gets_the_VSA_pack_and_both_rule_sets() {
        Assert.That(Lines("warning UMP010201", "PeekRequest.cs"), Is.Not.Empty, build.Output);
        Assert.That(Lines("UMP010101", "LongLine.cs"), Is.Not.Empty, build.Output);
        Assert.That(Lines("UMP010001"), Is.Empty, build.Output);
        Assert.That(Lines("CS8032"), Is.Empty, build.Output);
    }

    static IReadOnlyList<string> Entries(string package) {
        using var nupkg = ZipFile.OpenRead(Path.Combine(LocalFeed.Path, $"{package}.{LocalFeed.Version}.nupkg"));
        return [.. nupkg.Entries.Select(e => e.FullName)];
    }

    static IReadOnlyList<string> Dependencies(string package) =>
        [.. Nuspec(package).Descendants().Where(e => e.Name.LocalName == "dependency")
            .Select(e => (string?)e.Attribute("id") ?? "").Distinct()];

    static IReadOnlyList<string> Namespaces(string package) {
        using var nupkg = ZipFile.OpenRead(Path.Combine(LocalFeed.Path, $"{package}.{LocalFeed.Version}.nupkg"));
        using var stream = new MemoryStream();
        using (var dll = nupkg.GetEntry($"lib/netstandard2.0/{package}.dll")!.Open()) {
            dll.CopyTo(stream);
        }
        stream.Position = 0;
        using var pe = new PEReader(stream);
        var reader = pe.GetMetadataReader();
        return [.. reader.TypeDefinitions.Select(handle => reader.GetString(reader.GetTypeDefinition(handle).Namespace)).Distinct()];
    }

    static System.Xml.Linq.XDocument Nuspec(string package) {
        using var nupkg = ZipFile.OpenRead(Path.Combine(LocalFeed.Path, $"{package}.{LocalFeed.Version}.nupkg"));
        using var stream = nupkg.GetEntry($"{package}.nuspec")!.Open();
        return System.Xml.Linq.XDocument.Load(stream);
    }

    IEnumerable<string> Lines(string text) => Lines(build, text);

    IEnumerable<string> Lines(string text, string file) => Lines(build, text, file);

    static IEnumerable<string> Lines(DotnetResult result, string text) =>
        result.Output.Split('\n').Where(line => line.Contains(text, StringComparison.Ordinal));

    static IEnumerable<string> Lines(DotnetResult result, string text, string file) =>
        Lines(result, text).Where(line => line.Contains(file, StringComparison.Ordinal));
}
