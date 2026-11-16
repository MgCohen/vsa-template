namespace Umpire.Cli.Scaffolding;

using Umpire.Engine.Versions;

static class RootProject {
    public static string BlockOfCurrentFolder() =>
        BlockFolder.Find(Environment.CurrentDirectory, needsUmpirePin: true)
        ?? throw new UmpireException("This folder is not in an Umpire app. "
            + "Run the command in the app folder, or make an app with 'umpire new app <Name>'.");

    public static string Name(string block) {
        var projects = Directory.GetFiles(block, "*.csproj");
        return projects.Length == 1
            ? Path.GetFileNameWithoutExtension(projects[0])
            : throw new UmpireException($"The block folder {block} has {projects.Length} .csproj files. "
                + "Keep one root project of the app in this folder.");
    }
}
