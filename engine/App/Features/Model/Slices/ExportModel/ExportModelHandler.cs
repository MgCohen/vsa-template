namespace Model.Slices.ExportModel;

using Facts.Domain.Shared;
using Facts.Infrastructure;
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

sealed class ExportModelHandler(RunOptions options) : CommandHandler<ExportModelCommand, ExportModelResult> {
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
        return new ExportModelResult([.. Setup(settings, FolderAliasConfig.Of(config, block).BadKeys), .. problems], state.Folder);
    }

    IEnumerable<Finding> Setup(BlockSettings settings, IReadOnlyList<string> badAliases) {
        var found = options.References.Where(folder => !Directory.Exists(folder)).Select(folder => new Finding(
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

    static Finding NoFacts(string assembly, string reason) => new(DataCodes.NoSnapshot, FindingSeverity.Warning,
        $"The check has no facts of the types of {assembly}: {reason}. A use of these types is not checked. "
        + "Build the project again, for example with dotnet build --no-incremental.");
}
