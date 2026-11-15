using VSA.Architecture.Versions;

namespace VSA.Cli.Scaffolding;

static class AppRoot {
    public static string Find() {
        for (var folder = new DirectoryInfo(Environment.CurrentDirectory); folder is not null; folder = folder.Parent) {
            var file = Path.Combine(folder.FullName, VersionSlots.GlobalJsonFile);
            if (File.Exists(file) && VersionSlots.SdkVersion(file) is not null) {
                return folder.FullName;
            }
        }
        throw new VsaException("This folder is not in a VSA app. "
            + "Run the command in the app folder, or make an app with 'vsa new app <Name>'.");
    }
}
