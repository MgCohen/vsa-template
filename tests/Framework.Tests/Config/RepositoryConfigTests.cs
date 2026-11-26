using Tests.Shared;

namespace Framework.Tests.Config;

using System.Text.RegularExpressions;
using Umpire.Config;

public sealed class RepositoryConfigTests {
    const string OutputFolder = "artifacts~";

    static readonly Dictionary<string, Dictionary<string, string>> expected = new() {
        ["engine/.umpireconfig"] = new() { ["umpire.check.mode"] = "Error" },
        ["tests/Fixtures/Blocks/App/Tools/.umpireconfig"] = new() {
            ["umpire.architecture"] = "Umpire.Vsa.Definition.VsaArchitecture, Umpire.Vsa",
        },
        ["tests/Fixtures/Built/OneLine/.umpireconfig"] = new() { ["umpire.UMP010101.max"] = "60" },
        ["tests/Fixtures/Built/Shop/.umpireconfig"] = [],
        ["tests/Fixtures/Notes/.umpireconfig"] = [],
        ["tools/cli/Templates/.umpireconfig"] = [],
    };

    static readonly Dictionary<string, Dictionary<string, string>> documentBlocks = new() {
        ["docs/architecture-rules.md"] = new() {
            ["umpire.UMP010201.severity"] = "error",
            ["umpire.check_responses"] = "false",
            ["umpire.UMP010201.check_responses"] = "true",
        },
        ["docs/composition.md"] = new() {
            ["Notebook.MaxNotes"] = "50",
            ["Notebook.Limits.MaxLines"] = "80",
            ["Notebook.Modes.easy"] = "1",
            ["Notebook.Modes.hard"] = "5",
        },
        ["docs/getting-started.md"] = new() {
            ["umpire.UMP010101.max"] = "140",
            ["umpire.UMP010104.severity"] = "none",
        },
        ["docs/architecture-engine.md"] = new() { ["umpire.UMP020101.severity"] = "warning" },
    };

    [Test]
    public void Each_umpireconfig_file_of_the_repository_has_its_expected_values() =>
        Assert.That(Files(), Is.EquivalentTo(expected.Keys));

    [TestCaseSource(nameof(Files))]
    public void The_reader_gives_the_values_of_the_file(string file) =>
        Assert.That(UmpireConfig.In(Path.GetDirectoryName(Repo.Path(file))).Values()
            .ToDictionary(pair => pair.Key, pair => pair.Value), Is.EqualTo(expected[file]));

    [TestCase("docs/architecture-rules.md")]
    [TestCase("docs/composition.md")]
    [TestCase("docs/getting-started.md")]
    public void The_reader_gives_the_values_of_the_toml_block_of_a_document(string document) =>
        Assert.That(Blocks(document).Single(), Is.EqualTo(documentBlocks[document]));

    [Test]
    public void The_reader_gives_the_values_of_each_toml_block_of_the_engine_document() {
        var blocks = Blocks("docs/architecture-engine.md");

        Assert.That(blocks[0], Is.EqualTo(new Dictionary<string, string>()));
        Assert.That(UmpireConfig.Parse(BlockTexts("docs/architecture-engine.md")[0]).Lists("umpire.check.folder_aliases")
            .ToDictionary(pair => pair.Key, pair => pair.Value),
            Is.EqualTo(new Dictionary<string, IReadOnlyList<string>?> { ["src/Game"] = [""], ["Source"] = [""] }));
        Assert.That(blocks[1], Is.EqualTo(documentBlocks["docs/architecture-engine.md"]));
    }

    static IEnumerable<string> Files() =>
        Directory.EnumerateFiles(Repo.Root, UmpireConfig.FileName, SearchOption.AllDirectories)
            .Select(file => Path.GetRelativePath(Repo.Root, file).Replace('\\', '/'))
            .Where(file => !file.Split('/').Contains(OutputFolder) && !file.StartsWith(".", StringComparison.Ordinal))
            .Order(StringComparer.Ordinal);

    static List<Dictionary<string, string>> Blocks(string document) =>
        [.. BlockTexts(document).Select(text => UmpireConfig.Parse(text).Values().ToDictionary(pair => pair.Key, pair => pair.Value))];

    static List<string> BlockTexts(string document) =>
        [.. Regex.Matches(File.ReadAllText(Repo.Path(document)), "```toml\\r?\\n(.*?)```", RegexOptions.Singleline)
            .Select(match => match.Groups[1].Value)];
}
