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
        var classes = DefinitionFinder.Find(app);
        if (AppClassError(classes) is { } error) {
            return error;
        }
        var definition = DefinitionFinder.ArchitectureOf(classes.Usable[0]);
        var externals = definition.Parts().Where(p => p.IsExternal).Select(p => p.Assembly!).Distinct().ToList();
        return new AppInput(definition, [.. app.Concat(loader.ExternalAssemblies(externals))], root);
    }

    static Error? AppClassError(AppClasses classes) => classes switch {
        { NoConstructor.Count: > 0 } => new Error("VSA3006",
            $"The app class {DefinitionFinder.Names(classes.NoConstructor)} has no public constructor with no parameters, "
            + "so the engine cannot make it and read its architecture. Add a public constructor with no parameters."),
        { Usable.Count: 0 } => new Error("VSA3003", "The app has no app class. "
            + "Add one class that inherits App<TPack>, for example App<Vsa<WebVsaHost>>."),
        { Usable.Count: > 1 } => new Error("VSA3005",
            $"The app has more than one app class: {DefinitionFinder.Names(classes.Usable)}. "
            + "Keep one, or make one inherit the other."),
        _ => null,
    };

    static string[] Names(string list) => list.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
