using Jobs.Slices.BuildJob;
using Microsoft.Extensions.DependencyInjection;
using Operations.Contracts.Requests;
using Operations.Domain.Shared;
using Tests.Shared;
using Umpire.Kernels;
using UmpireEngine;

namespace Engine.Tests;

public sealed class JobTests {
    static readonly string Notes = Repo.Path("tests", "Fixtures", "Notes");
    string root = "";

    [SetUp]
    public void BrokenProject() {
        root = TempFolder.Create();
        File.WriteAllText(Path.Combine(root, "Broken.csproj"), "<Project>");
    }

    [TearDown]
    public void Delete() => TempFolder.Delete(root);

    [Test]
    public async Task The_check_job_builds_and_a_build_that_fails_stops_the_check() {
        var result = await RunEngineTests.Run(block: root, project: root, build: true);

        Assert.That(result.ExitCode, Is.EqualTo(1));
        Assert.That(result.Lines, Has.One.Contains("error UMP030101: The build of"));
        Assert.That(result.Lines, Has.One.Contains("UMP030002: FindBlocks skipped"));
    }

    [Test]
    public async Task A_build_that_fails_is_an_error_also_in_Warn_mode() {
        var result = await RunEngineTests.Run(block: root, project: root, build: true, mode: ReportMode.Warn);

        Assert.That(result.ExitCode, Is.EqualTo(1));
        Assert.That(result.Lines, Has.One.Contains("error UMP030101: The build of"));
    }

    [Test]
    public async Task The_build_job_does_not_build_the_project() {
        var result = await RunEngineTests.Run(block: root, project: root);

        Assert.That(result.Lines, Has.None.Contains("UMP030101"));
    }

    [Test]
    public async Task The_build_job_finds_no_errors_in_Notes() {
        var report = await RunEngineTests.Report();

        Assert.That(report, Is.InstanceOf<BuildJobReport>());
        Assert.That(report.Findings, Has.None.Matches<Finding>(f => f.Severity == FindingSeverity.Error),
            string.Join(Environment.NewLine, ConsoleReport.Of(report).Lines));
    }

    [Test]
    public async Task The_check_job_gives_the_findings_of_the_build_job_in_Notes() {
        using var notes = FixtureLock.Take("Notes");
        var checkOnly = await RunEngineTests.Run(project: Notes);

        var withBuild = await RunEngineTests.Run(project: Notes, build: true);

        Assert.That(withBuild.ExitCode, Is.Zero, string.Join(Environment.NewLine, withBuild.Lines));
        Assert.That(withBuild.Lines, Is.SupersetOf(checkOnly.Lines));
        Assert.That(withBuild.Lines, Has.None.Contains("UMP030101").And.None.Contains("UMP030002"));
    }

    [TestCase(ReportMode.Error, 1)]
    [TestCase(ReportMode.Warn, 0)]
    public async Task The_console_entry_converts_the_report_of_a_job_into_the_exit_code(ReportMode mode, int exitCode) {
        var host = new EngineConsoleHost { ModuleProvider = new FixedModules(RunEngineTests.EngineModules()) };
        try {
            string[] args = ["jobs", "build", "--project", root, "--bin", root, "--mode", mode.ToString()];
            var services = host.Build(args, new EngineApp<EngineConsoleHost>());
            using var scope = services.CreateScope();

            var result = await scope.ServiceProvider.SendAsync(
                new Operation("jobs/build", new Dictionary<string, string> { ["project"] = root, ["bin"] = root, ["mode"] = mode.ToString() }));

            Assert.That(result.Value, Is.InstanceOf<BuildJobReport>());
            Assert.That(ConsoleReport.Of((JobReport)result.Value!).ExitCode, Is.EqualTo(exitCode));
            Assert.That(ConsoleReport.Of((JobReport)result.Value!).Lines, Has.One.Contains("UMP030303"));
        } finally {
            host.Dispose();
        }
    }

