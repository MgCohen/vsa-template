namespace Rules.Slices.CheckRules;

using BuiltApp.Infrastructure;
using Rules.Domain;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Commands;

sealed class CheckRulesHandler : CommandHandler<CheckRulesCommand, CheckRulesResult> {
    public override Task<Result<CheckRulesResult>> HandleAsync(Envelope<CheckRulesCommand> envelope) {
        var app = envelope.Message.App;
        var config = new RuleConfig(UmpireConfigFile.Read(app.Blocks.Current.Folder));
        var check = ArchitectureCheck.Load(app.Architecture, app.Dlls, app.Sources, app.Blocks, config);
        return Task.FromResult<Result<CheckRulesResult>>(new CheckRulesResult(check.Findings()));
    }
}
