using Facts.Domain.Shared;
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

    [Test]
    public void A_change_in_the_dll_of_the_definition_or_a_shared_dll_changes_the_block_hash() {
        var root = TempFolder.Create();
        try {
            var block = Directory.CreateDirectory(Path.Combine(root, "Block")).FullName;
            var own = new BinDll(Dll(root, "Block", "own"), "Probe", false, [], [Path.Combine(block, "Own.cs")], [], [], true);
            var definition = Named(Dll(root, "Kernel", "v1"), "Definitions", isShared: false);
            var shared = Named(Dll(root, "Shared", "v1"), "Api", isShared: true);
            var entry = new BlockEntry(block, EntryForm.Metadata, new TypeName("Definitions.Probe", "Definitions"), block);
            var dlls = BlockDlls.Of(entry, new BinIndex(root, [own, definition, shared], [entry]));
            var before = BlockHash.Inputs(block, Definition, dlls, "run");

            File.WriteAllText(definition.Path, "v2");
            var afterDefinition = BlockHash.Inputs(block, Definition, dlls, "run");
            File.WriteAllText(shared.Path, "v2");
            var afterShared = BlockHash.Inputs(block, Definition, dlls, "run");

            Assert.That(dlls, Is.EquivalentTo(new[] { own.Path, definition.Path, shared.Path }));
            Assert.That(new[] { before, afterDefinition, afterShared }, Is.Unique);
        } finally {
            TempFolder.Delete(root);
        }
    }

    [Test]
    public void The_dlls_of_an_app_block_have_the_kernel_definition_and_its_references() {
        var block = Path.GetFullPath("C:/a");
        var own = new BinDll("C:/a/A.dll", "A", false, [], [Path.Combine(block, "A.cs")], [], [], true);
        var definition = new BinDll("C:/bin/D.dll", "D", false, ["Base"], [], [], []) {
            Definitions = [new DefinitionType(new("D.Def", "D"), null, new("K.Kernel", "K"), [])],
        };
        var bases = new BinDll("C:/bin/Base.dll", "Base", false, [], [], [], []);
        var bin = new BinIndex("C:/bin", [own, definition, bases], []);

        var app = BlockDlls.Of(new BlockEntry(block, EntryForm.App, null, block), bin);
        var library = BlockDlls.Of(new BlockEntry(block, EntryForm.Metadata, null, block), bin);

        Assert.That(app, Is.EquivalentTo(new[] { own.Path, definition.Path, bases.Path }));
        Assert.That(library, Is.EqualTo(new[] { own.Path }));
    }

    [Test]
    public void A_stamp_that_cannot_be_read_is_no_stamp() {
        var root = TempFolder.Create();
        try {
            var file = BlockStamp.Write(BlockStamp.In(root, "view"), "hash");
            using var locked = new FileStream(file, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

            Assert.That(BlockStamp.Read(file), Is.Null);
        } finally {
            TempFolder.Delete(root);
        }
    }

    static BinDll Named(string path, string name, bool isShared) => new(path, name, isShared, [], [], [], [], true);

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
