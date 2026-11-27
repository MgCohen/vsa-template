using Tests.Shared;

namespace Engine.Tests;

public sealed class DefinitionFindingTests {
    static readonly string Blocks = Repo.Path("tests", "Fixtures", "Blocks");
    string bin = null!;

    [SetUp]
    public void Create() => bin = TempFolder.Create();

    [TearDown]
    public void Delete() => TempFolder.Delete(bin);

    [Test]
    public async Task A_definition_that_is_not_in_the_bin_gives_UMP030512() {
        CopyTheBinWithout("Blocks.Definition");

        var result = await RunEngineTests.Run(project: Blocks, bin: bin);

        Assert.That(result.Lines, Has.Some.Contains("UMP030512"), string.Join(Environment.NewLine, result.Lines));
    }

    [Test]
    public async Task An_ignored_definition_folder_gives_no_UMP030512() {
        CopyTheBinWithout("Blocks.Definition");

        var result = await RunEngineTests.Run(project: Blocks, bin: bin, ignoredFolders: "Definition");

        Assert.That(result.Lines, Has.None.Contains("UMP030512").And.None.Contains("UMP030308"),
            string.Join(Environment.NewLine, result.Lines));
    }

    [Test]
    public async Task A_definition_that_no_build_loads_gives_the_warning_UMP030513() {
        var folder = Path.Combine(Blocks, "App", "Packs", "Alpha", "Definition");
        CopyTheBinWithout("Alpha.Definition");

        var result = await RunEngineTests.Run(project: Blocks, bin: bin);

        Assert.That(result.Lines, Has.One.StartsWith(folder).And.Contains("warning UMP030513"),
            string.Join(Environment.NewLine, result.Lines));
    }

    [Test]
    public async Task An_ignored_definition_that_no_build_loads_gives_no_UMP030513() {
        CopyTheBinWithout("Alpha.Definition");

        var result = await RunEngineTests.Run(project: Blocks, bin: bin, ignoredFolders: "App/Packs/Alpha/Definition");

        Assert.That(result.Lines, Has.None.Contains("UMP030513"), string.Join(Environment.NewLine, result.Lines));
    }

    [Test]
    public async Task A_definition_that_the_build_loads_gives_no_UMP030513() {
        var result = await RunEngineTests.Run(project: Blocks);

        Assert.That(result.Lines, Has.None.Contains("UMP030513"), string.Join(Environment.NewLine, result.Lines));
    }

    void CopyTheBinWithout(string assembly) {
        foreach (var file in Directory.GetFiles(AppContext.BaseDirectory)
            .Where(file => !Path.GetFileName(file).StartsWith($"{assembly}.", StringComparison.Ordinal))) {
            File.Copy(file, Path.Combine(bin, Path.GetFileName(file)));
        }
    }
}
