namespace Umpire.Engine.Versions;

public static class BlockFolder {
    public const string Marker = VersionSlots.GlobalJsonFile;

    public static string? Find(string start, string? declared = null, bool needsUmpirePin = false) {
        if (!string.IsNullOrWhiteSpace(declared)) {
            return Path.GetFullPath(declared);
        }
        for (var folder = new DirectoryInfo(Path.GetFullPath(start)); folder is not null; folder = folder.Parent) {
            var file = Path.Combine(folder.FullName, Marker);
            if (File.Exists(file) && (!needsUmpirePin || VersionSlots.SdkVersion(file) is not null)) {
                return folder.FullName;
            }
        }
        return null;
    }

    public static string NotFound(string start) =>
        $"No block folder contains {Path.GetFullPath(start)}: no folder above it has a {Marker} file. "
        + $"Add a {Marker} file to the folder of the app, "
        + "or give the folder with --block (MSBuild property UmpireBlock).";
}
