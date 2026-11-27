namespace Model.Infrastructure;

using Facts.Domain.Shared.Compile;
using Facts.Infrastructure;
using Umpire.CheckModel;
using Umpire.CheckModel.Export;
using Umpire.Config;

public sealed class ModelRun {
    const string MappedRoot = "/_/";

    readonly CompileProject project;
    readonly Loaded? loaded;
    readonly IFactSource<ModelQuery, IModelFacts>? unloaded;

    ModelRun(CompileProject project, Loaded? loaded, IReadOnlyList<Finding> notices, IFactSource<ModelQuery, IModelFacts>? unloaded = null) {
        this.project = project;
        this.loaded = loaded;
        this.unloaded = unloaded;
        Notices = notices;
    }

    public IReadOnlyList<Finding> Notices { get; }

    public bool IsLoaded => loaded is not null;

    public static ModelRun Start(CompileProject project, IFactSource<ModelQuery, IModelFacts> source) {
        try {
            return SavesOnly(project.Model)
                ? new ModelRun(project, null, [], source)
                : new ModelRun(project, Load(project, source), [], source);
        } catch (Exception e) when (e is IOException or UnauthorizedAccessException or FormatException or InvalidOperationException
            or ArgumentException or NotSupportedException) {
            return new ModelRun(project, null, [new Finding(DataCodes.NoModel, Severity.Warning, $"The check model cannot load: {e.Message}")]);
        }
    }

    public IReadOnlyList<Finding> Check(TypeName name) {
        if (loaded is not { } run || run.App.Find(name) is not { } type || type.Project != project.Name) {
            return [];
        }
        var report = new Report();
        LocationCheck.Run(run.App, [type], report);
        ContentCheck.Run(run.App, [type], report);
        ReferenceCheck.Run(run.App, [type], report);
        return [.. report.Findings.Select(finding => finding with { Severity = SeverityOf(run.App, finding) })];
    }

    public IReadOnlyList<Finding> End(bool hasErrors) {
        if (loaded is not { } run) {
            return unloaded is not null && project.Model.Saves && !hasErrors && SavesOnly(project.Model)
                ? [.. Notices, .. Save()]
                : Notices;
        }
        var app = run.App;
        var referenced = project.References;
        var findings = new List<Finding>();
        findings.AddRange(app.Problems.Where(p => p.IsState || p.IsLibrary || referenced.Contains(p.Project)).Select(p => Data(DataCodes.NoSnapshot, DataCheck.Text(p))));
        findings.AddRange(app.Trust.OldLibraries.Select(library => Data(DataCodes.OldSnapshot, DataCheck.Text(library))));
        findings.AddRange(app.Saved.Select(s => s.Snapshot.Project).Where(p => referenced.Contains(p) && !app.Trust.IsSure(p))
            .Select(p => Data(DataCodes.OldSnapshot, DataCheck.OldText(p))));
        if (DataCheck.BlockText(app) is { } text) {
            findings.Add(Data(DataCodes.OldSnapshot, text));
        }
        if (!project.Model.Saves || hasErrors) {
            return findings;
        }
        if (run.NotCompiled is null) {
            findings.Add(Data(DataCodes.NotSaved, $"The snapshot of {project.Name} is not saved: a folder of the project cannot be read."));
            return findings;
        }
        try {
            if (project.Model.State is { Length: > 0 } || project.Model.Snapshot is not { Length: > 0 } own) {
                run.State.Save(app.WithSurfaces(Full(run.Facts, run.Block, run.NotCompiled) with { Root = null }));
            } else {
                StateFolder.SaveSnapshot(own, app.WithSurfaces(Full(run.Facts, run.Block, run.NotCompiled)));
            }
        } catch (Exception e) when (e is IOException or UnauthorizedAccessException) {
            findings.Add(Data(DataCodes.NotSaved, $"The snapshot of {project.Name} is not saved: {e.Message}"));
        }
        return findings;
    }

    static bool SavesOnly(ModelSettings model) =>
        model.Saves && model.State is not { Length: > 0 } && (model.Snapshot is { Length: > 0 } || model.Library is { Length: > 0 });

    IReadOnlyList<Finding> Save() {
        var folder = FolderOf(project);
        if (folder is null) {
            return [Data(DataCodes.NotSaved, $"The snapshot of {project.Name} is not saved: the project has no source folder.")];
        }
        try {
            var facts = unloaded!.Read(new ModelQuery(project, folder));
            if (NotCompiled(facts, folder) is not { } notCompiled) {
                return [Data(DataCodes.NotSaved, $"The snapshot of {project.Name} is not saved: a folder of the project cannot be read.")];
            }
            var full = Full(facts, folder, notCompiled);
            if (project.Model.Library is { Length: > 0 } library) {
                StateFolder.SaveSnapshot(library, LibraryExport.Of(full, null, path => LibraryPath(path, project.Model.LibraryRoot)));
            }
            if (project.Model.Snapshot is { Length: > 0 } file) {
                StateFolder.SaveSnapshot(file, full);
            }
            return [];
        } catch (Exception e) when (e is IOException or UnauthorizedAccessException or FormatException or InvalidOperationException
            or ArgumentException or NotSupportedException) {
            return [Data(DataCodes.NotSaved, $"The snapshot of {project.Name} is not saved: {e.Message}")];
        }
    }

