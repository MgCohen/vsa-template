namespace VSA.Architecture.Data;

public sealed record DefinitionOptions(
    Permission DefaultPermission, Boundary Boundary, bool UseSameConcept, Depth Depth, Access Access);
