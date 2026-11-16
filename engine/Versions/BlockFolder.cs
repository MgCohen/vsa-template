namespace Umpire.Engine.Versions;

public static class BlockFolder {
    public const string Marker = VersionSlots.GlobalJsonFile;

    public static string? Find(string start, bool needsUmpirePin = false) {
        for (var folder = new DirectoryInfo(Path.GetFullPath(start)); folder is not null; folder = folder.Parent) {
            var file = Path.Combine(folder.FullName, Marker);
            if (File.Exists(file) && (!needsUmpirePin || VersionSlots.SdkVersion(file) is not null)) {
                return folder.FullName;
            }
        }
        return null;
    }
}