    static string LibraryPath(string path, string? root) => root is { Length: > 0 } && BlockPath.IsUnder(root, path)
        ? MappedRoot + BlockPath.Of(root, path)
        : path.Replace('\\', '/');

    static string? FolderOf(CompileProject project) =>
        project.Model.ProjectFolder is { Length: > 0 } given ? BlockPath.Full(given)
        : project.Model.SourceFolders.Count > 0 ? project.Model.SourceFolders[0]
        : null;

    static Loaded? Load(CompileProject project, IFactSource<ModelQuery, IModelFacts> source) {
        if (StateOf(project) is not { } state) {
            return null;
        }
        var settings = state.ReadBlock();
        if (!settings.Projects.Contains(project.Name, StringComparer.OrdinalIgnoreCase)) {
            return null;
        }
        var block = state.BlockOf(settings);
        var facts = source.Read(new ModelQuery(project, block));
        var app = AppModel.Load(state, settings, facts.Current, CheckModes.Parse(project.Model.Mode), project.Model.Target);
        return new Loaded(state, block, facts, app, project.Model.Saves ? NotCompiled(facts, block) : []);
    }

    static StateFolder? StateOf(CompileProject project) {
        var model = project.Model;
        if (model.State is { Length: > 0 } given) {
            return StateFolder.At(Path.Combine(model.ProjectFolder ?? "", given)) is { HasDefinition: true } state ? state : null;
        }
        if (model.Snapshot is { Length: > 0 } snapshot && StatePointer.Read(snapshot) is { HasDefinition: true } pointed) {
            return pointed;
        }
        if (project.ConfigPath is { Length: > 0 } file
            && Path.GetDirectoryName(Path.GetFullPath(file)) is { } folder
            && StateFolder.Of(folder, Configured(project)) is { HasDefinition: true } configured) {
            return configured;
        }
        return model.SourceFolders.Select(StateFolder.Above).FirstOrDefault(state => state is not null);
    }

    static string? Configured(CompileProject project) {
        try {
            return UmpireConfig.Parse(project.ConfigText).Value(StateFolder.Key);
        } catch (Exception e) when (e is not OperationCanceledException) {
            return null;
        }
    }

    static IReadOnlyList<string>? NotCompiled(IModelFacts facts, string block) {
        try {
            var compiled = new HashSet<string>(facts.Current.Snapshot.Files.Select(f => f.Path), StringComparer.OrdinalIgnoreCase);
            return facts.Folder is not { } folder ? [] : [.. SourceFiles.In(folder).Select(f => BlockPath.Of(block, f))
                .Where(f => !compiled.Contains(f)).OrderBy(f => f, StringComparer.Ordinal)];
        } catch (Exception e) when (e is IOException or UnauthorizedAccessException) {
            return null;
        }
    }

    Snapshot Full(IModelFacts facts, string block, IReadOnlyList<string> notCompiled) {
        var full = facts.Full();
        return full with {
            NotCompiled = notCompiled,
            Files = [.. full.Files.Select(file => Dated(file, BlockPath.In(block, file.Path)))],
            Root = BlockPath.Full(block),
            Target = project.Model.Target,
        };
    }

    static FileFacts Dated(FileFacts file, string path) {
        if (file.Hash.Length == 0) {
            return file;
        }
        var modified = File.GetLastWriteTimeUtc(path).Ticks;
        return file with { Modified = FileHash.Of(path) == file.Hash ? modified : 0 };
    }

    static Finding Data(string code, string text) => new(code, Severity.Warning, text);

    static Severity SeverityOf(AppModel app, Finding finding) =>
        finding is { Code: RuleCodes.Count, Severity: Severity.Error } && (app.Settings.Projects.Count > 1 || app.Settings.OtherStates.Count > 0)
            ? Severity.Warning
            : finding.Severity;

    sealed class Loaded(StateFolder state, string block, IModelFacts facts, AppModel app, IReadOnlyList<string>? notCompiled) {
        public StateFolder State { get; } = state;

        public string Block { get; } = block;

        public IModelFacts Facts { get; } = facts;

        public AppModel App { get; } = app;

        public IReadOnlyList<string>? NotCompiled { get; } = notCompiled;
    }
}
