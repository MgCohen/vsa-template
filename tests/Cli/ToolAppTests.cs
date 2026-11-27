using Tests.Shared;
using Umpire.Cli;
using Umpire.Cli.Commands;
using Umpire.Cli.Host;
using Umpire.Kernels;

namespace Cli.Tests;

public sealed class ToolAppTests {
    static readonly string ToolFile = Path.Combine(AppContext.BaseDirectory, "Umpire.Cli.dll");

    static IEnumerable<string[]> HelpForms() => [[], ["help"], ["-h"], ["--help"]];

    static IReadOnlyList<Operation> Operations() =>
        [.. CommandList.All.Select(ToolOperations.Of), ToolOperations.Help, ToolOperations.Version];

    [Test]
    public void TheVersionIsTheVersionOfTheTool() {
        var result = Run(Repo.Root, "--version");

        Assert.That(result.ExitCode, Is.Zero, result.Output);
        Assert.That(result.Output.Trim(), Is.EqualTo(ToolVersion.Current));
    }

    [TestCaseSource(nameof(HelpForms))]
    public void EachHelpFormShowsTheUsage(string[] args) {
        var result = Run(Repo.Root, args);

        Assert.That(result.ExitCode, Is.Zero, result.Output);
        Assert.That(result.Output, Does.StartWith("Usage:").And.Contain("umpire check"));
    }

    [Test]
    public void AnUnknownCommandGivesTheUsageAndExitCodeOne() {
        var result = Run(Repo.Root, "add", "thing", "X");

        Assert.That(result.ExitCode, Is.EqualTo(1));
        Assert.That(result.Output, Does.StartWith("umpire: Unknown command: add thing X").And.Contain("Usage:"));
    }

    [Test]
    public void AnErrorOfTheCommandLineIsOneLineWithTheToolName() {
        var result = Run(Repo.Root, "add", "module");

        Assert.That(result.ExitCode, Is.EqualTo(1));
        Assert.That(result.Output.Trim(), Is.EqualTo("umpire: Give the name: umpire add module <Name>"));
    }

    [Test]
    public void AnErrorOfACommandIsOneLineWithTheToolName() {
        var folder = TempFolder.Create();
        try {
            var result = Run(folder, "doctor");

            Assert.That(result.ExitCode, Is.EqualTo(1));
            Assert.That(result.Output, Does.StartWith("umpire: This folder is not in an Umpire app."));
        } finally {
            TempFolder.Delete(folder);
        }
    }

    [TestCase(new[] { "new", "app", "Hello", "--source", "a" }, new[] { "new", "app" })]
    [TestCase(new[] { "add", "request", "X", "-m", "Billing" }, new[] { "add", "request" })]
    [TestCase(new[] { "update", "--version", "1.0.0" }, new[] { "update" })]
    [TestCase(new[] { "check", "-c", "Release" }, new[] { "check" })]
    [TestCase(new[] { "--version" }, new[] { "version" })]
    [TestCase(new[] { "add", "thing", "X" }, new[] { "help" })]
    [TestCase(new[] { "-h" }, new[] { "help" })]
    public void TheHostGivesTheWordsOfOneOperation(string[] args, string[] words) =>
        Assert.That(new ToolLine(args).Words, Is.EqualTo(words));

    [Test]
    public void TheWordsOfEachCommandSelectOnlyItsOwnOperation() {
        var operations = Operations();

        foreach (var command in CommandList.All) {
            var words = command.Words;
            var selected = operations.Where(o => words.Count == 1
                ? o.Name == words[0]
                : o.Group == words[0] && o.Name == words[1]);
            Assert.That(selected.Count(), Is.EqualTo(1), string.Join(' ', words));
        }
    }

    static DotnetResult Run(string folder, params string[] args) => Dotnet.Run(folder, null, [ToolFile, .. args]);
}
