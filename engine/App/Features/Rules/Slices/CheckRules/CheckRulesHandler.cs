namespace Rules.Slices.CheckRules;

using BuiltApp.Infrastructure;
using LoadApp.Domain.Shared;
using Rules.Domain;
using Steps.Domain.Shared;
using Steps.Infrastructure;
using Umpire.Config;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Commands;

sealed class CheckRulesHandler(RunOptions options) : CommandHandler<CheckRulesCommand, CheckRulesResult> {
    public override Task<Result<CheckRulesResult>> HandleAsync(IEnvelope<CheckRulesCommand> envelope) =>
        Task.FromResult<Result<CheckRulesResult>>(new CheckRulesResult(Load(envelope.Message.App, options).Findings()));

    internal static ArchitectureCheck Load(AppInput app, RunOptions options) {
        var block = app.Blocks.Current.Folder;
        var file = UmpireConfig.Find(block);
        var config = RuleConfig.Of(RuleKeys.Of(file));
        return ArchitectureCheck.Load(
            app.Architecture, app.Dlls, app.Sources, app.Blocks, config, options.IgnoredFolders, options.References,
            FolderAliasConfig.Of(file, block));
    }
}
