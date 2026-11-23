using Baseline;
using Blocks;
using Engine.Slices.RunEngine;
using LoadApp;
using LoadTemplates;
using Microsoft.Extensions.DependencyInjection;
using PlanScaffold;
using Report;
using Report.Domain.Shared;
using Rules;
using Scaffold;
using Scaffold.Domain.Shared;
using Scaffold.Infrastructure;
using Steps;
using Tests.Shared;
using Umpire.Vsa.Contracts.Commands;
using Versions;
using WriteScaffold;

namespace Engine.Tests;

public sealed class ScaffoldTests {
    const string ArchiveNoteHandler = """
        namespace Notes.Features.Notebook.Slices.ArchiveNote;

        using Umpire.Messages.Contracts;
        using Umpire.Vsa.Handlers.Requests;

        sealed class ArchiveNoteHandler : RequestHandler<ArchiveNoteRequest, ArchiveNoteResponse> {
            public override Task<Result<ArchiveNoteResponse>> HandleAsync(Envelope<ArchiveNoteRequest> envelope) =>
                Task.FromResult<Result<ArchiveNoteResponse>>(new ArchiveNoteResponse());
        }

        """;

    static readonly string Notes = Repo.Path("tests", "Fixtures", "Notes");
    static readonly string Templates = Repo.Path("tools", "cli", "Templates");
    static readonly string NewSlices = Path.Combine(Notes, "Features", "Notebook", "Slices");

    [Test]
    public async Task A_request_scaffold_writes_the_request_the_response_and_the_handler_of_the_template() {
        var folder = Path.Combine(NewSlices, "ArchiveNote");
        try {
            var result = await Scaffold("request", "ArchiveNote", "Feature=notebook");

            Assert.That(result.ExitCode, Is.Zero, string.Join(Environment.NewLine, result.Lines));
            Assert.That(Directory.GetFiles(folder).Select(Path.GetFileName), Is.EquivalentTo(new[] {
                "ArchiveNoteHandler.cs", "ArchiveNoteRequest.cs", "ArchiveNoteResponse.cs",
            }));
            Assert.That(File.ReadAllText(Path.Combine(folder, "ArchiveNoteHandler.cs")),
                Is.EqualTo(ArchiveNoteHandler.ReplaceLineEndings("\n")));
            Assert.That(File.ReadAllText(Path.Combine(folder, "ArchiveNoteRequest.cs")),
                Does.Contain("""public static string Route => "/api/notebook/archive-note";"""));
            Assert.That(result.Lines,
                Has.One.Contains("UMP030901: created Features/Notebook/Slices/ArchiveNote/ArchiveNoteHandler.cs"));
        } finally {
            TempFolder.Delete(folder);
        }
    }

    [Test]
    public async Task A_command_scaffold_writes_the_command_the_result_and_the_handler_of_the_template() {
        var folder = Path.Combine(NewSlices, "PinNote");
        try {
            var result = await Scaffold("command", "PinNote", "Feature=Notebook");

            Assert.That(result.ExitCode, Is.Zero, string.Join(Environment.NewLine, result.Lines));
            Assert.That(Directory.GetFiles(folder).Select(Path.GetFileName), Is.EquivalentTo(new[] {
                "PinNoteCommand.cs", "PinNoteHandler.cs", "PinNoteResult.cs",
            }));
            Assert.That(File.ReadAllText(Path.Combine(folder, "PinNoteCommand.cs")),
                Does.Contain("public sealed record PinNoteCommand : ICommand<PinNoteResult>;"));
        } finally {
            TempFolder.Delete(folder);
        }
    }

