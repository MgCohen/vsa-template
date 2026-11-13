using Definition.Domain.Shared;
using Definition.Infrastructure;
using VSA.Contracts;
using VSA.Handlers.Commands;

namespace Definition.Slices.LoadDefinition;

sealed class LoadDefinitionHandler : CommandHandler<LoadDefinitionCommand, LoadedApp> {
    public override Task<Result<LoadedApp>> HandleAsync(Envelope<LoadDefinitionCommand> envelope) =>
        Task.FromResult(Load(envelope.Message));

    static Result<LoadedApp> Load(LoadDefinitionCommand command) {
        var root = Path.GetFullPath(command.AppRoot);
        var bin = Path.GetFullPath(command.Bin);
        var loader = new AppLoader(bin);
        var versions = loader.VersionMismatches();
        if (versions.Count > 0) {
            return new Error("VSA3001",
                $"The app and the engine use different versions: {string.Join(", ", versions)}. Use one version: run dotnet vsa update.");
        }
        var app = loader.AppAssemblies(root, Names(command.Skip));
        if (app.Count == 0) {
            return new Error("VSA3002", $"No assembly in {bin} has source files under the app root {root}. "
                + "Build the host project of the app, and give its output folder to --bin.");
        }
        var definition = DefinitionFinder.Find(app);
        if (definition is null) {
            return new Error("VSA3002", "The app has no architecture definition. "
                + "Add an app class that gives the definition (IArchitectureProvider), or a class that inherits ArchitectureDefinition.");
        }
        var externals = definition.Parts().Where(p => p.IsExternal).Select(p => p.Assembly!).Distinct().ToList();
        return new LoadedApp(definition, [.. app.Concat(loader.ExternalAssemblies(externals))], root);
    }

    static string[] Names(string list) => list.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
