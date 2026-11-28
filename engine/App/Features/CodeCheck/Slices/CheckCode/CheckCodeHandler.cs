namespace CodeCheck.Slices.CheckCode;

using Facts.Domain.Shared.Compile;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Commands;

sealed class CheckCodeHandler(CodeRuns runs) : CommandHandler<CheckCodeCommand, CheckCodeResult> {
    public override Task<Result<CheckCodeResult>> HandleAsync(IEnvelope<CheckCodeCommand> envelope) {
        var command = envelope.Message;
        if (command.Stage == CompileStage.Load) {
            return Task.FromResult<Result<CheckCodeResult>>(new CheckCodeResult([], false) { Codes = CodeRule.Codes });
        }
        var run = runs.Of(command.Project);
        var findings = command.Stage switch {
            CompileStage.Start => run.Notices,
            CompileStage.File when command.File is { } file => run.Check(file),
            CompileStage.Type when command.Type is { } type => run.Check(type),
            _ => [],
        };
        return Task.FromResult<Result<CheckCodeResult>>(new CheckCodeResult(findings, run.ChecksTypes));
    }
}
