using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;

namespace Architecture.Tests;

sealed class UnityProjectFolder : IDisposable {
    public const string VersionFile = "ProjectSettings/ProjectVersion.txt";

    UnityProjectFolder(string root) {
        Root = root;
        Output = root + "-out";
        Directory.CreateDirectory(Output);
        Write("global.json", "{}");
        Write(VersionFile, "m_EditorVersion: 6000.6.0f1");
    }

    public string Root { get; }

    public string Output { get; }

    public static UnityProjectFolder Create() =>
        new(Path.Combine(Path.GetTempPath(), "vsa-unity-" + Guid.NewGuid().ToString("N")[..8]));

    public string Write(string relativePath, string text) {
        var path = Path.Combine(Root, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text);
        return path;
    }

    public void Compile(string assemblyName, string outputFolder, params string[] sources) {
        Directory.CreateDirectory(outputFolder);
        var trees = sources.Select(path => CSharpSyntaxTree.ParseText(
            SourceText.From(File.ReadAllText(path), Encoding.UTF8), new CSharpParseOptions(LanguageVersion.Latest), path));
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
            .Append(typeof(VSA.Module.Module).Assembly.Location)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(path => MetadataReference.CreateFromFile(path));
}
