using Rules.Infrastructure;
using Steps.Domain.Shared;
using VSA.Contracts;
using VSA.Handlers.Commands;

namespace Rules.Slices.CheckRules;

sealed class CheckRulesHandler : CommandHandler<CheckRulesCommand, CheckRulesResult> {
    public override Task<Result<CheckRulesResult>> HandleAsync(Envelope<CheckRulesCommand> envelope) {
        var app = envelope.Message.App;
        var check = ArchUnitRuleChecker.Check(app.Definition, app.Assemblies, app.AppRoot);
        return Task.FromResult<Result<CheckRulesResult>>(new CheckRulesResult([.. check.Findings.Select(ToFinding)], check.Types));
    }

    static Finding ToFinding(RuleCheckFinding finding) => new(
        finding.Code, finding.IsWarning ? FindingSeverity.Warning : FindingSeverity.Error, finding.Text, finding.File, finding.Line);
}
