using VSA.Architecture.Data;

namespace VSA.Architecture.Engine;

public static class ArchitectureExport {
    public static ArchitectureFile Of(ArchitectureMap architecture, string name) =>
        new(ArchitectureJson.Version, new AppExport(architecture).Data(name));
}
