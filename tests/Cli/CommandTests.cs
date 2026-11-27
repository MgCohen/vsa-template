using Tests.Shared;
using Umpire.Cli;
using Umpire.Cli.Commands;
using Umpire.Cli.Commands.Bases;

namespace Cli.Tests;

public sealed class CommandTests {
    static readonly Type[] Bases = [typeof(ToolCommand), typeof(AppCommand)];

    [Test]
    public void EachCommandInheritsOneBaseAndIsInTheList() {
        var commands = typeof(ToolCommand).Assembly.GetTypes()
            .Where(t => t.Namespace == "Umpire.Cli.Commands" && t.Name.EndsWith("Command", StringComparison.Ordinal))
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
        Assert.That(usage, Does.Contain("umpire --version"));
    }

    [TestCase("add", "request", "X", "-m", "A", "-m", "B")]
    [TestCase("update", "--version", "1.0.0", "--version", "2.0.0")]
    public void AnOptionWithOneValueRejectsTwo(params string[] args) =>
        Assert.That(() => CommandList.Run(args), Throws.TypeOf<UmpireException>().With.Message.Contain("one time only"));

    [Test]
    public void ARequiredOptionIsChecked() =>
        Assert.That(() => RunInAnApp(["add", "command", "ChangeLimit"]),
            Throws.TypeOf<UmpireException>().With.Message.Contain("Give the module: umpire add command <Name> -m <Module>"));

    [Test]
    public void TestStopsWhenTheAppHasNoTestProject() =>
        Assert.That(() => RunInAnApp(["test"]),
            Throws.TypeOf<UmpireException>().With.Message.Contain("has no test project"));

    [Test]
    public void ACommandThatTakesANameStopsWithoutIt() =>
        Assert.That(() => CommandList.Run(["add", "module"]),
            Throws.TypeOf<UmpireException>().With.Message.Contain("Give the name: umpire add module <Name>"));

    [TestCase("add", "module", "Billing", "--force", "true")]
    [TestCase("doctor", "--version")]
    public void AnUnknownOptionStops(params string[] args) =>
        Assert.That(() => CommandList.Run(args), Throws.TypeOf<UmpireException>().With.Message.Contain("Unknown option"));

    [Test]
    public void AnUnknownCommandShowsTheUsage() =>
        Assert.That(() => CommandList.Run(["add", "thing", "X"]),
            Throws.TypeOf<UmpireException>().With.Message.Contain("Unknown command").And.Message.Contain("Usage:"));

    [Test]
    public void ASourceCanBeGivenManyTimes() {
        var input = CommandInput.Parse(new NewAppCommand(), ["Hello", "--source", "a", "--source", "b"]);

        Assert.That(input.Name, Is.EqualTo("Hello"));
        Assert.That(input.All(new NewAppCommand().Options[0]), Is.EqualTo(new[] { "a", "b" }));
    }

    [TestCase("add", "module", "bad")]
    [TestCase("add", "request", "bad")]
    [TestCase("add", "command", "bad", "-m", "Billing")]
    public void AnInvalidNameStopsBeforeTheBlockFolderAndTheOptions(params string[] args) =>
        Assert.That(() => RunOutsideAnApp(args), Throws.TypeOf<UmpireException>().With.Message.Contain("is not valid"));

    [Test]
    public void AValidNameOutsideAnAppStopsAtTheBlockFolder() =>
        Assert.That(() => RunOutsideAnApp(["add", "request", "CreateInvoice"]),
            Throws.TypeOf<UmpireException>().With.Message.Contain("not in an Umpire app"));

    [Test]
    public void CheckOutsideAnAppStopsAtTheBlockFolder() =>
        Assert.That(() => RunOutsideAnApp(["check"]),
            Throws.TypeOf<UmpireException>().With.Message.Contain("not in an Umpire app"));

