namespace EngineCore.Tests;

using BuiltApp.Infrastructure;
using Umpire.Architecture;

public sealed class UnityPackageSourcesTests {
    const string PackageFolder = "Packages/com.vendor.game";

    UnityProjectFolder project = null!;
    string scripts = null!;

    [OneTimeSetUp]
    public void CompilePackageCodeWithRelativeDocuments() {
        project = UnityProjectFolder.Create();
        var host = project.Write($"{PackageFolder}/App/Host/GameHost.cs", "namespace Game.App.Host; public sealed class GameHost { public int Value() => 1; }");
        var tool = project.Write("Library/PackageCache/com.vendor.tool/Tool.cs", "namespace Vendor; public sealed class Tool { public int Value() => 1; }");
        scripts = Path.Combine(project.Root, "Library", "ScriptAssemblies");
        project.CompileWithRelativeDocuments("Game.Package", scripts, host);
        project.CompileWithRelativeDocuments("Vendor.Tool", scripts, tool);
    }

    [OneTimeTearDown]
    public void DeleteTheProject() => project.Dispose();

    [Test]
    public void A_relative_document_in_a_Unity_PDB_resolves_against_the_Unity_project_root() {
        var sources = BinReader.Sources([Path.Combine(scripts, "Game.Package.dll")]);

        Assert.That(sources["Game.App.Host.GameHost"].File,
            Is.EqualTo(Path.Combine(project.Root, "Packages", "com.vendor.game", "App", "Host", "GameHost.cs")));
    }

    [Test]
    public void A_DLL_with_relative_documents_has_source_under_its_package_block() {
        using var reader = new BinReader(scripts);
        var dll = reader.Index().Dlls.Single(d => d.Name == "Game.Package");

        Assert.That(dll.HasSourceUnder(Path.Combine(project.Root, "Packages", "com.vendor.game")), Is.True);
    }

    [Test]
    public void A_relative_document_under_Library_stays_outside_the_app() {
        using var reader = new BinReader(scripts);
        var dll = reader.Index().Dlls.Single(d => d.Name == "Vendor.Tool");

        Assert.That(dll.HasSourceUnder(project.Root), Is.False);
    }
}
