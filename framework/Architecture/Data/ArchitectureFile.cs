namespace VSA.Architecture.Data;

public sealed record ArchitectureFile(int Version, DefinitionData Definition, AppData App);
