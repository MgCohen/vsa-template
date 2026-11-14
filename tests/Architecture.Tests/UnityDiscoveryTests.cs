using VSA.Architecture.Engine;
using VSA.Architecture.Vsa;
using VSA.Kernel;

namespace Architecture.Tests;

public sealed class UnityDiscoveryTests {
    const string ModuleClass = "public sealed class {0} : VSA.Module.Module {{ public override void Configure(VSA.Kernel.AnchorBuilder module) {{ }} }}";

    UnityProjectFolder project = null!;
    ArchitectureMap map = null!;

    [OneTimeSetUp]
    public void BuildTheUnityProjectAndItsDotnetBuild() {
        project = UnityProjectFolder.Create();
        var module = project.Write("Assets/Game/Features/Notes/NotesModule.cs", "namespace Notes; " + string.Format(ModuleClass, "NotesModule"));
        var note = project.Write("Assets/Game/Features/Notes/Domain/Note.cs", "namespace Notes.Domain; public sealed class Note { }");
        var view = project.Write("Assets/Game/Views/Board.cs", "namespace Game.Views; public sealed class Board { public Notes.Domain.Note? Shown; }");
        var package = project.Write("Library/PackageCache/com.vendor.tool/Tool.cs", "namespace Vendor; public sealed class Tool { }");
        var tasks = project.Write("Source/Features/Tasks/TasksModule.cs", "namespace Tasks; " + string.Format(ModuleClass, "TasksModule"));
        project.Compile("Assembly-CSharp", Path.Combine(project.Root, "Library", "ScriptAssemblies"), module, note, view, package);
        project.Compile("Game.Modules", project.Output, module, note, tasks);
        map = FixtureApps.Load(new VsaDefinition(), project.Output, project.Root);
    }

    [OneTimeTearDown]
    public void DeleteTheProject() => project.Dispose();

    [Test]
    public void The_loader_reads_the_script_assemblies_of_Unity_and_the_dotnet_build() {
        Assert.That(map.App.Files.Select(Path.GetFileNameWithoutExtension), Is.EquivalentTo(new[] { "Assembly-CSharp", "Game.Modules" }));
        Assert.That(map.App.Warnings, Is.Empty);
    }

    [Test]
    public void A_type_that_Unity_and_the_dotnet_build_both_compile_is_loaded_once() {
        var copies = map.App.Types.Where(t => t.FullName == "Notes.Domain.Note");

        Assert.That(copies.Select(t => t.Assembly.Name.Split(',')[0]), Is.EqualTo(new[] { "Assembly-CSharp" }));
    }

    [TestCase("Notes.NotesModule", "Module", "Notes")]
    [TestCase("Notes.Domain.Note", "Domain", "Notes")]
    [TestCase("Tasks.TasksModule", "Module", "Tasks")]
    public void A_module_in_the_Unity_app_folder_or_the_source_folder_binds_to_its_feature(string type, string part, string feature) {
        var binding = map.Bindings.All.Single(b => b.Type.FullName == type);

        Assert.That((binding.Part.Name, binding.Values["Feature"]), Is.EqualTo((part, feature)));
    }

    [Test]
    public void A_view_in_the_Unity_app_folder_binds_to_the_app_views() {
        var binding = map.Bindings.All.Single(b => b.Type.FullName == "Game.Views.Board");

        Assert.That((binding.Part.Name, binding.Part.Parent!.Name), Is.EqualTo(("Views", "App")));
    }

    [Test]
    public void The_checks_read_the_uses_of_a_view_in_the_Unity_build() {
        var board = map.App.Types.Single(t => t.FullName == "Game.Views.Board");

        Assert.That(map.Findings(board).Select(f => f.Code), Has.Some.EqualTo("VSA2001"));
    }

    [Test]
    public void A_source_file_under_Library_is_not_app_code() {
        Assert.That(map.Bindings.AppTypes.Select(t => t.FullName), Has.None.EqualTo("Vendor.Tool"));
    }

    [Test]
    public void A_Unity_project_with_no_script_assemblies_gives_the_warning_VSA2103() {
        using var unbuilt = UnityProjectFolder.Create();
        var module = unbuilt.Write("Source/Features/Lists/ListsModule.cs", "namespace Lists; " + string.Format(ModuleClass, "ListsModule"));
        unbuilt.Compile("Unbuilt.Modules", unbuilt.Output, module);

        var discovered = FixtureApps.Load(new VsaDefinition(), unbuilt.Output, unbuilt.Root);

        Assert.That(discovered.Findings().Where(f => f.Severity == RuleSeverity.Warning).Select(f => f.Code + ": " + f.Text),
            Has.One.StartsWith("VSA2103: ").And.Contains("has no Library/ScriptAssemblies folder"));
    }
}
