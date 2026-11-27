namespace Umpire.Cli.Features.Scaffold.Domain;

using Umpire.Cli.Plugins.Commands.Domain.Shared;

static class AddFiles {
    public static int Run(AddKind kind, string name, string? module, string missingModule) {
        Names.Check(name, kind.Noun, kind.Suffixes);
        var block = RootProject.AppOfCurrentFolder();
        if (kind.InModule && module is null) {
            throw new UmpireException(missingModule);
        }
        kind.EnsureFree(block, name);
        var names = new Dictionary<string, string> {
            [kind.SampleName] = name,
            [Templates.App] = RootProject.Name(block),
        };
        var moduleFolder = kind.InModule ? ModuleFolder.Find(block, module!) : null;
        if (moduleFolder is not null) {
            names[Templates.Module] = moduleFolder;
        }
        var inModule = moduleFolder is null ? "" : $" to the module {moduleFolder}";
        Console.WriteLine($"Adding the {kind.Shown} {name}{inModule}:");
        var files = Templates.Files(kind.Template, names, ToolVersion.Current);
        NewFiles.Write(block, Templates.WithCentralVersions(files, CentralVersions.Above(block)));
        if (kind.NextStep?.Invoke(name) is string next) {
            Console.WriteLine(next);
        }
        return 0;
    }
}
