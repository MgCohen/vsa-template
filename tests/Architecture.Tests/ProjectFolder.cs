namespace EngineCore.Tests;

using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;

sealed class ProjectFolder : IDisposable {
    ProjectFolder(string block) {
        Root = block;
        Output = block + "-out";
        Directory.CreateDirectory(Output);
        Write("global.json", "{}");
    }

    public string Root { get; }

    public string Output { get; }

    public static ProjectFolder Create() =>
        new(Path.Combine(Path.GetTempPath(), "umpire-project-" + Guid.NewGuid().ToString("N")[..8]));

    public string Write(string relativePath, string text) {
        var path = Path.Combine(Root, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text);
        return path;
    }

    public void Compile(string assemblyName, string outputFolder, params string[] sources) =>
        Emit(assemblyName, outputFolder, sources, path => path);

    public void CompileWithRelativeDocuments(string assemblyName, string outputFolder, params string[] sources) =>
        Emit(assemblyName, outputFolder, sources, path => @".\" + Path.GetRelativePath(Root, path).Replace('/', '\\'));

    void Emit(string assemblyName, string outputFolder, string[] sources, Func<string, string> documentName) {
        Directory.CreateDirectory(outputFolder);
        var trees = sources.Select(path => CSharpSyntaxTree.ParseText(
            SourceText.From(File.ReadAllText(path), Encoding.UTF8), new CSharpParseOptions(LanguageVersion.Latest), documentName(path)));
        var compilation = CSharpCompilation.Create(assemblyName, trees, References(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
        using var dll = File.Create(Path.Combine(outputFolder, assemblyName + ".dll"));
        using var pdb = File.Create(Path.Combine(outputFolder, assemblyName + ".pdb"));
        var result = compilation.Emit(dll, pdb, options: new EmitOptions(debugInformationFormat: DebugInformationFormat.PortablePdb));
        if (!result.Success) {
            throw new InvalidOperationException(string.Join(Environment.NewLine, result.Diagnostics));
        }
    }

    public void Dispose() {
        foreach (var folder in new[] { Root, Output }) {
            try {
                Directory.Delete(folder, recursive: true);
            } catch (Exception e) when (e is IOException or UnauthorizedAccessException) {
                TestContext.Progress.WriteLine($"The test could not delete {folder}: {e.Message}");
            }
        }
    }

    static IEnumerable<MetadataReference> References() =>
        ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
            .Append(typeof(Umpire.Kernels.Module).Assembly.Location)
            .Append(typeof(Microsoft.Extensions.DependencyInjection.IServiceCollection).Assembly.Location)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(path => MetadataReference.CreateFromFile(path));
}