    [Test]
    public async Task A_scaffold_does_not_write_over_a_file_and_gives_UMP030805() {
        var folder = Path.Combine(NewSlices, "StarNote");
        try {
            await Scaffold("request", "StarNote", "Feature=Notebook");
            var handler = Path.Combine(folder, "StarNoteHandler.cs");
            File.WriteAllText(handler, "changed");

            var result = await Scaffold("request", "StarNote", "Feature=Notebook");

            Assert.That(result.ExitCode, Is.EqualTo(1));
            Assert.That(result.Lines, Has.One.Contains("error UMP030805").And.Contains("StarNoteHandler.cs"));
            Assert.That(File.ReadAllText(handler), Is.EqualTo("changed"));
        } finally {
            TempFolder.Delete(folder);
        }
    }

    [TestCase("event", "ArchiveNote", "Feature=Notebook", "error UMP030801", "The kinds are: command, request.")]
    [TestCase("request", "archiveNote", "Feature=Notebook", "error UMP030802", "PascalCase")]
    [TestCase("request", "ArchiveNote", "", "error UMP030803", "Give it as Feature=<Name>.")]
    [TestCase("request", "ArchiveNote", "Feature=Billing", "error UMP030804", "The Feature Billing does not exist")]
    public async Task A_scaffold_with_wrong_input_stops_and_writes_nothing(
        string kind, string name, string groups, string code, string text) {
        var result = await Scaffold(kind, name, groups);

        Assert.That(result.ExitCode, Is.EqualTo(1));
        Assert.That(result.Lines, Has.One.Contains(code).And.Contains(text));
        Assert.That(Directory.Exists(Path.Combine(NewSlices, name)), Is.False);
    }

    [Test]
    public async Task A_scaffold_with_no_template_block_gives_UMP030701() {
        var missing = Path.Combine(Templates, "Missing");

        var result = await Scaffold("request", "ArchiveNote", "Feature=Notebook", templates: missing);

        Assert.That(result.Lines, Has.One.Contains("error UMP030701"));
        Assert.That(result.Lines, Has.One.Contains("UMP030002: PlanScaffold skipped: Templates is missing"));
    }

    [Test]
    public void The_input_reads_the_group_values_of_the_command_line() {
        var input = ScaffoldInput.From("request", "ArchiveNote", "Feature=Notebook; Area = Notes ;broken");

        Assert.That(input!.Groups,
            Is.EquivalentTo(new Dictionary<string, string> { ["Feature"] = "Notebook", ["Area"] = "Notes" }));
        Assert.That(input.Groups["feature"], Is.EqualTo("Notebook"));
        Assert.That(ScaffoldInput.From("", "ArchiveNote", ""), Is.Null);
    }

    [Test]
    public void The_token_map_replaces_the_longest_token_first_and_adds_the_kebab_forms() {
        var tokens = new TokenMap().Add("SampleModule", "Notebook").Add("SampleModulePage", "NotebookView");

        Assert.That(tokens.Apply("SampleModulePage SampleModule /api/sample-module"),
            Is.EqualTo("NotebookView Notebook /api/notebook"));
        Assert.That(TokenMap.Kebab("HTTPServerCall"), Is.EqualTo("http-server-call"));
    }

    static string TemplatesBin() =>
        Directory.GetDirectories(Path.Combine(Templates, "artifacts~", "bin", "SampleApp")).Single();

    static async Task<ReportResult> Scaffold(string kind, string name, string groups, string? templates = null) {
        using var host = new EngineTestApp(
            new StepsPlugin(), new ScaffoldPlugin(), new EngineModule(), new BlocksModule(), new VersionsModule(),
            new LoadAppModule(), new LoadTemplatesModule(), new PlanScaffoldModule(), new WriteScaffoldModule(),
            new RulesModule(), new BaselineModule(), new ReportModule()).Build();
        await using var scope = host.Services.CreateAsyncScope();
        var command = new RunEngineCommand(Notes, AppContext.BaseDirectory, Notes,
            ScaffoldKind: kind, ScaffoldName: name, ScaffoldGroups: groups,
            ScaffoldTemplates: templates ?? Templates, ScaffoldTemplatesBin: TemplatesBin());
        var result = await scope.ServiceProvider.GetRequiredService<ICommandSender>().SendAsync(command, default);
        return result.Value;
    }
}
