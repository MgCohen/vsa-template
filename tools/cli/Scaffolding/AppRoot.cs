using Umpire.Engine.Versions;

namespace Umpire.Cli.Scaffolding;

static class AppRoot {
    public static string Find() {
        for (var folder = new DirectoryInfo(Environment.CurrentDirectory); folder is not null; folder = folder.Parent) {
            var file = Path.Combine(folder.FullName, VersionSlots.GlobalJsonFile);
            if (File.Exists(file) && VersionSlots.SdkVersion(file) is not null) {
                return folder.FullName;
            }
        }
        throw new UmpireException("This folder is not in an Umpire app. "
            + "Run the command in the app folder, or make an app with 'umpire new app <Name>'.");
    }

    public static string Name(string root) {
        var projects = Directory.GetFiles(root, "*.csproj");
        return projects.Length == 1
            ? Path.GetFileNameWithoutExtension(projects[0])
            : throw new UmpireException($"The app root {root} has {projects.Length} .csproj files. Keep one root project of the app in this folder.");
    }
}
