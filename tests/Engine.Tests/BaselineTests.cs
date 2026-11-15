using Baseline;
using Baseline.Slices.ApplyBaseline;
using Microsoft.Extensions.DependencyInjection;
using Report;
using Report.Slices.WriteReport;
using Steps;
using Steps.Domain.Shared;
using Tests.Shared;
using VSA.Contracts;
using VSA.Contracts.Commands;

namespace Engine.Tests;

public sealed class BaselineTests {
    const string FileName = "vsa-baseline.txt";
    string root = "";

    [SetUp]
    public void CreateRoot() => root = TempFolder.Create();

    [TearDown]
    public void DeleteRoot() => TempFolder.Delete(root);

    [Test]
    public async Task An_error_in_the_baseline_is_accepted() {
        var error = Error("VSA2001", "App/Host/Probe.cs", 4, "Probe in Host");
        WriteBaseline("VSA2001|App/Host/Probe.cs|Probe in Host");

        var result = await Apply([error]);

        Assert.That(result.Value.Accepted, Is.EqualTo(new[] { error }));
        Assert.That(result.Value.Findings, Is.Empty);
    }

    [Test]
    public async Task An_error_that_is_not_in_the_baseline_is_new() {
        WriteBaseline("VSA2001|App/Host/Probe.cs|Probe in Host");

        var result = await Apply([Error("VSA2001", "App/Host/Other.cs", 4, "Other in Host")]);

        Assert.That(result.Value.Accepted, Is.Empty);
    }

