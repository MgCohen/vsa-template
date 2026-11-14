using Tests.Shared;
using VSA.Architecture.Data;

namespace Viewer.Tests.Export;

public static class MvvmApp
{
    private static readonly Lazy<string> folder = new(Make);

    public static string Folder => folder.Value;

    public static string ArchitectureJson => File.ReadAllText(Path.Combine(Folder, DataFiles.Architecture("Mvvm")));

    public static string SnapshotJson => File.ReadAllText(Path.Combine(Folder, DataFiles.Snapshot("MvvmApp", "Mvvm")));

    public static DotnetRun Engine(params string[] arguments)
    {
        return EngineProcess.Run(EngineProcess.Metadata("MvvmRoot"), EngineProcess.Metadata("MvvmOutput"), arguments);
    }

    public static DotnetRun Check(string architectureJson, params string[] arguments)
    {
        string file = Path.Combine(TempFolder.Create(), DataFiles.Architecture("edited"));
        File.WriteAllText(file, architectureJson);
        return Engine(["--definition", file, .. arguments]);
    }

    public static IReadOnlyList<string> Errors(DotnetRun run)
    {
        return [.. run.Output.Split('\n').Select(l => l.Trim()).Where(l => l.Contains(": error ", StringComparison.Ordinal))];
    }

    private static string Make()
    {
        string output = TempFolder.Create();
        DotnetRun run = Engine("--export", output);
        if (!run.Output.Contains("VSA3005", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"The rule engine did not export the MVVM app. {run.Output}");
        }
        return output;
    }
}
