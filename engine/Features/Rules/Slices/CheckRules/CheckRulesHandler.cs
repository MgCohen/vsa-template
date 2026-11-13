using VSA.Architecture.Engine;
using VSA.Contracts;
using VSA.Handlers.Commands;

namespace Rules.Slices.CheckRules;

sealed class CheckRulesHandler : CommandHandler<CheckRulesCommand, CheckRulesResult> {
    public override Task<Result<CheckRulesResult>> HandleAsync(Envelope<CheckRulesCommand> envelope) {
        var command = envelope.Message;
        var map = ArchitectureMap.Load(command.Definition, command.Assemblies, command.AppRoot);
        var findings = map.AppTypes.SelectMany(map.Findings).Select(text => FindingText.Parse(text, map.AppRoot)).ToList();
        return Task.FromResult<Result<CheckRulesResult>>(new CheckRulesResult(findings, map.AppTypes.Count));
    }
}
