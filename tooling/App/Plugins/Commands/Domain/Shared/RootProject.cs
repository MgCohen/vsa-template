namespace Umpire.Cli.Plugins.Commands.Domain.Shared;

using Umpire.Engine.Versions;

public static class RootProject {
    public static string AppOfCurrentFolder() =>
        AppFolder.Find(Environment.CurrentDirectory)
        ?? throw new UmpireException("This folder is not in an Umpire app. An app folder has a .umpireconfig file, "
            + "or a project with an Umpire package, for example "
            + "<PackageReference Include=\"Umpire.Web\" Version=\"0.2.0\" />. "
            + "Run the command in the app folder, or make an app with 'umpire new app <Name>'.");

    public static string Name(string app) {
        var projects = Directory.GetFiles(app, "*.csproj");
        return projects.Length == 1
            ? Path.GetFileNameWithoutExtension(projects[0])
            : throw new UmpireException($"The app folder {app} has {projects.Length} .csproj files. "
                + "Keep one root project of the app in this folder.");
    }
}
