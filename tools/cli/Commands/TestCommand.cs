namespace Umpire.Cli.Commands;

using Umpire.Cli.Commands.Bases;
using Umpire.Cli.Scaffolding;
using Umpire.Engine.Versions;

sealed class TestCommand : AppCommand {
    const string TestProjectEnd = ".Tests.csproj";
    static readonly CommandOption Configuration = new(["-c", "--configuration"], "configuration");

    public override IReadOnlyList<string> Words => ["test"];

    public override string Summary =>
        "Runs the tests of each project in the app whose name ends with .Tests. It needs no global.json.";

    public override IReadOnlyList<CommandOption> Options => [Configuration];

    internal static IReadOnlyList<string> TestProjects(string block) =>
        [.. VersionSlots.ProjectFiles(block).Where(file => file.EndsWith(TestProjectEnd, StringComparison.Ordinal))];

    protected override int Run(string block, CommandInput input) {
        var projects = TestProjects(block);
        if (projects is []) {
            throw new UmpireException($"The app in {block} has no test project. A test project is a project "
                + "whose name ends with .Tests, for example App/Tests/MyApp.Tests.csproj.");
        }
        string[] configuration = input.Single(Configuration) is string value ? ["-c", value] : [];
        var failed = projects
            .Select(project => {
                Console.WriteLine($"Tests of {project}:");
                return new TestRun(project, Dotnet.Stream(block, ["run", "--project", project, .. configuration]));
            })
            .Where(run => run.ExitCode != 0)
            .ToList();
        if (failed is []) {
            Console.WriteLine($"OK: the tests of {projects.Count} project(s) passed.");
            return 0;
        }
        var names = failed.Select(run => $"{run.Project} (exit code {run.ExitCode})");
        Console.WriteLine("Failed: " + string.Join(", ", names));
        return failed[0].ExitCode;
    }

    sealed record TestRun(string Project, int ExitCode);
}
