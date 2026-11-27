using Blocks.Slices.FindBlocks;
using Facts.Infrastructure;
using Changes.Infrastructure;
using Tests.Shared;

namespace Engine.Tests;

[NonParallelizable]
public sealed class SpikeRunTests {
    static readonly string spike = Repo.Path("tests", "Fixtures", "Spike");
    static readonly string alpha = Path.Combine(spike, "Alpha");
    static readonly string bravo = Path.Combine(spike, "Bravo");
    static readonly string charlie = Path.Combine(spike, "Charlie");
    static readonly string[] blocks = [alpha, bravo, charlie];

    [Test]
    public async Task One_run_at_Charlie_checks_each_spike_block_and_writes_one_stamp_for_each() {
        DeleteStamps();

        var result = await RunEngineTests.Run(project: charlie, bin: Bin("Spike.Charlie"));

        Assert.That(result.Lines, Has.None.Contains(": error ").And.None.Contains(": warning "),
            string.Join(Environment.NewLine, result.Lines));
        Assert.That(blocks.Select(b => Stamps(b).Length), Is.EqualTo(new[] { 1, 1, 1 }));
    }

    [Test]
    public void Charlie_uses_Alpha_and_Bravo_one_time_each() {
        using var reader = new BinReader(Bin("Spike.Charlie"));
        var index = reader.Index();

        var used = UsedBlocks.Of(charlie, [], index.Entries, index);

        Assert.That(used, Is.Unique);
        Assert.That(used.Where(blocks.Contains), Is.EquivalentTo(new[] { alpha, bravo }));
    }

    [Test]
    public async Task A_run_at_Alpha_after_the_run_at_Charlie_skips_Alpha_and_Bravo() {
        DeleteStamps();
        await RunEngineTests.Run(project: charlie, bin: Bin("Spike.Charlie"));

        var lower = await RunEngineTests.Run(project: alpha, bin: Bin("Spike.Alpha"));

        Assert.That(lower.Lines.Where(l => l.Contains("UMP030202")),
            Has.One.Contains(alpha + " ").And.One.Contains(bravo + " "), string.Join(Environment.NewLine, lower.Lines));
    }

    static string Bin(string project) => Path.Combine(spike, "artifacts~", "bin", project, Configuration());

    static string Configuration() =>
        Path.GetFileName(AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

    static string[] Stamps(string block) {
        var folder = Path.GetDirectoryName(BlockStamp.In(block, "view"))!;
        return Directory.Exists(folder) ? Directory.GetFiles(folder, "*" + BlockStamp.Extension) : [];
    }

    static void DeleteStamps() {
        foreach (var block in blocks) {
            var folder = Path.GetDirectoryName(BlockStamp.In(block, "view"))!;
            if (Directory.Exists(folder)) {
                Directory.Delete(folder, recursive: true);
            }
        }
    }
}
