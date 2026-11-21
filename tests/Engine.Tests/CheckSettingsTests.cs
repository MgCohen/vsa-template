using Engine.Infrastructure;
using Tests.Shared;
using Umpire.Architecture;

namespace Engine.Tests;

public sealed class CheckSettingsTests {
    const string Config = """
        [umpire.check]
        references = "Scripts; Plugins/Lib"
        source_root = "."
        ignored_folders = "Cache, Temp"
        project_checks = false
        """;

    string block = null!;

    [SetUp]
    public void WriteTheConfigFile() {
        block = TempFolder.Create();
        File.WriteAllText(Path.Combine(block, ".umpireconfig"), Config);
    }

    [TearDown]
    public void DeleteTheBlock() => TempFolder.Delete(block);

    [Test]
    public void The_settings_come_from_the_config_file_with_folders_from_its_folder() {
        var settings = CheckSettings.Of(block, "", "", "", null);

        Assert.That(settings.References, Is.EqualTo(new[] { Path.Combine(block, "Scripts"), Path.Combine(block, "Plugins", "Lib") }));
        Assert.That(settings.SourceRoot, Is.EqualTo(Path.GetFullPath(block)));
        Assert.That(settings.IgnoredFolders, Is.EqualTo(new[] { "Cache", "Temp" }));
        Assert.That(settings.ProjectChecks, Is.False);
    }

    [Test]
    public void The_folder_aliases_come_from_their_table_in_the_config_file() {
        File.AppendAllText(Path.Combine(block, ".umpireconfig"), """

            [umpire.check.folder_aliases]
            "Assets/Dice" = ["", "App"]
            "Source" = [""]
            """);

        var aliases = CheckSettings.Of(block, "", "", "", null).FolderAliases;

        Assert.That(aliases, Is.EqualTo(new[] { new FolderAlias("Assets/Dice", ""), new FolderAlias("Assets/Dice", "App"), new FolderAlias("Source", "") }));
    }

    [Test]
    public void A_flag_overrides_the_config_file() {
        var other = Path.Combine(block, "Other");

        var settings = CheckSettings.Of(block, other, other, "Logs", true);

        Assert.That(settings.References, Is.EqualTo(new[] { other }));
        Assert.That(settings.SourceRoot, Is.EqualTo(other));
        Assert.That(settings.IgnoredFolders, Is.EqualTo(new[] { "Logs" }));
        Assert.That(settings.ProjectChecks, Is.True);
    }

    [Test]
    public void With_no_config_and_no_flag_the_settings_are_the_defaults() {
        File.Delete(Path.Combine(block, ".umpireconfig"));

        var settings = CheckSettings.Of(block, "", "", "", null);

        Assert.That(settings.References, Is.Empty);
        Assert.That(settings.SourceRoot, Is.Null);
        Assert.That(settings.IgnoredFolders, Is.Empty);
        Assert.That(settings.ProjectChecks, Is.True);
    }
}
