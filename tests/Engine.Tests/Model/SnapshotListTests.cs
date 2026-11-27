namespace CheckModel.Tests;

using Operations.Infrastructure;
using Umpire.CheckModel;
using Umpire.CheckModel.Export;

public sealed class SnapshotListTests {
    [Test]
    public void The_list_reads_each_line_and_each_part_of_a_line_one_time() {
        using var temp = new TempState();
        var a = Path.Combine(temp.Block, "a.json");
        var b = Path.Combine(temp.Block, "b.json");
        var list = Path.Combine(temp.Block, "snapshots.txt");
        File.WriteAllLines(list, [$"A|net8.0|{a};B||{b}", $"A|net8.0|{a}", "not a line"]);

        var places = SnapshotList.Read(list);

        Assert.That(places, Is.EqualTo(new[] { new SnapshotPlace("A", a, "net8.0"), new SnapshotPlace("B", b) }));
    }

    [Test]
    public void A_missing_list_has_no_snapshot() => Assert.That(SnapshotList.Read(Path.Combine(Path.GetTempPath(), "no-such-list.txt")), Is.Empty);

    [TestCase("artifacts~/bin/App/debug", "artifacts~/obj/App/debug/umpire")]
    [TestCase("App/bin/Debug/net10.0", "App/obj/Debug/net10.0/umpire")]
    public void The_own_folder_of_a_bin_is_the_umpire_folder_of_its_intermediate_output(string bin, string own) {
        var root = Path.GetTempPath();

        Assert.That(RunOptions.OwnFolderOf(Path.Combine(root, bin)), Is.EqualTo(Path.GetFullPath(Path.Combine(root, own))));
    }

    [Test]
    public void A_folder_with_no_bin_part_has_no_own_folder() =>
        Assert.That(RunOptions.OwnFolderOf(Path.Combine(Path.GetTempPath(), "output")), Is.Null);
}
