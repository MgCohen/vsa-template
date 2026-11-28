namespace Model.Slices.CheckTypes;

using Facts.Domain.Shared.Compile;
using Model.Infrastructure;
using Operations.Domain.Shared;
using Umpire.CheckModel;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Commands;
using ModelFinding = Umpire.CheckModel.Finding;

sealed class CheckTypesHandler(ModelRuns runs) : CommandHandler<CheckTypesCommand, CheckTypesResult> {
    const string RuleTitle = "An architecture rule of the definition";
    const string DataTitle = "The data of the check model";

    static readonly IReadOnlyList<FindingCode> Codes = [
        .. RuleCodes.All.Select(code => new FindingCode(code, RuleTitle, AtEnd: false)),
        .. new[] { DataCodes.NoModel, DataCodes.NoSnapshot, DataCodes.OldSnapshot, DataCodes.NotSaved }
            .Select(code => new FindingCode(code, DataTitle, AtEnd: true)),
    ];

    public override Task<Result<CheckTypesResult>> HandleAsync(IEnvelope<CheckTypesCommand> envelope) {
        var command = envelope.Message;
        if (command.Stage == CompileStage.Load) {
            return Task.FromResult<Result<CheckTypesResult>>(new CheckTypesResult([], false) { Codes = Codes });
        }
        var run = runs.Of(command.Project);
        var findings = command.Stage switch {
            CompileStage.Type when command.Type is { } type => run.Check(new TypeName(type.FullName, type.Assembly)),
            CompileStage.End => run.End(command.HasErrors),
            _ => [],
        };
        return Task.FromResult<Result<CheckTypesResult>>(new CheckTypesResult([.. findings.Select(Converted)], run.IsLoaded));
    }

    static Operations.Domain.Shared.Finding Converted(ModelFinding finding) => new(
        finding.Code, SeverityOf(finding.Severity), finding.Text, finding.File, finding.File is null ? null : finding.Line,
        finding.File is null ? null : finding.Column);

    static FindingSeverity SeverityOf(Severity severity) => severity switch {
        Severity.Error => FindingSeverity.Error,
        Severity.Warning => FindingSeverity.Warning,
        _ => FindingSeverity.Info,
    };
}
