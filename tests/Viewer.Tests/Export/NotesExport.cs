using VSA.Architecture.Data;

namespace Viewer.Tests.Export;

public static class NotesExport
{
    private static readonly Lazy<string> folder = new(Make);

    public static string Folder => folder.Value;

    public static string SnapshotFile => Path.Combine(Folder, DataFiles.Snapshot("Notes"));

    public static string SnapshotJson => File.ReadAllText(SnapshotFile);

    public static string ArchitectureFile => Path.Combine(Folder, DataFiles.Architecture("Vsa"));

    public static string ArchitectureJson => File.ReadAllText(ArchitectureFile);

    public static DotnetRun Engine(params string[] arguments)
    {
        return EngineProcess.Run(EngineProcess.Metadata("NotesRoot"), EngineProcess.Metadata("NotesOutput"), arguments);
    }

    private static string Make()
    {
        string output = Path.Combine(Path.GetTempPath(), $"vsa-viewer-{Environment.ProcessId}");
        DotnetRun run = Engine("--export", output);
        if (run.ExitCode != 0)
        {
            throw new InvalidOperationException($"The rule engine stopped with exit code {run.ExitCode}. {run.Output}");
        }
        return output;
    }
}
