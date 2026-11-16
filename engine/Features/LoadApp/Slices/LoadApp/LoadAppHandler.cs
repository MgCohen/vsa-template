using System.Reflection;
using BuiltApp.Domain.Shared;
using LoadApp.Domain.Shared;
using LoadApp.Infrastructure;
using Umpire.Architecture;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Commands;

namespace LoadApp.Slices.LoadApp;

sealed class LoadAppHandler : CommandHandler<LoadAppCommand, AppInput> {
    public override Task<Result<AppInput>> HandleAsync(Envelope<LoadAppCommand> envelope) =>
        Task.FromResult(Load(envelope.Message));

    static Result<AppInput> Load(LoadAppCommand command) {
        var blocks = command.Blocks;
        var root = Path.GetFullPath(blocks.Current.Folder);
        var bin = Path.GetFullPath(command.Bin);
        var loader = new AppLoader(bin);
        var app = loader.AppAssemblies(root, Names(command.Skip));
        if (app.Count == 0) {
            return new Error(LoadAppCodes.NoAppAssembly, $"No assembly in {bin} has source files under the block folder {root}. "
                + "Build the host project of the app, and give its output folder to --bin.");
        }
        var found = Definition(blocks.Current, app, loader);
        if (found.Error is { } error) {
            return error;
        }
        var definition = found.Value;
        var externals = definition.Parts().Where(p => p.IsExternal).Select(p => p.Assembly!).Distinct().ToList();
        var assemblies = app.Concat(loader.ExternalAssemblies(externals)).ToList();
        return new AppInput(definition, assemblies, root, blocks.InnerFolders);
    }

    static Result<Architecture> Definition(BlockEntry entry, List<Assembly> app, AppLoader loader) {
        if (entry.Form != EntryForm.App) {
            return DefinitionFinder.ArchitectureOf(entry.Architecture!, loader);
        }
        var classes = DefinitionFinder.Find(app);
        if (AppClassError(classes) is { } error) {
            return error;
        }
        return DefinitionFinder.ArchitectureOf(classes.Usable[0]);
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