    [Test]
    public async Task A_moved_line_still_matches_the_entry() {
        WriteBaseline("VSA2001|App/Host/Probe.cs|Probe in Host");

        var result = await Apply([Error("VSA2001", "App/Host/Probe.cs", 40, "Probe in Host")]);

        Assert.That(result.Value.Accepted, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task Two_entries_accept_two_errors_and_a_third_error_is_new() {
        WriteBaseline("VSA2001|App/Host/Probe.cs|Probe in Host", "VSA2001|App/Host/Probe.cs|Probe in Host");
        Finding[] errors = [
            Error("VSA2001", "App/Host/Probe.cs", 4, "Probe in Host"),
            Error("VSA2001", "App/Host/Probe.cs", 5, "Probe in Host"),
            Error("VSA2001", "App/Host/Probe.cs", 6, "Probe in Host"),
        ];

        var result = await Apply(errors);

        Assert.That(result.Value.Accepted, Is.EqualTo(errors[..2]));
    }

    [Test]
    public async Task An_entry_with_no_error_gives_VSA3005_at_its_line() {
        WriteBaseline("VSA2001|App/Host/Probe.cs|Probe in Host", "VSA2001|App/Host/Fixed.cs|Fixed in Host");

        var result = await Apply([Error("VSA2001", "App/Host/Probe.cs", 4, "Probe in Host")]);

        var stale = result.Value.Findings.Single();
        Assert.That(stale.Code, Is.EqualTo("VSA3005"));
        Assert.That(stale.Severity, Is.EqualTo(FindingSeverity.Warning));
        Assert.That(stale.File, Is.EqualTo(Path.Combine(root, FileName)));
        Assert.That(stale.Line, Is.EqualTo(2));
        Assert.That(stale.Message, Does.Contain("App/Host/Fixed.cs").And.Contain("-p:VsaUpdateBaseline=true"));
    }

    [Test]
    public async Task A_line_that_does_not_parse_gives_VSA3006_and_accepts_no_error() {
        WriteBaseline("VSA2001|App/Host/Probe.cs|Probe in Host", "not an entry");

        var result = await Apply([Error("VSA2001", "App/Host/Probe.cs", 4, "Probe in Host")]);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error!.Code, Is.EqualTo("VSA3006"));
        Assert.That(result.Error.Message, Does.Contain($"{FileName}(2,1)"));
    }

    [Test]
    public async Task The_update_writes_the_errors_in_sorted_order_and_removes_stale_entries() {
        WriteBaseline("VSA2001|App/Host/Fixed.cs|Fixed in Host");
        Finding[] findings = [
            Error("VSA2102", "Features/B.cs", 1, "B text"),
            Error("VSA2001", "Features/A.cs", 9, "A text"),
            new Finding("VSA1008", FindingSeverity.Warning, "a warning", Path.Combine(root, "Features/C.cs"), 1),
        ];

        var result = await Apply(findings, update: true);

        Assert.That(File.ReadAllText(Path.Combine(root, FileName)),
            Is.EqualTo("VSA2001|Features/A.cs|A text\nVSA2102|Features/B.cs|B text\n"));
        Assert.That(result.Value.Accepted, Has.Count.EqualTo(2));
        Assert.That(result.Value.Findings, Is.Empty);
    }

    [Test]
    public async Task An_engine_error_VSA3xxx_is_never_accepted_or_written() {
        var crash = new Finding("VSA3000", FindingSeverity.Error, "LoadDefinition could not run: boom");

        var result = await Apply([crash], update: true);

        Assert.That(result.Value.Accepted, Is.Empty);
        Assert.That(File.Exists(Path.Combine(root, FileName)), Is.False);
        Assert.That(result.Value.Findings.Single().Code, Is.EqualTo("VSA3007"));
    }

    [Test]
    public async Task An_update_after_a_failed_step_keeps_the_file_and_gives_VSA3007() {
        WriteBaseline("VSA2001|App/Host/Probe.cs|Probe in Host", "VSA2001|App/Host/Other.cs|Other in Host");
        Finding[] findings = [
            Error("VSA2001", "App/Host/Probe.cs", 4, "Probe in Host"),
            new Finding("VSA3004", FindingSeverity.Info, "CheckRules skipped: App is missing"),
        ];

        var result = await Apply(findings, update: true);

        Assert.That(File.ReadAllLines(Path.Combine(root, FileName)), Has.Length.EqualTo(2));
        Assert.That(result.Value.Findings.Select(f => f.Code), Is.EqualTo(new[] { "VSA3007", "VSA3005" }));
        Assert.That(result.Value.Findings[0].Severity, Is.EqualTo(FindingSeverity.Warning));
        Assert.That(result.Value.Accepted, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task No_baseline_file_accepts_no_error() {
        var result = await Apply([Error("VSA2001", "App/Host/Probe.cs", 4, "Probe in Host")]);

        Assert.That(result.Value.Accepted, Is.Empty);
        Assert.That(result.Value.Findings, Is.Empty);
    }

    [Test]
    public async Task The_report_sorts_the_findings() {
        Finding[] findings = [
            Error("VSA2102", "B.cs", 1, "b"),
            Error("VSA2001", "A.cs", 9, "a late"),
            new Finding("VSA3004", FindingSeverity.Info, "skipped"),
            Error("VSA2001", "A.cs", 2, "a early"),
        ];

        var report = await Send(new WriteReportCommand("Error", findings));

        Assert.That(report.Value.Lines.Select(line => line.Split(": ").Last()),
            Is.EqualTo(new[] { "skipped", "a early", "a late", "b" }));
    }

    static async Task<Result<T>> Send<T>(ICommand<T> command) {
        using var host = new EngineTestApp(new StepsPlugin(), new BaselineModule(), new ReportModule()).Build();
        await using var scope = host.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<ICommandSender>().SendAsync(command, default);
    }

    Finding Error(string code, string file, int line, string message) =>
        new(code, FindingSeverity.Error, message, Path.Combine(root, file), line);

    void WriteBaseline(params string[] lines) =>
        File.WriteAllText(Path.Combine(root, FileName), string.Concat(lines.Select(line => line + "\n")));

    Task<Result<ApplyBaselineResult>> Apply(IReadOnlyList<Finding> findings, bool update = false) =>
        Send(new ApplyBaselineCommand(root, update, findings));
}
