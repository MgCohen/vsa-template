namespace Umpire.CheckModel;

public static class StatePointer {
    public const string Name = "state.txt";

    public static string FileOf(string snapshot) => Path.Combine(Path.GetDirectoryName(Path.GetFullPath(snapshot))!, Name);

    public static StateFolder? Read(string snapshot) {
        try {
            var file = FileOf(snapshot);
            return File.Exists(file) && StateFile.Read(file).Trim() is { Length: > 0 } folder ? StateFolder.At(folder) : null;
        } catch (Exception e) when (e is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException) {
            return null;
        }
    }

    public static bool Write(string snapshot, StateFolder state) => StateFile.Save(FileOf(snapshot), state.Folder + "\n");
}
