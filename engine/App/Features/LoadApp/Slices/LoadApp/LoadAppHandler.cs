using Blocks.Domain.Shared;
using Facts.Domain.Shared;
using Facts.Infrastructure;
using LoadApp.Domain.Shared;
using Operations.Domain.Shared;
using Operations.Infrastructure;
using Umpire.Architecture;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Commands;

namespace LoadApp.Slices.LoadApp;

sealed class LoadAppHandler(RunOptions options, IFactSource<DefinitionQuery, IDefinitionLoader> definitions)
    : CommandHandler<LoadAppCommand, AppInput> {
    public override Task<Result<AppInput>> HandleAsync(IEnvelope<LoadAppCommand> envelope) =>
        Task.FromResult(Load(envelope.Message));

    Result<AppInput> Load(LoadAppCommand command) {
        var blocks = command.Blocks;
        var root = blocks.Current.Folder;
        var bin = command.Bin;
        var loader = definitions.Read(new DefinitionQuery(bin.Bin, options.References));
        var skip = Names(command.Skip);
        var app = bin.AppDlls(root, skip);
        if (app.Count == 0) {
            return new Error(LoadAppCodes.NoAppAssembly, $"No assembly in {bin.Bin} has source files under the block folder {root}. "
                + "Build the host project of the app, and give its output folder to --bin.");
        }
        var searched = app.Select(dll => dll with { Unloaded = [.. dll.Unloaded.Where(u => !IsSkipped(u.Source, root))] }).ToList();
        var found = Definition(blocks, searched, [.. bin.Definitions.Where(d => !IsSkipped(d.Source, root))], loader,
            DefinitionFolder.Of(root, options.IgnoredFolders).HasSource);
        if (found.Error is { } error) {
            return error;
        }
        var definition = found.Value;
        var externals = new ResolvedArchitecture(definition).Parts.Where(p => p.IsExternal).Select(p => p.Assembly!).Distinct().ToList();
        var dlls = app.Concat(bin.ExternalDlls(externals)).Concat(bin.ReferencedDlls(app, skip)).Distinct().ToList();
        return new AppInput(definition, blocks, [.. dlls.Select(d => d.Path)], BinIndex.Sources(dlls)) {
            Findings = command.IsUsed || !options.ProjectChecks ? [] : Unloaded(root, bin),
            Projects = [.. app.Select(dll => dll.Name)],
        };
    }

    List<Finding> Unloaded(string root, BinIndex bin) => [.. DefinitionFolder.Below(root, options.IgnoredFolders)
        .Where(folder => !bin.Dlls.Any(dll => dll.HasSourceUnder(folder)))
        .Select(folder => new Finding(LoadAppCodes.UnloadedDefinition, FindingSeverity.Warning,
            $"The folder {folder} has definition source files, and no DLL of the bin has them, so no check uses them. "
            + $"If the folder has no project, add one, for example <Name>.{DefinitionType.FolderName}.csproj. If the folder is below "
            + "a nested project, reference the nested project from the root project. If the files are not a definition of this app, add the folder to "
            + "umpire.check.ignored_folders.", folder))];

    bool IsSkipped(string? source, string root) =>
        source is not null && DefinitionFolder.IsSkippedSource(root, source, options.IgnoredFolders);

    internal static Result<Architecture> Definition(
        BlockTree blocks, IReadOnlyList<BinDll> app, IReadOnlyList<DefinitionType> definitions, IDefinitionLoader loader,
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

    static string[] Names(string list) => [.. list.Split(',').Select(name => name.Trim()).Where(name => name.Length > 0)];
}
