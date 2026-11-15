using Definition.Domain.Shared;
using Definition.Infrastructure;
using VSA.Contracts;
using VSA.Handlers.Commands;

namespace Definition.Slices.LoadDefinition;

sealed class LoadDefinitionHandler : CommandHandler<LoadDefinitionCommand, AppInput> {
    public override Task<Result<AppInput>> HandleAsync(Envelope<LoadDefinitionCommand> envelope) =>
        Task.FromResult(Load(envelope.Message));

    static Result<AppInput> Load(LoadDefinitionCommand command) {
        var root = Path.GetFullPath(command.AppRoot);
        var bin = Path.GetFullPath(command.Bin);
        var loader = new AppLoader(bin);
        var app = loader.AppAssemblies(root, Names(command.Skip));
        if (app.Count == 0) {
            return new Error("VSA3002", $"No assembly in {bin} has source files under the app root {root}. "
                + "Build the host project of the app, and give its output folder to --bin.");
        }
        var definition = DefinitionFinder.Find(app);
        if (definition is null) {
            return new Error("VSA3003", "The app has no app class. "
                + "Add one class that inherits App<TPack>, for example App<Vsa<WebVsaHost>>.");
        }
        var externals = definition.Parts().Where(p => p.IsExternal).Select(p => p.Assembly!).Distinct().ToList();
        return new AppInput(definition, [.. app.Concat(loader.ExternalAssemblies(externals))], root);
    }

    static string[] Names(string list) => list.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
