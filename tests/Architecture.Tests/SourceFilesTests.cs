using VSA.Architecture.Engine;

namespace Architecture.Tests;

public sealed class SourceFilesTests {
    const string Generator = "obj/Debug/net10.0/Microsoft.CodeAnalysis.Razor.Compiler/Microsoft.NET.Sdk.Razor.SourceGenerators.RazorSourceGenerator";

    UnityProjectFolder project = null!;
    Dictionary<string, Source> sources = null!;

    [OneTimeSetUp]
    public void CompileGeneratedFiles() {
        project = UnityProjectFolder.Create();
        var page = project.Write($"Host/{Generator}/App/Views/Pages/NotesPage_razor.g.cs", "namespace Views; public sealed class NotesPage { }");
        var info = project.Write("Host/obj/Debug/net10.0/Host.AssemblyInfo.cs", "namespace Host; public sealed class Info { public int Value() => 1; }");
        var flat = project.Write($"Host/{Generator}/Flat_razor.g.cs", "namespace Views; public sealed class Flat { }");
        var embedded = project.Write("Host/obj/Debug/net10.0/Embedded.g.cs", """
            namespace Microsoft.CodeAnalysis { internal sealed class EmbeddedAttribute : System.Attribute { } }
            namespace Validation { [Microsoft.CodeAnalysis.Embedded] internal sealed class ValidatableTypeAttribute : System.Attribute { } }
            """);
        project.Compile("Host", project.Output, page, info, flat, embedded);
        sources = SourceFiles.Read([Path.Combine(project.Output, "Host.dll")]);
    }

    [OneTimeTearDown]
    public void DeleteTheProject() => project.Dispose();

    [Test]
    public void A_generated_file_keeps_its_hint_path_under_the_project_folder() {
        Assert.That(Relative(sources["Views.NotesPage"]), Is.EqualTo("Host/App/Views/Pages/NotesPage_razor.g.cs"));
    }

    [Test]
    public void A_generated_file_with_no_hint_folder_maps_to_the_project_folder() {
        Assert.That(Relative(sources["Views.Flat"]), Is.EqualTo("Host/Flat_razor.g.cs"));
    }

    [Test]
    public void A_file_of_the_build_in_obj_maps_to_the_project_folder() {
        Assert.That(Relative(sources["Host.Info"]), Is.EqualTo("Host/Host.AssemblyInfo.cs"));
    }

    [Test]
    public void A_type_with_the_embedded_attribute_has_no_source() {
        Assert.That(sources.Keys, Has.None.EqualTo("Validation.ValidatableTypeAttribute"));
    }

    string Relative(Source source) => Path.GetRelativePath(project.Root, source.File).Replace('\\', '/');
}
