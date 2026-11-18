namespace Rules.Slices.CheckRules;

using Rules.Domain;
using Umpire.Config;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Commands;

sealed class CheckRulesHandler : CommandHandler<CheckRulesCommand, CheckRulesResult> {
    public override Task<Result<CheckRulesResult>> HandleAsync(Envelope<CheckRulesCommand> envelope) {
        var app = envelope.Message.App;
        var config = RuleConfig.Of(UmpireConfig.Find(app.Blocks.Current.Folder).Values());
        var check = ArchitectureCheck.Load(app.Architecture, app.Dlls, app.Sources, app.Blocks, config);
        return Task.FromResult<Result<CheckRulesResult>>(new CheckRulesResult(check.Findings()));
    }
}
