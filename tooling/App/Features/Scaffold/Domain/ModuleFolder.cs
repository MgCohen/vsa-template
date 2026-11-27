namespace Umpire.Cli.Features.Scaffold.Domain;

using Umpire.Cli.Plugins.Commands.Domain.Shared;

static class ModuleFolder {
    const string Modules = "App/Features";

    public static string Find(string block, string module) {
        var modules = Path.Combine(block, Modules);
        var folderNameOnDisk = Directory.Exists(modules)
            ? Directory.EnumerateDirectories(modules)
                .Select(Path.GetFileName)
                .FirstOrDefault(folder => string.Equals(folder, module, StringComparison.OrdinalIgnoreCase))
            : null;
        if (folderNameOnDisk is null) {
            throw new UmpireException($"The module {module} does not exist. Add it with 'umpire add module {module}'.");
        }
        return Names.Check(folderNameOnDisk, "module");
    }
}
