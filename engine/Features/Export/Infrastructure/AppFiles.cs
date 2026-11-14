using Definition.Domain.Shared;
using VSA.Architecture.Data;
using ArchitectureMap = VSA.Architecture.Engine.ArchitectureMap;
using SnapshotExport = VSA.Architecture.Engine.SnapshotExport;

namespace Export.Infrastructure;

public static class AppFiles {
    public static ExportSummary Write(LoadedApp app, string folder) {
        var architecture = DefinitionExport.Of(app.Definition);
        var architectureFile = Path.Combine(folder, DataFiles.Architecture(architecture.Name));
        DefinitionJson.Save(architecture, architectureFile);
        var map = ArchitectureMap.Load(app.Definition, app.Assemblies, app.AppRoot);
        var snapshot = SnapshotExport.Of(map, Path.GetFileName(Path.TrimEndingDirectorySeparator(app.AppRoot)));
        var snapshotFile = Path.Combine(folder, DataFiles.Snapshot(snapshot.App.Name, architecture.Name));
        SnapshotJson.Save(snapshot, snapshotFile);
        return new ExportSummary(
            Path.GetFileName(architectureFile), architecture.Parts.Count, architecture.Rules.Count,
            Path.GetFileName(snapshotFile), snapshot.App.Types.Count);
    }
}
