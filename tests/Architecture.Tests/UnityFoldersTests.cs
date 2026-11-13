using VSA.Architecture;
using VSA.Architecture.Vsa;

namespace Architecture.Tests;

public sealed class UnityFoldersTests {
    [Test]
    public void A_dotnet_app_has_no_folder_aliases() {
        using var project = UnityProjectFolder.Create();
        File.Delete(Path.Combine(project.Root, UnityProjectFolder.VersionFile));

        Assert.That(new VsaDefinition().FolderAliases(project.Root), Is.Empty);
    }

    [Test]
    public void A_Unity_app_maps_its_app_folder_and_the_source_folder() {
        using var project = UnityProjectFolder.Create();
        project.Write("Assets/Game/Features/Notes/NotesModule.cs", "");
        project.Write("Assets/Art/Board.png", "");

        Assert.That(new VsaDefinition().FolderAliases(project.Root), Is.EqualTo(new[] {
            new FolderAlias("Assets/Game", ""), new FolderAlias("Assets/Game", "App"), new FolderAlias("Source", ""),
        }));
    }

    [Test]
    public void The_definition_can_name_the_app_folder_and_the_source_folder() {
        using var project = UnityProjectFolder.Create();

        var definition = new VsaDefinition { UnityApp = "Cards", UnitySource = "Modules" };

        Assert.That(definition.FolderAliases(project.Root), Is.EqualTo(new[] {
            new FolderAlias("Assets/Cards", ""), new FolderAlias("Assets/Cards", "App"), new FolderAlias("Modules", ""),
        }));
    }

    [Test]
    public void A_Unity_project_with_no_app_folder_maps_only_the_source_folder() {
        using var project = UnityProjectFolder.Create();
        project.Write("Assets/Plugins/VSA/Notes.dll", "");

        Assert.That(new VsaDefinition().FolderAliases(project.Root), Is.EqualTo(new[] { new FolderAlias("Source", "") }));
    }

    [Test]
    public void Two_app_folders_in_Assets_are_an_error() {
        using var project = UnityProjectFolder.Create();
        project.Write("Assets/Game/Views/Board.cs", "");
        project.Write("Assets/Tools/Plugins/Grid/GridPlugin.cs", "");

        var error = Assert.Throws<InvalidOperationException>(() => new VsaDefinition().FolderAliases(project.Root));

        Assert.That(error!.Message, Does.StartWith("The Unity project has 2 app folders in Assets: Game, Tools."));
    }
}
