using Changes.Infrastructure;
using Tests.Shared;

namespace Engine.Tests;

public sealed class BlockStampTests {
    const string Definition = "Base: Probe|C:/probe";
    static readonly string pack = Repo.Path("packs", "Vsa");

    [Test]
    public async Task A_run_at_a_lower_level_skips_a_block_that_a_top_level_run_checked() {
        Delete(Repo.Path("engine"));
        Delete(pack);

        var top = await RunEngineTests.Run(project: Repo.Path("engine"));
        var lower = await RunEngineTests.Run(project: pack);

        Assert.That(top.Lines, Has.None.Contains(": error "), string.Join(Environment.NewLine, top.Lines));
        Assert.That(Stamps(pack), Is.Not.Empty);
        Assert.That(lower.Lines, Has.One.Contains("UMP030202").And.Contains(pack));
    }

    [Test]
    public async Task A_block_with_an_error_gets_no_stamp() {
        var app = Repo.Path("tests", "Fixtures", "TwoApps");
        Delete(app);

        var result = await RunEngineTests.Run(block: app);

        Assert.That(result.ExitCode, Is.EqualTo(1));
        Assert.That(Stamps(app), Is.Empty);
    }

    [Test]
    public void The_block_hash_reads_the_content_of_a_dll_and_not_its_bin() {
        var root = TempFolder.Create();
        try {
            var first = Dll(root, "first", "same");
            var second = Dll(root, "second", "same");
            var other = Dll(root, "other", "different");

            var hash = BlockHash.Inputs(root, Definition, [first], "run");

            Assert.That(BlockHash.Inputs(root, Definition, [second], "run"), Is.EqualTo(hash));
            Assert.That(BlockHash.Inputs(root, Definition, [other], "run"), Is.Not.EqualTo(hash));
        } finally {
            TempFolder.Delete(root);
        }
    }

    static string Dll(string root, string folder, string content) {
        var file = Path.Combine(Directory.CreateDirectory(Path.Combine(root, folder)).FullName, "Probe.dll");
        File.WriteAllText(file, content);
        return file;
    }

    static string[] Stamps(string block) {
        var folder = Path.GetDirectoryName(BlockStamp.In(block, "view"))!;
        return Directory.Exists(folder) ? Directory.GetFiles(folder, "*" + BlockStamp.Extension) : [];
    }

    static void Delete(string block) {
        var folder = Path.GetDirectoryName(BlockStamp.In(block, "view"))!;
        if (Directory.Exists(folder)) {
            Directory.Delete(folder, recursive: true);
        }
    }
}
