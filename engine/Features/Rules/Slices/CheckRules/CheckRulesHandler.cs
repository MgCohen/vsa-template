namespace Rules.Slices.CheckRules;

using Rules.Domain;
using Steps.Domain.Shared;
using Steps.Infrastructure;
using Umpire.Config;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Commands;

sealed class CheckRulesHandler(RunOptions options) : CommandHandler<CheckRulesCommand, CheckRulesResult> {
    public override Task<Result<CheckRulesResult>> HandleAsync(IEnvelope<CheckRulesCommand> envelope) {
        var app = envelope.Message.App;
        var config = RuleConfig.Of(RuleKeys.Of(UmpireConfig.Find(app.Blocks.Current.Folder)));
        var check = ArchitectureCheck.Load(app.Architecture, app.Dlls, app.Sources, app.Blocks, config, options.IgnoredFolders);
        return Task.FromResult<Result<CheckRulesResult>>(new CheckRulesResult(check.Findings()));
    }
}
