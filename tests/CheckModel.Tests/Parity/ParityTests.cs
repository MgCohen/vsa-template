using Tests.Shared;

namespace CheckModel.Tests;

using Umpire.Vsa.Definition;
using Data = Umpire.CheckModel;

[NonParallelizable]
public sealed class ParityTests {
    static readonly string configuration =
        Path.GetFileName(AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

    [OneTimeTearDown]
    public void Clear() => ModelRun.Clear();

    [TestCase("GoodApp", 0)]
    [TestCase("BadApp", 19)]
    [TestCase("VsaApp", 20)]
    public void An_architecture_case_gives_the_findings_of_the_engine(string app, int findings) =>
        Same(EngineRun.Of(DefinitionOf(app), Bin("Architecture", app), Repo.Path("tests", "Fixtures", "Architecture", app)), findings);

    [TestCase("Notes", "", "Notes")]
    [TestCase("ShellApp", "", "ShellApp")]
    [TestCase("PackVariant", "", "PackVariant")]
    [TestCase("PackVariant", "App/Packs/Ecs", "PackVariant")]
    [TestCase("Blocks", "", "Blocks")]
    [TestCase("Spike", "Alpha", "Spike.Alpha")]
    [TestCase("Spike", "Bravo", "Spike.Bravo")]
    [TestCase("Spike", "Charlie", "Spike.Charlie")]
    public void A_fixture_block_gives_the_findings_of_the_engine(string fixture, string block, string project) =>
        Same(EngineRun.Of(Bin(fixture, project), Repo.Path("tests", "Fixtures", fixture, block)), 0);

    [TestCase("engine")]
    [TestCase("packs", "Vsa")]
    [TestCase("framework", "Kernel")]
    [TestCase("architecture")]
    public void A_block_of_the_repository_gives_the_findings_of_the_engine(params string[] block) =>
        Same(EngineRun.Of(Repo.Path("engine", "artifacts~", "bin", "Engine", configuration), Repo.Path(block)), 0);

    static void Same(EngineRun engine, int findings) {
        var facts = new FactParity(engine, Data.AppModel.Load(ModelRun.StateOf(engine)));
        var report = Data.CheckJob.Run(ModelRun.StateOf(engine));
        var expected = engine.Check.Findings().Select(f => $"{f.File} {f.Code}: {f.Message}").Order().ToList();
        var actual = report.Findings.Select(f => $"{f.File} {f.Code}: {f.Text}").Order().ToList();
        Assert.Multiple(() => {
            Assert.That(report.Reason, Is.Null);
            Assert.That(report.Metadata["types"], Is.Not.EqualTo("0"));
            Assert.That(expected, Has.Count.EqualTo(findings));
            Assert.That(expected.Except(actual), Is.Empty, "only in the engine");
            Assert.That(actual.Except(expected), Is.Empty, "only in the check model");
            Assert.That(actual, Has.Count.EqualTo(findings));
            Assert.That(facts.Differences(), Is.Empty, "facts");
        });
    }

    static Umpire.Architecture.Architecture DefinitionOf(string app) => app switch {
        "GoodApp" => new EngineCore.Tests.GoodArchitecture(),
        "BadApp" => new EngineCore.Tests.BadArchitecture(),
        _ => new VsaArchitecture(),
    };

    static string Bin(string fixture, string project) =>
        Repo.Path("tests", "Fixtures", fixture, "artifacts~", "bin", project, configuration);
}
