namespace Rules.Slices.CheckRules;

using Rules.Infrastructure;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Commands;

sealed class CheckRulesHandler : CommandHandler<CheckRulesCommand, CheckRulesResult> {
    public override Task<Result<CheckRulesResult>> HandleAsync(Envelope<CheckRulesCommand> envelope) {
        var app = envelope.Message.App;
        var config = UmpireConfigFile.Read(app.Blocks.Current.Folder);
        var check = ArchitectureCheck.Load(app.Architecture, app.Dlls, app.Sources, app.Blocks, config);
        return Task.FromResult<Result<CheckRulesResult>>(new CheckRulesResult(check.Findings()));
    }
}
