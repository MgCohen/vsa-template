using VSA.Architecture.Data;

namespace Architecture.Tests;

public sealed class DataFilesTests {
    [Test]
    public void The_architecture_file_has_the_architecture_name() {
        Assert.That(DataFiles.Architecture("Mvvm"), Is.EqualTo("mvvm.json"));
    }

    [Test]
    public void The_snapshot_file_has_the_app_name_then_the_architecture_name() {
        Assert.That(DataFiles.Snapshot("MyApp", "Mvvm"), Is.EqualTo("myapp.mvvm.json"));
        Assert.That(DataFiles.IsSnapshot("myapp.mvvm.json"), Is.True);
        Assert.That(DataFiles.IsSnapshot("mvvm.json"), Is.False);
    }

    [Test]
    public void A_dot_in_a_name_becomes_a_hyphen() {
        Assert.That(DataFiles.Snapshot("My.App", "Game.Mvvm"), Is.EqualTo("my-app.game-mvvm.json"));
    }

    [Test]
    public void A_name_that_other_tools_use_fails() {
        Assert.That(() => DataFiles.Architecture("Global"),
            Throws.InvalidOperationException.With.Message.Contains("global.json"));
    }
}
