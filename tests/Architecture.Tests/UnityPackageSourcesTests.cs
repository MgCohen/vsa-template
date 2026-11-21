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
        project.CompileWithRelativeDocuments("Outside", project.Output, host);
        var plugin = project.Write("Assets/Plugins/Lib/Lib.cs", "namespace Vendor.Lib; public sealed class Lib { public int Value() => 1; }");
        project.CompileWithRelativeDocuments("Vendor.Lib", Path.Combine(project.Root, "Assets", "Plugins"), plugin);
    }

    [Test]
    public void A_relative_document_with_no_Unity_root_stays_relative_and_outside_the_block() {
        using var reader = new BinReader(project.Output);
        var dll = reader.Index().Dlls.Single(d => d.Name == "Outside");

        Assert.That(dll.Documents, Has.All.StartsWith(@".\"));
        Assert.That(dll.HasSourceUnder(project.Root), Is.False);
    }

    [Test]
    public void A_parent_segment_in_a_relative_document_resolves_to_its_full_path() {
        Assert.That(SymbolReader.Resolve(project.Root, @".\Packages\..\Assets\Game\Note.cs"),
            Is.EqualTo(Path.Combine(project.Root, "Assets", "Game", "Note.cs")));
    }

    [TestCase(@"C:\build\Game\Note.cs")]
    [TestCase(@"c:/build/Game/Note.cs")]
    [TestCase(@"\\server\share\Game\Note.cs")]
    public void A_Windows_absolute_document_stays_as_it_is_on_each_platform(string document) {
        Assert.That(SymbolReader.Resolve(project.Root, document), Is.EqualTo(document));
    }

    [Test]
    public void A_precompiled_DLL_under_Assets_is_not_in_the_script_assemblies_of_the_app() {
        using var reader = new BinReader(scripts);

        Assert.That(reader.Index().Dlls.Select(d => d.Name), Has.None.EqualTo("Vendor.Lib"));
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
