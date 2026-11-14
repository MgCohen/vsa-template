using VSA.Architecture.Data;

namespace VSA.Architecture.Engine;

public static class ArchitectureExport {
    public static ArchitectureFile Of(ArchitectureMap architecture, string name, string root) {
        var definition = new DefinitionExport(architecture.Definition);
        var app = new AppExport(architecture, definition);
        return new ArchitectureFile(ArchitectureJson.Version, definition.Data(), app.Data(name, root));
    }
}
