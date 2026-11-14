using Definition.Slices.LoadDefinition;
using Export.Slices.ExportFiles;
using Report.Slices.WriteReport;
using Rules.Slices.CheckRules;
using Steps.Domain.Shared;
using Steps.Infrastructure;
using VSA.Contracts;
using VSA.Contracts.Commands;
using VSA.Handlers.Commands;

namespace Engine.Slices.RunEngine;

sealed class RunEngineHandler(ICommandSender commands, StepLog log) : CommandHandler<RunEngineCommand, ReportResult> {
    public override async Task<Result<ReportResult>> HandleAsync(Envelope<RunEngineCommand> envelope) {
        var run = envelope.Message;
        log.FailFast = run.FailFast;
        await Run(run, envelope.Cancellation);
        return await commands.SendAsync(new WriteReportCommand(run.Mode, log.Findings), envelope.Cancellation);
    }

    async Task Run(RunEngineCommand run, CancellationToken ct) {
        var app = await commands.SendAsync(new LoadDefinitionCommand(run.Root, run.Bin, run.Skip, run.Definition), ct);
        if (run.Export.Length > 0) {
            await commands.SendAsync(new ExportFilesCommand(app.Value, run.Export), ct);
        }
        await commands.SendAsync(new CheckRulesCommand(app.Value), ct);
    }
}
