using System.IO.Compression;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Tests.Shared;

namespace Rules.Tests;

// The Unity build of VSA.Sdk, in a real build from the packed packages. The built fixture tests/Fixtures/Built/Unity is a Unity app:
// the module Score in Source/ (netstandard2.1), the module Board in Assets/Game/Features/, a view and the .NET test project Game.Tests.
[NonParallelizable]
public sealed class UnityBuildTests {
    Workspace workspace = null!;

    string UnityPlugins => Path.Combine(workspace.Src, "Assets", "Plugins", "VSA");

    [OneTimeSetUp]
    public void Create() => workspace = Workspace.Create(Repo.Path("tests", "Fixtures", "Built", "Unity"));

    [OneTimeTearDown]
    public void Delete() => workspace.Dispose();

    [SetUp]
    public void DeleteCopies() => TempFolder.Delete(UnityPlugins);

    DotnetResult Build(string project, params string[] properties) =>
        workspace.Dotnet(["build", project, "-nodeReuse:false", .. properties]);

    static void Fails(DotnetResult result, string code, string text) {
        Assert.That(result.Succeeded, Is.False, $"The build passed. It must fail with {code}.");
        Assert.That(result.Output, Does.Contain($"error {code}").And.Contain(text), result.Output);
    }

    [Test]
    public void A_module_build_copies_each_DLL_and_PDB_to_the_Unity_plugins_folder() {
        Build("Source/Features/Score/Score.csproj").EnsureSuccess();

        var copied = Directory.GetFiles(UnityPlugins).Select(Path.GetFileName);
        Assert.That(copied, Is.SupersetOf(new[] {
            "Score.dll", "Score.pdb", "Score.Contracts.Requests.dll", "Score.Contracts.Requests.pdb",
            "Score.Handlers.Requests.dll", "Score.Handlers.Requests.pdb" }));
    }

    [Test]
    public void VsaUnityCopy_false_stops_the_copy() {
        Build("Source/Features/Score/Score.csproj", "-p:VsaUnityCopy=false").EnsureSuccess();

        Assert.That(Directory.Exists(UnityPlugins), Is.False);
    }

    [Test]
    public void A_module_project_that_Unity_cannot_load_fails() {
        var project = Path.Combine(workspace.Src, "Source", "Features", "Score", "Score.csproj");
        var before = File.ReadAllText(project);
        File.WriteAllText(project, """
            <Project Sdk="VSA.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
              </PropertyGroup>
            </Project>
            """);
        try {
            Fails(Build("Source/Features/Score/Score.csproj"), "VSA0007", "Unity loads only netstandard2.1");
        } finally {
            File.WriteAllText(project, before);
        }
    }

    [Test]
    public void The_test_project_compiles_the_module_sources_of_Assets_and_not_the_views() {
        var result = Build("Source/Game.Tests/Game.Tests.csproj");

        result.EnsureSuccess();
        Assert.That(result.Output, Does.Not.Contain("UnityEngine"), result.Output);
    }

    [Test]
    public void A_module_source_in_Assets_that_uses_UnityEngine_fails_the_test_build() {
        var file = Path.Combine(workspace.Src, "Assets", "Game", "Features", "Board", "Domain", "Spin.cs");
        File.WriteAllText(file, """
            using UnityEngine;

            namespace Board.Domain;

            public sealed class Spin : MonoBehaviour { }
            """);
        try {
            Fails(Build("Source/Game.Tests/Game.Tests.csproj"), "VSA0008", "Features/Board/Domain/Spin.cs");
        } finally {
            File.Delete(file);
        }
    }

    [Test]
    public void The_analyzers_reference_a_compiler_that_Unity_runs() {
        using var package = ZipFile.OpenRead(Path.Combine(LocalFeed.Path, $"VSA.Analyzers.{LocalFeed.Version}.nupkg"));
        using var dll = new MemoryStream();
        using (var entry = package.GetEntry("analyzers/dotnet/cs/VSA.Analyzers.dll")!.Open()) {
            entry.CopyTo(dll);
        }
        dll.Position = 0;
        using var reader = new PEReader(dll);
        var metadata = reader.GetMetadataReader();

        var compilers = metadata.AssemblyReferences.Select(metadata.GetAssemblyReference)
            .Where(reference => metadata.GetString(reference.Name).StartsWith("Microsoft.CodeAnalysis", StringComparison.Ordinal))
            .Select(reference => reference.Version)
            .ToList();
        Assert.That(compilers, Is.Not.Empty);
        Assert.That(compilers, Has.All.LessThanOrEqualTo(UnityCompiler));
    }

    static readonly Version UnityCompiler = new(4, 10, 0, 0);
}
