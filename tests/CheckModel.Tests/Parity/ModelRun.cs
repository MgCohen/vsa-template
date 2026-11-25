using Tests.Shared;

namespace CheckModel.Tests;

using System.Collections.Concurrent;
using Umpire.Architecture;
using Umpire.CheckModel.Export;
using Data = Umpire.CheckModel;

static class ModelRun {
    static readonly ConcurrentDictionary<string, Lazy<Data.StateFolder>> states = new();

    public static Data.StateFolder StateOf(EngineRun engine) =>
        states.GetOrAdd(engine.Block, _ => new(() => Make(engine))).Value;

    public static void Clear() {
        foreach (var state in states.Values.Where(s => s.IsValueCreated)) {
            TempFolder.Delete(state.Value.Folder);
        }
        states.Clear();
    }

    static Data.StateFolder Make(EngineRun engine) {
        var state = Data.StateFolder.At(TempFolder.Create());
        state.Save(DefinitionExport.Of(new ResolvedArchitecture(engine.Definition)));
        state.Save(BlockExport.Of(
            state, engine.Block, [.. engine.App.Select(dll => dll.Name)], engine.Blocks.InnerFolders, [], [], [], engine.Config.Values()));
        foreach (var dll in engine.Others.Where(dll => dll.Types.Count > 0)) {
            state.SaveLibrary(LibraryExport.Of(dll.Name, dll.Types.Select(t =>
                new LibraryType(t.FullName, t.Source.File, t.Source.Line, t.Source.Column))));
        }
        foreach (var dll in engine.App) {
            Compile(ProjectOf(engine.Block, dll.Name), dll.Name, state);
        }
        return state;
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

    static void Compile(string project, string name, Data.StateFolder state) {
        var output = TempFolder.Create();
        var result = Dotnet.Run(Path.GetDirectoryName(project)!, null,
            "build", project, "-t:Compile", "--no-restore", "-nologo", "-p:BuildProjectReferences=false", "-p:UmpireCheck=false",
            $"-p:IntermediateOutputPath={output}/obj/", "-p:ProduceReferenceAssembly=false",
            $"-p:CustomAfterMicrosoftCommonTargets={Repo.Path("tests", "CheckModel.Tests", "Parity", "Inject.targets")}",
            $"-p:UmpireCheckState={state.Folder}",
            $"-p:UmpireModelAnalyzers={Path.TrimEndingDirectorySeparator(AppContext.BaseDirectory)}/");
        TempFolder.Delete(output);
        Assert.That(state.SnapshotFile(name), Does.Exist, result.Output);
    }
}
