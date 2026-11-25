using Tests.Shared;

namespace CheckModel.Tests;

using Umpire.Architecture;
using Umpire.Vsa.Definition;
using Data = Umpire.CheckModel;

[NonParallelizable]
public sealed class ParityTests {
    static readonly string configuration =
        Path.GetFileName(AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

    [TestCase("GoodApp", 0)]
    [TestCase("BadApp", 19)]
    [TestCase("VsaApp", 20)]
    public void An_architecture_case_gives_the_findings_of_the_engine(string app, int findings) =>
        Same(EngineRun.Of(DefinitionOf(app), Bin("Architecture", app), Fixture("Architecture", app)), findings);

    [Test]
    public void An_app_with_an_ignored_folder_gives_the_findings_of_the_engine() =>
        Same(EngineRun.Of(
            DefinitionOf("BadApp"), Bin("Architecture", "BadApp"), Fixture("Architecture", "BadApp"), ["Features/Notes"]), 16);

    [Test]
    public void An_app_with_a_folder_alias_gives_the_findings_of_the_engine() =>
        Same(EngineRun.Of(
            DefinitionOf("BadApp"), Bin("Architecture", "BadApp"), Fixture("Architecture", ""), [], [new FolderAlias("BadApp", "")]), 19);

    [TestCase("Notes", "", "Notes")]
    [TestCase("ShellApp", "", "ShellApp")]
    [TestCase("PackVariant", "", "PackVariant")]
    [TestCase("PackVariant", "App/Packs/Ecs", "PackVariant")]
    [TestCase("Blocks", "", "Blocks")]
    [TestCase("Spike", "Alpha", "Spike.Alpha")]
    [TestCase("Spike", "Bravo", "Spike.Bravo")]
    [TestCase("Spike", "Charlie", "Spike.Charlie")]
    public void A_fixture_block_gives_the_findings_of_the_engine(string fixture, string block, string project) =>
        Same(EngineRun.Of(Bin(fixture, project), Fixture(fixture, block)), 0);

    [TestCase("engine")]
    [TestCase("packs", "Vsa")]
    [TestCase("framework", "Kernel")]
    [TestCase("architecture")]
    public void A_block_of_the_repository_gives_the_findings_of_the_engine(params string[] block) =>
        Same(EngineRun.Of(EngineBin(), Repo.Path(block)), 0);

    [Test]
    public void An_inner_block_with_its_own_state_gives_the_findings_of_the_engine() =>
        Same(EngineRun.Of(EngineBin(), Repo.Path("engine")), 0, "Umpire.Engine.Versions");

    static void Same(EngineRun engine, int findings, params string[] otherProjects) {
        using var run = new ModelRun(engine, otherProjects);
        var facts = new FactParity(engine, Data.AppModel.Load(run.State));
        var report = Data.CheckJob.Run(run.State);
        var expected = engine.Check.Findings().Select(f => $"{f.File} {f.Severity} {f.Code}: {f.Message}").Order().ToList();
        var actual = report.Findings.Select(f => $"{f.File} {f.Severity} {f.Code}: {f.Text}").Order().ToList();
        Assert.Multiple(() => {
            Assert.That(report.Reason, Is.Null);
            Assert.That(report.Metadata["types"], Is.Not.EqualTo("0"));
            Assert.That(expected, Has.Count.EqualTo(findings), string.Join(Environment.NewLine, expected));
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(run.Diagnostics.Select(d => d.ToString()).Order(), Is.EqualTo(report.Findings.Select(f => Expected(f, engine).ToString()).Order()),
                "analyzer");
            Assert.That(facts.Differences(), Is.Empty, "facts");
        });
    }

    static Data.Finding Expected(Data.Finding finding, EngineRun engine) =>
        finding.Code == Data.RuleCodes.Count && engine.App.Count > 1 ? finding with { Severity = Data.Severity.Warning } : finding;

    static Architecture DefinitionOf(string app) => app switch {
        "GoodApp" => new EngineCore.Tests.GoodArchitecture(),
        "BadApp" => new EngineCore.Tests.BadArchitecture(),
        _ => new VsaArchitecture(),
    };

    static string Fixture(string fixture, string block) =>
        Path.TrimEndingDirectorySeparator(Repo.Path("tests", "Fixtures", fixture, block));

    static string EngineBin() => Repo.Path("engine", "artifacts~", "bin", "Engine", configuration);

    static string Bin(string fixture, string project) =>
        Repo.Path("tests", "Fixtures", fixture, "artifacts~", "bin", project, configuration);
}
