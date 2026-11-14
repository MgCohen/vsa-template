using VSA.Architecture.Data;

namespace ArchitectureViewer.Infrastructure;

public static class DataFolder
{
    public const string Name = "Data";

    public const string InputKey = "Viewer:Snapshot";

    public static InputFiles Read(string appFolder, string? input)
    {
        string snapshot = input ?? SnapshotIn(Path.Combine(appFolder, Name));
        string json = File.ReadAllText(snapshot);
        string folder = Path.GetDirectoryName(Path.GetFullPath(snapshot))!;
        string architecture = Path.Combine(folder, DataFiles.Architecture(SnapshotJson.Read(json).Architecture.Name));
        return new InputFiles(json, File.ReadAllText(architecture));
    }

    private static string SnapshotIn(string folder)
    {
        string[] files = Directory.GetFiles(folder, "*" + DataFiles.SnapshotExtension);
        return files.Length == 1
            ? files[0]
            : throw new InvalidOperationException(
                $"The folder {folder} has {files.Length} snapshot files, and the viewer needs one. "
                + $"Give the snapshot with --{InputKey}.");
    }
}
