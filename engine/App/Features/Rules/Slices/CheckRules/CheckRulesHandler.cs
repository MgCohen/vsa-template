namespace Rules.Slices.CheckRules;

using Facts.Infrastructure;
using LoadApp.Domain.Shared;
using Rules.Domain;
using Operations.Domain.Shared;
using Operations.Infrastructure;
using Umpire.Config;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Commands;

sealed class CheckRulesHandler(RunOptions options, IFactSource<TypeFactsQuery, LoadedApp> types)
    : CommandHandler<CheckRulesCommand, CheckRulesResult> {
    public override Task<Result<CheckRulesResult>> HandleAsync(IEnvelope<CheckRulesCommand> envelope) {
        var findings = Load(envelope.Message.App, options, types).Findings();
        return Task.FromResult<Result<CheckRulesResult>>(new CheckRulesResult(findings));
    }

    internal static ArchitectureCheck Load(
        AppInput app, RunOptions options, IFactSource<TypeFactsQuery, LoadedApp> types) {
        var block = app.Blocks.Current.Folder;
        var file = UmpireConfig.Find(block);
        var config = RuleConfig.Of(RuleKeys.Of(file));
        return ArchitectureCheck.Load(
            types, app.Architecture, app.Dlls, app.Sources, app.Blocks, config, options.IgnoredFolders,
            options.References, FolderAliasConfig.Of(file, block));
    }
}
