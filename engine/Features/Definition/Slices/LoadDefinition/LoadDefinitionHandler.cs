using Report.Domain.Shared;
using VSA.Contracts;
using VSA.Handlers.Commands;

namespace Definition.Slices.LoadDefinition;

sealed class LoadDefinitionHandler : CommandHandler<LoadDefinitionCommand, LoadDefinitionResult> {
    public override Task<Result<LoadDefinitionResult>> HandleAsync(Envelope<LoadDefinitionCommand> envelope) {
        var command = envelope.Message;
        var loader = new AppLoader(command.Bin);
        var versions = loader.VersionFindings().ToList();
        if (versions.Count > 0) {
            return Done(new LoadDefinitionResult(null, [], versions));
        }
        var app = loader.AppAssemblies(command.AppRoot);
        if (app.Count == 0) {
            return Done(Failed($"No assembly in {command.Bin} has source files under the app root {command.AppRoot}. "
                + "Build the host project of the app, and give its output folder to --bin."));
        }
        var definition = DefinitionFinder.Find(app);
        if (definition is null) {
            return Done(Failed("The app has no architecture definition. "
                + "Add an app class that gives the definition (IArchitectureProvider), or a class that inherits ArchitectureDefinition."));
        }
        var externals = definition.Parts().Where(p => p.IsExternal).Select(p => p.Assembly!).Distinct().ToList();
        return Done(new LoadDefinitionResult(definition, [.. app.Concat(loader.ExternalAssemblies(externals))], []));
    }

    static LoadDefinitionResult Failed(string message) => new(null, [], [new Finding("VSA3002", FindingSeverity.Error, message)]);

    static Task<Result<LoadDefinitionResult>> Done(LoadDefinitionResult result) => Task.FromResult<Result<LoadDefinitionResult>>(result);
}
