namespace CheckModel.Tests;

using Umpire.CheckModel;

public sealed class StateFolderTests {
    [Test]
    public void The_default_folder_is_in_the_intermediate_output_of_the_block() {
        using var temp = new TempState();

        Assert.That(temp.State.Folder, Is.EqualTo(Path.Combine(temp.Block, "artifacts~", "umpire")));
    }

    [Test]
    public void One_setting_moves_all_saved_files() {
        using var temp = new TempState();
        var state = StateFolder.Of(temp.Block, "Library/Umpire");

        state.Save(Samples.Layers);
        state.Save(Samples.Settings());
        state.Save(Snapshot.Of("Sample", []));
        state.SaveLibrary(Snapshot.Of("Other", []));

        var files = Directory.GetFiles(temp.Block, "*", SearchOption.AllDirectories);
        Assert.That(files, Has.Length.EqualTo(4));
        Assert.That(files, Has.All.StartsWith(Path.Combine(temp.Block, "Library", "Umpire")));
    }

    [Test]
    public void A_snapshot_with_the_same_facts_is_not_written_again() {
        using var temp = new TempState();
        var first = Snapshot.Of("Sample", [Samples.File("Ui/Screen.cs", Samples.Type("Screen"))]);

        Assert.Multiple(() => {
            Assert.That(temp.State.Save(first), Is.True);
            Assert.That(temp.State.Save(first with { MadeAt = "later" }), Is.False);
            Assert.That(temp.State.Save(first with { Files = [] }), Is.True);
        });
    }

    [Test]
    public void The_state_folder_is_found_from_a_folder_below_the_block() {
        using var temp = new TempState();
        temp.State.Save(Samples.Layers);
        temp.State.Save(Samples.Settings());

        var found = StateFolder.Above(Path.Combine(temp.Block, "App", "Features"));

        Assert.That(found?.Folder, Is.EqualTo(temp.State.Folder));
    }

    [Test]
    public void Many_writers_of_one_snapshot_leave_one_valid_file() {
        using var temp = new TempState();

        Parallel.For(0, 8, i => temp.State.Save(Snapshot.Of("Sample", [Samples.File($"Ui/Screen{i}.cs")])));

        var file = temp.State.SnapshotFile("Sample");
        Assert.That(Directory.GetFiles(Path.GetDirectoryName(file)!), Has.Length.EqualTo(1));
        Assert.That(Snapshot.Read(File.ReadAllText(file)).Files, Has.Count.EqualTo(1));
    }
}