    [TestCase("module", "module")]
    [TestCase("plugin", "plugin module")]
    [TestCase("pack", "pack")]
    public void AModuleWithTheAppNameStops(string kind, string noun) =>
        Assert.That(() => RunInAnApp(["add", kind, "Shop"], app: "Shop"),
            Throws.TypeOf<UmpireException>().With.Message.Contain($"The {noun} name Shop is the name of the app.")
                .And.Message.Contain("CS0234"));

    [Test]
    public void AddPackWritesTheKernelAndItsArchitecture() {
        var files = FilesAfter(["add", "pack", "Ecs"], app: "Shop");

        Assert.That(files, Is.EquivalentTo(new[] { "App/Packs/Ecs/Definition/EcsArchitecture.cs", "App/Packs/Ecs/Kernel/EcsKernel.cs" }));
    }

    [Test]
    public void AddPackStopsOnTheNameOfABasePack() =>
        Assert.That(() => RunInAnApp(["add", "pack", "Vsa"], app: "Shop"),
            Throws.TypeOf<UmpireException>().With.Message.Contain("The pack name Vsa is the name of a base pack.").And.Message.Contain("CS0146"));

    [Test]
    public void AddPackStopsIfThePackExists() =>
        Assert.That(() => RunInFolder(["add", "pack", "Ecs"], "Shop", folder => Directory.CreateDirectory(Path.Combine(folder, "App", "Packs", "Ecs"))),
            Throws.TypeOf<UmpireException>().With.Message.Contain("The pack Ecs exists: App/Packs/Ecs."));

    [TestCase("module", "App/Features", "The module Billing exists: App/Features/Billing. Use a different name.")]
    [TestCase("plugin", "App/Plugins", "The plugin module Billing exists: App/Plugins/Billing. Use a different name.")]
    [TestCase("module", "App/Plugins", "The plugin module Billing exists: App/Plugins/Billing. A module and a plugin module")]
    [TestCase("plugin", "App/Features", "The module Billing exists: App/Features/Billing. A module and a plugin module")]
    public void AModuleStopsIfItsFolderOrTheFolderOfTheOtherModuleKindExists(string kind, string folder, string message) =>
        Assert.That(() => RunInFolder(["add", kind, "Billing"], "Shop", root => Directory.CreateDirectory(Path.Combine(root, folder, "Billing"))),
            Throws.TypeOf<UmpireException>().With.Message.Contain(message));

    [Test]
    public void ASliceKindHasNoFolderCheck() =>
        Assert.That(() => AddKind.Request.EnsureFree("no-folder", "Shop"), Throws.Nothing);

    static int RunOutsideAnApp(string[] args) => RunInFolder(args, app: null);

    static int RunInAnApp(string[] args, string app = "Hello") => RunInFolder(args, app);

    static IReadOnlyList<string> FilesAfter(string[] args, string app) {
        var files = new List<string>();
        RunInFolder(args, app, inspect: folder => files.AddRange(Directory.GetFiles(folder, "*.cs", SearchOption.AllDirectories)
            .Select(file => Path.GetRelativePath(folder, file).Replace('\\', '/'))));
        return files;
    }

    static int RunInFolder(string[] args, string? app, Action<string>? prepare = null, Action<string>? inspect = null) {
        var folder = TempFolder.Create();
        var current = Environment.CurrentDirectory;
        try {
            if (app is not null) {
                File.WriteAllText(Path.Combine(folder, $"{app}.csproj"), $"<Project Sdk=\"Microsoft.NET.Sdk\"><ItemGroup><PackageReference Include=\"Umpire.Vsa\" Version=\"{ToolVersion.Current}\" /></ItemGroup></Project>");
            }
            prepare?.Invoke(folder);
            Environment.CurrentDirectory = folder;
            var exitCode = CommandList.Run(args);
            inspect?.Invoke(folder);
            return exitCode;
        } finally {
            Environment.CurrentDirectory = current;
            TempFolder.Delete(folder);
        }
    }
}
