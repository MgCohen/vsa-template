namespace EngineCore.Tests;

using BuiltApp.Domain.Shared;
using BuiltApp.Infrastructure;

public sealed class SourceRootTests {
    const string PackageFolder = "Packages/com.vendor.game";

    UnityProjectFolder project = null!;
    string output = null!;

    [OneTimeSetUp]
    public void CompileCodeWithRelativeDocuments() {
        project = UnityProjectFolder.Create();
        var host = project.Write($"{PackageFolder}/App/Host/GameHost.cs", "namespace Game.App.Host; public sealed class GameHost { public int Value() => 1; }");
        var tool = project.Write("Cache/com.vendor.tool/Tool.cs", "namespace Vendor; public sealed class Tool { public int Value() => 1; }");
        output = Path.Combine(project.Root, "Out");
        project.CompileWithRelativeDocuments("Game.Package", output, host);
        project.CompileWithRelativeDocuments("Vendor.Tool", output, tool);
    }

    [OneTimeTearDown]
    public void DeleteTheProject() => project.Dispose();

    [Test]
    public void A_relative_document_resolves_against_the_source_root() {
        var sources = BinReader.Sources([Path.Combine(output, "Game.Package.dll")], project.Root);

        Assert.That(sources["Game.App.Host.GameHost"].File,
            Is.EqualTo(Path.Combine(project.Root, "Packages", "com.vendor.game", "App", "Host", "GameHost.cs")));
    }

    [Test]
    public void A_DLL_with_relative_documents_has_source_under_its_block_with_a_source_root() {
        using var reader = new BinReader(output, new ReadOptions([], project.Root, []));
        var dll = reader.Index().Dlls.Single(d => d.Name == "Game.Package");

        Assert.That(dll.HasSourceUnder(Path.Combine(project.Root, "Packages", "com.vendor.game")), Is.True);
    }

    [Test]
    public void A_relative_document_with_no_source_root_stays_relative_and_outside_the_block() {
        using var reader = new BinReader(output);
        var dll = reader.Index().Dlls.Single(d => d.Name == "Game.Package");

        Assert.That(dll.Documents, Has.All.StartsWith(@".\"));
        Assert.That(dll.HasSourceUnder(project.Root), Is.False);
    }

    [Test]
    public void A_document_in_an_ignored_folder_is_not_source_of_the_block() {
        using var reader = new BinReader(output, new ReadOptions([], project.Root, ["Cache"]));
        var index = reader.Index();

        Assert.That(index.AppDlls(project.Root, []).Select(d => d.Name), Is.EqualTo(new[] { "Game.Package" }));
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
}
