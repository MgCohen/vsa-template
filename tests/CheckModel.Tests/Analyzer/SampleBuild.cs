namespace CheckModel.Tests;

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Umpire.CheckModel;
using Umpire.CheckModel.Analyzer;

sealed class SampleBuild : IDisposable {
    static readonly ImmutableArray<MetadataReference> runtime = [..
        ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
            .Where(f => Path.GetFileName(f).StartsWith("System.", StringComparison.Ordinal) || Path.GetFileName(f) is "netstandard.dll")
            .Select(f => MetadataReference.CreateFromFile(f))];

    readonly TempState temp = new();

    public SampleBuild(params string[] projects) {
        State.Save(Samples.Layers);
        State.Save(BlockSettings.Of("../..", projects.Length > 0 ? projects : ["Sample"]));
    }

    public string Block => temp.Block;
    public StateFolder State => temp.State;
    public Dictionary<string, string> Options { get; } = [];

    public string Write(string path, string text) {
        var file = Path.Combine(Block, path);
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file, text);
        return file;
    }

    public CSharpCompilation Compile(string project, params string[] paths) => Compile(project, [], paths);

    public CSharpCompilation Compile(string project, IReadOnlyList<Compilation> references, params string[] paths) => CSharpCompilation.Create(
        project,
        paths.Select(path => Path.Combine(Block, path)).Select(file => {
            using var stream = File.OpenRead(file);
            return CSharpSyntaxTree.ParseText(SourceText.From(stream, checksumAlgorithm: SourceHashAlgorithm.Sha256), path: file);
        }),
        [.. runtime, .. references.Select(r => r.ToMetadataReference())],
        new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

    public IReadOnlyList<Diagnostic> Analyze(Compilation compilation) {
        var options = new AnalyzerOptions([], new SampleOptions(Options));
        var run = compilation.WithAnalyzers([new CheckAnalyzer()], options);
        var diagnostics = run.GetAnalyzerDiagnosticsAsync().GetAwaiter().GetResult();
        Assert.That(diagnostics.Where(d => d.Id == "AD0001"), Is.Empty);
        return diagnostics;
    }

    public Snapshot Saved(string project = "Sample") => Snapshot.Read(File.ReadAllText(State.SnapshotFile(project)));

    public void Dispose() => temp.Dispose();

    sealed class SampleOptions(IReadOnlyDictionary<string, string> values) : AnalyzerConfigOptionsProvider {
        public override AnalyzerConfigOptions GlobalOptions { get; } = new SampleConfig(values);

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => GlobalOptions;

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => GlobalOptions;
    }

    sealed class SampleConfig(IReadOnlyDictionary<string, string> values) : AnalyzerConfigOptions {
        public override bool TryGetValue(string key, out string value) {
            value = values.GetValueOrDefault(key) ?? "";
            return values.ContainsKey(key);
        }
    }
}
