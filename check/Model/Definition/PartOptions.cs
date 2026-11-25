namespace Umpire.CheckModel;

public sealed record PartOptions(
    Permission DefaultPermission, Boundary Boundary, bool UseSameConcept, Depth Depth, Access Access, bool Required);
