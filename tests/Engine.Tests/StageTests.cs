using Report.Domain.Shared;
using Steps.Domain.Shared;
using Tests.Shared;

namespace Engine.Tests;

public sealed class StageTests {
    string root = "";
    string project = "";

    [SetUp]
    public void BrokenProject() {
        root = TempFolder.Create();
        project = Path.Combine(root, "Broken.csproj");
        File.WriteAllText(project, "<Project>");
    }

    [TearDown]
    public void Delete() => TempFolder.Delete(root);

    [Test]
    public async Task A_run_with_all_stages_builds_and_a_build_that_fails_stops_the_check() {
        var result = await RunEngineTests.Run(block: root, project: root, stage: Stage.All);

        Assert.That(result.ExitCode, Is.EqualTo(1));
        Assert.That(result.Lines, Has.One.Contains("error UMP030101: The build of"));
        Assert.That(result.Lines, Has.One.Contains("UMP030002: FindBlocks skipped"));
    }

    [Test]
    public async Task A_build_that_fails_is_an_error_also_in_Warn_mode() {
        var result = await RunEngineTests.Run(block: root, project: root, stage: Stage.All, mode: ReportMode.Warn);

        Assert.That(result.ExitCode, Is.EqualTo(1));
        Assert.That(result.Lines, Has.One.Contains("error UMP030101: The build of"));
    }

    [Test]
    public async Task The_build_stage_runs_only_the_build() {
        var result = await RunEngineTests.Run(block: root, project: root, stage: Stage.Build);

        Assert.That(result.Lines, Has.One.Contains("error UMP030101: The build of"));
        Assert.That(result.Lines, Has.None.Contains("FindBlocks"));
    }

    [Test]
    public async Task The_check_stage_does_not_build_the_project() {
        var result = await RunEngineTests.Run(block: root, project: root, stage: Stage.Check);

        Assert.That(result.Lines, Has.None.Contains("UMP030101"));
    }
}
