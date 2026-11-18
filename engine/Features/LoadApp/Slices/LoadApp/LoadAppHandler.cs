using BuiltApp.Domain.Shared;
using BuiltApp.Infrastructure;
using LoadApp.Domain.Shared;
using Umpire.Architecture;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Commands;

namespace LoadApp.Slices.LoadApp;

sealed class LoadAppHandler : CommandHandler<LoadAppCommand, AppInput> {
    public override Task<Result<AppInput>> HandleAsync(Envelope<LoadAppCommand> envelope) =>
        Task.FromResult(Load(envelope.Message));

    static Result<AppInput> Load(LoadAppCommand command) {
        var blocks = command.Blocks;
        var root = blocks.Current.Folder;
        var bin = command.Bin;
        var loader = new AppLoader(bin.Bin);
        var skip = Names(command.Skip);
        var app = bin.AppDlls(root, skip);
        if (app.Count == 0) {
            return new Error(LoadAppCodes.NoAppAssembly, $"No assembly in {bin.Bin} has source files under the block folder {root}. "
                + "Build the host project of the app, and give its output folder to --bin.");
        }
        var found = Definition(blocks.Current, app, loader);
        if (found.Error is { } error) {
            return error;
        }
        var definition = found.Value;
        var externals = new ResolvedArchitecture(definition).Parts.Where(p => p.IsExternal).Select(p => p.Assembly!).Distinct().ToList();
        var dlls = app.Concat(bin.ExternalDlls(externals)).Concat(bin.ReferencedDlls(app, skip)).Distinct().ToList();
        return new AppInput(definition, blocks, [.. dlls.Select(d => d.Path)], BinIndex.Sources(dlls));
    }

    static Result<Architecture> Definition(BlockEntry entry, IReadOnlyList<BinDll> app, AppLoader loader) {
        if (entry.Form != EntryForm.App) {
            return loader.CreateArchitecture(entry.Type!);
        }
        var classes = AppClass.Leaves([.. app.SelectMany(d => d.AppClasses)]);
        if (AppClassError(classes) is { } error) {
            return error;
        }
        var appClass = classes[0];
        return loader.ArchitectureOfApp(appClass.Type, app.First(d => d.AppClasses.Contains(appClass)).Path);
    }

    static Error? AppClassError(IReadOnlyList<AppClass> classes) {
        var noConstructor = classes.Where(c => !c.HasEmptyConstructor).ToList();
        if (noConstructor.Count > 0) {
            return new Error(LoadAppCodes.NoEmptyConstructor,
                $"The app class {Names(noConstructor)} has no public constructor with no parameters, "
                + "so the engine cannot make it and read its architecture. Add a public constructor with no parameters.");
        }
        return classes.Count switch {
            0 => new Error(LoadAppCodes.NoAppClass, "The app has no app class. "
                + "Add one class that inherits App<TPack>, for example App<Vsa<WebHost>>."),
            > 1 => new Error(LoadAppCodes.ManyAppClasses,
                $"The app has more than one app class: {Names(classes)}. Keep one, or make one inherit the other."),
            _ => null,
        };
    }

    static string Names(IEnumerable<AppClass> classes) =>
        string.Join(", ", classes.Select(c => c.Type.FullName).Order(StringComparer.Ordinal));

    static string[] Names(string list) => list.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
