namespace EngineCore.Tests;

using BuiltApp.Infrastructure;
using Umpire.Architecture;

public sealed class ArtifactsSourceFilesTests {
    const string ProjectOutput = "artifacts~/obj/Host/debug";
    const string Generator = "Microsoft.CodeAnalysis.Razor.Compiler/Microsoft.NET.Sdk.Razor.SourceGenerators.RazorSourceGenerator";

    UnityProjectFolder project = null!;
    IReadOnlyDictionary<string, Source> sources = null!;

    [OneTimeSetUp]
    public void CompileGeneratedFilesInArtifacts() {
        project = UnityProjectFolder.Create();
        project.Write("App/Host/Host.csproj", "<Project />");
        var code = project.Write("App/Host/HostApp.cs", "namespace Host; public sealed class HostApp { public int Value() => 1; }");
        var page = project.Write($"{ProjectOutput}/{Generator}/App/Views/Pages/NotesPage_razor.g.cs", "namespace Views; public sealed class NotesPage { }");
        var info = project.Write($"{ProjectOutput}/Host.AssemblyInfo.cs", "namespace Host; public sealed class Info { public int Value() => 1; }");
        project.Compile("Host", project.Output, code, page, info);
        sources = BinReader.Sources([Path.Combine(project.Output, "Host.dll")]);
    }

    [OneTimeTearDown]
    public void DeleteTheProject() => project.Dispose();

    [Test]
    public void A_generated_file_in_artifacts_keeps_its_hint_path_under_the_folder_of_its_project_file() {
        Assert.That(Relative(sources["Views.NotesPage"]), Is.EqualTo("App/Host/App/Views/Pages/NotesPage_razor.g.cs"));
    }

    [Test]
    public void A_file_of_the_build_in_artifacts_maps_to_the_folder_of_its_project_file() {
        Assert.That(Relative(sources["Host.Info"]), Is.EqualTo("App/Host/Host.AssemblyInfo.cs"));
    }

    string Relative(Source source) => Path.GetRelativePath(project.Root, source.File).Replace('\\', '/');
}
