namespace EngineCore.Tests;

using BuiltApp.Domain.Shared;
using Rules.Slices.CheckRules;
using Steps.Domain.Shared;
using Steps.Infrastructure;
using Umpire.Architecture;
using Umpire.Vsa.Definition;

public sealed class FolderAliasConfigTests {
    const string ModuleClass = "public sealed class {0} : Umpire.Kernels.Module {{ public override void Configure("
        + "Microsoft.Extensions.DependencyInjection.IServiceCollection services) {{ }} }}";
    const string AppAliases = """
        [umpire.check.folder_aliases]
        "Assets/Game" = ["", "App"]
        """;

    ProjectFolder project = null!;

    string ConfigFile => Path.Combine(project.Root, ".umpireconfig");

    string Inner => Path.Combine(project.Root, "Inner");

    [OneTimeSetUp]
    public void CompileAnAppWithAnAppFolderARootFeatureAndAnInnerBlock() {
        project = ProjectFolder.Create();
        var view = project.Write("Assets/Game/Views/Board.cs", "namespace Game.Views; public sealed class Board { }");
        var notes = project.Write("Assets/Game/Features/Notes/NotesModule.cs", "namespace Notes; " + string.Format(ModuleClass, "NotesModule"));
        var tasks = project.Write("Source/Features/Tasks/TasksModule.cs", "namespace Tasks; " + string.Format(ModuleClass, "TasksModule"));
        var root = project.Write("Features/Root/RootModule.cs", "namespace Root; " + string.Format(ModuleClass, "RootModule"));
        var lists = project.Write("Inner/Features/Lists/ListsModule.cs", "namespace Lists; " + string.Format(ModuleClass, "ListsModule"));
        project.Compile("Game", project.Output, view, notes, tasks, root, lists);
    }

    [OneTimeTearDown]
    public void DeleteTheProject() => project.Dispose();

    [SetUp]
    public void RemoveTheConfigFile() => File.Delete(ConfigFile);

    [Test]
    public void The_aliases_of_the_config_file_bind_the_app_folder_through_the_check_step() {
        File.WriteAllText(ConfigFile, AppAliases);

        var check = Check(new VsaArchitecture(), project.Root);

        Assert.That(Part("Game.Views.Board", check), Is.EqualTo("App/Views"));
        Assert.That(Part("Notes.NotesModule", check), Is.EqualTo("Feature/Module"));
    }

    [Test]
    public void An_alias_does_not_apply_to_an_inner_block_with_no_config_file_of_its_own() {
        File.WriteAllText(ConfigFile, AppAliases);

        var check = Check(new VsaArchitecture(), Inner);

        Assert.That(Part("Lists.ListsModule", check), Is.EqualTo("Feature/Module"));
    }

    [Test]
    public void With_an_alias_a_root_folder_with_no_alias_is_not_read() {
        File.WriteAllText(ConfigFile, AppAliases);

        var check = Check(new VsaArchitecture(), project.Root);

        Assert.That(check.Bindings.Of("Root.RootModule"), Is.Null);
    }

    [Test]
    public void The_block_root_key_keeps_the_root_folders() {
        File.WriteAllText(ConfigFile, AppAliases + "\n\"\" = [\"\"]\n\"Source\" = [\"\"]\n");

        var check = Check(new VsaArchitecture(), project.Root);

        Assert.That(Part("Root.RootModule", check), Is.EqualTo("Feature/Module"));
        Assert.That(Part("Tasks.TasksModule", check), Is.EqualTo("Feature/Module"));
    }

    [Test]
    public void The_aliases_of_the_code_and_of_the_config_file_apply_together() {
        File.WriteAllText(ConfigFile, AppAliases);

        var check = Check(new SourceAliasArchitecture(), project.Root);

        Assert.That(Part("Game.Views.Board", check), Is.EqualTo("App/Views"));
        Assert.That(Part("Tasks.TasksModule", check), Is.EqualTo("Feature/Module"));
    }

    [Test]
    public void An_empty_alias_table_gives_no_alias() {
        File.WriteAllText(ConfigFile, "[umpire.check.folder_aliases]\n");

        var check = Check(new VsaArchitecture(), project.Root);

        Assert.That(check.Bindings.Unmapped.Select(u => u.Type.FullName), Does.Contain("Game.Views.Board"));
    }

    [Test]
    public void A_value_that_is_not_a_list_gives_the_warning_UMP020206() {
        File.WriteAllText(ConfigFile, "[umpire.check.folder_aliases]\n\"Assets/Game\" = \"App\"\n");

        var warning = Check(new VsaArchitecture(), project.Root).Findings().Single(f => f.Code == "UMP020206");

        Assert.That(warning.Severity, Is.EqualTo(FindingSeverity.Warning));
        Assert.That(warning.Message, Does.StartWith("The folder alias Assets/Game in umpire.check.folder_aliases is not a list"));
    }

    [TestCase(@".\Assets\Game\", "Assets/Game")]
    [TestCase("./Source", "Source")]
    [TestCase(".", "")]
    [TestCase("", "")]
    public void A_folder_of_an_alias_is_normalized(string folder, string normal) =>
        Assert.That(FolderAliasConfig.Normal(folder), Is.EqualTo(normal));

    ArchitectureCheck Check(Architecture definition, string block) =>
        CheckRulesHandler.Load(FixtureApps.Input(definition, project.Output, block, ReadOptions.None), new RunOptions());

    static string Part(string type, ArchitectureCheck check) {
        var part = check.Bindings.Of(type)!.Part;
        return part.Parent is { Parent: not null } parent ? $"{parent.Name}/{part.Name}" : part.Name;
    }

    sealed class SourceAliasArchitecture : VsaArchitecture {
        public override IReadOnlyList<FolderAlias> FolderAliases(string blockFolder) => [new("Source", "")];
    }
}
