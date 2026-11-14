namespace VSA.Architecture.Data;

public sealed record AppData(
    string Name, List<InstanceData> Instances, List<TypeData> Types, List<DependencyData> Dependencies,
    List<FindingData> Findings);
