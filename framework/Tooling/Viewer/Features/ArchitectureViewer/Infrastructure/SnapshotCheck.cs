using VSA.Architecture.Data;

namespace ArchitectureViewer.Infrastructure;

public static class SnapshotCheck
{
    public static string? Warning(string snapshotJson, string architectureJson)
    {
        SnapshotFile snapshot = SnapshotJson.Read(snapshotJson);
        DefinitionFile architecture = DefinitionJson.Read(architectureJson);
        if (snapshot.Architecture.Hash == DefinitionJson.Hash(architecture))
        {
            return null;
        }
        string app = snapshot.App.Name;
        return snapshot.Architecture.Name == architecture.Name
            ? $"The App view shows {app} against another version of the architecture {architecture.Name}. "
                + "Run the engine with --export on this architecture file to see the app against it."
            : $"The App view shows {app} against the architecture {snapshot.Architecture.Name}, "
                + $"and the loaded architecture is {architecture.Name}.";
    }
}
