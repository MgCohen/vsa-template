using System.Reflection;
using Report.Domain.Shared;
using VSA.Architecture;

namespace Definition.Slices.LoadDefinition;

public sealed record LoadDefinitionResult(
    ArchitectureDefinition? Definition, IReadOnlyList<Assembly> Assemblies, IReadOnlyList<Finding> Findings);
