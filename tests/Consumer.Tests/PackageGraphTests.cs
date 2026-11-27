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
        Assert.That(noPackAssets, Does.Contain("\"Umpire/").And.Contain("\"Umpire.Kernels.Analyzers/")
            .And.Not.Contain("\"Umpire.Vsa/").And.Not.Contain("\"Umpire.Vsa.Analyzers/"));

    [Test]
    public void An_app_with_no_default_pack_compiles_with_the_kernel_analyzer_and_no_VSA_analyzer() {
        var names = noPackAnalyzers.Select(Path.GetFileName).ToList();
        Assert.That(names, Has.Exactly(1).EqualTo("Umpire.Kernels.Analyzers.dll"), string.Join("\n", noPackAnalyzers));
        Assert.That(names, Has.None.EqualTo("Umpire.Vsa.Analyzers.dll"), string.Join("\n", noPackAnalyzers));
    }

    static IReadOnlyList<string> Analyzers(Workspace workspace, string project) {
        var result = workspace.Dotnet("msbuild", project, "-t:ResolveReferences", "-getItem:Analyzer", "-nodeReuse:false")
            .EnsureSuccess();
        using var json = JsonDocument.Parse(result.Output[result.Output.IndexOf('{')..]);
        return [.. json.RootElement.GetProperty("Items").GetProperty("Analyzer").EnumerateArray()
            .Select(item => item.GetProperty("FullPath").GetString()!)
            .Where(path => Path.GetFileName(path).StartsWith("Umpire.", StringComparison.Ordinal))];
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

    [TestCase("Umpire", "Umpire.Kernels.Analyzers")]
    [TestCase("Umpire.Vsa", "Umpire.Vsa.Analyzers")]
    public void A_runtime_package_brings_its_analyzers(string package, string analyzers) {
        var dependencies = Nuspec(package).Descendants().Where(e => e.Name.LocalName == "dependency")
            .Where(e => (string?)e.Attribute("id") == analyzers).ToList();
        Assert.That(dependencies, Is.Not.Empty);
        Assert.That(dependencies.Select(e => (string?)e.Attribute("exclude") ?? ""),
            Has.None.Contains("Analyzers").And.None.Contains("Build"));
    }

    [Test]
    public void The_core_package_holds_the_target_that_keeps_one_kernel_analyzer() =>
        Assert.That(Entries("Umpire"), Is.SupersetOf(new[] { "build/Umpire.targets", "buildTransitive/Umpire.targets" }));

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
        Assert.That(assets, Does.Contain("\"Umpire/").And.Contain("\"Umpire.Vsa/").And.Contain("\"Umpire.Vsa.Analyzers/"));

    [Test]
    public void An_app_compiles_with_one_kernel_analyzer_the_copy_next_to_the_VSA_analyzer() {
        var kernel = analyzers.Where(path => Path.GetFileName(path) == "Umpire.Kernels.Analyzers.dll").ToList();
        Assert.That(kernel, Has.Count.EqualTo(1), string.Join("\n", analyzers));
        Assert.That(kernel[0], Does.Contain("umpire.vsa.analyzers"), string.Join("\n", analyzers));
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
