using LoadApp.Domain.Shared;
using LoadApp.Infrastructure;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Commands;

namespace LoadApp.Slices.LoadApp;

sealed class LoadAppHandler : CommandHandler<LoadAppCommand, AppInput> {
    public override Task<Result<AppInput>> HandleAsync(Envelope<LoadAppCommand> envelope) =>
        Task.FromResult(Load(envelope.Message));

    static Result<AppInput> Load(LoadAppCommand command) {
        var root = Path.GetFullPath(command.AppRoot);
        var bin = Path.GetFullPath(command.Bin);
        var loader = new AppLoader(bin);
        var app = loader.AppAssemblies(root, Names(command.Skip));
        if (app.Count == 0) {
            return new Error(LoadAppCodes.NoAppAssembly, $"No assembly in {bin} has source files under the app root {root}. "
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
        { NoConstructor.Count: > 0 } => new Error(LoadAppCodes.NoEmptyConstructor,
            $"The app class {DefinitionFinder.Names(classes.NoConstructor)} has no public constructor with no parameters, "
            + "so the engine cannot make it and read its architecture. Add a public constructor with no parameters."),
        { Usable.Count: 0 } => new Error(LoadAppCodes.NoAppClass, "The app has no app class. "
            + "Add one class that inherits App<TPack>, for example App<Vsa<WebVsaHost>>."),
        { Usable.Count: > 1 } => new Error(LoadAppCodes.ManyAppClasses,
            $"The app has more than one app class: {DefinitionFinder.Names(classes.Usable)}. "
            + "Keep one, or make one inherit the other."),
        _ => null,
    };

    static string[] Names(string list) => list.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
