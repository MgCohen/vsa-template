using Rules.Infrastructure;
using VSA.Contracts;
using VSA.Handlers.Commands;

namespace Rules.Slices.CheckRules;

sealed class CheckRulesHandler : CommandHandler<CheckRulesCommand, CheckRulesResult> {
    public override Task<Result<CheckRulesResult>> HandleAsync(Envelope<CheckRulesCommand> envelope) {
        var command = envelope.Message;
        var check = ArchUnitRuleChecker.Check(command.Definition, command.Assemblies, command.AppRoot);
        var findings = check.Findings.Select(text => FindingText.Parse(text, check.AppRoot)).ToList();
        return Task.FromResult<Result<CheckRulesResult>>(new CheckRulesResult(findings, check.Types));
    }
}
