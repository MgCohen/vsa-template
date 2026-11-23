namespace EngineCore.Tests;

using Rules.Slices.CheckRules;
using BuiltApp.Domain.Shared;
using Steps.Domain.Shared;
using Umpire.Architecture;
using Umpire.Vsa.Definition;
using Umpire.Kernels;

public sealed class ReferenceFoldersTests {
    const string ModuleClass = "public sealed class {0} : Umpire.Kernels.Module {{ public override void Configure("
        + "Microsoft.Extensions.DependencyInjection.IServiceCollection services) {{ }} }}";

    ProjectFolder project = null!;
    ArchitectureCheck map = null!;

    string Scripts => Path.Combine(project.Root, "Scripts");

    string Missing => Path.Combine(project.Root, "Missing");

    [OneTimeSetUp]
    public void BuildTwoOutputsOfOneBlock() {
        project = ProjectFolder.Create();
        var module = project.Write("Assets/Game/Features/Notes/NotesModule.cs", "namespace Notes; " + string.Format(ModuleClass, "NotesModule"));
        var note = project.Write("Assets/Game/Features/Notes/Domain/Note.cs", "namespace Notes.Domain; public sealed class Note { }");
        var view = project.Write("Assets/Game/Views/Board.cs", "namespace Game.Views; public sealed class Board { public Notes.Domain.Note? Shown; }");
        var package = project.Write("Cache/com.vendor.tool/Tool.cs", "namespace Vendor; public sealed class Tool { }");
        var tasks = project.Write("Source/Features/Tasks/TasksModule.cs", "namespace Tasks; " + string.Format(ModuleClass, "TasksModule"));
        project.Compile("Assembly-CSharp", Scripts, module, note, view, package);
        project.Compile("Game.Modules", project.Output, module, note, tasks);
        FolderAlias[] aliases = [new("Assets/Game", ""), new("Assets/Game", "App"), new("Source", "")];
        map = FixtureApps.Load(
            new VsaArchitecture(), project.Output, project.Root, new ReadOptions([Scripts, Missing], null, ["Cache"]), aliases);
    }

    [OneTimeTearDown]
    public void DeleteTheProject() => project.Dispose();

    [Test]
    public void The_loader_reads_the_bin_and_the_reference_folders() {
        Assert.That(map.App.Files.Select(Path.GetFileNameWithoutExtension), Is.EquivalentTo(new[] { "Assembly-CSharp", "Game.Modules" }));
    }

    [Test]
    public void A_type_that_two_outputs_both_compile_is_loaded_once() {
        var copies = map.App.Types.Where(t => t.FullName == "Notes.Domain.Note");

        Assert.That(copies.Select(t => t.Assembly.Name.Split(',')[0]), Is.EqualTo(new[] { "Assembly-CSharp" }));
    }

    [TestCase("Notes.NotesModule", "RootClass", "Notes")]
    [TestCase("Notes.Domain.Note", "Domain", "Notes")]
    [TestCase("Tasks.TasksModule", "RootClass", "Tasks")]
    public void A_module_in_an_aliased_folder_binds_to_its_feature(string type, string part, string feature) {
        var binding = map.Bindings.All.Single(b => b.Type.FullName == type);

        Assert.That(binding.Part.Name, Is.EqualTo(part));
        Assert.That(binding.Values["Feature"], Is.EqualTo(feature));
    }

    [Test]
    public void A_view_in_an_aliased_folder_binds_to_the_app_views() {
        var binding = map.Bindings.All.Single(b => b.Type.FullName == "Game.Views.Board");

        Assert.That(binding.Part.Name, Is.EqualTo("Views"));
        Assert.That(binding.Part.Parent!.Name, Is.EqualTo("(root)"));
    }

    [Test]
    public void The_checks_read_the_uses_of_a_view_in_a_reference_folder() {
        var file = map.Bindings.Of("Game.Views.Board")!.Source!.File;

        Assert.That(map.Findings().Where(f => f.File == file).Select(f => f.Code), Has.Some.EqualTo("UMP020101"));
    }

    [Test]
    public void A_source_file_in_an_ignored_folder_is_not_app_code() {
        Assert.That(map.Bindings.AppTypes.Select(t => t.FullName), Has.None.EqualTo("Vendor.Tool"));
    }

    [Test]
    public void With_no_folder_aliases_a_view_in_an_app_folder_is_unmapped() {
        var plain = FixtureApps.Load(new VsaArchitecture(), project.Output, project.Root, new ReadOptions([Scripts], null, ["Cache"]));

        Assert.That(plain.Bindings.Unmapped.Select(u => u.Type.FullName), Does.Contain("Game.Views.Board"));
    }

    [Test]
    public void A_reference_folder_that_does_not_exist_gives_the_warning_UMP020205() {
        var warning = map.Findings().Single(f => f.Code == "UMP020205");

        Assert.That(warning.Severity, Is.EqualTo(FindingSeverity.Warning));
        Assert.That(warning.Message, Does.StartWith($"The reference folder {Missing} does not exist"));
    }
}
