using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Commands;

namespace Rules.Slices.CheckRules;

sealed class CheckRulesHandler : CommandHandler<CheckRulesCommand, CheckRulesResult> {
    public override Task<Result<CheckRulesResult>> HandleAsync(Envelope<CheckRulesCommand> envelope) {
        var app = envelope.Message.App;
        var check = ArchitectureCheck.Load(app.Architecture, app.Assemblies.Select(a => a.Location), app.AppRoot);
        return Task.FromResult<Result<CheckRulesResult>>(new CheckRulesResult(check.Findings(), check.Bindings.AppTypes.Count));
    }
}
