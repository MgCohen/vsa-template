using Tests.Shared;

namespace CheckModel.Tests;

using System.Text.RegularExpressions;
using BuiltApp.Domain.Shared;
using Umpire.Architecture;
using Umpire.CheckModel.Export;
using Data = Umpire.CheckModel;

sealed partial class ModelRun : IDisposable {
    readonly EngineRun engine;
    readonly List<Data.StateFolder> states = [];

    public ModelRun(EngineRun engine, params string[] otherProjects) {
        this.engine = engine;
        State = NewState();
        var own = engine.App.Select(dll => dll.Name).Except(otherProjects).ToList();
        var others = otherProjects.Length == 0 ? [] : new[] { NewState() };
        var known = engine.Check.App.Facts.ToLookup(f => f.FullName, f => f.Assembly);
        var bases = engine.Check.App.Facts.ToDictionary(f => f.FullName, f => f.Bases);
        var libraries = engine.Others.Where(dll => dll.Types.Count > 0).ToList();
        var resolved = new ResolvedArchitecture(engine.Definition);
        var definition = DefinitionExport.Of(resolved);
        var input = new BlockInput(engine.Block, own) {
            Libraries = [.. libraries.Select(dll => dll.Name)],
            InnerBlocks = engine.Blocks.InnerFolders,
            OtherStates = others,
            IgnoredFolders = engine.IgnoredFolders,
            FolderAliases = [.. engine.Aliases.Select(a => new Data.FolderAlias(a.Folder, a.As))],
            DefinitionSources = DefinitionSources.Of(
                resolved, engine.Block, engine.Sources.ToDictionary(s => s.Key, s => s.Value.File)),
            InputFiles = [
                .. engine.Config.FilePath is { } config ? new[] { config } : [],
                .. engine.App.Select(dll => ProjectOf(engine.Block, dll.Name)).Distinct()],
            Config = engine.Config.Values(),
        };
        State.Save(definition, BlockExport.Of(State, input) with { Mode = Data.CheckMode.Error });
        foreach (var other in others) {
            other.Save(definition, BlockExport.Of(other, input with { Projects = otherProjects, OtherStates = [] }) with { Mode = Data.CheckMode.Error });
        }
        foreach (var dll in libraries) {
            var types = dll.Types.Select(t => new LibraryType(t.FullName, t.Source.File, t.Source.Line, t.Source.Column,
                [.. (bases.GetValueOrDefault(t.FullName) ?? []).Select(b => new Data.TypeName(b, known[b].FirstOrDefault() ?? ""))]));
            var library = LibraryExport.Of(dll.Name, Data.FileMark.Of(engine.Block, dll.Path), types);
            foreach (var state in states) {
                state.SaveLibrary(library);
            }
        }
        for (var pass = engine.App.Count > 1 ? 0 : 1; pass < 2; pass++) {
            foreach (var dll in InBuildOrder(engine.App)) {
                var output = Compile(ProjectOf(engine.Block, dll.Name), dll.Name, otherProjects.Contains(dll.Name) ? others[0] : State);
                if (pass == 1) {
                    Read(output);
                }
            }
        }
    }

    public Data.StateFolder State { get; }

    public List<Data.Finding> Diagnostics { get; } = [];

    public void Dispose() {
        foreach (var state in states) {
            TempFolder.Delete(state.Folder);
        }
    }

    static List<BinDll> InBuildOrder(IReadOnlyList<BinDll> dlls) {
        var left = dlls.OrderBy(dll => dll.Name, StringComparer.Ordinal).ToList();
        var ordered = new List<BinDll>();
        while (left.Count > 0) {
            var next = left.FirstOrDefault(dll => !dll.References.Any(name => left.Any(other => other.Name == name)))
                ?? left[0];
            ordered.Add(next);
            left.Remove(next);
        }
        return ordered;
    }

    static string ProjectOf(string block, string name) {
        if (Directory.GetFiles(block, $"{name}.csproj", SearchOption.AllDirectories) is [var own, ..]) {
            return own;
        }
        var folder = block;
        while (Directory.GetFiles(folder, "*.csproj").Length == 0) {
            folder = Path.GetDirectoryName(folder)!;
        }
        return Directory.GetFiles(folder, "*.csproj")[0];
    }

    [GeneratedRegex(@"^\s*(?<file>[^(\r\n]+)\((?<line>\d+),(?<column>\d+)\): (?<severity>error|warning) (?<code>UMP02\d+): (?<text>.*) \[[^\]]+\]\s*$",
        RegexOptions.Multiline)]
    private static partial Regex DiagnosticLine();

    Data.StateFolder NewState() {
        var state = Data.StateFolder.At(TempFolder.Create());
        states.Add(state);
        return state;
    }

    string Compile(string project, string name, Data.StateFolder state) {
        var output = TempFolder.Create();
        var result = Dotnet.Run(Path.GetDirectoryName(project)!, null,
            "build", project, "-t:Compile", "--no-restore", "-nologo", "-p:BuildProjectReferences=false", "-p:UmpireCheck=false",
            $"-p:IntermediateOutputPath={output}/obj/", "-p:ProduceReferenceAssembly=false",
            $"-p:CustomAfterMicrosoftCommonTargets={Repo.Path("tests", "CheckModel.Tests", "Parity", "Inject.targets")}",
            $"-p:UmpireCheckState={state.Folder}",
            $"-p:UmpireModelAnalyzers={Path.TrimEndingDirectorySeparator(AppContext.BaseDirectory)}/");
        TempFolder.Delete(output);
        Assert.That(state.SnapshotFile(name), Does.Exist, result.Output);
        return result.Output;
    }

    void Read(string output) {
        string[] codes = ["AD0001", Data.DataCodes.NoModel, Data.DataCodes.NoSnapshot, Data.DataCodes.OldSnapshot, Data.DataCodes.NotSaved];
        Assert.That(codes.Where(code => output.Contains(code)), Is.Empty, output);
        Diagnostics.AddRange(DiagnosticLine().Matches(output).Select(m => new Data.Finding(
            m.Groups["code"].Value,
            m.Groups["severity"].Value == "error" ? Data.Severity.Error : Data.Severity.Warning,
            m.Groups["text"].Value, m.Groups["file"].Value, int.Parse(m.Groups["line"].Value), int.Parse(m.Groups["column"].Value))).Distinct());
    }
}
