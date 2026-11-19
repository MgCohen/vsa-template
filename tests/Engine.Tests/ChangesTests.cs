using Changes.Infrastructure;
using Tests.Shared;

namespace Engine.Tests;

[NonParallelizable]
public sealed class ChangesTests {
    static readonly string Notes = Repo.Path("tests", "Fixtures", "Notes");
    static readonly string TwoApps = Repo.Path("tests", "Fixtures", "TwoApps");
    static readonly string Bin = AppContext.BaseDirectory;

    [TearDown]
    public void Delete() => StampFile.Delete(Bin);

    [Test]
    public async Task A_check_with_no_finding_writes_the_stamp() {
        var result = await RunEngineTests.Run(project: Notes);

        Assert.That(result.ExitCode, Is.Zero, string.Join("\n", result.Lines));
        Assert.That(StampFile.Read(Bin), Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public async Task A_check_with_a_finding_writes_no_stamp() {
        var result = await RunEngineTests.Run(block: TwoApps);

        Assert.That(result.ExitCode, Is.EqualTo(1), string.Join("\n", result.Lines));
        Assert.That(StampFile.Read(Bin), Is.Null);
    }

    [Test]
    public async Task A_stamp_with_the_same_inputs_stops_the_check() {
        StampFile.Write(Bin, InputHash.Of(TwoApps, Bin, $"Error||{TwoApps}"));

        var result = await RunEngineTests.Run(block: TwoApps, keepStamp: true);

        Assert.That(result.ExitCode, Is.Zero, string.Join("\n", result.Lines));
        Assert.That(result.Lines, Is.Empty);
    }

    [Test]
    public void A_change_in_the_umpireconfig_file_changes_the_input_hash() {
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
    public void The_same_inputs_give_the_same_hash() {
        var first = InputHash.Of(Notes, Bin, "run");

        Assert.That(InputHash.Of(Notes, Bin, "run"), Is.EqualTo(first));
    }

    [Test]
    public void Other_run_options_give_another_hash() =>
        Assert.That(InputHash.Of(Notes, Bin, "Warn"), Is.Not.EqualTo(InputHash.Of(Notes, Bin, "Error")));
}
