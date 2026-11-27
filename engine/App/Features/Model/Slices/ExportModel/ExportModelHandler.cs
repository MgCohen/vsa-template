namespace Model.Slices.ExportModel;

using Facts.Domain.Shared;
using Facts.Domain.Shared.Dll;
using Facts.Infrastructure;
using Model.Infrastructure;
using LoadApp.Domain.Shared;
using Operations.Domain.Shared;
using Operations.Infrastructure;
using Umpire.Architecture;
using Umpire.CheckModel;
using Umpire.CheckModel.Export;
using Umpire.Config;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Commands;
using Data = Umpire.CheckModel;
using Finding = Operations.Domain.Shared.Finding;

sealed class ExportModelHandler(RunOptions options, IFactSource<DllQuery, IReadOnlyList<DllFacts>> dlls)
    : CommandHandler<ExportModelCommand, ExportModelResult> {
    const string LibraryFolder = "libraries";

    public override Task<Result<ExportModelResult>> HandleAsync(IEnvelope<ExportModelCommand> envelope) =>
        Task.FromResult<Result<ExportModelResult>>(Export(envelope.Message));

    ExportModelResult Export(ExportModelCommand command) {
        var app = command.App;
        var block = app.Blocks.Current.Folder;
        var state = StateFolder.At(command.State);
        var places = SnapshotList.Read(command.Snapshots);
        var projects = new HashSet<string>(app.Projects, StringComparer.OrdinalIgnoreCase);
        var own = places.Where(place => projects.Contains(place.Project)).ToList();
        var resolved = new ResolvedArchitecture(app.Architecture);
        var config = UmpireConfig.Find(block);
        var problems = new List<Finding>();
        var libraries = Libraries(command, places, projects, block, problems);
        libraries.AddRange(FromDlls(command, places, projects, resolved, state, problems));
        var input = new BlockInput(block, app.Projects) {
            Libraries = [.. libraries.Select(library => library.Project)],
            InnerBlocks = app.Blocks.InnerFolders,
            IgnoredFolders = options.IgnoredFolders,
            FolderAliases = [.. FolderAliasConfig.Of(config, block).Aliases.Select(alias => new Data.FolderAlias(alias.Folder, alias.As))],
            DefinitionSources = DefinitionSources.Of(resolved, block, BinIndex.Sources(command.Bin.Dlls).ToDictionary(s => s.Key, s => s.Value.File)),
            InputFiles = [.. config.FilePath is { } file ? new[] { file } : [], .. SourceFiles.ProjectFiles(block)],
            Config = config.Values(),
            Snapshots = own,
        };
        var before = Before(state);
        var mode = command.Mode == ReportMode.Error ? Data.CheckMode.Error : Data.CheckMode.Warn;
        var settings = BlockExport.Of(state, input) with { Mode = mode };
        state.Save(DefinitionExport.Of(resolved), settings);
        foreach (var library in libraries) {
            state.SaveLibrary(library);
        }
        SnapshotFiles.Complete(state, own);
        SnapshotFiles.RemoveOld(before, own);
        return new ExportModelResult(
            [.. Setup(options.References, settings, FolderAliasConfig.Of(config, block).BadKeys), .. problems], state.Folder);
    }

    internal static IEnumerable<Finding> Setup(IReadOnlyList<string> references, BlockSettings settings, IReadOnlyList<string> badAliases) {
        var found = references.Where(folder => !Directory.Exists(folder)).Select(folder => new Finding(
            RuleCodes.MissingReference, FindingSeverity.Warning,
            $"The reference folder {folder} does not exist, so the checks do not read its DLLs. "
            + "Make the folder, or remove it from umpire.check.references (--references)."))
            .Concat(badAliases.Select(key => new Finding(RuleCodes.BadFolderAlias, FindingSeverity.Warning,
                $"The folder alias {key} in {FolderAliasConfig.Key} is not a list of folders, so the checks do not use it. "
                + $"Write it as \"{key}\" = [\"\", \"App\"], with quotes on the key.")));
        foreach (var finding in found) {
            if (settings.SeverityOf(finding.Code, Data.Severity.Warning) is { } severity) {
                yield return finding with { Severity = SeverityOf(severity) };
            }
        }
    }

    static FindingSeverity SeverityOf(Data.Severity severity) => severity switch {
        Data.Severity.Error => FindingSeverity.Error,
        Data.Severity.Warning => FindingSeverity.Warning,
        _ => FindingSeverity.Info,
    };

    static string? Shipped(string assembly) {
        var folder = Path.GetDirectoryName(typeof(ExportModelHandler).Assembly.Location);
        var file = folder is null ? null : Path.Combine(folder, LibraryFolder, assembly + ".json");
        return file is not null && File.Exists(file) ? file : null;
    }

    static IReadOnlyList<SnapshotPlace> Before(StateFolder state) {
        try {
            return File.Exists(state.BlockFile)
                ? [.. state.ReadBlock().Snapshots.Select(place => place with { File = state.SnapshotFile(place.Project, place.File) })]
                : [];
        } catch (Exception e) when (e is IOException or UnauthorizedAccessException or FormatException) {
            return [];
        }
    }

    static List<Data.Snapshot> Libraries(
        ExportModelCommand command, IReadOnlyList<SnapshotPlace> places, HashSet<string> projects, string block, List<Finding> problems) {
        var libraries = new List<Data.Snapshot>();
        var ofProjects = new HashSet<string>(
            command.Bin.Dlls.Where(dll => dll.IsProject).Select(dll => dll.Name), StringComparer.OrdinalIgnoreCase);
        foreach (var dll in command.App.Dlls) {
            var name = Path.GetFileNameWithoutExtension(dll);
            if (projects.Contains(name)) {
                continue;
            }
            try {
                var place = places.FirstOrDefault(p => string.Equals(p.Project, name, StringComparison.OrdinalIgnoreCase));
                if (place is not null && File.Exists(place.File)) {
                    libraries.Add(LibraryExport.Of(Data.Snapshot.Read(File.ReadAllText(place.File)), FileMark.Of(block, dll)));
                } else if (Shipped(name) is { } shipped) {
                    libraries.Add(Data.Snapshot.Read(File.ReadAllText(shipped)) with { Source = FileMark.Of(block, dll) });
                } else if (ofProjects.Contains(name)) {
                    problems.Add(NoFacts(name, "the list of the snapshots has no snapshot of the project"));
                }
            } catch (Exception e) when (e is IOException or UnauthorizedAccessException or FormatException) {
                problems.Add(NoFacts(name, e.Message));
            }
        }
        return libraries;
    }

    List<Data.Snapshot> FromDlls(
        ExportModelCommand command, IReadOnlyList<SnapshotPlace> places, HashSet<string> projects, ResolvedArchitecture resolved,
        StateFolder state, List<Finding> problems) {
        var block = command.App.Blocks.Current.Folder;
        var externals = resolved.Parts.Where(p => p.IsExternal).Select(p => p.Assembly!).Distinct().ToList();
        var bin = command.Bin.Dlls.ToDictionary(dll => dll.Path, StringComparer.OrdinalIgnoreCase);
        var unread = command.App.Dlls.Where(dll => bin.TryGetValue(dll, out var found) && !found.IsProject
            && !places.Any(p => string.Equals(p.Project, found.Name, StringComparison.OrdinalIgnoreCase) && File.Exists(p.File))).ToList();
        var own = unread.Where(dll => projects.Contains(bin[dll].Name)).ToList();
        var named = unread.Where(dll => !projects.Contains(bin[dll].Name) && Shipped(bin[dll].Name) is null
            && externals.Any(e => bin[dll].Name == e || bin[dll].Name.StartsWith(e + ".", StringComparison.Ordinal))).ToList();
        var read = dlls.Read(new DllQuery([.. own, .. named], [.. command.Bin.Dlls.Select(d => d.Path)], options.SourceRoot))
            .ToDictionary(facts => facts.Path, StringComparer.OrdinalIgnoreCase);
        foreach (var dll in own) {
            if (File.Exists(state.SnapshotFile(bin[dll].Name))) {
                File.Delete(state.SnapshotFile(bin[dll].Name));
            }
            if (!read.ContainsKey(dll)) {
                problems.Add(NoFacts(bin[dll].Name, $"the DLL {dll} or its PDB cannot be read"));
            }
        }
        foreach (var snapshot in DllSnapshots.Of([.. own.Where(read.ContainsKey).Select(dll => read[dll])], block)) {
            state.Save(snapshot);
        }
        return [.. named.Where(read.ContainsKey).Select(dll => LibraryExport.Of(DllSnapshots.Of([read[dll]], block)[0], FileMark.Of(block, dll)))];
    }

    static Finding NoFacts(string assembly, string reason) => new(DataCodes.NoSnapshot, FindingSeverity.Warning,
        $"The check has no facts of the types of {assembly}: {reason}. A use of these types is not checked. "
        + "Build the project again, for example with dotnet build --no-incremental.");
}
