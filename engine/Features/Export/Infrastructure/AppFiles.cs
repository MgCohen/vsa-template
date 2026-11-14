using Definition.Domain.Shared;
using VSA.Architecture.Data;
using ArchitectureExport = VSA.Architecture.Engine.ArchitectureExport;
using ArchitectureMap = VSA.Architecture.Engine.ArchitectureMap;

namespace Export.Infrastructure;

public static class AppFiles {
    public const string DefinitionFile = "definition.json";
    public const string ArchitectureFile = "architecture.json";

    public static ExportSummary Write(LoadedApp app, string folder) {
        var definition = DefinitionExport.Of(app.Definition);
        DefinitionJson.Save(definition, Path.Combine(folder, DefinitionFile));
        var map = ArchitectureMap.Load(app.Definition, app.Assemblies, app.AppRoot);
        var name = Path.GetFileName(Path.TrimEndingDirectorySeparator(app.AppRoot));
        var architecture = ArchitectureExport.Of(map, name, app.AppRoot.Replace(Path.DirectorySeparatorChar, '/'));
        ArchitectureJson.Save(architecture, Path.Combine(folder, ArchitectureFile));
        return new ExportSummary(definition.Parts.Count, definition.Rules.Count, architecture.App.Types.Count);
    }
}
