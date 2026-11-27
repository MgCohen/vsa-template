using Tests.Shared;
using Umpire.Cli.Plugins.Commands.Domain.Shared;

namespace Cli.Tests;

public sealed class ToolAppTests {
    static readonly string ToolFile = Path.Combine(AppContext.BaseDirectory, "Umpire.Cli.dll");

    static IEnumerable<string[]> HelpForms() => [[], ["help"], ["-h"], ["--help"]];

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

    [TestCase("version")]
    [TestCase("help", "me")]
    public void AWordThatIsNotACommandIsUnknown(params string[] args) {
        var result = Run(Repo.Root, args);

        Assert.That(result.ExitCode, Is.EqualTo(1));
        Assert.That(result.Output, Does.StartWith($"umpire: Unknown command: {string.Join(' ', args)}"));
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
    [TestCase(new[] { "version" }, new[] { "help" })]
    [TestCase(new[] { "add", "thing", "X" }, new[] { "help" })]
    [TestCase(new[] { "-h" }, new[] { "help" })]
    public void TheHostSelectsTheRequestOfTheCommandLine(string[] args, string[] words) =>
        Assert.That(Tool.Commands.Select(args).Words, Is.EqualTo(words));

    [Test]
    public void EachCommandHasItsOwnOperationName() =>
        Assert.That(Tool.Commands.Forms.Select(form => $"{form.Group} {form.Name}"), Is.Unique);

    [TestCase(new[] { "test" }, true)]
    [TestCase(new[] { "check", "--mode", "Error" }, true)]
    [TestCase(new[] { "doctor" }, false)]
    [TestCase(new[] { "new", "app", "Hello" }, false)]
    [TestCase(new[] { "add", "thing" }, false)]
    public void OnlyTheCommandsWithAChildProcessWaitForItAfterAStopSignal(string[] args, bool waits) =>
        Assert.That(Tool.Commands.CancelsOnSignal(args), Is.EqualTo(waits));

    static DotnetResult Run(string folder, params string[] args) => Dotnet.Run(folder, null, [ToolFile, .. args]);
}
