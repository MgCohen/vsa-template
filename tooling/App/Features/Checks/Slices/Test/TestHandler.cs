namespace Umpire.Cli.Features.Checks.Slices.Test;

using Umpire.Cli.Plugins.Commands.Domain.Shared;
using Umpire.Cli.Plugins.Commands.Handlers.Requests;
using Umpire.Cli.Plugins.Commands.Infrastructure;
using Umpire.Engine.Versions;

sealed class TestHandler : ToolHandler<TestRequest, TestResponse> {
    const string TestProjectEnd = ".Tests.csproj";

    internal static IReadOnlyList<string> TestProjects(string block) =>
        [.. VersionSlots.ProjectFiles(block).Where(file => file.EndsWith(TestProjectEnd, StringComparison.Ordinal))];

    protected override async Task<TestResponse> Run(TestRequest request, CancellationToken ct) {
        var block = RootProject.AppOfCurrentFolder();
        var projects = TestProjects(block);
        if (projects is []) {
            throw new UmpireException($"The app in {block} has no test project. A test project is a project "
                + "whose name ends with .Tests, for example App/Tests/MyApp.Tests.csproj.");
        }
        string[] configuration = request.Configuration is string value ? ["-c", value] : [];
        var runs = new List<TestRun>();
        foreach (var project in projects) {
            if (ct.IsCancellationRequested) {
                break;
            }
            Console.WriteLine($"Tests of {project}:");
            var exitCode = await Dotnet.Stream(block, ["run", "--project", project, .. configuration], ct)
                .ConfigureAwait(false);
            runs.Add(new TestRun(project, exitCode));
        }
        return new TestResponse(Report(projects.Count, runs, ct.IsCancellationRequested));
    }

    static int Report(int projects, IReadOnlyList<TestRun> runs, bool stopped) {
        if (stopped) {
            Console.WriteLine($"Stopped by a stop signal: the tests of {runs.Count} of {projects} project(s) ran.");
        }
        if (stopped && runs.Count < projects) {
            return runs is [.., { ExitCode: not 0 and var last }] ? last : ChildProcess.StoppedExitCode;
        }
        var failed = runs.Where(run => run.ExitCode != 0).ToList();
        if (failed is []) {
            Console.WriteLine($"OK: the tests of {projects} project(s) passed.");
            return 0;
        }
        var names = failed.Select(run => $"{run.Project} (exit code {run.ExitCode})");
        Console.WriteLine("Failed: " + string.Join(", ", names));
        return failed[0].ExitCode;
    }

    sealed record TestRun(string Project, int ExitCode);
}
