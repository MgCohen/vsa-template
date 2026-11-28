namespace CheckModel.Tests;

using Umpire.CheckModel;

public sealed class StateFileTests {
    [Test]
    public void A_failed_write_leaves_no_temporary_file() {
        using var temp = new TempState();
        var file = temp.State.SnapshotFile("Sample");
        Directory.CreateDirectory(file);

        Assert.That(() => temp.State.Save(Snapshot.Of("Sample", [Samples.File("Ui/Screen.cs")])),
            Throws.InstanceOf<UnauthorizedAccessException>().Or.InstanceOf<IOException>());
        Assert.That(Directory.GetFiles(Path.GetDirectoryName(file)!), Is.Empty);
    }

    [TestCase("definition")]
    [TestCase("block")]
    [TestCase("snapshot")]
    public void A_reader_waits_for_a_writer(string name) {
        using var temp = new TempState();
        temp.State.Save(Samples.Layers);
        temp.Load(Samples.Layers);
        var file = name switch {
            "definition" => temp.State.DefinitionFile,
            "block" => temp.State.BlockFile,
            _ => temp.State.SnapshotFile("Sample"),
        };
        var writer = new FileStream(file, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        File.SetLastWriteTimeUtc(writer.SafeFileHandle, DateTime.UtcNow.AddSeconds(1));
        _ = Task.Delay(200).ContinueWith(_ => writer.Dispose(), TaskScheduler.Default);

        var app = AppModels.Load(temp.State);

        Assert.That(app.Problems, Is.Empty);
    }
}
