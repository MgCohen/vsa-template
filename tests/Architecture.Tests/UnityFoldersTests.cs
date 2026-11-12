using VSA.Architecture;
using VSA.Architecture.Vsa;

namespace Architecture.Tests;

public sealed class UnityFoldersTests {
    [Test]
    public void A_dotnet_app_has_no_folder_maps() {
        using var project = UnityProjectFolder.Create();
        File.Delete(Path.Combine(project.Root, UnityProject.VersionFile));

        Assert.That(new VsaDefinition().Folders(project.Root), Is.Empty);
    }

    [Test]
    public void A_Unity_app_maps_its_app_folder_and_the_source_folder() {
        using var project = UnityProjectFolder.Create();
        project.Write("Assets/Game/Features/Notes/NotesModule.cs", "");
        project.Write("Assets/Art/Board.png", "");

        Assert.That(new VsaDefinition().Folders(project.Root), Is.EqualTo(new[] {
            new FolderMap("Assets/Game", ""), new FolderMap("Assets/Game", "App"), new FolderMap("Source", ""),
        }));
    }

    [Test]
    public void The_definition_can_name_the_app_folder_and_the_source_folder() {
        using var project = UnityProjectFolder.Create();

        var definition = new VsaDefinition { UnityApp = "Cards", UnitySource = "Modules" };

        Assert.That(definition.Folders(project.Root), Is.EqualTo(new[] {
            new FolderMap("Assets/Cards", ""), new FolderMap("Assets/Cards", "App"), new FolderMap("Modules", ""),
        }));
    }

    [Test]
    public void A_Unity_project_with_no_app_folder_maps_only_the_source_folder() {
        using var project = UnityProjectFolder.Create();
        project.Write("Assets/Plugins/VSA/Notes.dll", "");

        Assert.That(new VsaDefinition().Folders(project.Root), Is.EqualTo(new[] { new FolderMap("Source", "") }));
    }

    [Test]
    public void Two_app_folders_in_Assets_are_an_error() {
        using var project = UnityProjectFolder.Create();
        project.Write("Assets/Game/Views/Board.cs", "");
        project.Write("Assets/Tools/Plugins/Grid/GridPlugin.cs", "");

        var error = Assert.Throws<InvalidOperationException>(() => new VsaDefinition().Folders(project.Root));

        Assert.That(error!.Message, Does.StartWith("The Unity project has 2 app folders in Assets: Game, Tools."));
    }
}
