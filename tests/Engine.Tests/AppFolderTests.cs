using Tests.Shared;
using Umpire.Engine.Versions;

namespace Engine.Tests;

public sealed class AppFolderTests {
    string root = "";

    [SetUp]
    public void Create() => root = TempFolder.Create();

    [TearDown]
    public void Delete() => TempFolder.Delete(root);

    [Test]
    public void The_tool_finds_the_nearest_folder_with_an_umpireconfig_file() {
        File.WriteAllText(Path.Combine(root, AppFolder.ConfigFile), "");
        Write("Tests/App.Tests.csproj", """<Project Sdk="Umpire.Sdk/0.0.1" />""");

        Assert.That(AppFolder.Find(Path.Combine(root, "Tests")), Is.EqualTo(Path.GetFullPath(root)));
    }

    [Test]
    public void With_no_umpireconfig_file_the_tool_finds_the_nearest_project_with_an_Umpire_SDK() {
        Write("App.csproj", """<Project Sdk="Umpire.Sdk.Web/0.0.1" />""");
        Write("Inner/Other.csproj", """<Project Sdk="Microsoft.NET.Sdk" />""");

        Assert.That(AppFolder.Find(Path.Combine(root, "Inner")), Is.EqualTo(Path.GetFullPath(root)));
    }

    [Test]
    public void A_project_with_an_Umpire_SDK_and_no_version_marks_the_app_so_doctor_can_report_it() {
        Write("App.csproj", """<Project Sdk="Umpire.Sdk" />""");

        Assert.That(AppFolder.Find(root), Is.EqualTo(Path.GetFullPath(root)));
    }

    [Test]
    public void A_project_with_only_another_sdk_is_no_marker() {
        Write("App.csproj", """<Project Sdk="Microsoft.NET.Sdk" />""");

        Assert.That(AppFolder.Find(root), Is.Not.EqualTo(Path.GetFullPath(root)));
    }

    void Write(string path, string content) {
        var file = Path.Combine(root, path);
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file, content);
    }
}
