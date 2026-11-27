using Facts.Infrastructure;
using Changes.Infrastructure;
using Tests.Shared;

namespace Engine.Tests;

[NonParallelizable]
public sealed class ChangesTests {
    static readonly string Notes = Repo.Path("tests", "Fixtures", "Notes");
    static readonly string TwoApps = Repo.Path("tests", "Fixtures", "TwoApps");
    static readonly string Bin = AppContext.BaseDirectory;
    static readonly string State = RunEngineTests.StateOf(Bin);

    [TearDown]
    public void Delete() {
        if (Directory.Exists(State)) {
            StampFile.Delete(State);
        }
    }

    [Test]
    public async Task A_check_with_no_finding_writes_the_stamp() {
        var result = await RunEngineTests.Run(project: Notes);

        Assert.That(result.ExitCode, Is.Zero, string.Join("\n", result.Lines));
        Assert.That(StampFile.Read(State), Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public async Task A_check_with_a_finding_writes_no_stamp() {
        var result = await RunEngineTests.Run(block: TwoApps);

        Assert.That(result.ExitCode, Is.EqualTo(1), string.Join("\n", result.Lines));
        Assert.That(StampFile.Read(State), Is.Null);
    }

    [Test]
    public async Task A_stamp_with_the_same_inputs_stops_the_check() {
        StampFile.Write(State, InputHash.Of(TwoApps, Bin, $"Error||{TwoApps}||||True"));

        var result = await RunEngineTests.Run(block: TwoApps, keepStamp: true);

        Assert.That(result.ExitCode, Is.Zero, string.Join("\n", result.Lines));
        Assert.That(result.Lines, Is.EqualTo(new[] {
            "umpire-engine: UMP030201: No input changed since the last clean check, so the check did not run.",
        }));
    }

    [Test]
    public void A_dll_in_the_definitions_folder_changes_the_input_hash() {
        var bin = TempFolder.Create();
        try {
            var before = InputHash.Of(Notes, bin, "run");
            var definitions = Directory.CreateDirectory(BinReader.DefinitionsFolder(bin)).FullName;
            File.WriteAllText(Path.Combine(definitions, "A.Definition.dll"), "");

            Assert.That(InputHash.Of(Notes, bin, "run"), Is.Not.EqualTo(before));
        } finally {
            TempFolder.Delete(bin);
        }
    }

    [Test]
    public void A_change_in_the_umpireconfig_file_changes_the_input_hash() {
        using var notes = FixtureLock.Take("Notes");
        var config = Path.Combine(Notes, ".umpireconfig");
        var time = File.GetLastWriteTimeUtc(config);
        var before = InputHash.Of(Notes, Bin, "run");
        try {
            File.SetLastWriteTimeUtc(config, time.AddMinutes(1));

            Assert.That(InputHash.Of(Notes, Bin, "run"), Is.Not.EqualTo(before));
        } finally {
            File.SetLastWriteTimeUtc(config, time);
        }
    }

    [Test]
    public void A_new_DLL_in_a_reference_folder_changes_the_input_hash() {
        var references = TempFolder.Create();
        try {
            var before = InputHash.Of(Notes, Bin, "run", [references]);
            File.WriteAllText(Path.Combine(references, "New.dll"), "");

            Assert.That(InputHash.Of(Notes, Bin, "run", [references]), Is.Not.EqualTo(before));
        } finally {
            TempFolder.Delete(references);
        }
    }

    [Test]
    public void The_input_hash_does_not_read_an_ignored_folder() {
        var block = TempFolder.Create();
        try {
            Directory.CreateDirectory(Path.Combine(block, "Cache"));
            var before = InputHash.Of(block, Bin, "run", ignoredFolders: ["./Cache"]);
            File.WriteAllText(Path.Combine(block, "Cache", "Tool.csproj"), "<Project />");

            Assert.That(InputHash.Of(block, Bin, "run", ignoredFolders: ["./Cache"]), Is.EqualTo(before));
            Assert.That(InputHash.Of(block, Bin, "run"), Is.Not.EqualTo(before));
        } finally {
            TempFolder.Delete(block);
        }
    }

    [Test]
    public void The_same_inputs_give_the_same_hash() {
        var first = InputHash.Of(Notes, Bin, "run");

        Assert.That(InputHash.Of(Notes, Bin, "run"), Is.EqualTo(first));
    }

    [TestCase("Directory.Packages.props")]
    [TestCase("global.json")]
    public void A_change_in_a_version_file_above_the_block_changes_the_input_hash(string name) {
        var root = TempFolder.Create();
        try {
            var block = Path.Combine(root, "App");
            Directory.CreateDirectory(block);
            var file = Path.Combine(root, name);
            File.WriteAllText(file, "<Project />");
            var before = InputHash.Of(block, Bin, "run");

            File.WriteAllText(file, "<Project><ItemGroup /></Project>");

            Assert.That(InputHash.Of(block, Bin, "run"), Is.Not.EqualTo(before));
        } finally {
            TempFolder.Delete(root);
        }
    }

    [TestCase("node_modules")]
    [TestCase(".git")]
    [TestCase("artifacts~")]
    public void A_project_file_in_an_ignored_folder_does_not_change_the_input_hash(string folder) {
        var root = TempFolder.Create();
        try {
            var before = InputHash.Of(root, Bin, "run");
            Directory.CreateDirectory(Path.Combine(root, folder));

            File.WriteAllText(Path.Combine(root, folder, "Other.csproj"), "<Project />");

            Assert.That(InputHash.Of(root, Bin, "run"), Is.EqualTo(before));
        } finally {
            TempFolder.Delete(root);
        }
    }

    [Test]
    public void Other_run_options_give_another_hash() =>
        Assert.That(InputHash.Of(Notes, Bin, "Warn"), Is.Not.EqualTo(InputHash.Of(Notes, Bin, "Error")));
}