    [Test]
    public void The_old_hook_form_with_no_job_word_gives_UMP030401_with_the_fix() {
        string[] args = ["--stage", "Check", "--project", root, "--bin", root, "--mode", "Error"];

        var output = ConsoleReport.Of(HookMismatch.Mode(HostArguments.Read(args)), [HookMismatch.Finding]);

        Assert.That(HookMismatch.HasNoJobWord(args), Is.True);
        Assert.That(output.ExitCode, Is.EqualTo(1));
        Assert.That(output.Lines, Is.EqualTo(new[] { $"umpire-engine: error UMP030401: {HookMismatch.Message}" }));
        Assert.That(HookMismatch.Message, Does.Contain("Umpire.Engine").And.Contain("umpire update"));
    }

    [Test]
    public void In_Warn_mode_the_old_hook_form_gives_a_warning() {
        string[] args = ["--stage", "Check", "--project", root, "--bin", root, "--mode", "Warn"];

        var output = ConsoleReport.Of(HookMismatch.Mode(HostArguments.Read(args)), [HookMismatch.Finding]);

        Assert.That(output.ExitCode, Is.Zero);
        Assert.That(output.Lines, Has.One.StartsWith("umpire-engine: warning UMP030401: "));
    }

    [Test]
    public void The_engine_reports_UMP030401_for_a_call_of_the_old_hook_and_runs_no_job() {
        var configuration = Path.GetFileName(
            AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        var engine = Repo.Path("engine", "artifacts~", "bin", "Engine.Console", configuration, "umpire-engine.dll");

        var run = Dotnet.Run(root, null, "exec", engine, "--stage", "Check", "--project", root, "--bin", root,
            "--mode", "Error");

        Assert.That(run.ExitCode, Is.EqualTo(1), run.Output);
        Assert.That(run.Output, Does.Contain("umpire-engine: error UMP030401: The engine got no job word")
            .And.Not.Contain("UMP030303").And.Not.Contain("Unhandled"), run.Output);
    }

    [Test]
    public void The_console_entry_names_the_two_jobs() {
        var host = new EngineConsoleHost { ModuleProvider = new FixedModules(RunEngineTests.EngineModules()) };
        try {
            var services = host.Build(["jobs", "check"], new EngineApp<EngineConsoleHost>());

            var names = services.GetRequiredService<IOperationConverter>().Names;

            Assert.That(names, Is.EquivalentTo(new[] { "jobs/build", "jobs/check" }));
        } finally {
            host.Dispose();
        }
    }

    [Test]
    public void The_console_report_line_has_the_line_and_the_column_of_the_finding() {
        var finding = new Finding("UMP020101", FindingSeverity.Error, "Probe in Host", "Probe.cs", 4, 9);

        var output = ConsoleReport.Of(new BuildJobReport(ReportMode.Error, [finding]));

        Assert.That(output.Lines, Is.EqualTo(new[] { "Probe.cs(4,9): error UMP020101: Probe in Host" }));
        Assert.That(output.ExitCode, Is.EqualTo(1));
    }

    [Test]
    public void In_Warn_mode_the_console_report_keeps_a_build_error_and_makes_each_other_error_a_warning() {
        Finding[] findings = [
            new("UMP030101", FindingSeverity.Error, "The build failed."),
            new("CS0103", FindingSeverity.Error, "No name.", "A.cs", 2, 3),
            new("UMP020101", FindingSeverity.Error, "Probe in Host", "B.cs", 1, 1),
            new("UMP020101", FindingSeverity.Error, "Probe in Host", "B.cs", 1, 1),
        ];

        var output = ConsoleReport.Of(new BuildJobReport(ReportMode.Warn, findings));

        Assert.That(output.Lines, Is.EqualTo(new[] {
            "umpire-engine: error UMP030101: The build failed.",
            "A.cs(2,3): error CS0103: No name.",
            "B.cs(1,1): warning UMP020101: Probe in Host",
        }));
        Assert.That(output.Errors, Is.EqualTo(2));
        Assert.That(output.Warnings, Is.EqualTo(1));
    }
}
