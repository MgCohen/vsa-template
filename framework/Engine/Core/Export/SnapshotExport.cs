using VSA.Architecture.Data;

namespace VSA.Architecture.Engine;

public static class SnapshotExport {
    public static SnapshotFile Of(ArchitectureMap map, string app) {
        var definition = map.Definition;
        var architecture = new SnapshotArchitecture(
            DefinitionExport.NameOf(definition),
            DefinitionExport.SourceOf(definition),
            DefinitionJson.Hash(DefinitionExport.Of(definition)));
        return new SnapshotFile(SnapshotJson.Version, architecture, new AppExport(map).Data(app));
    }
}
