using Tests.Shared;
using VSA.Cli;
using VSA.Cli.Commands;
using VSA.Cli.Commands.Bases;

namespace Cli.Tests;

public sealed class CommandTests {
    static readonly Type[] Bases = [typeof(ToolCommand), typeof(AppCommand), typeof(AddCommand)];

    [Test]
    public void EachCommandInheritsOneBaseAndIsInTheList() {
        var commands = typeof(ToolCommand).Assembly.GetTypes()
            .Where(t => t.Namespace == "VSA.Cli.Commands" && t.Name.EndsWith("Command", StringComparison.Ordinal))
            .ToList();

        Assert.That(commands, Is.Not.Empty);
        Assert.That(commands, Has.All.Matches<Type>(t => t.IsSubclassOf(typeof(ToolCommand)) && t.IsSealed));
        Assert.That(commands.Select(t => t.BaseType), Has.All.Matches<Type>(b => Bases.Contains(b)));
        Assert.That(CommandList.All.Select(c => c.GetType()).Distinct(), Is.EquivalentTo(commands));
    }

    [Test]
    public void EachCommandHasItsOwnWords() =>
        Assert.That(CommandList.All.Select(c => string.Join(' ', c.Words)), Is.Unique);

    [Test]
    public void TheUsageListsEachCommand() {
        var usage = CommandList.Usage;

        foreach (var command in CommandList.All) {
            Assert.That(usage, Does.Contain(command.Usage).And.Contain(command.Summary));
        }
        Assert.That(usage, Does.Contain("vsa --version"));
    }

    [TestCase("add", "request", "X", "-m", "A", "-m", "B")]
    [TestCase("update", "--version", "1.0.0", "--version", "2.0.0")]
    public void AnOptionWithOneValueRejectsTwo(params string[] args) =>
        Assert.That(() => CommandList.Run(args), Throws.TypeOf<VsaException>().With.Message.Contain("one time only"));

    [Test]
    public void ARequiredOptionIsChecked() =>
        Assert.That(() => RunInAnApp(["add", "command", "ChangeLimit"]),
            Throws.TypeOf<VsaException>().With.Message.Contain("Give the module: vsa add command <Name> -m <Module>"));

    [Test]
    public void ACommandThatTakesANameStopsWithoutIt() =>
        Assert.That(() => CommandList.Run(["add", "module"]),
            Throws.TypeOf<VsaException>().With.Message.Contain("Give the name: vsa add module <Name>"));

    [TestCase("add", "module", "Billing", "--force", "true")]
    [TestCase("doctor", "--version")]
    public void AnUnknownOptionStops(params string[] args) =>
        Assert.That(() => CommandList.Run(args), Throws.TypeOf<VsaException>().With.Message.Contain("Unknown option"));

    [Test]
    public void AnUnknownCommandShowsTheUsage() =>
        Assert.That(() => CommandList.Run(["add", "thing", "X"]),
            Throws.TypeOf<VsaException>().With.Message.Contain("Unknown command").And.Message.Contain("Usage:"));

    [Test]
    public void ASourceCanBeGivenManyTimes() {
        var input = CommandInput.Parse(new NewAppCommand(), ["Hello", "--source", "a", "--source", "b"]);

        Assert.That(input.Name, Is.EqualTo("Hello"));
        Assert.That(input.All(new NewAppCommand().Options[0]), Is.EqualTo(new[] { "a", "b" }));
    }

    [TestCase("add", "module", "bad")]
    [TestCase("add", "request", "bad")]
    [TestCase("add", "command", "bad", "-m", "Billing")]
    public void AnInvalidNameStopsBeforeTheAppRootAndTheOptions(params string[] args) =>
        Assert.That(() => RunOutsideAnApp(args), Throws.TypeOf<VsaException>().With.Message.Contain("is not valid"));

    [Test]
    public void AValidNameOutsideAnAppStopsAtTheAppRoot() =>
        Assert.That(() => RunOutsideAnApp(["add", "request", "CreateInvoice"]),
            Throws.TypeOf<VsaException>().With.Message.Contain("not in a VSA app"));

    static int RunOutsideAnApp(string[] args) => RunInFolder(args, isApp: false);

    static int RunInAnApp(string[] args) => RunInFolder(args, isApp: true);

    static int RunInFolder(string[] args, bool isApp) {
        var folder = TempFolder.Create();
        var current = Environment.CurrentDirectory;
        try {
            if (isApp) {
                File.WriteAllText(Path.Combine(folder, "global.json"), $$"""{ "msbuild-sdks": { "VSA.Sdk": "{{ToolVersion.Current}}" } }""");
            }
            Environment.CurrentDirectory = folder;
            return CommandList.Run(args);
        } finally {
            Environment.CurrentDirectory = current;
            TempFolder.Delete(folder);
        }
    }
}
