using Blocks.Domain.Shared;
using BuiltApp.Domain.Shared;
using BuiltApp.Infrastructure;
using LoadApp.Domain.Shared;
using Steps.Infrastructure;
using Umpire.Architecture;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Commands;

namespace LoadApp.Slices.LoadApp;

sealed class LoadAppHandler(RunOptions options) : CommandHandler<LoadAppCommand, AppInput> {
    public override Task<Result<AppInput>> HandleAsync(IEnvelope<LoadAppCommand> envelope) =>
        Task.FromResult(Load(envelope.Message));

    Result<AppInput> Load(LoadAppCommand command) {
        var blocks = command.Blocks;
        var root = blocks.Current.Folder;
        var bin = command.Bin;
        var loader = new AppLoader(bin.Bin, options.References);
        var skip = Names(command.Skip);
        var app = bin.AppDlls(root, skip);
        if (app.Count == 0) {
            return new Error(LoadAppCodes.NoAppAssembly, $"No assembly in {bin.Bin} has source files under the block folder {root}. "
                + "Build the host project of the app, and give its output folder to --bin.");
        }
        var found = Definition(blocks, app, [.. bin.Definitions.Where(d => !IsSkipped(d, root))], loader, DefinitionFolder.Of(root).HasSource);
        if (found.Error is { } error) {
            return error;
        }
        var definition = found.Value;
        var externals = new ResolvedArchitecture(definition).Parts.Where(p => p.IsExternal).Select(p => p.Assembly!).Distinct().ToList();
        var dlls = app.Concat(bin.ExternalDlls(externals)).Concat(bin.ReferencedDlls(app, skip)).Distinct().ToList();
        return new AppInput(definition, blocks, [.. dlls.Select(d => d.Path)], BinIndex.Sources(dlls));
    }

    bool IsSkipped(DefinitionType definition, string root) {
        if (definition.Source is not { } source) {
            return false;
        }
        var relative = Path.GetRelativePath(root, source);
        return ReadOptions.IsIgnored(relative, options.IgnoredFolders)
            || (!relative.StartsWith("..", StringComparison.Ordinal)
                && relative.Split(Path.DirectorySeparatorChar).Contains(DefinitionFolder.Tests, StringComparer.OrdinalIgnoreCase));
    }

    internal static Result<Architecture> Definition(
        BlockTree blocks, IReadOnlyList<BinDll> app, IReadOnlyList<DefinitionType> definitions, AppLoader loader,
        bool hasDefinitionSource = false) {
        var entry = blocks.Current;
        var found = DefinitionSearch.Find(entry, app, definitions, blocks.Inner, hasDefinitionSource);
        if (found.Error is { } error) {
            return error;
        }
        var packed = found.Value == entry.Type ? entry.Packed : null;
        return packed is not null && !loader.TakesPacked(found.Value)
            ? new Error(LoadAppCodes.NoPackedConstructor,
                $"The block architecture {found.Value.FullName} has no public constructor with one Architecture parameter, "
                + $"so the engine cannot check the kernel of the pack with {packed.FullName}. "
                + "Use the same Umpire version for the pack and the engine (see UMP030401).")
            : loader.CreateArchitecture(found.Value, packed);
    }

    static string[] Names(string list) => list.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
